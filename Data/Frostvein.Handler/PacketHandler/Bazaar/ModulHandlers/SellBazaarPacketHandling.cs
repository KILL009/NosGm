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
using System.Linq;

namespace Frostvein.Handler.Bazaar
{
    public class SellBazaarPacketHandling : IPacketHandler
    {
        #region Instantiation

        public SellBazaarPacketHandling(ClientSession session) => Session = session;

        #endregion Instantiation

        #region Properties

        private ClientSession Session { get; }

        private static readonly KeepAliveClient _keepAliveClient = KeepAliveClient.Instance;


        #endregion Properties

        #region Methods

        public void SellBazaar(CRegPacket cRegPacket)
        {

            if (!_keepAliveClient.IsBazaarOnline())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo("Uh oh, it looks like the bazaar server is offline ! Please inform a staff member about it as soon as possible !"));
                return;
            }

            if (ServerManager.Instance.InShutdown)
            {
                return;
            }

            if (Session.Character == null || Session.Character.InExchangeOrTrade || Session.Character.HasShopOpened)
            {
                return;
            }

            InventoryType currentInventoryType = cRegPacket.Inventory == 4 ? InventoryType.Equipment : (InventoryType)cRegPacket.Inventory;

            InventoryType[] allowedInventoryTypes = { InventoryType.Equipment, InventoryType.Main, InventoryType.Etc };

            if (allowedInventoryTypes.All(s => s != currentInventoryType))
            {
                return;
            }

            if (!Session.Character.CanUseNosBazaar())
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
                return;
            }

            if (cRegPacket.Type == 9)
            {
                return;
            }

            if (cRegPacket.Inventory == 9)
            {
                Logger.Log.Info($"{Session.Character.Name} tried to dupe via bazar");
                ServerManager.Instance.Kick(Session.Character.Name);
                return;
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
                        team.SendPacket(team.Character.GenerateSay($"User {Session.Character.Name} Try dup in Bazaar LMAO !", 12));
                    }
                }

                PenaltyLogDTO log = new()
                {
                    AccountId = Session.Account.AccountId,
                    Reason = "Attempted Bazaar Dupe",
                    Penalty = PenaltyType.Banned,
                    DateStart = DateTime.Now,
                    DateEnd = DateTime.Now.AddYears(20),
                    AdminName = "NosMoon SYSTEM"
                };
                Character.InsertOrUpdatePenalty(log);
                Session.Disconnect();
                return;
            }

            StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s =>
                s.StaticBonusType == StaticBonusType.BazaarMedalGold
                || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);

            long price = cRegPacket.Price * cRegPacket.Amount;
            long taxmax = price > 100000 ? price / 200 : 500;
            long taxmin = price >= 4000
                ? (60 + ((price - 4000) / 2000 * 30) > 10000 ? 10000 : 60 + ((price - 4000) / 2000 * 30))
                : 50;
            long tax = medal == null ? taxmax : taxmin;
            long maxGold = ServerManager.Instance.Configuration.MaxGold;
            if (Session.Character.Gold < tax || cRegPacket.Amount <= 0
                || Session.Character.ExchangeInfo?.ExchangeList.Count > 0 || Session.Character.IsShopping)
            {
                return;
            }

            ItemInstance it = Session.Character.Inventory.LoadBySlotAndType(cRegPacket.Slot,
                currentInventoryType);

            if (it == null || !it.Item.IsSoldable || !it.Item.IsTradable || it.IsBound || it.ItemDeleteTime != null || it.Amount < 1)
            {
                return;
            }

            if (it.Item.SellToNpcPrice * cRegPacket.Amount > 1000000000) // If price exceeds 6250 x shining blue soul for example
            {
                PenaltyLogDTO log = new PenaltyLogDTO
                {
                    AccountId = Session.Account.AccountId,
                    Reason = "Attempted Bazaar Dupe",
                    Penalty = PenaltyType.Banned,
                    DateStart = DateTime.Now,
                    DateEnd = DateTime.Now.AddYears(20),
                    AdminName = "Frostvein SYSTEM"
                };
                Character.InsertOrUpdatePenalty(log);
                Session.Disconnect();
                return;
            }

            if (it.Item.SellToNpcPrice * cRegPacket.Amount > 500000000)
            {
                foreach (var team in ServerManager.Instance.Sessions.Where(s => s.Account.Authority >= AuthorityType.ADMIN))
                {
                    if (team.HasSelectedCharacter)
                    {
                        team.SendPacket(team.Character.GenerateSay($"User {Session.Character.Name} Might be trying to dupe, please be careful. (c_reg packet, bazaar)", 12));
                    }
                }
            }

            if (Session.Character.Inventory.CountItemInAnInventory(InventoryType.Bazaar) >= 10 * (medal == null ? 2 : 10))
            {
                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LIMIT_EXCEEDED"), 0));
                return;
            }

            if (cRegPacket.Price >= (medal == null ? 1000000 : maxGold))
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PRICE_EXCEEDED"), 0));
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

            ItemInstance bazaar = Session.Character.Inventory.AddIntoBazaarInventory(
                currentInventoryType, cRegPacket.Slot,
                cRegPacket.Amount);
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

            BazaarItemDTO bazaarItem = new BazaarItemDTO
            {
                Amount = bazaar.Amount,
                DateStart = DateTime.Now,
                Duration = duration,
                IsPackage = cRegPacket.IsPackage != 0,
                MedalUsed = medal != null,
                Price = cRegPacket.Price,
                SellerId = Session.Character.CharacterId,
                ItemInstanceId = bazaar.Id,
            };

            var itemId = BazaarHttpClient.Instance.InsertOrUpdateBazaar(new InsertOrUpdateBazaarItemCommand() { BazaarItem = bazaarItem });

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

            var item = ServerManager.GetItem(bazaar.ItemVNum);

            Session.Character.Gold -= tax;
            Session.SendPacket(Session.Character.GenerateGold());

            Session.Character.BazaarItems.TryAdd(itemId, bazaarItem);

            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("OBJECT_IN_BAZAAR"),
                10));
            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("OBJECT_IN_BAZAAR"),
                0));

            Logger.LogUserEvent("BAZAAR_INSERT", Session.GenerateIdentity(),
                $"BazaarId: {bazaarItem.BazaarItemId}, IIId: {bazaarItem.ItemInstanceId} VNum: {bazaar.ItemVNum} Amount: {cRegPacket.Amount} Price: {cRegPacket.Price} Time: {duration}");
            Logger.LogUserEvent("BAZAAR_INSERT_PACKET", Session.GenerateIdentity(), $"Packet string: {cRegPacket.OriginalContent.ToString()}");
            Session.SendPacket("rc_reg 1");
        }

        #endregion Methods
    }
}