using Frostvein.DAL;
using Frostvein.Domain;
using Frostvein.GameObject.Networking;
using Frostvein.Master.Library.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Load
{
    public static class PluginLoadMapsAndContent
    {
        public static void Load()
        {
            try
            {
                var i = 0;
                var monstercount = 0;

                var monsters = DAOFactory.MapMonsterDAO.LoadAll().GroupBy(s => s.MapId)
                    .ToDictionary(s => s.Key, s => s.ToArray());
                var npcs = DAOFactory.MapNpcDAO.LoadAll().GroupBy(s => s.MapId)
                    .ToDictionary(s => s.Key, s => s.ToArray());
                var portals = DAOFactory.PortalDAO.LoadAll().GroupBy(s => s.SourceMapId)
                    .ToDictionary(s => s.Key, s => s.ToArray());
                var mapTypes = DAOFactory.MapTypeMapDAO.LoadAll().ToArray();
                var mapTypeMap = DAOFactory.MapTypeDAO.LoadAll().ToArray();
                var respawns = DAOFactory.RespawnMapTypeDAO.LoadAll();

                foreach (var map in DAOFactory.MapDAO.LoadAll().ToArray())
                {
                    var guid = Guid.NewGuid();
                    var mapinfo = new Map(map.MapId, map.GridMapId, map.Data)
                    {
                        Music = map.Music,
                        Name = map.Name,
                        ShopAllowed = map.ShopAllowed,
                        XpRate = map.XpRate
                    };
                    var newMap = new MapInstance(mapinfo, guid, map.ShopAllowed,
                        MapInstanceType.BaseMapInstance, new InstanceBag(), true);
                    ServerManager._mapinstances.TryAdd(guid, newMap);

                    if (portals.TryGetValue(map.MapId, out var port))
                    {
                        newMap.LoadPortals(port);
                    }

                    if (npcs.TryGetValue(map.MapId, out var np))
                    {
                        newMap.LoadNpcs(np);
                    }

                    if (monsters.TryGetValue(map.MapId, out var monst))
                    {
                        newMap.LoadMonsters(monst);
                    }

                    foreach (var mapNpc in newMap.Npcs)
                    {
                        mapNpc.MapInstance = newMap;
                        newMap.AddNPC(mapNpc);
                    }

                    foreach (var mapMonster in newMap.Monsters)
                    {
                        mapMonster.MapInstance = newMap;
                        newMap.AddMonster(mapMonster);
                    }


                    monstercount += newMap.Monsters.Count;
                    ServerManager.Maps.Add(mapinfo);
                    i++;
                }

                LoggerService.LogServer.Logger.UpdateLoadOutput($"{i} Maps - Status: Successful", LogType.LOAD);
                LoggerService.LogServer.Logger.UpdateLoadOutput($"{monstercount} Map Monster - Status: Successful", LogType.LOAD);
            }
            catch (Exception e)
            {
                LoggerService.LogServer.Logger.LogAsync($"[Error] {e}", LogType.ERROR);
            }
        }
    }
}
