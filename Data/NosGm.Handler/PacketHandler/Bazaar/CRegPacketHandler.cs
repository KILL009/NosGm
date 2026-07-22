using NosTale.Configuration;
using NosTale.Packets.Packets.ClientPackets;
using OpenNos.Core;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenNos.Handler.PacketHandler.Bazaar
{
    public class CRegPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CRegPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task SellBazaarAsync(CRegPacket cRegPacket)
        {
            if (!GameConfiguration.BazaarEnabled)
            {
                Session.SendPacket("info The Bazaar Server is currently offline");
                return;
            }
            if (Session.Character.Level < 85)
            {
                Session.SendPacket("info You need to be at least Level 85\nYou need to have at least 90.000 Reputation");
                return;
            }
            if (Session.Character.Reputation < 90000)
            {
                Session.SendPacket("info You need to be at least Level 85\nYou need to have at least 90.000 Reputation");
                return;
            }
            if (cRegPacket.Inventory != 0 && cRegPacket.Inventory != 1 && cRegPacket.Inventory != 2 && cRegPacket.Inventory != 4)
            {
                return;
            }

            if (Session.Character.Channel.ChannelId > 1)
            {
                Session.SendPacket("info The NosBazaar can only be accessed on Channel 1");
                return;
            }

            if (Session.Character.LastBazaarInsert.AddSeconds(5) > DateTime.Now)
            {
                return;
            }

            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            if (ServerManager.Instance.InShutdown)
            {
                return;
            }
            if (Session.Character == null || Session.Character.InExchangeOrTrade || Session.Character.HasShopOpened || Session.Character.MapInstance?.Map.MapId == 20001)
            {
                return;
            }

            if (Session.Character.IsMuted())
            {
                return;
            }

            if (!Session.Character.CanUseNosBazaar())
            {
                return;
            }

           
            InventoryType currentInventoryType = cRegPacket.Inventory == 4 ? InventoryType.Equipment : (InventoryType)cRegPacket.Inventory;
            InventoryType[] allowedInventoryTypes = { InventoryType.Equipment, InventoryType.Main, InventoryType.Etc };

            if (allowedInventoryTypes.All(s => s != currentInventoryType))
            {
                return;
            }
            if ((InventoryType)cRegPacket.Inventory == InventoryType.Bazaar)
            {
                return;
            }
            if (cRegPacket.Type == 9)
            {
                return;
            }
            if (cRegPacket.Inventory == 9)
            {
                Logger.Log.Info($"{Session.Character.Name} tried to dupe via bazar");
                await Session.SendPacketAsync("msg 4 You will be kicked now");
                await Task.Delay(1000);
                ServerManager.Instance.Kick(Session.Character.Name);
                return;
            }

            if (cRegPacket.Inventory != 0 && cRegPacket.Inventory != 1 &&
              cRegPacket.Inventory != 2 && cRegPacket.Inventory != 4)
            {
                Session.SendPacket("info Action has been logged");
            }

            if (cRegPacket.Inventory < 0 || cRegPacket.Inventory >= 9 || cRegPacket.Inventory > 4 || cRegPacket.Inventory == 3 || cRegPacket.Taxes < 1 || cRegPacket.Taxes > 2000000000 || cRegPacket.Price < 1 || cRegPacket.Price > 2000000000 || cRegPacket.Durability > 4 || cRegPacket.Durability < 1)
            {
                Logger.Log.Info($"{Session.Character.Name} tried to dupe via bazar");
                ServerManager.Instance.Kick(Session.Character.Name);
                Logger.LogUserEvent("BAZAAR_CHEAT_TRY", Session.GenerateIdentity(), $"Packet string: {cRegPacket.OriginalContent.ToString()}");
                return;
            }

            if (cRegPacket.Inventory != 0 && cRegPacket.Inventory != 1 && cRegPacket.Inventory != 2 && cRegPacket.Inventory != 4)
            {
                foreach (var team in ServerManager.Instance.Sessions.Where(s => s.Account.Authority >= AuthorityType.GM))
                {
                    if (team.HasSelectedCharacter)
                    {
                        team.SendPacket(team.Character.GenerateSay($"User {Session.Character.Name} tried to dupe via Bazaar.", 12));
                    }
                }

                PenaltyLogDTO log = new PenaltyLogDTO
                {
                    AccountId = Session.Account.AccountId,
                    Reason = "Dupe Bazaar",
                    Penalty = PenaltyType.Banned,
                    DateStart = DateTime.Now,
                    DateEnd = DateTime.Now.AddYears(10),
                    AdminName = "Administrator"
                };
                Character.InsertOrUpdatePenalty(log);
                Session?.Disconnect();
                return;
            }

            SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);
            var medal = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
            var price = cRegPacket.Price * cRegPacket.Amount;
            var taxmax = price > 100000 ? price / 200 : 500;
            var taxmin = price >= 4000 ? 60 + (price - 4000) / 2000 * 30 > 10000 ? 10000 : 60 + (price - 4000) / 2000 * 30 : 50;
            var tax = medal == null ? taxmax : taxmin;
            var maxGold = GameConfiguration.MaxGold;
            if (Session.Character.Gold < tax || cRegPacket.Amount <= 0 || Session.Character.ExchangeInfo?.ExchangeList.Count > 0 || Session.Character.IsShopping)
            {
                return;
            }
            var it = Session.Character.Inventory.LoadBySlotAndType(cRegPacket.Slot, cRegPacket.Inventory == 4 ? 0 : (InventoryType)cRegPacket.Inventory);

            // This is done on purpose, because when an item is fresh created and has shell (hero eq), its shell effects are being deleted cause they're not being saved in DB before adding items to NB.
            // TODO: Find a better way to do it, ending the issue from the root.
            Session.Character.PerformItemSave(it);

            if (it == null || !it.Item.IsSoldable || !it.Item.IsTradable || it.IsBound && it.ItemDeleteTime != null || it.Amount < 1)
            {
                return;
            }

            if (it.IsBound)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo("You can't add in Bazaar items used like that."));
                return;
            }

            if (it.ItemVNum >= 7185 && it.ItemVNum <= 7190 || it.ItemVNum >= 7412 && it.ItemVNum <= 7414)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo("You Can't Add Cash in NosBazaar."));
                return;
            }

            if (it.Item.SellToNpcPrice * cRegPacket.Amount > 1000000000)
            {
                PenaltyLogDTO log = new PenaltyLogDTO
                {
                    AccountId = Session.Account.AccountId,
                    Reason = "Attempted Bazaar Dupe",
                    Penalty = PenaltyType.Banned,
                    DateStart = DateTime.Now,
                    DateEnd = DateTime.Now.AddYears(20),
                    AdminName = "Administrator"
                };
                Character.InsertOrUpdatePenalty(log);
                Session.Disconnect();
                return;
            }

            if (cRegPacket.Inventory < 0 || cRegPacket.Inventory > 4 || cRegPacket.Inventory == 3 || cRegPacket.Taxes < 1 || cRegPacket.Taxes > 2000000000 || cRegPacket.Price < 1 || cRegPacket.Price > 2000000000 || cRegPacket.Durability > 4 || cRegPacket.Durability < 1)
            {
                Logger.Log.Info($"{Session.Character.Name} tried to dupe via bazaar");
                ServerManager.Instance.Kick(Session.Character.Name);
                Logger.Log.Info("BAZAAR_CHEAT_TRY " + Session.GenerateIdentity() + $" Packet string: {cRegPacket.OriginalContent.ToString()}");
                return;
            }

            if (Session.Character.Inventory.CountItemInAnInventory(InventoryType.Bazaar) >= 10 * (medal == null ? 2 : 10))
            {
                await Session.SendPacketAsync(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LIMIT_EXCEEDED"), 0));
                return;
            }

            if (it.Amount < 1)
            {
                return;
            }
            if (cRegPacket.Price >= (medal == null ? 1000000 : maxGold))
            {
                await Session.SendPacketAsync(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PRICE_EXCEEDED"), 0));
                return;
            }
            if (cRegPacket.Amount > 1 && cRegPacket.Amount * cRegPacket.Price >= maxGold)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PRICE_EXCEEDED"), 0));
                return;
            }
            if (cRegPacket.Price <= 0)
            {
                return;
            }

            var bazaar = Session.Character.Inventory.AddIntoBazaarInventory(cRegPacket.Inventory == 4 ? 0 : (InventoryType)cRegPacket.Inventory, cRegPacket.Slot, cRegPacket.Amount);

            if (bazaar == null)
            {
                return;
            }

            short duration;
            switch (cRegPacket.Durability)
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
                    return;
            }

            DAOFactory.ItemInstanceDAO.InsertOrUpdate(bazaar);
            var bazaarItem = new BazaarItemDTO
            {
                AccountId = Session.Account.AccountId,
                RegistrationIP = Session.Account.RegistrationIP,
                CurrentIp = Session.Character.CurrentIp,
                Amount = bazaar.Amount,
                DateStart = DateTime.Now,
                Duration = duration,
                IsPackage = cRegPacket.IsPackage != 0,
                MedalUsed = medal != null,
                Price = cRegPacket.Price,
                SellerId = Session.Character.CharacterId,
                ItemInstanceId = bazaar.Id
            };
           
            DAOFactory.BazaarItemDAO.InsertOrUpdate(ref bazaarItem);
            if (bazaar is ItemInstance instance)
            {
                instance.ShellEffects.ForEach(s =>
                {
                    s.EquipmentSerialId = instance.EquipmentSerialId;
                    DAOFactory.ShellEffectDAO.InsertOrUpdate(s);
                });

                instance.CellonOptions.ForEach(s =>
                {
                    s.EquipmentSerialId = instance.EquipmentSerialId;
                    DAOFactory.CellonOptionDAO.InsertOrUpdate(s);
                });
            }

            if (bazaarItem == null || bazaar.Amount < bazaar.Amount)
            {
                Logger.Log.Info($"{Session.Character.Name} tried to dupe via bazar");
                ServerManager.Instance.Kick(Session.Character.Name);
                Logger.LogUserEvent("BAZAAR_CHEAT_TRY", Session.GenerateIdentity(), $"Packet string: {cRegPacket.OriginalContent.ToString()}");
                return;
            }

            ServerManager.Instance.BazaarRefresh(bazaarItem.BazaarItemId);

            Session.Character.Gold -= tax;
            await Session.SendPacketAsync(Session.Character.GenerateGold());

            await Session.SendPacketAsync(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("OBJECT_IN_BAZAAR"), 10));
            await Session.SendPacketAsync(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("OBJECT_IN_BAZAAR"), 0));

            Logger.LogUserEvent("BAZAAR_INSERT", Session.GenerateIdentity(), $"BazaarId: {bazaarItem.BazaarItemId}, IIId: {bazaarItem.ItemInstanceId} VNum: {bazaar.ItemVNum} Amount: {cRegPacket.Amount} Price: {cRegPacket.Price} Time: {duration}");
            Logger.LogUserEvent("BAZAAR_INSERT_PACKET", Session.GenerateIdentity(), $"Packet string: {cRegPacket.OriginalContent.ToString()}");

            Session.SendPacket("rc_reg 1");

            Session.Character.LastBazaarInsert = DateTime.Now;

        }

        #endregion
    }
}