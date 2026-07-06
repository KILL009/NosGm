using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.GameObject.Networking;
using Frostvein.LoggerService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadFishes
    {
        public static void Load()
        {
            ServerManager.Instance.FishingSpots = new ConcurrentDictionary<FishingPositionDto, List<FishingInformationsDto>>();
            var spots = DAOFactory.FishingPositionDao.LoadAll();
            var infos = DAOFactory.FishingInformationDao.LoadAll();

            foreach (var spot in spots)
            {
                var spotInfo = new List<FishingInformationsDto>();
                spotInfo.AddRange(infos.Where(s => s.MapId1 == spot.MapId || s.MapId2 == spot.MapId || s.MapId3 == spot.MapId));
                ServerManager.Instance.FishingSpots.TryAdd(spot, spotInfo);
            }

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.FishingSpots.Count} Fishing Spots - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
