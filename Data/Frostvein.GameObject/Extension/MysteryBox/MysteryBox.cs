using Frostvein.Domain;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.DAL;

namespace Frostvein.GameObject.Extension
{
    public static class MysteryBoxExtension
    {
        public static void GenerateMysteryBox(ClientSession Session, NRunPacket packet)
        {
            MapNpc npc = Session.CurrentMapInstance.Npcs.Find(s => s.MapNpcId == packet.NpcId);

            if (npc == null)
            {
                return;
            }
            if (Session.Character.Gold < 500000)
            {
                Session.SendPacket("info You don't have enough Gold");
                return;
            }

            var reward = MysteryBoxConfigrationExtension.PullReward();
            Session.Character.AddMysteryBoxReward(reward.Vnum, reward.Amount);
            Session.Character.Gold -= 500000;
            Session.SendPacket(Session.Character.GenerateGold());
            Session.Character.MysteryBoxCount += 1;
            if (reward.IsLegendary)
                Session.CurrentMapInstance?.Broadcast($"msg 2 {Session.Character.Name} has got the Legendary Price '{DAOFactory.ItemDAO.LoadById(reward.Vnum).Name}' from the Holy Altar!", ReceiverType.All);

        }

        public static void GenerateMysteryBoxLooped(ClientSession Session, NRunPacket packet)
        {
            if (Session.Character.Gold < 2500000 * packet.Type)
            {
                Session.SendPacket("info You don't have enough Gold");
                return;
            }

            for (var i = 0; i < (5 * packet.Type); i++)
            {
                GenerateMysteryBox(Session, packet);
            }
        }
    }
}