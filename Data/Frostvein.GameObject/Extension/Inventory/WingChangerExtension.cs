using Frostvein.Domain;
using Frostvein.GameObject.Networking;

namespace Frostvein.GameObject.Extension
{
    public static class WingChangerExtension
    {
        public static void GenerateChange(ClientSession session)
        {
            var specialist = session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            var rnd = ServerManager.RandomNumber(0, 27);
            switch (rnd)
            {
                case 27: { specialist.Design = 27; session.Character.MorphUpgrade2 = 27; specialist.NewWings = 9451; } break;
                case 26: { specialist.Design = 26; session.Character.MorphUpgrade2 = 26; specialist.NewWings = 9450; } break;
                case 25: { specialist.Design = 25; session.Character.MorphUpgrade2 = 25; specialist.NewWings = 9449; } break;
                case 24: { specialist.Design = 24; session.Character.MorphUpgrade2 = 24; specialist.NewWings = 9448; } break;
                case 23: { specialist.Design = 23; session.Character.MorphUpgrade2 = 23; specialist.NewWings = 9447; } break;
                case 22: { specialist.Design = 22; session.Character.MorphUpgrade2 = 22; specialist.NewWings = 9446; } break;
                case 21: { specialist.Design = 21; session.Character.MorphUpgrade2 = 21; specialist.NewWings = 9445; } break;
                case 20: { specialist.Design = 20; session.Character.MorphUpgrade2 = 20; specialist.NewWings = 9546; } break;
                case 19: { specialist.Design = 19; session.Character.MorphUpgrade2 = 19; specialist.NewWings = 9242; } break;
                case 18: { specialist.Design = 18; session.Character.MorphUpgrade2 = 18; specialist.NewWings = 9212; } break;
                case 17: { specialist.Design = 17; session.Character.MorphUpgrade2 = 17; specialist.NewWings = 9176; } break;
                case 16: { specialist.Design = 16; session.Character.MorphUpgrade2 = 16; specialist.NewWings = 5800; } break;
                case 15: { specialist.Design = 15; session.Character.MorphUpgrade2 = 15; specialist.NewWings = 5702; } break;
                case 14: { specialist.Design = 14; session.Character.MorphUpgrade2 = 14; specialist.NewWings = 5837; } break;
                case 13: { specialist.Design = 13; session.Character.MorphUpgrade2 = 13; specialist.NewWings = 5591; } break;
                case 12: { specialist.Design = 12; session.Character.MorphUpgrade2 = 12; specialist.NewWings = 5560; } break;
                case 11: { specialist.Design = 11; session.Character.MorphUpgrade2 = 11; specialist.NewWings = 5553; } break;
                case 10: { specialist.Design = 10; session.Character.MorphUpgrade2 = 10; specialist.NewWings = 5499; } break;
                case 9: { specialist.Design = 9; session.Character.MorphUpgrade2 = 9; specialist.NewWings = 5498; } break;
                case 8: { specialist.Design = 8; session.Character.MorphUpgrade2 = 8; specialist.NewWings = 5432; } break;
                case 7: { specialist.Design = 7; session.Character.MorphUpgrade2 = 7; specialist.NewWings = 5431; } break;
                case 6: { specialist.Design = 6; session.Character.MorphUpgrade2 = 6; specialist.NewWings = 5372; } break;
                case 5: { specialist.Design = 6; session.Character.MorphUpgrade2 = 6; specialist.NewWings = 5372; } break;
                case 4: { specialist.Design = 4; session.Character.MorphUpgrade2 = 4; specialist.NewWings = 5203; } break;
                case 3: { specialist.Design = 3; session.Character.MorphUpgrade2 = 3; specialist.NewWings = 5087; } break;
                case 2: { specialist.Design = 2; session.Character.MorphUpgrade2 = 2; specialist.NewWings = 1686; } break;
                case 1: { specialist.Design = 1; session.Character.MorphUpgrade2 = 1; specialist.NewWings = 1685; } break;
                case 0: { specialist.Design = 1; session.Character.MorphUpgrade2 = 1; specialist.NewWings = 1685; } break;
            }
            specialist.ChangedWings = true;
            session.CurrentMapInstance?.Broadcast(session.Character.GenerateCMode());
            session.SendPacket(session.Character.GenerateStat());
            session.SendPackets(session.Character.GenerateStatChar());
        }
    }
}