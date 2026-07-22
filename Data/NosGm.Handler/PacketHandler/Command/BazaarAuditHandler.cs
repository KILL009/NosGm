using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.Packets.Packets.CommandPackets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.Handler.PacketHandler.Command
{
    /// <summary>
    /// Read-only GM inspector for active bazaar state and the four atomic bazaar ledgers.
    /// It deliberately has no repair or mutation subcommand.
    /// </summary>
    public sealed class BazaarAuditHandler : IPacketHandler
    {
        private const int DefaultTake = 15;
        private const int MaximumTake = 50;

        public BazaarAuditHandler(ClientSession session)
        {
            Session = session;
        }

        public ClientSession Session { get; }

        public void BazaarAudit(BazaarAuditPacket packet)
        {
            if (Session?.Character == null)
            {
                return;
            }

            string[] parts = (packet?.Contents ?? string.Empty)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string mode = parts.FirstOrDefault()?.ToLowerInvariant() ?? "status";

            Logger.LogUserEvent("GMCOMMAND", Session.GenerateIdentity(),
                $"[BazaarAudit] {packet?.Contents ?? "status"}");

            switch (mode)
            {
                case "status":
                    ShowStatus();
                    return;

                case "recent":
                    ShowEvents("Recent bazaar operations",
                        BazaarAuditService.Instance.GetRecent(ReadTake(parts, 1)));
                    return;

                case "suspicious":
                case "anomalies":
                case "alerts":
                    ShowAnomalies(ReadTake(parts, 1, 20));
                    return;

                case "listing":
                case "bazaar":
                    if (parts.Length < 2 || !long.TryParse(parts[1], out long bazaarItemId) || bazaarItemId <= 0)
                    {
                        SendHelp();
                        return;
                    }
                    ShowListing(bazaarItemId, ReadTake(parts, 2, 30));
                    return;

                case "character":
                case "char":
                    if (!TryResolveCharacter(parts, 1, out CharacterDTO character))
                    {
                        Send("Character not found.", 11);
                        return;
                    }
                    ShowEvents($"Bazaar operations for {character.Name} ({character.CharacterId})",
                        BazaarAuditService.Instance.GetByCharacter(character.CharacterId,
                            ReadTake(parts, 2, 30)));
                    return;

                case "item":
                case "instance":
                    if (parts.Length < 2 || !Guid.TryParse(parts[1], out Guid itemInstanceId) ||
                        itemInstanceId == Guid.Empty)
                    {
                        SendHelp();
                        return;
                    }
                    ShowEvents($"Bazaar operations for item {itemInstanceId}",
                        BazaarAuditService.Instance.GetByItem(itemInstanceId,
                            ReadTake(parts, 2, 30)));
                    return;

                default:
                    SendHelp();
                    return;
            }
        }

        private void ShowStatus()
        {
            BazaarAuditStatusDTO status = BazaarAuditService.Instance.GetStatus();
            Send("===== Bazaar audit status =====", 12);
            Send($"Core bazaar schema: {(BazaarAuditService.Instance.IsAvailable() ? "AVAILABLE" : "MISSING")}", 10);
            Send($"Atomic ledgers: listing={Flag(status.ListingOperationAvailable)} " +
                 $"purchase={Flag(status.PurchaseOperationAvailable)} " +
                 $"price={Flag(status.PriceChangeOperationAvailable)} " +
                 $"recollect={Flag(status.RecollectOperationAvailable)}", 10);
            Send($"Active listings={status.ActiveListingCount} bazaar-items={status.BazaarInventoryItemCount}", 10);
            Send($"Ledger rows: listing={status.ListingOperationCount} purchase={status.PurchaseOperationCount} " +
                 $"price={status.PriceChangeOperationCount} recollect={status.RecollectOperationCount}", 10);

            if (!status.IsComplete)
            {
                Send("One or more atomic bazaar migrations are missing. History and anomaly coverage are partial.", 11);
            }
            else
            {
                Send("All atomic bazaar ledgers are available.", 10);
            }
        }

        private void ShowListing(long bazaarItemId, int take)
        {
            BazaarAuditListingDTO listing = BazaarAuditService.Instance.GetListing(bazaarItemId);
            Send($"===== Bazaar listing {bazaarItemId} =====", 12);

            if (listing == null)
            {
                Send("No active listing exists. Historical ledger events are shown below.", 11);
            }
            else
            {
                DateTime expires = listing.DateStart.AddHours(listing.Duration);
                int calculatedSold = listing.RemainingAmount < 0
                    ? -1
                    : listing.ListedAmount - listing.RemainingAmount;
                Send($"seller={listing.SellerName ?? "unknown"}({listing.SellerCharacterId}) " +
                     $"account={listing.SellerAccountId}", 10);
                Send($"item={listing.ItemVNum} instance={listing.ItemInstanceId} serial={FormatGuid(listing.EquipmentSerialId)}", 10);
                Send($"listed={listing.ListedAmount} remaining={listing.RemainingAmount} sold={calculatedSold} " +
                     $"ledgerPurchased={listing.PurchasedAmount}", 10);
                Send($"price={listing.UnitPrice} package={listing.IsPackage} medal={listing.MedalUsed} " +
                     $"expires={expires:yyyy-MM-dd HH:mm:ss}", 10);
                Send($"itemOwner={listing.ItemOwnerCharacterId} inventoryType={listing.InventoryType} " +
                     $"purchaseRows={listing.PurchaseCount} atomicListing={listing.HasListingOperation == 1}", 10);
            }

            WriteEvents(BazaarAuditService.Instance.GetByListing(bazaarItemId, take).ToList());
        }

        private void ShowEvents(string title, IEnumerable<BazaarAuditEventDTO> source)
        {
            Send($"===== {title} =====", 12);
            WriteEvents(source?.ToList() ?? new List<BazaarAuditEventDTO>());
        }

        private void WriteEvents(IReadOnlyCollection<BazaarAuditEventDTO> events)
        {
            if (events == null || events.Count == 0)
            {
                Send("No bazaar ledger events were found.", 10);
                return;
            }

            foreach (BazaarAuditEventDTO auditEvent in events)
            {
                switch (auditEvent.EventType)
                {
                    case BazaarAuditEventType.Listing:
                        Send($"{auditEvent.OccurredAtUtc:yyyy-MM-dd HH:mm:ss}Z LIST " +
                             $"bz={auditEvent.BazaarItemId} seller={auditEvent.PrimaryCharacterId} " +
                             $"vnum={auditEvent.ItemVNum} amount={auditEvent.Amount} price={auditEvent.UnitPrice} " +
                             $"goldDelta={auditEvent.GoldDelta} op={auditEvent.OperationId}", 10);
                        break;

                    case BazaarAuditEventType.Purchase:
                        Send($"{auditEvent.OccurredAtUtc:yyyy-MM-dd HH:mm:ss}Z BUY " +
                             $"bz={auditEvent.BazaarItemId} buyer={auditEvent.PrimaryCharacterId} " +
                             $"seller={FormatNullable(auditEvent.CounterpartyCharacterId)} vnum={auditEvent.ItemVNum} " +
                             $"amount={auditEvent.Amount} remain={auditEvent.RemainingAmount} " +
                             $"price={auditEvent.UnitPrice} goldDelta={auditEvent.GoldDelta} op={auditEvent.OperationId}", 10);
                        break;

                    case BazaarAuditEventType.PriceChange:
                        Send($"{auditEvent.OccurredAtUtc:yyyy-MM-dd HH:mm:ss}Z PRICE " +
                             $"bz={auditEvent.BazaarItemId} seller={auditEvent.PrimaryCharacterId} " +
                             $"vnum={auditEvent.ItemVNum} {auditEvent.PreviousUnitPrice}->{auditEvent.UnitPrice} " +
                             $"op={auditEvent.OperationId}", 10);
                        break;

                    case BazaarAuditEventType.Recollect:
                        Send($"{auditEvent.OccurredAtUtc:yyyy-MM-dd HH:mm:ss}Z RECOLLECT " +
                             $"bz={auditEvent.BazaarItemId} seller={auditEvent.PrimaryCharacterId} " +
                             $"vnum={auditEvent.ItemVNum} sold={auditEvent.Amount} remain={auditEvent.RemainingAmount} " +
                             $"proceeds={auditEvent.GoldDelta} op={auditEvent.OperationId}", 10);
                        break;

                    default:
                        Send($"{auditEvent.OccurredAtUtc:yyyy-MM-dd HH:mm:ss}Z UNKNOWN " +
                             $"bz={auditEvent.BazaarItemId} op={auditEvent.OperationId}", 11);
                        break;
                }
            }
        }

        private void ShowAnomalies(int take)
        {
            List<BazaarAuditAnomalyDTO> anomalies = BazaarAuditService.Instance
                .GetAnomalies(take)
                .ToList();
            Send("===== Bazaar anomaly inspector =====", 12);

            if (anomalies.Count == 0)
            {
                Send("No bazaar database anomalies were detected by the read-only checks.", 10);
                return;
            }

            foreach (BazaarAuditAnomalyDTO anomaly in anomalies)
            {
                int color = anomaly.Severity == BazaarAuditSeverity.Critical ? 13 : 11;
                Send($"[{anomaly.Severity}] {anomaly.Code} " +
                     $"bz={FormatNullable(anomaly.BazaarItemId)} item={FormatGuid(anomaly.ItemInstanceId)} " +
                     $"char={FormatNullable(anomaly.CharacterId)} vnum={FormatNullable(anomaly.ItemVNum)} " +
                     $"at={FormatDate(anomaly.OccurredAtUtc)} | {anomaly.Detail}", color);
            }
        }

        private static bool TryResolveCharacter(string[] parts, int index, out CharacterDTO character)
        {
            character = null;
            if (parts == null || parts.Length <= index || string.IsNullOrWhiteSpace(parts[index]))
            {
                return false;
            }

            character = long.TryParse(parts[index], out long characterId)
                ? DAOFactory.CharacterDAO.LoadById(characterId)
                : DAOFactory.CharacterDAO.LoadByName(parts[index]);
            return character != null;
        }

        private static int ReadTake(string[] parts, int index, int fallback = DefaultTake)
        {
            if (parts == null || parts.Length <= index || !int.TryParse(parts[index], out int take))
            {
                return fallback;
            }

            return Math.Max(1, Math.Min(MaximumTake, take));
        }

        private void SendHelp()
        {
            Send(BazaarAuditPacket.ReturnHelp(), 10);
            Send("$BazaarAudit status", 10);
            Send("$BazaarAudit recent [take]", 10);
            Send("$BazaarAudit suspicious [take]", 10);
            Send("$BazaarAudit listing <BazaarItemId> [take]", 10);
            Send("$BazaarAudit character <CharacterId|Name> [take]", 10);
            Send("$BazaarAudit item <ItemInstanceId> [take]", 10);
        }

        private void Send(string message, int type)
        {
            Session.SendPacket(Session.Character.GenerateSay(message, type));
        }

        private static string Flag(int value) => value == 1 ? "OK" : "MISSING";

        private static string FormatNullable(long? value) => value?.ToString() ?? "n/a";

        private static string FormatNullable(short? value) => value?.ToString() ?? "n/a";

        private static string FormatGuid(Guid? value) => value.HasValue && value.Value != Guid.Empty
            ? value.Value.ToString()
            : "n/a";

        private static string FormatDate(DateTime? value) => value.HasValue
            ? value.Value.ToString("yyyy-MM-dd HH:mm:ss") + "Z"
            : "n/a";
    }
}
