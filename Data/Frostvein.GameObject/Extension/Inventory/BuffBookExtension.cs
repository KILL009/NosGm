using Frostvein.Configuration;
using Frostvein.Domain;
using Frostvein.GameObject.Extension.Message;

namespace Frostvein.GameObject.Extension.Inventory
{
    public static class BuffBookExtension
    {
        public static void Buy(ClientSession session)
        {
            if (session.Character.Gold >= 10000000)
            {
                session.Character.GiftAdd(14003, 1);
                session.Character.Gold -= 10000000;
                session.SendPacket(session.Character.GenerateGold());
                session.Character.BuffCharge = 100;
                MessageExtension.SendGrey(session, "You received the Buff Book including 100 free Charges!");
            }
            else
            {
                session.SendPacket("info You dont have enough Gold");
            }
        }

        public static void ChargeBy10(ClientSession session)
        {
            if (session.Character.BuffCharge >= 100)
            {
                MessageExtension.SendRed(session, "You can not have more than 100 Charges at the same time");
                return;
            }
            if (session.Character.BuffCharge > 90)
            {
                session.SendPacket("info Buff Charges can not be higher than 100");
                return;
            }
            if (session.Character.Gold >= 500000)
            {
                session.Character.Gold -= 500000;
                session.SendPacket(session.Character.GenerateGold());
                session.Character.BuffCharge += 10;
                MessageExtension.SendGrey(session, "You received 10 Charges!");
            }
            else
            {
                session.SendPacket("info You dont have enough Gold");
            }
        }

        public static void ChargeBy50(ClientSession session)
        {
            if (session.Character.BuffCharge >= 100)
            {
                MessageExtension.SendRed(session, "You can not have more than 100 Charges at the same time");
                return;
            }
            if (session.Character.BuffCharge > 60)
            {
                session.SendPacket("info Buff Charges can not be higher than 100");
                return;
            }
            if (session.Character.Gold >= 1000000)
            {
                session.Character.Gold -= 1000000;
                session.SendPacket(session.Character.GenerateGold());
                session.Character.BuffCharge += 50;
                MessageExtension.SendGrey(session, "You received 50 Charges!");
            }
            else
            {
                session.SendPacket("info You dont have enough Gold");
            }
        }

        public static void ChargeFull(ClientSession session)
        {
            if (session.Character.BuffCharge == 100)
            {
                session.SendPacket("msg 4 You already have 100 Charges");
                return;
            }
            if (session.Character.Gold >= 1250000)
            {
                session.Character.Gold -= 1250000;
                session.SendPacket(session.Character.GenerateGold());
                session.Character.BuffCharge = 100;
                MessageExtension.SendGrey(session, "Your Charges have been set to 100");
            }
            else
            {
                session.SendPacket("info You dont have enough Gold");
            }
        }

        public static void ApplyBuffs(ClientSession session)
        {
            foreach (short buff in GameConfiguration.BuffsToAdd)
            {
                session.Character.AddBuff(new Buff(buff, session.Character.Level), session.Character.BattleEntity);
            }
            session.Character.BuffCharge--;
            MessageExtension.SendGrey(session, $"Remaining charges: {session.Character.BuffCharge}");
            session.CurrentMapInstance?.Broadcast($"eff 1 {session.Character.CharacterId} 8138", ReceiverType.All);
        }
    }
}
