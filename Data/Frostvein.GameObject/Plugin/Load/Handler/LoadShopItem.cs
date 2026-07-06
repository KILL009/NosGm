using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadShopItem
    {
        public static void Load()
        {
            ServerManager.Instance._shopItems = new Dictionary<int, List<ShopItemDTO>>();
            foreach (var shopItemGrouping in DAOFactory.ShopItemDAO.LoadAll().GroupBy(s => s.ShopId))
            {
                ServerManager.Instance._shopItems[shopItemGrouping.Key] = shopItemGrouping.ToList();
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance._shopItems.Sum(i => i.Value.Count)} Shop Items - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
