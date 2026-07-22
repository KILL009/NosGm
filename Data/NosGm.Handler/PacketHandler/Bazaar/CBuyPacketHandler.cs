using NosTale.Configuration;
using NosTale.Packets.Packets.ClientPackets;
using NosTale.Packets.Packets.ServerPackets;
using OpenNos.Core;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using OpenNos.Master.Library.Client;
using OpenNos.Master.Library.Data;
using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenNos.Handler.PacketHandler.Bazaar
{
    public class CBuyPacketHandler : IPacketHandler
    {
        #region Instantiation

        public CBuyPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task BuyBazaarAsync(CBuyPacket cBuyPacket)
        {
            Session.Character.BazaarRequests++;

            if (Session == null || Session.Character == null)
            {
                return;
            }

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

            if (Session.Character.Channel.ChannelId > 1)
            {
                Session.SendPacket("info The NosBazaar can only be accessed on Channel 1");
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

            if (Session.Character.InExchangeOrTrade || Session.Character.HasShopOpened)
            {
                return;
            }
            if (Session.Character.IsMuted())
            {
                return;
            }
            if (Session.Character.BazaarRequests > 20)
            {
                PenaltyLogDTO log = new PenaltyLogDTO
                {
                    AccountId = Session.Account.AccountId,
                    Reason = "Auto ban c_buy PL",
                    Penalty = PenaltyType.Banned,
                    DateStart = DateTime.Now,
                    DateEnd = DateTime.Now.AddYears(2),
                    AdminName = "Administrator"
                };
                Character.InsertOrUpdatePenalty(log);
                Session?.Disconnect();
                return;
            }
            Observable.Timer(TimeSpan.FromSeconds(10)).Subscribe(x =>
            {
                if (Session?.Character?.BazaarRequests > 0)
                    Session.Character.BazaarRequests = 0;
            });

            SpinWait.SpinUntil(() => !ServerManager.Instance.InBazaarRefreshMode);

            var bz = DAOFactory.BazaarItemDAO.LoadById(cBuyPacket.BazaarId);
            if (bz != null && cBuyPacket.Amount > 0)
            {
                lock (bz)
                {
                    var price = cBuyPacket.Amount * bz.Price;
                    if (Session.Character.Gold >= price)
                    {
                        var bzcree = new BazaarItemLink { BazaarItem = bz };
                        if (DAOFactory.CharacterDAO.LoadById(bz.SellerId) != null)
                        {
                            bzcree.Owner = DAOFactory.CharacterDAO.LoadById(bz.SellerId)?.Name;
                            bzcree.Item = new ItemInstance(DAOFactory.ItemInstanceDAO.LoadById(bz.ItemInstanceId));
                        }
                        else
                        {
                            return;
                        }

                        if (bz == null || cBuyPacket.Amount <= 0)
                        {
                            //ADD MONGO LOG
                            return;
                        }

                        if (cBuyPacket.Amount <= bzcree.Item.Amount)
                        {
                            if (!Session.Character.Inventory.CanAddItem(bzcree.Item.ItemVNum))
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"), 0));
                                return;
                            }

                            if (bzcree.Item == null)
                            {
                                return;
                            }

                            if (bz.Price != cBuyPacket.Price)
                            {
                                Logger.LogUserEvent("[BazaarHack] : Incorrect price", Session.GenerateIdentity(),
                                      $"BazaarId: {cBuyPacket.BazaarId} VNum: {cBuyPacket.VNum} Amount: {cBuyPacket.Amount} Price: {cBuyPacket.Price}");
                                return;
                            }

                            if (bz.AccountId == Session.Account.AccountId)
                            {
                                return;
                            }

                            if (bz.RegistrationIP == Session.Account.RegistrationIP)
                            {
                                Session.SendPacket("info You can not buy an Item from yourself");
                                return;
                            }

                            if (bz.CurrentIp == Session.Character.CurrentIp)
                            {
                                Session.SendPacket("info You can not buy an Item from yourself");
                                return;
                            }

                            if (bz.SellerId == Session.Character.CharacterId)
                            {
                                PenaltyLogDTO log = new()
                                {
                                    AccountId = Session.Account.AccountId,
                                    Reason = "Bazaar_Buy_Exploit_Same_Target",
                                    Penalty = PenaltyType.Banned,
                                    DateStart = DateTime.Now,
                                    DateEnd = DateTime.Now.AddYears(10),
                                    AdminName = "Administrator"
                                };
                                Session.SendPacket("info Your Account has been banned.");
                                Character.InsertOrUpdatePenalty(log);
                                Session.Disconnect();
                                return;
                            }

                            

                            if (Session.Character.LastBazaarModeration.AddSeconds(2) > DateTime.Now)
                            {
                                Session.SendPacket("info You have to wait 2 seconds");
                                return;
                            }

                            

                            if (bzcree.Item != null)
                            {
                                if (bz.IsPackage && cBuyPacket.Amount != bz.Amount)
                                {
                                    return;
                                }

                                var bzitemdto = DAOFactory.ItemInstanceDAO.LoadById(bzcree.BazaarItem.ItemInstanceId);
                                if (bzitemdto.Amount < cBuyPacket.Amount)
                                {
                                    return;
                                }

                                // Edit this soo we dont generate new guid every single time we take
                                // something out.
                                var newBz = bzcree.Item.DeepCopy();
                                newBz.Id = Guid.NewGuid();
                                newBz.Amount = cBuyPacket.Amount;
                                newBz.Type = newBz.Item.Type;
                                var newInv = Session.Character.Inventory.AddToInventory(newBz);

                                if (newInv.Count <= 0)
                                {
                                    return;
                                }
                                if (newInv.Count > 0)
                                {
                                    bzitemdto.Amount -= cBuyPacket.Amount;
                                    Session.Character.Gold -= price;
                                    Session.SendPacket(Session.Character.GenerateGold());
                                    DAOFactory.ItemInstanceDAO.InsertOrUpdate(bzitemdto);
                                    ServerManager.Instance.BazaarRefresh(bzcree.BazaarItem.BazaarItemId);
                                    Session.SendPacket($"rc_buy 1 {bzcree.Item.Item.VNum} {bzcree.Owner} {cBuyPacket.Amount} {cBuyPacket.Price} 0 0 0");
                                    Session.SendPacket(Session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {bzcree.Item.Item.Name} x{cBuyPacket.Amount}", 10));
                                    Session.SendPacket("rc_reg 1");
                                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                                    {
                                        DestinationCharacterId = bz.SellerId,
                                        SourceWorldId = ServerManager.Instance.WorldId,
                                        Message = StaticPacketHelper.Say(1, bz.SellerId, 12, string.Format(Language.Instance.GetMessageFromKey("BAZAAR_ITEM_SOLD"), Session.Character.Name, bzcree.Item.Item.Name, cBuyPacket.Amount)),
                                        Type = MessageType.Other
                                    });

                                    //ADD MONGODB
                                    Logger.LogUserEvent("BAZAAR_BUY", Session.GenerateIdentity(), $"BazaarId: {cBuyPacket.BazaarId} VNum: {cBuyPacket.VNum} Amount: {cBuyPacket.Amount} Price: {cBuyPacket.Price}");
                                    Logger.LogUserEvent("BAZAAR_BUY_PACKET", Session.GenerateIdentity(), $"Packet string: {cBuyPacket.OriginalContent.ToString()}");
                                    Session.Character.LastBazaarModeration = DateTime.Now;
                                }
                            }
                        }
                        else
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("STATE_CHANGED"), 1));
                        }
                    }
                    else
                    {
                        Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 10));
                        Session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("NOT_ENOUGH_MONEY"), 1));
                    }
                }
            }
            else
            {
                await Session.SendPacketAsync(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("STATE_CHANGED"), 1));
            }
        }

        #endregion
    }
}