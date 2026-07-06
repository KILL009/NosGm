using Frostvein.GameObject.Networking;

namespace Frostvein.GameObject.Extension
{
    public static class TrophyExtension
    {
        public static void GenerateTrophy(ClientSession session, ItemInstance inv)
        {
            string say = "info You already have this Trophy";
            switch (inv.ItemVNum)
            {
                case 12001: if (session.Character.Trophy1 > 0) { session.SendPacket(say); return; } session.Character.Trophy1 += 12001; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12002: if (session.Character.Trophy2 > 0) { session.SendPacket(say); return; } session.Character.Trophy2 += 12002; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12003: if (session.Character.Trophy3 > 0) { session.SendPacket(say); return; } session.Character.Trophy3 += 12003; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12004: if (session.Character.Trophy4 > 0) { session.SendPacket(say); return; } session.Character.Trophy4 += 12004; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12005: if (session.Character.Trophy5 > 0) { session.SendPacket(say); return; } session.Character.Trophy5 += 12005; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12006: if (session.Character.Trophy6 > 0) { session.SendPacket(say); return; } session.Character.Trophy6 += 12006; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12007: if (session.Character.Trophy7 > 0) { session.SendPacket(say); return; } session.Character.Trophy7 += 12007; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12008: if (session.Character.Trophy8 > 0) { session.SendPacket(say); return; } session.Character.Trophy8 += 12008; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12009: if (session.Character.Trophy9 > 0) { session.SendPacket(say); return; } session.Character.Trophy9 += 12009; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12010: if (session.Character.Trophy10 > 0) { session.SendPacket(say); return; } session.Character.Trophy10 += 12010; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12011: if (session.Character.Trophy11 > 0) { session.SendPacket(say); return; } session.Character.Trophy11 += 12011; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12012: if (session.Character.Trophy12 > 0) { session.SendPacket(say); return; } session.Character.Trophy12 += 12012; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12013: if (session.Character.Trophy13 > 0) { session.SendPacket(say); return; } session.Character.Trophy13 += 12013; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12014: if (session.Character.Trophy14 > 0) { session.SendPacket(say); return; } session.Character.Trophy14 += 12014; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12015: if (session.Character.Trophy15 > 0) { session.SendPacket(say); return; } session.Character.Trophy15 += 12015; session.Character.TrophyCount += 1; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;

                case 12017: if (session.Character.LegendaryTrophy > 0) { session.SendPacket(say); return; } session.Character.LegendaryTrophy += 12017; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12018: if (session.Character.LegendaryTrophy > 0) { session.SendPacket(say); return; } session.Character.LegendaryTrophy += 12018; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12019: if (session.Character.LegendaryTrophy > 0) { session.SendPacket(say); return; } session.Character.LegendaryTrophy += 12019; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12020: if (session.Character.LegendaryTrophy > 0) { session.SendPacket(say); return; } session.Character.LegendaryTrophy += 12020; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12021: if (session.Character.LegendaryTrophy > 0) { session.SendPacket(say); return; } session.Character.LegendaryTrophy += 12021; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12022: if (session.Character.LegendaryTrophy > 0) { session.SendPacket(say); return; } session.Character.LegendaryTrophy += 12022; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12023: if (session.Character.LegendaryTrophy > 0) { session.SendPacket(say); return; } session.Character.LegendaryTrophy += 12023; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
                case 12024: if (session.Character.LegendaryTrophy > 0) { session.SendPacket(say); return; } session.Character.LegendaryTrophy += 12024; session.Character.Inventory.RemoveItemFromInventory(inv.Id); break;
            }
        }

        public static void AddTrophy(ClientSession Session, short ItemVNum)
        {
            int chance = ServerManager.RandomNumber(0, 100);
            if (chance < 2)
            {
                Session.Character.GiftAdd(ItemVNum, 1);
            }
        }
    }
}