using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;

namespace Frostvein.GameObject.Extension.Inventory
{
    public static class UpgradeDragonCardExtension
    {
        private const short AngelFeatherVNum = 2282;
        private const short FullMoonCrystalVNum = 1030;
        private const short DragonGemVNum = 2630;
        private const short DragonProtectionScrollVNum = 9287;

        // Index 0 upgrades +15 -> +16; index 4 upgrades +19 -> +20.
        private static readonly double[] SuccessRates = { 1.2, 1.0, 0.8, 0.6, 0.4 };
        private static readonly int[] GoldCosts = { 1250000, 1500000, 1750000, 2000000, 2250000 };
        private static readonly short[] AngelFeatherCosts = { 80, 90, 100, 110, 120 };
        private static readonly short[] FullMoonCosts = { 32, 34, 36, 38, 40 };
        private static readonly short[] DragonGemCosts = { 2, 4, 6, 8, 10 };

        public static void UpgradeDragonCard(this ItemInstance specialist, ClientSession session, byte value)
        {
            // This route is the Dragon Card Protection Scroll upgrade flow.
            // The scroll converts the official "soul destroyed" outcome into a normal failure.
            if (specialist == null || session?.Character?.Inventory == null ||
                specialist.Upgrade < 15 || specialist.Upgrade >= 20)
            {
                return;
            }

            int index = specialist.Upgrade - 15;
            int goldCost = GoldCosts[index];
            short featherCost = AngelFeatherCosts[index];
            short fullMoonCost = FullMoonCosts[index];
            short dragonGemCost = DragonGemCosts[index];

            if (session.Character.Gold < goldCost ||
                session.Character.Inventory.CountItem(AngelFeatherVNum) < featherCost ||
                session.Character.Inventory.CountItem(FullMoonCrystalVNum) < fullMoonCost ||
                session.Character.Inventory.CountItem(DragonGemVNum) < dragonGemCost ||
                session.Character.Inventory.CountItem(DragonProtectionScrollVNum) < 1)
            {
                session.SendPacket(session.Character.GenerateSay(
                    "Not enough gold or materials for the specialist upgrade.", 10));
                return;
            }

            session.Character.Inventory.RemoveItemAmount(AngelFeatherVNum, featherCost);
            session.Character.Inventory.RemoveItemAmount(FullMoonCrystalVNum, fullMoonCost);
            session.Character.Inventory.RemoveItemAmount(DragonGemVNum, dragonGemCost);
            session.Character.Inventory.RemoveItemAmount(DragonProtectionScrollVNum, 1);
            session.Character.Gold -= goldCost;
            session.SendPacket(session.Character.GenerateGold());

            double roll = ServerManager.NextDoubleLinear(0, 100);
            if (roll < SuccessRates[index])
            {
                specialist.Upgrade++;
                session.CurrentMapInstance?.Broadcast(
                    StaticPacketHelper.GenerateEff(UserType.Player,
                        session.Character.CharacterId, 3005),
                    session.Character.PositionX, session.Character.PositionY);
                session.SendPacket("msg 4 Upgrade successful");
                session.SendPacket(session.Character.GenerateSay(
                    "The Specialist Upgrade was successful.", 12));
            }
            else
            {
                session.SendPacket("msg 4 Upgrade failed");
                session.SendPacket(session.Character.GenerateSay(
                    "The Specialist Upgrade failed.", 11));
            }

            session.SendShopEnd();
            session.SendPacket(session.Character.GenerateEq());
            session.SendPacket(specialist.GenerateInventoryAdd());
        }
    }
}
