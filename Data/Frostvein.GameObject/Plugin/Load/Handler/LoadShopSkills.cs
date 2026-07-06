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
    public static class PluginLoadShopSkills
    {
        public static void Load()
        {
            ServerManager.Instance._shopSkills = new Dictionary<int, List<ShopSkillDTO>>();
            foreach (var shopSkillGrouping in DAOFactory.ShopSkillDAO.LoadAll().GroupBy(s => s.ShopId))
            {
                ServerManager.Instance._shopSkills[shopSkillGrouping.Key] = shopSkillGrouping.ToList();
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance._shopSkills.Sum(i => i.Value.Count)} Shop Skills - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
