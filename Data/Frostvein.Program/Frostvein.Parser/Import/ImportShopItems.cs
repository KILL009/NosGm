using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.Parser.Import
{
    public class ImportShopItems : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportShopItems(ImportConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void Import()
        {
            var existingShops = DAOFactory.ShopDAO.LoadAll().Select(x => x.MapNpcId).ToHashSet();
            var existingShopItems = DAOFactory.ShopItemDAO.LoadAll().ToList();

            var shopItems = new List<ShopItemDTO>();
            var itemCounter = 0;

            byte type = 0;
            foreach (var currentPacket in _configuration.Packets.Where(o =>
                o[0].Equals("n_inv") || o[0].Equals("shopping")))
                if (currentPacket[0].Equals("n_inv"))
                {
                    if (!existingShops.Contains(int.Parse(currentPacket[2]))) continue;

                    for (var i = 5; i < currentPacket.Length; i++)
                    {
                        var item = currentPacket[i].Split('.');
                        ShopItemDTO shopItem = null;

                        switch (item.Length)
                        {
                            case 5:
                                shopItem = new ShopItemDTO
                                {
                                    ShopId = DAOFactory.ShopDAO.LoadByNpc(short.Parse(currentPacket[2])).ShopId,
                                    Type = type,
                                    Slot = byte.Parse(item[1]),
                                    ItemVNum = short.Parse(item[2])
                                };
                                break;
                            case 6:
                                shopItem = new ShopItemDTO
                                {
                                    ShopId = DAOFactory.ShopDAO.LoadByNpc(short.Parse(currentPacket[2])).ShopId,
                                    Type = type,
                                    Slot = byte.Parse(item[1]),
                                    ItemVNum = short.Parse(item[2]),
                                    Rare = sbyte.Parse(item[3]),
                                    Upgrade = byte.Parse(item[4])
                                };
                                break;
                        }

                        if (shopItem == null ||
                            shopItems.Any(x => x.ItemVNum == shopItem.ItemVNum && x.ShopId == shopItem.ShopId) ||
                            existingShopItems.Any(x => x.ShopId == shopItem.ShopId && x.ItemVNum == shopItem.ItemVNum))
                            continue;

                        shopItems.Add(shopItem);
                        itemCounter++;
                    }
                }
                else
                {
                    if (currentPacket.Length > 3) type = byte.Parse(currentPacket[1]);
                }

            DAOFactory.ShopItemDAO.Insert(shopItems);
            Logger.Log.Info($"{itemCounter} Shop ITEMS parsed");
        }
    }
}