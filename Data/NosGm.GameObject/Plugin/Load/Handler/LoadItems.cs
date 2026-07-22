using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Load
{
    public static class PluginLoadItems
    {
        public static void Load()
        {
            var items = DAOFactory.ItemDAO.LoadAll();
            var bcards = DAOFactory.BCardDAO.LoadAll().Where(s => s.ItemVNum.HasValue)
                .GroupBy(s => s.ItemVNum).ToDictionary(s => s.Key, s => s.ToArray());
            //var raidbox = DAOFactory.RaidboxDAO.LoadAll().GroupBy(s => s.OriginalItemVNum)
            //    .ToDictionary(s => s.Key, s => s.ToArray());
            var item = new Dictionary<short, Item>();
            foreach (var itemDto in items)
            {
                Item newItem;
                switch (itemDto.ItemType)
                {
                    case ItemType.Box:
                        newItem = new BoxItem(itemDto);
                        newItem.PluginType = ItemPluginType.Box;
                        break;

                    case ItemType.Fashion:
                    case ItemType.Jewelery:
                    case ItemType.Specialist:
                    case ItemType.Weapon:
                    case ItemType.Armor:
                        newItem = new WearableItem(itemDto);
                        newItem.PluginType = ItemPluginType.Wearable;
                        break;

                    case ItemType.Food:
                        newItem = new FoodItem(itemDto);
                        newItem.PluginType = ItemPluginType.Food;
                        break;

                    case ItemType.Special:
                        newItem = new SpecialItem(itemDto);
                        newItem.PluginType = ItemPluginType.Special;
                        break;

                    case ItemType.Magical:
                    case ItemType.Shell:
                    case ItemType.Event:
                        newItem = new MagicalItem(itemDto);
                        newItem.PluginType = ItemPluginType.Magical;
                        break;

                    case ItemType.Potion:
                        newItem = new PotionItem(itemDto);
                        newItem.PluginType = ItemPluginType.Potion;
                        break;

                    case ItemType.Production:
                        newItem = new ProduceItem(itemDto);
                        newItem.PluginType = ItemPluginType.Produce;
                        break;

                    case ItemType.Snack:
                        newItem = new SnackItem(itemDto);
                        newItem.PluginType = ItemPluginType.Snack;
                        break;

                    case ItemType.Teacher:
                        newItem = new TeacherItem(itemDto);
                        newItem.PluginType = ItemPluginType.Teacher;
                        break;

                    case ItemType.Upgrade:
                        newItem = new UpgradeItem(itemDto);
                        newItem.PluginType = ItemPluginType.Upgrade;
                        break;

                    case ItemType.Title:
                        newItem = new TitleItem(itemDto);
                        newItem.PluginType = ItemPluginType.Title;
                        break;

                    default:
                        newItem = new NoFunctionItem(itemDto);
                        newItem.PluginType = ItemPluginType.NoFunction;
                        break;
                }

                if (bcards.TryGetValue(newItem.VNum, out var bcardDtos))
                {
                    foreach (var b in bcardDtos)
                    {
                        newItem.BCards.Add(new BCard(b));
                    }
                }

                //if (raidbox.TryGetValue(newItem.VNum, out var rolls2))
                //{
                //    newItem.Raidbox.AddRange(rolls2);
                //}

                item[itemDto.VNum] = newItem;
            }

            ServerManager.Items.AddRange(item.Values);
            //LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Items.Count} Items - Status: Successful", LogType.LOAD);
        }
    }
}
