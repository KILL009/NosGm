using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject._Guri;
using NosGm.GameObject._Guri.Event;
using NosGm.GameObject.Helpers;
using System.Threading.Tasks;

namespace Plugins.BasicImplementations.Guri.Handler
{
    public class G203 : IGuriHandler
    {
        public long GuriEffectId => 203;

        public void Execute(ClientSession Session, GuriEvent e)
        {
            if (e.Type == 203 && e.Argument == 0)
            {
                // SP points initialization
                int[] listPotionResetVNums = { 1366, 1427, 5115, 9040 };
                var vnumToUse = -1;
                foreach (var vnum in listPotionResetVNums)
                {
                    if (Session.Character.Inventory.CountItem(vnum) > 0)
                    {
                        vnumToUse = vnum;
                    }
                }

                if (vnumToUse != -1)
                {
                    if (Session.Character.UseSp)
                    {
                        var specialistInstance =
                            Session.Character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp,
                                InventoryType.Wear);
                        if (specialistInstance != null)
                        {
                            specialistInstance.SlDamage = 0;
                            specialistInstance.SlDefence = 0;
                            specialistInstance.SlElement = 0;
                            specialistInstance.SlHP = 0;

                            specialistInstance.DamageMinimum = 0;
                            specialistInstance.DamageMaximum = 0;
                            specialistInstance.HitRate = 0;
                            specialistInstance.CriticalLuckRate = 0;
                            specialistInstance.CriticalRate = 0;
                            specialistInstance.DefenceDodge = 0;
                            specialistInstance.DistanceDefenceDodge = 0;
                            specialistInstance.ElementRate = 0;
                            specialistInstance.DarkResistance = 0;
                            specialistInstance.LightResistance = 0;
                            specialistInstance.FireResistance = 0;
                            specialistInstance.WaterResistance = 0;
                            specialistInstance.CriticalDodge = 0;
                            specialistInstance.CloseDefence = 0;
                            specialistInstance.DistanceDefence = 0;
                            specialistInstance.MagicDefence = 0;
                            specialistInstance.HP = 0;
                            specialistInstance.MP = 0;

                            Session.Character.Inventory.RemoveItemAmount(vnumToUse);
                            Session.Character.Inventory.DeleteFromSlotAndType((byte)EquipmentType.Sp,
                                InventoryType.Wear);
                            Session.Character.Inventory.AddToInventoryWithSlotAndType(specialistInstance,
                                InventoryType.Wear, (byte)EquipmentType.Sp);
                            Session.SendPacket(Session.Character.GenerateCond());
                            Session.SendPacket(Session.Character.GenerateLev());
                            Session.SendPackets(Session.Character.GenerateStatChar());
                            Session.SendPacket(Session.Character.GenerateStat());
                            Session.SendPacket(specialistInstance.GenerateSlInfo(Session));


                            Session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("POINTS_RESET"),
                                    0));
                        }
                    }
                    else
                    {
                        Session.SendPacket(
                            Session.Character.GenerateSay(
                                Language.Instance.GetMessageFromKey("TRANSFORMATION_NEEDED"), 10));
                    }
                }
                else
                {
                    Session.SendPacket(
                        Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("NOT_ENOUGH_POINTS"),
                            10));
                }
            }
        }
    }
}