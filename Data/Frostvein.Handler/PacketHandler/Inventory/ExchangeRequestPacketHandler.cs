using Frostvein.Configuration;
using Frostvein.Extension.Extension.Packet;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Service;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Inventory
{
    public class ExchangeRequestPacketHandler : IPacketHandler
    {
        #region Instantiation

        public ExchangeRequestPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task ExchangeRequest(ExchangeRequestPacket exchangeRequestPacket)
        {
            if (Session.Account.IsLimited)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("LIMITED_ACCOUNT")));
                return;
            }

            if (exchangeRequestPacket != null)
            {
                var sess = ServerManager.Instance.GetSessionByCharacterId(exchangeRequestPacket.CharacterId);

                if (sess != null && Session.Character.MapInstanceId != sess.Character.MapInstanceId)
                {
                    sess.Character.ExchangeInfo = null;
                    Session.Character.ExchangeInfo = null;
                }
                else
                {
                    switch (exchangeRequestPacket.RequestType)
                    {
                        case RequestExchangeType.Requested:
                            if (!Session.HasCurrentMapInstance) return;

                            var targetSession = Session.CurrentMapInstance.GetSessionByCharacterId(exchangeRequestPacket.CharacterId);
                            if (targetSession?.Account == null) return;

                            if (targetSession.Account.IsLimited)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("CANNOT_TRADE_LIMITED_ACCOUNT")));
                                return;
                            }

                            if (targetSession.CurrentMapInstance?.MapInstanceType == MapInstanceType.TalentArenaMapInstance) return;

                            if (targetSession.Character.Group != null && targetSession.Character.Group?.GroupType != GroupType.Group)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_IN_RAID"), 0));
                                return;
                            }

                            if (Session.Character.Group != null && Session.Character.Group?.GroupType != GroupType.Group)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_WITH_RAID_MEMBER"), 0));
                                return;
                            }

                            if (Session.Character.IsBlockedByCharacter(exchangeRequestPacket.CharacterId))
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("BLACKLIST_BLOCKED")));
                                return;
                            }


                            if (Session.Character.Speed == 0 || targetSession.Character.Speed == 0)
                            {
                                Session.Character.ExchangeBlocked = true;
                            }

                            if (targetSession.Character.LastSkillUse.AddSeconds(20) > DateTime.Now || targetSession.Character.LastDefence.AddSeconds(20) > DateTime.Now)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("PLAYER_IN_BATTLE"), targetSession.Character.Name)));
                                return;
                            }

                            if (Session.Character.LastSkillUse.AddSeconds(20) > DateTime.Now || Session.Character.LastDefence.AddSeconds(20) > DateTime.Now)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("IN_BATTLE")));
                                return;
                            }

                            if (Session.Character.HasShopOpened || targetSession.Character.HasShopOpened)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("HAS_SHOP_OPENED"), 10));
                                return;
                            }

                            if (targetSession.Character.ExchangeBlocked)
                            {
                                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TRADE_BLOCKED"), 11));
                            }
                            else
                            {
                                if (Session.Character.InExchangeOrTrade || targetSession.Character.InExchangeOrTrade)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("ALREADY_EXCHANGE"), 0));
                                }
                                else
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateModal(string.Format(Language.Instance.GetMessageFromKey("YOU_ASK_FOR_EXCHANGE"), targetSession.Character.Name), 0));

                                    Logger.LogUserEvent("TRADE_REQUEST", Session.GenerateIdentity(), $"[ExchangeRequest][{targetSession.GenerateIdentity()}]");

                                    Session.Character.TradeRequests.Add(targetSession.Character.CharacterId);
                                    targetSession.SendPacket(UserInterfaceHelper.GenerateDialog(
                                       $"#req_exc^2^{Session.Character.CharacterId} #req_exc^5^{Session.Character.CharacterId} Accept trade from {Session.Character.Name}({Session.Character.Level}+{Session.Character.HeroLevel}) | {Session.Character.Class}? "));
                                }
                            }

                            break;

                        case RequestExchangeType.Confirmed: // click Trade button in exchange window
                            if (Session.HasCurrentMapInstance && Session.HasSelectedCharacter
                                                              && Session.Character.ExchangeInfo != null
                                                              && Session.Character.ExchangeInfo.TargetCharacterId
                                                              != Session.Character.CharacterId)
                            {
                                if (!Session.HasCurrentMapInstance)
                                {
                                    return;
                                }

                                targetSession = Session.CurrentMapInstance.GetSessionByCharacterId(Session.Character.ExchangeInfo.TargetCharacterId);

                                if (targetSession == null)
                                {
                                    return;
                                }

                                if (Session.Character.Group != null && Session.Character.Group?.GroupType != GroupType.Group)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_IN_RAID"), 0));
                                    return;
                                }

                                if (targetSession.Character.Group != null && targetSession.Character.Group?.GroupType != GroupType.Group)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_WITH_RAID_MEMBER"), 0));
                                    return;
                                }

                                if (Session.IsDisposing || targetSession.IsDisposing)
                                {
                                    Session.CloseExchange(targetSession);
                                    return;
                                }

                                lock (targetSession.Character.Inventory)
                                {
                                    lock (Session.Character.Inventory)
                                    {
                                        var targetExchange = targetSession.Character.ExchangeInfo;
                                        var inventory = targetSession.Character.Inventory;

                                        var gold = targetSession.Character.Gold;
                                        var goldBank = targetSession.Character.GoldBank;
                                        var maxGold = GameConfiguration.MaxGold;
                                        var maxGoldBank = InventoryConfigrationExtension.MaxGoldBank;

                                        if (targetExchange == null || Session.Character.ExchangeInfo == null) return;

                                        if (Session.Character.ExchangeInfo.Validated && targetExchange.Validated)
                                        {
                                            Logger.LogUserEvent("TRADE_ACCEPT", Session.GenerateIdentity(), $"[ExchangeAccept][{targetSession.GenerateIdentity()}]");
                                            try
                                            {
                                                Session.Character.ExchangeInfo.Confirmed = true;
                                                if (targetExchange.Confirmed && Session.Character.ExchangeInfo.Confirmed)
                                                {
                                                    Session.SendPacket("exc_close 1");
                                                    targetSession.SendPacket("exc_close 1");

                                                    var continues = true;
                                                    var goldmax = false;
                                                    if (!Session.Character.Inventory.EnoughPlace(targetExchange
                                                        .ExchangeList))
                                                        continues = false;

                                                    continues &= inventory.EnoughPlace(Session.Character.ExchangeInfo.ExchangeList);
                                                    goldmax |= Session.Character.ExchangeInfo.Gold + gold > maxGold;
                                                    goldmax |= Session.Character.ExchangeInfo.BankGold + goldBank > maxGoldBank;
                                                    if (Session.Character.ExchangeInfo.Gold > Session.Character.Gold || Session.Character.ExchangeInfo.BankGold > Session.Character.GoldBank)
                                                        return;

                                                    goldmax |= targetExchange.Gold + Session.Character.Gold > maxGold;
                                                    goldmax |= targetExchange.BankGold + Session.Character.GoldBank >
                                                               maxGoldBank;
                                                    if (!continues || goldmax)
                                                    {
                                                        var message = !continues
                                                            ? UserInterfaceHelper.GenerateMsg(
                                                                Language.Instance.GetMessageFromKey("NOT_ENOUGH_PLACE"),
                                                                0)
                                                            : UserInterfaceHelper.GenerateMsg(
                                                                Language.Instance.GetMessageFromKey("MAX_GOLD"), 0);
                                                        Session.SendPacket(message);
                                                        targetSession.SendPacket(message);
                                                        Session.CloseExchange(targetSession);
                                                    }
                                                    else if (Session.Character.Gold <
                                                        Session.Character.ExchangeInfo.Gold || targetSession.Character
                                                                                                .Gold < targetExchange
                                                                                                .Gold
                                                                                            || Session.Character
                                                                                                .GoldBank <
                                                                                            Session.Character
                                                                                                .ExchangeInfo
                                                                                                .BankGold ||
                                                                                            targetSession.Character
                                                                                                .GoldBank <
                                                                                            targetExchange.BankGold)
                                                    {
                                                        var message = UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ERROR_ON_EXANGE"), 0);
                                                        Session.SendPacket(message);
                                                        targetSession.SendPacket(message);
                                                        Session.CloseExchange(targetSession);
                                                    }
                                                    else
                                                    {
                                                        if (Session.Character.ExchangeInfo.ExchangeList.Any(ei =>
                                                            !(ei.Item.IsTradable || ei.IsBound)))
                                                        {
                                                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_NOT_TRADABLE"), 0));
                                                            Session.CloseExchange(targetSession);
                                                        }

                                                        if (targetSession.Character.ExchangeInfo.ExchangeList.Any(ei => !(ei.Item.IsTradable || ei.IsBound)))
                                                        {
                                                            targetSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ITEM_NOT_TRADABLE"), 0));
                                                            targetSession.CloseExchange(Session);
                                                        }
                                                        else // all items can be traded
                                                        {
                                                            Session.Character.IsExchanging = targetSession.Character.IsExchanging = true;

                                                            // exchange all items from target to source
                                                            targetSession.Exchange(Session);

                                                            // exchange all items from source to target
                                                            Session.Exchange(targetSession);

                                                            Session.Character.IsExchanging = targetSession.Character.IsExchanging = false;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("IN_WAITING_FOR"), targetSession.Character.Name)));
                                                }
                                            }
                                            catch (NullReferenceException nre)
                                            {
                                                Logger.Error(nre);
                                            }
                                        }
                                    }
                                }
                            }

                            break;

                        case RequestExchangeType.Cancelled: // cancel trade thru exchange window

                            if (Session.HasCurrentMapInstance && Session.Character.ExchangeInfo != null)
                            {
                                targetSession = Session.CurrentMapInstance.GetSessionByCharacterId(Session.Character.ExchangeInfo.TargetCharacterId);
                                Session.CloseExchange(targetSession);
                            }

                            break;

                        case RequestExchangeType.List:
                            if (sess != null && (!Session.Character.InExchangeOrTrade || !sess.Character.InExchangeOrTrade))
                            {
                                var otherSession = ServerManager.Instance.GetSessionByCharacterId(exchangeRequestPacket.CharacterId);
                                if (exchangeRequestPacket.CharacterId == Session.Character.CharacterId
                                    || Session.Character.Speed == 0 || otherSession == null
                                    || otherSession.Character.TradeRequests.All(s => s != Session.Character.CharacterId)
                                )
                                    return;

                                if (Session.Character.Group != null && Session.Character.Group?.GroupType != GroupType.Group)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_IN_RAID"), 0));
                                    return;
                                }

                                if (otherSession.Character.Group != null && otherSession.Character.Group?.GroupType != GroupType.Group)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("EXCHANGE_NOT_ALLOWED_WITH_RAID_MEMBER"), 0));
                                    return;
                                }

                                Session.SendPacket($"exc_list 1 {exchangeRequestPacket.CharacterId} -1");
                                Session.Character.ExchangeInfo = new ExchangeInfo
                                {
                                    TargetCharacterId = exchangeRequestPacket.CharacterId,
                                    Confirmed = false
                                };
                                sess.Character.ExchangeInfo = new ExchangeInfo
                                {
                                    TargetCharacterId = Session.Character.CharacterId,
                                    Confirmed = false
                                };
                                Session.CurrentMapInstance?.Broadcast(Session, $"exc_list 1 {Session.Character.CharacterId} -1", ReceiverType.OnlySomeone, "", exchangeRequestPacket.CharacterId);
                            }
                            else
                            {
                                Session.CurrentMapInstance?.Broadcast(Session,UserInterfaceHelper.GenerateModal(Language.Instance.GetMessageFromKey("ALREADY_EXCHANGE"), 0),
                                    ReceiverType.OnlySomeone, "", exchangeRequestPacket.CharacterId);
                            }

                            break;

                        case RequestExchangeType.Declined:
                            if (sess != null) sess.Character.ExchangeInfo = null;
                            Session.Character.ExchangeInfo = null;
                            Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("YOU_REFUSED"), 10));
                            if (sess != null)
                            sess.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("EXCHANGE_REFUSED"),Session.Character.Name), 10));

                            break;

                        default:
                            Logger.Warn($"Exchange-Request-Type not implemented. RequestType: {exchangeRequestPacket.RequestType})");
                            break;
                    }
                }
            }
        }

        #endregion
    }
}