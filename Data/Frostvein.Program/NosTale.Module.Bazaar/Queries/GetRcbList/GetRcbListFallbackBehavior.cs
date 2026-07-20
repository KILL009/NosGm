using MediatR;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using Frostvein.Packets.Packets.ClientPackets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Queries.GetRcbList
{
    /// <summary>
    /// Keeps global bazaar searches usable when one malformed legacy listing causes the
    /// legacy handler to return an empty HTTP body. A valid empty rc_blist packet is not
    /// replaced, so this behavior only activates after an actual handler failure.
    /// </summary>
    public sealed class GetRcbListFallbackBehavior : IPipelineBehavior<GetRcbListQuery, string>
    {
        private readonly BazaarManager _bazaarManager;

        public GetRcbListFallbackBehavior(BazaarManager bazaarManager)
        {
            _bazaarManager = bazaarManager ?? throw new ArgumentNullException(nameof(bazaarManager));
        }

        public async Task<string> Handle(
            GetRcbListQuery request,
            RequestHandlerDelegate<string> next,
            CancellationToken cancellationToken)
        {
            string response = await next();
            if (!string.IsNullOrWhiteSpace(response))
            {
                return response;
            }

            int index = request?.Packet?.Index ?? 0;
            try
            {
                string fallback = BuildResponse(request?.Packet);
                Console.WriteLine($"[BAZAAR_SEARCH_FALLBACK] Recovered global search page {index}.");
                return fallback;
            }
            catch (Exception exception)
            {
                Console.WriteLine($"[BAZAAR_SEARCH_FALLBACK] Unable to rebuild page {index}: {exception}");
                return $"rc_blist {index}  ";
            }
        }

        private string BuildResponse(CBListPacket packet)
        {
            if (packet == null)
            {
                return "rc_blist 0  ";
            }

            HashSet<short> requestedVNums = ParseRequestedVNums(packet.ItemVNumFilter);
            DateTime now = DateTime.Now;

            List<BazaarItemLink> candidates = _bazaarManager.BazaarItemLinks.Values
                .Where(link => IsUsableLink(link, now))
                .Where(link => requestedVNums.Count == 0 || requestedVNums.Contains(link.Item.ItemVNum))
                .Where(link => MatchesFilters(link, packet))
                .ToList();

            candidates = ApplyOrder(candidates, packet.OrderFilter);

            var builder = new StringBuilder();
            foreach (BazaarItemLink link in candidates
                         .Skip(Math.Max(0, packet.Index) * 50)
                         .Take(50))
            {
                cancellationSafeAppend(builder, link, now);
            }

            return $"rc_blist {packet.Index} {builder} ";
        }

        private static bool IsUsableLink(BazaarItemLink link, DateTime now)
        {
            return link?.BazaarItem != null &&
                   link.Item?.Item != null &&
                   link.Item.Amount > 0 &&
                   link.BazaarItem.DateStart.AddHours(link.BazaarItem.Duration) > now;
        }

        private static HashSet<short> ParseRequestedVNums(string filter)
        {
            var values = new HashSet<short>();
            if (string.IsNullOrWhiteSpace(filter) || filter.Trim() == "0")
            {
                return values;
            }

            foreach (string token in filter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (short.TryParse(token, out short vnum))
                {
                    values.Add(vnum);
                }
            }

            return values;
        }

        private static bool MatchesFilters(BazaarItemLink link, CBListPacket packet)
        {
            ItemInstance instance = link.Item;
            var item = instance.Item;

            switch (packet.TypeFilter)
            {
                case BazaarListType.Default:
                    return true;

                case BazaarListType.Weapon:
                    return item.Type == InventoryType.Equipment &&
                           item.ItemType == ItemType.Weapon &&
                           MatchesClass(item.Class, packet.SubTypeFilter) &&
                           MatchesLevel(item.LevelMinimum, item.IsHeroic, packet.LevelFilter) &&
                           MatchesRare(instance, packet.RareFilter) &&
                           MatchesUpgrade(instance, packet.UpgradeFilter);

                case BazaarListType.Armor:
                    return item.Type == InventoryType.Equipment &&
                           item.ItemType == ItemType.Armor &&
                           MatchesClass(item.Class, packet.SubTypeFilter) &&
                           MatchesLevel(item.LevelMinimum, item.IsHeroic, packet.LevelFilter) &&
                           MatchesRare(instance, packet.RareFilter) &&
                           MatchesUpgrade(instance, packet.UpgradeFilter);

                case BazaarListType.Equipment:
                    return item.Type == InventoryType.Equipment &&
                           item.ItemType == ItemType.Fashion &&
                           MatchesFashionSlot(item.EquipmentSlot, packet.SubTypeFilter) &&
                           MatchesLevel(item.LevelMinimum, item.IsHeroic, packet.LevelFilter);

                case BazaarListType.Jewelery:
                    return item.Type == InventoryType.Equipment &&
                           item.ItemType == ItemType.Jewelery &&
                           MatchesJewellerySlot(item.EquipmentSlot, packet.SubTypeFilter) &&
                           MatchesLevel(item.LevelMinimum, item.IsHeroic, packet.LevelFilter);

                case BazaarListType.Specialist:
                    return item.Type == InventoryType.Equipment &&
                           item.ItemType == ItemType.Box &&
                           item.ItemSubType == 2 &&
                           MatchesHolder(instance.HoldingVNum, packet.SubTypeFilter) &&
                           MatchesSpLevel(instance.SpLevel, packet.LevelFilter) &&
                           MatchesUpgrade(instance, packet.UpgradeFilter);

                case BazaarListType.Pet:
                    return item.Type == InventoryType.Equipment &&
                           item.ItemType == ItemType.Box &&
                           item.ItemSubType == 0 &&
                           MatchesHolder(instance.HoldingVNum, packet.SubTypeFilter) &&
                           MatchesSpLevel(instance.SpLevel, packet.LevelFilter);

                case BazaarListType.Npc:
                    return item.Type == InventoryType.Equipment &&
                           ((item.ItemType == ItemType.Box && item.ItemSubType == 1) || instance.ItemVNum == 4801) &&
                           MatchesSpLevel(instance.SpLevel, packet.LevelFilter);

                case BazaarListType.Shell:
                    return item.Type == InventoryType.Equipment &&
                           item.ItemType == ItemType.Shell &&
                           (packet.SubTypeFilter == 0 || packet.SubTypeFilter == item.ItemSubType + 1) &&
                           MatchesRare(instance, packet.RareFilter) &&
                           MatchesSpLevel(instance.SpLevel, packet.LevelFilter);

                case BazaarListType.Main:
                    return item.Type == InventoryType.Main && MatchesMainSubtype(item.ItemType, packet.SubTypeFilter);

                case BazaarListType.Usable:
                    return item.Type == InventoryType.Etc && MatchesUsableSubtype(item.ItemType, packet.SubTypeFilter);

                case BazaarListType.Other:
                    return item.Type == InventoryType.Equipment && item.ItemType == ItemType.Box && !item.IsHolder;

                case BazaarListType.Vehicle:
                    return item.ItemType == ItemType.Box &&
                           item.ItemSubType == 4 &&
                           MatchesHolder(instance.HoldingVNum, packet.SubTypeFilter);

                default:
                    return true;
            }
        }

        private static bool MatchesClass(byte itemClass, byte filter) =>
            filter == 0 || ((itemClass + 1 >> filter) & 1) == 1;

        private static bool MatchesLevel(byte level, bool heroic, byte filter)
        {
            if (filter == 0)
            {
                return true;
            }

            if (filter == 11)
            {
                return heroic;
            }

            int minimum = filter * 10 - 9;
            int maximumExclusive = filter * 10 + 1;
            return level >= minimum && level < maximumExclusive;
        }

        private static bool MatchesSpLevel(byte level, byte filter)
        {
            if (filter == 0)
            {
                return true;
            }

            int minimum = filter * 10 - 9;
            int maximumExclusive = filter * 10 + 1;
            return level >= minimum && level < maximumExclusive;
        }

        private static bool MatchesRare(ItemInstance item, byte filter) =>
            filter == 0 || filter == item.Rare + 1;

        private static bool MatchesUpgrade(ItemInstance item, byte filter) =>
            filter == 0 || filter == item.Upgrade + 1;

        private static bool MatchesHolder(short holdingVNum, byte filter) =>
            filter == 0 || filter == 1 && holdingVNum == 0 || filter == 2 && holdingVNum != 0;

        private static bool MatchesFashionSlot(EquipmentType slot, byte filter)
        {
            return filter == 0 ||
                   filter == 1 && slot == EquipmentType.Hat ||
                   filter == 2 && slot == EquipmentType.Mask ||
                   filter == 3 && slot == EquipmentType.Gloves ||
                   filter == 4 && slot == EquipmentType.Boots ||
                   filter == 5 && slot == EquipmentType.CostumeSuit ||
                   filter == 6 && slot == EquipmentType.CostumeHat ||
                   filter == 7 && slot == EquipmentType.WeaponSkin;
        }

        private static bool MatchesJewellerySlot(EquipmentType slot, byte filter)
        {
            return filter == 0 ||
                   filter == 1 && slot == EquipmentType.Necklace ||
                   filter == 2 && slot == EquipmentType.Ring ||
                   filter == 3 && slot == EquipmentType.Bracelet ||
                   filter == 4 && slot == EquipmentType.Fairy ||
                   filter == 5 && slot == EquipmentType.Amulet;
        }

        private static bool MatchesMainSubtype(ItemType type, byte filter)
        {
            return filter == 0 ||
                   filter == 1 && type == ItemType.Main ||
                   filter == 2 && type == ItemType.Upgrade ||
                   filter == 3 && type == ItemType.Production ||
                   filter == 4 && type == ItemType.Special ||
                   filter == 5 && type == ItemType.Potion ||
                   filter == 6 && type == ItemType.Event;
        }

        private static bool MatchesUsableSubtype(ItemType type, byte filter)
        {
            return filter == 0 ||
                   filter == 1 && type == ItemType.Food ||
                   filter == 2 && type == ItemType.Snack ||
                   filter == 3 && type == ItemType.Magical ||
                   filter == 4 && type == ItemType.Part ||
                   filter == 5 && type == ItemType.Teacher ||
                   filter == 6 && type == ItemType.Sell;
        }

        private static List<BazaarItemLink> ApplyOrder(List<BazaarItemLink> items, byte order)
        {
            switch (order)
            {
                case 0:
                    return items.OrderBy(link => link.Item.Item.Name)
                        .ThenBy(link => link.BazaarItem.Price)
                        .ToList();
                case 1:
                    return items.OrderBy(link => link.Item.Item.Name)
                        .ThenByDescending(link => link.BazaarItem.Price)
                        .ToList();
                case 2:
                    return items.OrderBy(link => link.Item.Item.Name)
                        .ThenBy(link => link.BazaarItem.Amount)
                        .ToList();
                case 3:
                    return items.OrderBy(link => link.Item.Item.Name)
                        .ThenByDescending(link => link.BazaarItem.Amount)
                        .ToList();
                default:
                    return items.OrderBy(link => link.Item.Item.Name).ToList();
            }
        }

        private static void cancellationSafeAppend(StringBuilder builder, BazaarItemLink link, DateTime now)
        {
            try
            {
                long time = (long)(link.BazaarItem.DateStart.AddHours(link.BazaarItem.Duration) - now).TotalMinutes;
                string info = GenerateInfo(link.Item);
                string owner = link.Owner ?? string.Empty;

                builder.Append(link.BazaarItem.BazaarItemId).Append('|')
                    .Append(link.BazaarItem.SellerId).Append('|')
                    .Append(owner).Append('|')
                    .Append(link.Item.ItemVNum).Append('|')
                    .Append(link.Item.Amount).Append('|')
                    .Append(link.BazaarItem.IsPackage ? 1 : 0).Append('|')
                    .Append(link.BazaarItem.Price).Append('|')
                    .Append(time).Append("|2|0|")
                    .Append(link.Item.Rare).Append('|')
                    .Append(link.Item.Upgrade).Append("|0|0|")
                    .Append(info).Append(' ');
            }
            catch
            {
                // Skip only the malformed row; the remaining listings must still be searchable.
            }
        }

        private static string GenerateInfo(ItemInstance item)
        {
            if (item?.Item == null || item.Item.Type != InventoryType.Equipment)
            {
                return string.Empty;
            }

            if (item.Item.EquipmentSlot != EquipmentType.Sp)
            {
                return BazaarEquipmentInfoGenerator.Generate(item);
            }

            try
            {
                ItemInstance partnerItem = null;
                string raw = item.Item.SpType == 0 && item.Item.ItemSubType == 4
                    ? item.GeneratePslInfo(partnerItem)
                    : item.GenerateSlInfo();

                return string.IsNullOrWhiteSpace(raw)
                    ? string.Empty
                    : raw.Replace(' ', '^')
                        .Replace("slinfo^", string.Empty)
                        .Replace("e_info^", string.Empty);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
