using FluentValidation;
using MediatR;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Modules.Bazaar.Queries;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NosTale.Module.Bazaar.Queries.GetRcbList
{
    internal class GetRcbListQueryHandler : IRequestHandler<GetRcbListQuery, string>
    {
        private readonly GetRcbListQueryValidator _requestValidator = new();
        private readonly BazaarManager _bazaarManager;

        public GetRcbListQueryHandler(BazaarManager bazaarManager)
        {
            _bazaarManager = bazaarManager;
        }

        public async Task<string> Handle(GetRcbListQuery request, CancellationToken cancellation)
        {
            await _requestValidator.ValidateAndThrowAsync(request);

            if (request.Packet?.ItemVNumFilter == null)
            {
                return string.Empty;
            }
            var itembazar = string.Empty;

            var itemssearch = request.Packet.ItemVNumFilter == "0" ? new List<string>() : request.Packet.ItemVNumFilter.Split(' ').ToList();
            var bzlist = new List<BazaarItemLink>();
            try
            {
                foreach (var bz in _bazaarManager.BazaarItemLinks.Values)
                {
                    if (bz?.Item == null)
                    {
                        continue;
                    }

                    switch (request.Packet.TypeFilter)
                    {
                        case BazaarListType.Weapon:
                            if (bz.Item.Item.Type == InventoryType.Equipment &&
                                bz.Item.Item.ItemType == ItemType.Weapon &&
                                (request.Packet.SubTypeFilter == 0 || (bz.Item.Item.Class + 1 >> request.Packet.SubTypeFilter & 1) == 1) &&
                                (request.Packet.LevelFilter == 0 || request.Packet.LevelFilter == 11 && bz.Item.Item.IsHeroic ||
                                bz.Item.Item.LevelMinimum < request.Packet.LevelFilter * 10 + 1 &&
                                bz.Item.Item.LevelMinimum >= request.Packet.LevelFilter * 10 - 9) &&
                                (request.Packet.RareFilter == 0 || request.Packet.RareFilter == bz.Item.Rare + 1) &&
                                (request.Packet.UpgradeFilter == 0 || request.Packet.UpgradeFilter == bz.Item.Upgrade + 1))
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        case BazaarListType.Armor:
                            if (bz.Item.Item.Type == InventoryType.Equipment &&
                                bz.Item.Item.ItemType == ItemType.Armor &&
                                (request.Packet.SubTypeFilter == 0 ||
                                (bz.Item.Item.Class + 1 >> request.Packet.SubTypeFilter & 1) == 1) && (request.Packet.LevelFilter == 0 ||
                                request.Packet.LevelFilter == 11 && bz.Item.Item.IsHeroic ||
                                bz.Item.Item.LevelMinimum < request.Packet.LevelFilter * 10 + 1 && bz.Item.Item.LevelMinimum >= request.Packet.LevelFilter * 10 - 9) &&
                                (request.Packet.RareFilter == 0 || request.Packet.RareFilter == bz.Item.Rare + 1) &&
                                (request.Packet.UpgradeFilter == 0 || request.Packet.UpgradeFilter == bz.Item.Upgrade + 1))
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        case BazaarListType.Equipment:
                            if (bz.Item.Item.Type == InventoryType.Equipment &&
                                bz.Item.Item.ItemType == ItemType.Fashion &&
                                (request.Packet.SubTypeFilter == 0 ||
                                request.Packet.SubTypeFilter == 1 && bz.Item.Item.EquipmentSlot == EquipmentType.Hat ||
                                request.Packet.SubTypeFilter == 2 && bz.Item.Item.EquipmentSlot == EquipmentType.Mask ||
                                request.Packet.SubTypeFilter == 3 && bz.Item.Item.EquipmentSlot == EquipmentType.Gloves ||
                                request.Packet.SubTypeFilter == 4 && bz.Item.Item.EquipmentSlot == EquipmentType.Boots ||
                                request.Packet.SubTypeFilter == 5 && bz.Item.Item.EquipmentSlot == EquipmentType.CostumeSuit ||
                                request.Packet.SubTypeFilter == 6 && bz.Item.Item.EquipmentSlot == EquipmentType.CostumeHat ||
                                request.Packet.SubTypeFilter == 7 && bz.Item.Item.EquipmentSlot == EquipmentType.WeaponSkin) &&
                                (request.Packet.LevelFilter == 0 || request.Packet.LevelFilter == 11 && bz.Item.Item.IsHeroic ||
                                bz.Item.Item.LevelMinimum < request.Packet.LevelFilter * 10 + 1 &&
                                bz.Item.Item.LevelMinimum >= request.Packet.LevelFilter * 10 - 9))
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        case BazaarListType.Jewelery:
                            if (bz.Item.Item.Type == InventoryType.Equipment &&
                                bz.Item.Item.ItemType == ItemType.Jewelery &&
                                (request.Packet.SubTypeFilter == 0 ||
                                request.Packet.SubTypeFilter == 1 && bz.Item.Item.EquipmentSlot == EquipmentType.Necklace ||
                                request.Packet.SubTypeFilter == 2 && bz.Item.Item.EquipmentSlot == EquipmentType.Ring ||
                                request.Packet.SubTypeFilter == 3 && bz.Item.Item.EquipmentSlot == EquipmentType.Bracelet ||
                                request.Packet.SubTypeFilter == 4 && (bz.Item.Item.EquipmentSlot == EquipmentType.Fairy ||
                                request.Packet.SubTypeFilter == 5 && bz.Item.Item.EquipmentSlot == EquipmentType.Amulet ||
                                bz.Item.Item.ItemType == ItemType.Box && bz.Item.Item.ItemSubType == 5)) &&
                                (request.Packet.LevelFilter == 0 || request.Packet.LevelFilter == 11 && bz.Item.Item.IsHeroic ||
                                bz.Item.Item.LevelMinimum < request.Packet.LevelFilter * 10 + 1 &&
                                bz.Item.Item.LevelMinimum >= request.Packet.LevelFilter * 10 - 9))
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        case BazaarListType.Specialist:
                            if (bz.Item.Item.Type == InventoryType.Equipment && bz.Item.Item.ItemType == ItemType.Box && bz.Item.Item.ItemSubType == 2)
                            {
                                var morph = ServerManager.GetItem(bz.Item.HoldingVNum)?.Morph;
                                if (request.Packet.SubTypeFilter == 0 && (request.Packet.LevelFilter == 0 || bz.Item.SpLevel < request.Packet.LevelFilter * 10 + 1 && bz.Item.SpLevel >= request.Packet.LevelFilter * 10 - 9) && (request.Packet.UpgradeFilter == 0 || request.Packet.UpgradeFilter == bz.Item.Upgrade + 1) && (request.Packet.SubTypeFilter == 0 || request.Packet.SubTypeFilter == 1 && bz.Item.HoldingVNum == 0 || request.Packet.SubTypeFilter == 2 && bz.Item.HoldingVNum != 0))
                                {
                                    bzlist.Add(bz);
                                }
                                else if (bz.Item.HoldingVNum == 0 && request.Packet.SubTypeFilter == 1 && (request.Packet.LevelFilter == 0 || bz.Item.SpLevel < request.Packet.LevelFilter * 10 + 1 && bz.Item.SpLevel >= request.Packet.LevelFilter * 10 - 9) && (request.Packet.UpgradeFilter == 0 || request.Packet.UpgradeFilter == bz.Item.Upgrade + 1) && (request.Packet.SubTypeFilter == 0 || request.Packet.SubTypeFilter == 1 && bz.Item.HoldingVNum == 0 || request.Packet.SubTypeFilter == 2 && bz.Item.HoldingVNum != 0))
                                {
                                    bzlist.Add(bz);
                                }
                                else if (request.Packet.SubTypeFilter == 2 && morph == 10
                                         || request.Packet.SubTypeFilter == 3 && morph == 11
                                         || request.Packet.SubTypeFilter == 4 && morph == 2
                                         || request.Packet.SubTypeFilter == 5 && morph == 3
                                         || request.Packet.SubTypeFilter == 6 && morph == 13
                                         || request.Packet.SubTypeFilter == 7 && morph == 5
                                         || request.Packet.SubTypeFilter == 8 && morph == 12
                                         || request.Packet.SubTypeFilter == 9 && morph == 4
                                         || request.Packet.SubTypeFilter == 10 && morph == 7
                                         || request.Packet.SubTypeFilter == 11 && morph == 15
                                         || request.Packet.SubTypeFilter == 12 && morph == 6
                                         || request.Packet.SubTypeFilter == 13 && morph == 14
                                         || request.Packet.SubTypeFilter == 14 && morph == 9
                                         || request.Packet.SubTypeFilter == 15 && morph == 8
                                         || request.Packet.SubTypeFilter == 16 && morph == 1
                                         || request.Packet.SubTypeFilter == 17 && morph == 16
                                         || request.Packet.SubTypeFilter == 18 && morph == 17
                                         || request.Packet.SubTypeFilter == 19 && morph == 18
                                         || request.Packet.SubTypeFilter == 20 && morph == 19
                                         || request.Packet.SubTypeFilter == 21 && morph == 20
                                         || request.Packet.SubTypeFilter == 22 && morph == 21
                                         || request.Packet.SubTypeFilter == 23 && morph == 22
                                         || request.Packet.SubTypeFilter == 24 && morph == 23
                                         || request.Packet.SubTypeFilter == 25 && morph == 24
                                         || request.Packet.SubTypeFilter == 26 && morph == 25
                                         || request.Packet.SubTypeFilter == 27 && morph == 26
                                         || request.Packet.SubTypeFilter == 28 && morph == 27
                                         || request.Packet.SubTypeFilter == 29 && morph == 28
                                         || request.Packet.SubTypeFilter == 30 && morph == 29
                                         || request.Packet.SubTypeFilter == 32 && morph == 31
                                         || request.Packet.SubTypeFilter == 33 && morph == 32
                                         || request.Packet.SubTypeFilter == 34 && morph == 33
                                         || request.Packet.SubTypeFilter == 35 && morph == 34)
                                {
                                    if ((request.Packet.LevelFilter == 0 || bz.Item.SpLevel < request.Packet.LevelFilter * 10 + 1 && bz.Item.SpLevel >= request.Packet.LevelFilter * 10 - 9) && (request.Packet.UpgradeFilter == 0 || request.Packet.UpgradeFilter == bz.Item.Upgrade + 1) && (request.Packet.SubTypeFilter == 0 || request.Packet.SubTypeFilter == 1 && bz.Item.HoldingVNum == 0 || request.Packet.SubTypeFilter >= 2 && bz.Item.HoldingVNum != 0))
                                    {
                                        bzlist.Add(bz);
                                    }
                                }
                            }
                            break;

                        case BazaarListType.Pet:
                            if (bz.Item.Item.Type == InventoryType.Equipment &&
                                bz.Item.Item.ItemType == ItemType.Box &&
                                bz.Item.Item.ItemSubType == 0 &&
                                (request.Packet.LevelFilter == 0 ||
                                bz.Item.SpLevel < request.Packet.LevelFilter * 10 + 1 &&
                                bz.Item.SpLevel >= request.Packet.LevelFilter * 10 - 9) &&
                                (request.Packet.SubTypeFilter == 0 ||
                                request.Packet.SubTypeFilter == 1 && bz.Item.HoldingVNum == 0 ||
                                request.Packet.SubTypeFilter == 2 && bz.Item.HoldingVNum != 0))
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        case BazaarListType.Npc:
                            if (bz.Item.Item.Type == InventoryType.Equipment &&
                                bz.Item.Item.ItemType == ItemType.Box && bz.Item.Item.ItemSubType == 1 || bz.Item.ItemVNum == 4801 &&
                                (request.Packet.LevelFilter == 0 || bz.Item.SpLevel < request.Packet.LevelFilter * 10 + 1 && bz.Item.SpLevel >= request.Packet.LevelFilter * 10 - 9) &&
                                (request.Packet.SubTypeFilter == 0 ||
                                request.Packet.SubTypeFilter == 1 && bz.Item.HoldingVNum == 0 && bz.Item.ItemVNum == 286 ||
                                request.Packet.SubTypeFilter == 2 && bz.Item.HoldingVNum == (bz.Item.ItemVNum == 286 ? 1 : 0) && bz.Item.ItemVNum != 4801 ||
                                request.Packet.SubTypeFilter == 3 && bz.Item.HoldingVNum == 0 && bz.Item.ItemVNum == 4801 ||
                                request.Packet.SubTypeFilter == 4 && bz.Item.HoldingVNum != 0 && bz.Item.ItemVNum == 4801
                                && PartnerHelper.GetAttackType(bz.Item.Item.VNum) == AttackType.Melee ||
                                request.Packet.SubTypeFilter == 5 && bz.Item.HoldingVNum != 0 && bz.Item.ItemVNum == 4801
                                && PartnerHelper.GetAttackType(bz.Item.Item.VNum) == AttackType.Range ||
                                request.Packet.SubTypeFilter == 6 && bz.Item.HoldingVNum != 0 && bz.Item.ItemVNum == 4801
                                && PartnerHelper.GetAttackType(bz.Item.Item.VNum) == AttackType.Magical))
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        case BazaarListType.Shell:
                            if (bz.Item.Item.Type == InventoryType.Equipment &&
                                bz.Item.Item.ItemType == ItemType.Shell &&
                                (request.Packet.SubTypeFilter == 0 ||
                                request.Packet.SubTypeFilter == 1 && bz.Item.Item.ItemSubType == 0 ||
                                request.Packet.SubTypeFilter == 2 && bz.Item.Item.ItemSubType == 1 ||
                                bz.Item.Item.ItemSubType == bz.Item.Item.ItemSubType + 1) &&
                                (request.Packet.RareFilter == 0 || request.Packet.RareFilter == bz.Item.Rare + 1) &&
                                (request.Packet.LevelFilter == 0 ||
                                bz.Item.SpLevel < request.Packet.LevelFilter * 10 + 1 &&
                                bz.Item.SpLevel >= request.Packet.LevelFilter * 10 - 9))
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        case BazaarListType.Main:
                            if (bz.Item.Item.Type == InventoryType.Main &&
                                (request.Packet.SubTypeFilter == 0 ||
                                request.Packet.SubTypeFilter == 1 && bz.Item.Item.ItemType == ItemType.Main ||
                                request.Packet.SubTypeFilter == 2 && bz.Item.Item.ItemType == ItemType.Upgrade ||
                                request.Packet.SubTypeFilter == 3 && bz.Item.Item.ItemType == ItemType.Production ||
                                request.Packet.SubTypeFilter == 4 && bz.Item.Item.ItemType == ItemType.Special ||
                                request.Packet.SubTypeFilter == 5 && bz.Item.Item.ItemType == ItemType.Potion ||
                                request.Packet.SubTypeFilter == 6 && bz.Item.Item.ItemType == ItemType.Event))
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        case BazaarListType.Usable:
                            if (bz.Item.Item.Type == InventoryType.Etc &&
                                (request.Packet.SubTypeFilter == 0 ||
                                request.Packet.SubTypeFilter == 1 && bz.Item.Item.ItemType == ItemType.Food ||
                                request.Packet.SubTypeFilter == 2 && bz.Item.Item.ItemType == ItemType.Snack ||
                                request.Packet.SubTypeFilter == 3 && bz.Item.Item.ItemType == ItemType.Magical ||
                                request.Packet.SubTypeFilter == 4 && bz.Item.Item.ItemType == ItemType.Part ||
                                request.Packet.SubTypeFilter == 5 && bz.Item.Item.ItemType == ItemType.Teacher ||
                                request.Packet.SubTypeFilter == 6 && bz.Item.Item.ItemType == ItemType.Sell))
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        case BazaarListType.Other:
                            if (bz.Item.Item.Type == InventoryType.Equipment &&
                                bz.Item.Item.ItemType == ItemType.Box && !bz.Item.Item.IsHolder)
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        case BazaarListType.Vehicle:
                            if (bz.Item.Item.ItemType == ItemType.Box &&
                                bz.Item.Item.ItemSubType == 4 &&
                                (request.Packet.SubTypeFilter == 0 ||
                                request.Packet.SubTypeFilter == 1 && bz.Item.HoldingVNum == 0 ||
                                request.Packet.SubTypeFilter == 2 && bz.Item.HoldingVNum != 0))
                            {
                                bzlist.Add(bz);
                            }
                            break;

                        default:
                            bzlist.Add(bz);
                            break;
                    }
                }
                var bzlistsearched = bzlist.Where(s => itemssearch.Contains(s.Item.ItemVNum.ToString())).ToList();

                //price up price down quantity up quantity down
                var definitivelist = itemssearch.Count > 0 ? bzlistsearched : bzlist;

                definitivelist = request.Packet.OrderFilter switch
                {
                    0 => definitivelist.OrderBy(s => s.Item.Item.Name).ThenBy(s => s.BazaarItem.Price).ToList(),
                    1 => definitivelist.OrderBy(s => s.Item.Item.Name).ThenByDescending(s => s.BazaarItem.Price).ToList(),
                    2 => definitivelist.OrderBy(s => s.Item.Item.Name).ThenBy(s => s.BazaarItem.Amount).ToList(),
                    3 => definitivelist.OrderBy(s => s.Item.Item.Name).ThenByDescending(s => s.BazaarItem.Amount).ToList(),
                    _ => definitivelist.OrderBy(s => s.Item.Item.Name).ToList(),
                };
                foreach (var bzlink in definitivelist.Where(s => (s.BazaarItem.DateStart.AddHours(s.BazaarItem.Duration) - DateTime.Now).TotalMinutes > 0 && s.Item.Amount > 0).Skip(request.Packet.Index * 50).Take(50))
                {
                    var time = (long)(bzlink.BazaarItem.DateStart.AddHours(bzlink.BazaarItem.Duration) - DateTime.Now).TotalMinutes;
                    var info = string.Empty;
                    if (bzlink.Item.Item.Type == InventoryType.Equipment)
                    {
                        ItemInstance itemInstance = null;
                        info = (bzlink.Item.Item.EquipmentSlot != EquipmentType.Sp ?
                            bzlink.Item?.GenerateEInfo() : bzlink.Item.Item.SpType == 0 && bzlink.Item.Item.ItemSubType == 4 ?
                            bzlink.Item?.GeneratePslInfo(itemInstance) : bzlink.Item?.GenerateSlInfo()).Replace(' ', '^').Replace("slinfo^", "").Replace("e_info^", "");
                    }

                    // {TotalRune}|{Idk}|
                    itembazar += $"{bzlink.BazaarItem.BazaarItemId}|{bzlink.BazaarItem.SellerId}|{bzlink.Owner}|{bzlink.Item.Item.VNum}|{bzlink.Item.Amount}|{(bzlink.BazaarItem.IsPackage ? 1 : 0)}|{bzlink.BazaarItem.Price}|{time}|2|0|{bzlink.Item.Rare}|{bzlink.Item.Upgrade}|0|0|{info} ";
                }

                return $"rc_blist {request.Packet.Index} {itembazar} ";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return string.Empty;
            }
        }
    }
}
