using NosGm.Packets.Packets.ServerPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Basic
{
    public class GboxPacketHandler : IPacketHandler
    {
        #region Instantiation

        public GboxPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void BankAction(GboxPacket gboxPacket)
        {
            if (Session.Character.InExchangeOrTrade)
            {
                return;
            }

            var deposit = gboxPacket.Amount.ToString("N0").Replace(".", ",");
            var withdraw = gboxPacket.Amount.ToString("N0").Replace(".", ",");

            switch (gboxPacket.Type)
            {
                case BankActionType.Deposit:
                    if (gboxPacket.Option == 0)
                    {
                        Session.SendPacket($"qna #gbox^1^{gboxPacket.Amount}^1 Want to deposit {deposit},000 gold?");
                        return;
                    }

                    if (gboxPacket.Option == 1)
                    {
                        if (gboxPacket.Amount <= 0)
                        {
                            return;
                        }

                        Session.SendPacket(UserInterfaceHelper.GenerateShopMemo((byte)SmemoType.Information, string.Format(Language.Instance.GetMessageFromKey("BANK_DEPOSIT"), $"{deposit},")));
                        if (Session.Character.GoldBank + gboxPacket.Amount * 1000 > InventoryConfigrationExtension.MaxGoldBank)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("MAX_GOLD_BANK_REACHED")));
                            Session.SendPacket(UserInterfaceHelper.GenerateShopMemo((byte)SmemoType.Error, Language.Instance.GetMessageFromKey("MAX_GOLD_BANK_REACHED")));
                            return;
                        }

                        if (Session.Character.Gold < gboxPacket.Amount * 1000)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NOT_ENOUGH_GOLD")));
                            Session.SendPacket(UserInterfaceHelper.GenerateShopMemo((byte)SmemoType.Error, Language.Instance.GetMessageFromKey("NOT_ENOUGH_GOLD")));
                            return;
                        }

                        Session.Character.GoldBank += gboxPacket.Amount * 1000;
                        Session.Character.Gold -= gboxPacket.Amount * 1000;
                        var bankBalance = Session.Character.GoldBank.ToString("N0").Replace(".", ",");
                        var charGold = Session.Character.Gold.ToString("N0").Replace(".", ",");
                        Session.SendPacket(Session.Character.GenerateGold());
                        Session.SendPacket(Session.Character.GenerateGb((byte)GoldBankPacketType.Deposit));
                        Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("BANK_BALANCE"), bankBalance, charGold), 1));
                        Session.SendPacket(UserInterfaceHelper.GenerateShopMemo((byte)SmemoType.Balance, string.Format(Language.Instance.GetMessageFromKey("BANK_BALANCE"), $"{bankBalance}", charGold)));
                    }

                    break;

                case BankActionType.Withdraw:
                    if (gboxPacket.Option == 0)
                    {
                        Session.SendPacket($"qna #gbox^2^{gboxPacket.Amount}^1 Would you like to withdraw {withdraw},000 gold? (Fee: 0 gold)");
                        return;
                    }

                    if (gboxPacket.Option == 1)
                    {
                        if (gboxPacket.Amount <= 0)
                        {
                            return;
                        }

                        Session.SendPacket(UserInterfaceHelper.GenerateShopMemo((byte)SmemoType.Information, string.Format(Language.Instance.GetMessageFromKey("WITHDRAW_BANK"), $"{withdraw},")));
                        if (Session.Character.Gold + gboxPacket.Amount * 1000 > InventoryConfigrationExtension.MaxGoldBank)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("TOO_MUCH_GOLD")));
                            Session.SendPacket(UserInterfaceHelper.GenerateShopMemo((byte)SmemoType.Error, Language.Instance.GetMessageFromKey("TOO_MUCH_GOLD")));
                            return;
                        }

                        if (Session.Character.GoldBank < gboxPacket.Amount * 1000)
                        {
                            Session.SendPacket(UserInterfaceHelper.GenerateInfo("NOT_ENOUGH_FUNDS"));
                            Session.SendPacket(UserInterfaceHelper.GenerateShopMemo((byte)SmemoType.Error, Language.Instance.GetMessageFromKey("NOT_ENOUGH_FUNDS")));
                            return;
                        }

                        Session.Character.GoldBank -= gboxPacket.Amount * 1000;
                        Session.Character.Gold += gboxPacket.Amount * 1000;
                        var bankBalance = Session.Character.GoldBank.ToString("N0").Replace(".", ",");
                        var charGold = Session.Character.Gold.ToString("N0").Replace(".", ",");
                        Session.SendPacket(Session.Character.GenerateGold());
                        Session.SendPacket(Session.Character.GenerateGb((byte)GoldBankPacketType.Withdraw));
                        Session.SendPacket(UserInterfaceHelper.GenerateShopMemo((byte)SmemoType.Balance, string.Format(Language.Instance.GetMessageFromKey("BANK_BALANCE"), bankBalance, charGold)));
                    }

                    break;
            }
        }

        #endregion
    }
}