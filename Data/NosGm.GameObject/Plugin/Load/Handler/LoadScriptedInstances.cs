using NosGm.DAL;
using NosGm.Domain;
using NosGm.GameObject.ItemThread;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Data;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Load
{
    public static class PluginLoadScriptedInstances
    {
        public static void Load()
        {
            ServerManager.Instance.Raids = new ConcurrentBag<ScriptedInstance>();
            ServerManager.Instance.TimeSpaces = new ConcurrentBag<ScriptedInstance>();
            foreach (var map in ServerManager._mapinstances)
            {
                if (map.Value.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    map.Value.ScriptedInstances.Clear();
                    map.Value.Portals.Clear();
                    foreach (var si in DAOFactory.ScriptedInstanceDAO.LoadByMap(map.Value.Map.MapId)
                                                 .ToList())
                    {
                        var siObj = new ScriptedInstance(si);
                        switch (siObj.Type)
                        {
                            case ScriptedInstanceType.SkyTower:
                            case ScriptedInstanceType.TimeSpace:
                            case ScriptedInstanceType.QuestTimeSpace:
                                siObj.LoadGlobals();
                                if (siObj.Script != null)
                                {
                                    ServerManager.Instance.TimeSpaces.Add(siObj);
                                }

                                map.Value.ScriptedInstances.Add(siObj);
                                break;

                            case ScriptedInstanceType.Raid:
                                siObj.LoadGlobals();
                                if (siObj.Id != 23 && siObj.Id != 24)
                                {
                                    if (siObj.Script != null)
                                    {
                                        ServerManager.Instance.Raids.Add(siObj);
                                    }

                                    var port = new Portal
                                    {
                                        Type = (byte)PortalType.Raid,
                                        SourceMapId = siObj.MapId,
                                        SourceX = siObj.PositionX,
                                        SourceY = siObj.PositionY
                                    };
                                    map.Value.Portals.Add(port);
                                }
                                else
                                {
                                    if (siObj.Script != null)
                                    {

                                    }
                                }
                                break;
                        }
                    }

                    map.Value.LoadPortals();
                    map.Value.MapClear();
                }
            }
            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.Raids.Count} Raids - Status: Successful", LogType.LOAD);
            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.TimeSpaces.Count} TimeSpaces - Status: Successful", LogType.LOAD);
        }
    }
}
