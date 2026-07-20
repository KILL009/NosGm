using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.HttpClients;
using Frostvein.GameObject.Modules.Bazaar.Commands;
using Frostvein.GameObject.Networking;
using System;

namespace Frostvein.Handler.Bazaar
{
    public class SellBazaarPacketHandling : IPacketHandler
    {
        private static readonly KeepAliveClient KeepAliveClient = KeepAliveClient.Instance;
        private static readonly BazaarHttpClient BazaarClient = BazaarHttpClient.Instance;

        public SellBazaarPacketHandling(ClientSession session) => Session = session;

        private ClientSession Session { get; }

        public void SellBazaar(CRegPacket packet)
        {
            if (packet == null || Session?.Character?.Inventory == null || Session.Account == null)
            {
                return;
            }

            if (!CanUseBazaar())
            {
                return;
            }

            if (!TryResolvePacket(packet, out InventoryType inventoryType, out short duration))
            {
                LogRejectedPacket(packet, "Invalid authoritative packet fields");
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    "The NosBazaar request could not be validated. Please select the item again."));
                return;
            }

            BazaarListingDTO committedPlan;
            BazaarListingCommitResponseDTO response;

            lock (Session.Character.Inventory)
            {
                ItemInstance source = Session.Character.Inventory.LoadBySlotAndType(packet.Slot, inventoryType);
                if (!IsValidSource(source, packet.Amount))
                {
                    LogRejectedPacket(packet, DescribeInvalidSource(source));
                    SendInvalidItem();
                    return;
                }

                // The object is resolved from this character's live inventory. The dedicated
                // NosBazaar service still validates identity, owner and duplicate conflicts.
                source.CharacterId = Session.Character.CharacterId;
                source.Type = inventoryType;
                source.Slot = packet.Slot;

                if (source.Id == Guid.Empty)
                {
                    LogRejectedPacket(packet, "Live item has an empty ItemInstanceId");
                    SendInvalidItem();
                    return;
                }

                BazaarListingDTO requestedPlan = BuildPlan(source, packet, duration);
                response = BazaarClient.CommitListing(new CommitBazaarListingCommand
                {
                    Plan = requestedPlan
                });

                if (response == null ||
                    response.Result != BazaarListingResult.Success ||
                    !TryValidateCommittedPlan(requestedPlan, response.Plan, out string validationFailure))
                {
                    BazaarListingResult result = response?.Result ?? BazaarListingResult.Error;
                    string failure = response?.Message;
                    if (!string.IsNullOrWhiteSpace(validationFailure))
                    {
                        failure = string.IsNullOrWhiteSpace(failure)
                            ? validationFailure
                            : failure + " " + validationFailure;
                    }

                    LogCommitFailure(packet, source, requestedPlan, result, failure);
                    SendFailure(result, failure);
                    return;
                }

                committedPlan = response.Plan;
                ApplyPlan(committedPlan);
                RecordTrace(committedPlan);
            }

            UpdatePersonalCache(committedPlan.Listing);
            SendSuccess();

            if (!response.CacheRefreshed)
            {
                Logger.LogUserEvent("BAZAAR_CACHE_WARNING", Session.GenerateIdentity(),
                    $"OperationId={committedPlan.OperationId} BazaarId={committedPlan.Listing.BazaarItemId} " +
                    $"Message={response.Message}");
            }

            Logger.LogUserEvent("BAZAAR_INSERT_COMMIT", Session.GenerateIdentity(),
                $"OperationId={committedPlan.OperationId} BazaarId={committedPlan.Listing.BazaarItemId} " +
                $"ItemInstanceId={committedPlan.BazaarItemAfter.Id} VNum={committedPlan.BazaarItemAfter.ItemVNum} " +
                $"Amount={committedPlan.BazaarItemAfter.Amount} UnitPrice={committedPlan.Listing.Price} " +
                $"Tax={committedPlan.Tax} Duration={committedPlan.Listing.Duration} " +
                $"CacheRefreshed={response.CacheRefreshed}");
            Logger.LogUserEvent("BAZAAR_INSERT_PACKET", Session.GenerateIdentity(),
                $"Packet string: {packet.OriginalContent}");
        }

        private bool CanUseBazaar()
        {
            if (ServerManager.Instance.InShutdown ||
                Session.Character.InExchangeOrTrade ||
                Session.Character.HasShopOpened ||
                Session.Character.IsShopping ||
                Session.Character.ExchangeInfo?.ExchangeList.Count > 0)
            {
                return false;
            }

            if (!KeepAliveClient.IsBazaarOnline())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    "The NosBazaar service is offline. Please inform a staff member."));
                return false;
            }

            if (!Session.Character.CanUseNosBazaar())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                    Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
                return false;
            }

            return true;
        }

        private static bool TryResolvePacket(
            CRegPacket packet,
            out InventoryType inventoryType,
            out short duration)
        {
            inventoryType = packet.Inventory == 4
                ? InventoryType.Equipment
                : (InventoryType)packet.Inventory;
            duration = 0;

            // Type, Unknown1, Unknown2, Taxes and MedalUsed are client hints or legacy
            // fields. The service recalculates tax and medal from authoritative data.
            if (packet.Inventory != 0 && packet.Inventory != 1 &&
                packet.Inventory != 2 && packet.Inventory != 4 ||
                packet.Amount <= 0 ||
                packet.Price <= 0 ||
                packet.Price > 2000000000)
            {
                return false;
            }

            switch (packet.Durability)
            {
                case 1:
                    duration = 24;
                    break;
                case 2:
                    duration = 168;
                    break;
                case 3:
                    duration = 360;
                    break;
                case 4:
                    duration = 720;
                    break;
                default:
                    return false;
            }

            return inventoryType == InventoryType.Equipment ||
                   inventoryType == InventoryType.Main ||
                   inventoryType == InventoryType.Etc;
        }

        private static bool IsValidSource(ItemInstance source, short requestedAmount)
        {
            if (source?.Item == null ||
                requestedAmount <= 0 ||
                requestedAmount > source.Amount ||
                !source.Item.IsSoldable ||
                !source.Item.IsTradable ||
                source.IsBound ||
                source.ItemDeleteTime != null)
            {
                return false;
            }

            return source.Type != InventoryType.Equipment || requestedAmount == source.Amount;
        }

        private static string DescribeInvalidSource(ItemInstance source)
        {
            if (source == null)
            {
                return "No live item exists at the requested inventory slot";
            }

            return $"Live item rejected: id={source.Id} vnum={source.ItemVNum} amount={source.Amount} " +
                   $"owner={source.CharacterId} type={source.Type} slot={source.Slot} " +
                   $"soldable={source.Item?.IsSoldable} tradable={source.Item?.IsTradable} " +
                   $"boundCharacterId={source.BoundCharacterId} deleteTime={source.ItemDeleteTime}";
        }

        private BazaarListingDTO BuildPlan(ItemInstance source, CRegPacket packet, short duration)
        {
            ItemInstance sourceBefore = source.DeepCopy();
            ItemInstance sourceAfter = null;
            ItemInstance bazaarAfter;

            if (packet.Amount == source.Amount)
            {
                bazaarAfter = source.DeepCopy();
            }
            else
            {
                sourceAfter = source.DeepCopy();
                sourceAfter.Amount -= packet.Amount;

                bazaarAfter = source.DeepCopy();
                bazaarAfter.Id = Guid.NewGuid();
                bazaarAfter.EquipmentSerialId = Guid.NewGuid();
                bazaarAfter.Amount = packet.Amount;
            }

            bazaarAfter.CharacterId = Session.Character.CharacterId;
            bazaarAfter.Type = InventoryType.Bazaar;
            bazaarAfter.Slot = 0;

            return new BazaarListingDTO
            {
                OperationId = Guid.NewGuid(),
                SellerAccountId = Session.Account.AccountId,
                SellerCharacterId = Session.Character.CharacterId,
                GoldBefore = Session.Character.Gold,
                GoldAfter = Session.Character.Gold,
                MaximumGold = ServerManager.Instance.Configuration.MaxGold,
                SourceBefore = sourceBefore,
                SourceAfter = sourceAfter,
                BazaarItemAfter = bazaarAfter,
                Listing = new BazaarItemDTO
                {
                    AccountId = Session.Account.AccountId,
                    RegistrationIP = Session.Account.RegistrationIP,
                    CurrentIp = Session.Character.CurrentIp,
                    Amount = packet.Amount,
                    Duration = duration,
                    IsPackage = packet.IsPackage != 0,
                    Price = packet.Price,
                    SellerId = Session.Character.CharacterId,
                    ItemInstanceId = bazaarAfter.Id
                }
            };
        }

        private static bool TryValidateCommittedPlan(
            BazaarListingDTO requested,
            BazaarListingDTO committed,
            out string failure)
        {
            failure = null;
            if (requested == null || committed == null ||
                committed.OperationId != requested.OperationId ||
                committed.SellerAccountId != requested.SellerAccountId ||
                committed.SellerCharacterId != requested.SellerCharacterId ||
                committed.SourceBefore?.Id != requested.SourceBefore?.Id ||
                committed.SourceBefore?.ItemVNum != requested.SourceBefore?.ItemVNum ||
                committed.BazaarItemAfter == null ||
                committed.BazaarItemAfter.Id != requested.BazaarItemAfter?.Id ||
                committed.BazaarItemAfter.Type != InventoryType.Bazaar ||
                committed.BazaarItemAfter.CharacterId != requested.SellerCharacterId ||
                committed.BazaarItemAfter.Amount != requested.BazaarItemAfter?.Amount ||
                committed.Listing == null ||
                committed.Listing.BazaarItemId <= 0 ||
                committed.Listing.ItemInstanceId != committed.BazaarItemAfter.Id ||
                committed.Listing.SellerId != requested.SellerCharacterId ||
                committed.GoldAfter < 0 ||
                committed.GoldAfter > committed.GoldBefore ||
                committed.Tax != committed.GoldBefore - committed.GoldAfter)
            {
                failure = "The NosBazaar service returned a plan that did not match the requested operation.";
                return false;
            }

            return true;
        }

        private void ApplyPlan(BazaarListingDTO plan)
        {
            Session.Character.Inventory.Remove(plan.SourceBefore.Id);

            if (plan.SourceAfter != null)
            {
                var sourceAfter = new ItemInstance(plan.SourceAfter);
                Session.Character.Inventory[sourceAfter.Id] = sourceAfter;
                Session.SendPacket(sourceAfter.GenerateInventoryAdd());
            }
            else
            {
                Session.SendPacket(UserInterfaceHelper.Instance.GenerateInventoryRemove(
                    plan.SourceBefore.Type,
                    plan.SourceBefore.Slot));
            }

            var bazaarItem = new ItemInstance(plan.BazaarItemAfter);
            Session.Character.Inventory[bazaarItem.Id] = bazaarItem;

            Session.Character.Gold = plan.GoldAfter;
            Session.SendPacket(Session.Character.GenerateGold());
        }

        private void RecordTrace(BazaarListingDTO plan)
        {
            try
            {
                int sequence = 0;
                if (plan.SourceAfter == null)
                {
                    ItemTraceService.Instance.Record(
                        plan.OperationId,
                        sequence,
                        ItemTraceAction.Transferred,
                        ItemTraceSource.Bazaar,
                        plan.SourceBefore,
                        plan.BazaarItemAfter,
                        Session.Account.AccountId,
                        Session.Character.CharacterId,
                        Session.Character.Name,
                        "Atomic bazaar listing full transfer",
                        new
                        {
                            plan.Listing.BazaarItemId,
                            plan.Listing.Price,
                            plan.Listing.Duration,
                            plan.Listing.IsPackage,
                            plan.Tax
                        });
                }
                else
                {
                    ItemTraceService.Instance.Record(
                        plan.OperationId,
                        sequence++,
                        ItemTraceAction.StackChanged,
                        ItemTraceSource.Bazaar,
                        plan.SourceBefore,
                        plan.SourceAfter,
                        Session.Account.AccountId,
                        Session.Character.CharacterId,
                        Session.Character.Name,
                        "Atomic bazaar listing source split",
                        new { plan.Listing.BazaarItemId, plan.BazaarItemAfter.Amount });

                    ItemTraceService.Instance.Record(
                        plan.OperationId,
                        sequence,
                        ItemTraceAction.Created,
                        ItemTraceSource.Bazaar,
                        null,
                        plan.BazaarItemAfter,
                        Session.Account.AccountId,
                        Session.Character.CharacterId,
                        Session.Character.Name,
                        "Atomic bazaar listing split item",
                        new
                        {
                            plan.Listing.BazaarItemId,
                            plan.Listing.Price,
                            plan.Listing.Duration,
                            plan.Listing.IsPackage,
                            plan.Tax
                        });
                }
            }
            catch (Exception exception)
            {
                Logger.LogUserEventError("BAZAAR_LISTING_TRACE", Session.GenerateIdentity(),
                    $"Unable to record bazaar listing operation {plan.OperationId}.", exception);
            }
        }

        private void UpdatePersonalCache(BazaarItemDTO listing)
        {
            if (!Session.Character.BazaarItems.TryAdd(listing.BazaarItemId, listing))
            {
                Session.Character.BazaarItems[listing.BazaarItemId] = listing;
            }
        }

        private void SendSuccess()
        {
            Session.SendPacket(Session.Character.GenerateSay(
                Language.Instance.GetMessageFromKey("OBJECT_IN_BAZAAR"), 10));
            Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                Language.Instance.GetMessageFromKey("OBJECT_IN_BAZAAR"), 0));
            Session.SendPacket("rc_reg 1");
        }

        private void SendFailure(BazaarListingResult result, string message)
        {
            switch (result)
            {
                case BazaarListingResult.NotEnoughGold:
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 0));
                    break;

                case BazaarListingResult.ListingLimitReached:
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("LIMIT_EXCEEDED"), 0));
                    break;

                case BazaarListingResult.InvalidPrice:
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("PRICE_EXCEEDED"), 0));
                    break;

                case BazaarListingResult.InvalidItem:
                    SendInvalidItem();
                    break;

                case BazaarListingResult.MissingSchema:
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                        "The bazaar listing database migration is missing. Please contact an administrator."));
                    break;

                default:
                    Session.SendPacket(UserInterfaceHelper.GenerateModal(
                        Language.Instance.GetMessageFromKey("STATE_CHANGED"), 1));
                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo(message));
                    }
                    break;
            }
        }

        private void SendInvalidItem()
        {
            Session.SendPacket(UserInterfaceHelper.GenerateInfo(
                "This item cannot be registered in the bazaar."));
        }

        private void LogCommitFailure(
            CRegPacket packet,
            ItemInstance source,
            BazaarListingDTO plan,
            BazaarListingResult result,
            string failure)
        {
            Logger.LogUserEvent("BAZAAR_INSERT_FAILED", Session.GenerateIdentity(),
                $"Result={result} OperationId={plan.OperationId} ItemInstanceId={source.Id} " +
                $"VNum={source.ItemVNum} Owner={source.CharacterId} Type={source.Type} Slot={source.Slot} " +
                $"LiveAmount={source.Amount} RequestedAmount={packet.Amount} Price={packet.Price} " +
                $"ClientType={packet.Type} ClientTaxes={packet.Taxes} Durability={packet.Durability} " +
                $"Failure={failure} Packet={packet.OriginalContent}");
        }

        private void LogRejectedPacket(CRegPacket packet, string reason)
        {
            Logger.LogUserEvent("BAZAAR_INSERT_REJECTED", Session.GenerateIdentity(),
                $"{reason}. Type={packet.Type} Inventory={packet.Inventory} Slot={packet.Slot} " +
                $"Unknown1={packet.Unknown1} Unknown2={packet.Unknown2} Durability={packet.Durability} " +
                $"IsPackage={packet.IsPackage} Amount={packet.Amount} Price={packet.Price} " +
                $"ClientTaxes={packet.Taxes} MedalUsed={packet.MedalUsed} Packet={packet.OriginalContent}");
        }
    }
}
