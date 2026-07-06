using Frostvein.DAL;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadShops
    {
        public static void Load()
        {
            ServerManager.Instance._shops = new Dictionary<int, Shop>();
            foreach (var shopGrouping in DAOFactory.ShopDAO.LoadAll())
            {
                var shop = new Shop(shopGrouping);
                ServerManager.Instance._shops[shopGrouping.MapNpcId] = shop;
                shop.Initialize();
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance._shops.Count} Shops - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
