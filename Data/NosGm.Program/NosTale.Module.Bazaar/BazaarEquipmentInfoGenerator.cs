using NosGm.Domain;
using NosGm.GameObject;
using System;
using System.Linq;

namespace NosTale.Module.Bazaar
{
    /// <summary>
    /// Generates equipment information inside the standalone bazaar process, where an
    /// ItemInstance usually has no live World ClientSession attached to it.
    /// </summary>
    internal static class BazaarEquipmentInfoGenerator
    {
        public static string Generate(ItemInstance item)
        {
            if (item?.Item == null)
            {
                return string.Empty;
            }

            try
            {
                string info = item.Item.EquipmentSlot == EquipmentType.Fairy
                    ? GenerateFairyInfo(item)
                    : item.GenerateEInfo();

                return Normalize(info);
            }
            catch
            {
                // One malformed legacy listing must never abort the complete bazaar response.
                return string.Empty;
            }
        }

        private static string GenerateFairyInfo(ItemInstance item)
        {
            byte isMaxed = item.ElementRate == item.Item.MaxElementRate ? (byte)1 : (byte)2;
            int remainingMonsters = item.Session?.Character != null ? item.CalculateMonster() : 0;

            if (item.FairyLevel == 0)
            {
                return $"e_info 4 {item.ItemVNum} {item.Item.Element} {item.ElementRate} 0 0 0 0 {isMaxed} {remainingMonsters} 0 0 0 0 0";
            }

            string packetAddition = string.Concat(item.FairyEnchantments
                .Where(enchantment => enchantment.EquipmentSerialId == item.EquipmentSerialId)
                .Select(enchantment =>
                    $" {enchantment.FirstData}.{enchantment.SecondData}.{enchantment.ThirdData}"));

            long boundOwner = item.IsBound
                ? item.BoundCharacterId ?? item.CharacterId
                : 0;

            return $"e_info 4 {item.ItemVNum} {item.Item.Element} {item.ElementRate} 0 0 0 0 {isMaxed} " +
                   $"{remainingMonsters} {boundOwner} {item.Rare} 0 {item.FairyLevel} {packetAddition}";
        }

        private static string Normalize(string info)
        {
            return string.IsNullOrWhiteSpace(info)
                ? string.Empty
                : info.Replace(' ', '^')
                    .Replace("e_info^", string.Empty)
                    .Replace("slinfo^", string.Empty);
        }
    }
}
