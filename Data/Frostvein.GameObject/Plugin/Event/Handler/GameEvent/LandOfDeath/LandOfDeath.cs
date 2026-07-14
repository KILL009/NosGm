using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using Frostvein.Core;
using System.Threading.Tasks;
using System.Reactive.Disposables;

namespace Frostvein.GameObject.Plugin.Event
{
    public static class LandOfDeath
    {
        private static readonly object LodLock = new object();
        private static bool _isRunning;

        public static EventType EventType => EventType.LOD;

        public static void GenerateLandOfDeath()
        {
            lock (LodLock)
            {
                if (_isRunning)
                {
                    Logger.Warn("Land of Death is already running.");
                    return;
                }

                _isRunning = true;
            }

            const int lodTime = 60;
            const int hornTime = 45;
            const int hornStayTime = 1;
            const int hornRespawnTime = 3;

            EventHelper.Instance.RunEvent(
                new EventContainer(
                    ServerManager.GetMapInstance(
                        ServerManager.GetBaseMapInstanceIdByMapId(98)),
                    EventActionType.NPCSEFFECTCHANGESTATE,
                    true));

            Task.Run(() =>
            {
                try
                {
                    var lodThread = new LandOfDeathThread();

                    lodThread.Run(
                        lodTime * 60,
                        (hornTime + 1) * 60,
                        (hornRespawnTime + hornStayTime) * 60,
                        hornStayTime * 60);
                }
                catch (Exception ex)
                {
                    Logger.Error("Land of Death execution failed.", ex);
                }
                finally
                {
                    lock (LodLock)
                    {
                        _isRunning = false;
                    }
                }
            });
        }
    }

    public class LandOfDeathThread
    {
        public bool IsOpen { get; set; } = true;

        public void Run(int lodTime, int hornTime, int hornRespawn, int hornStay)
        {
            ChangePortalEffect(855);

            const int interval = 30;
            var dhspawns = 0;

            while (lodTime > 0)
            {
                refreshLOD(lodTime);

                if (lodTime == hornTime - hornRespawn * dhspawns)
                {
                    foreach (var fam in ServerManager.Instance.FamilyList.GetAllItems())
                    {
                        if (fam?.LandOfDeath == null)
                        {
                            continue;
                        }

                        EventHelper.Instance.RunEvent(
                            new EventContainer(
                                fam.LandOfDeath,
                                EventActionType.CHANGEXPRATE,
                                2));

                        EventHelper.Instance.RunEvent(
                            new EventContainer(
                                fam.LandOfDeath,
                                EventActionType.CHANGEDROPRATE,
                                3));

                        spawnDH(fam.LandOfDeath);
                    }
                }
                else if (lodTime == hornTime - hornRespawn * dhspawns - hornStay)
                {
                    foreach (var fam in ServerManager.Instance.FamilyList.GetAllItems())
                        if (fam.LandOfDeath != null)
                        {
                            despawnDH(fam.LandOfDeath);
                        }

                    dhspawns++;
                }

                lodTime -= interval;
                Thread.Sleep(interval * 1000);
            }

            endLOD();
        }

        private void ChangePortalEffect(short effectId)
        {
            ServerManager.Instance.GetMapNpcsByVNum(453).ForEach(mapNpc => mapNpc.Effect = effectId);
        }

        private void despawnDH(MapInstance LandOfDeath)
        {
            EventHelper.Instance.RunEvent(new EventContainer(ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(98)), EventActionType.NPCSEFFECTCHANGESTATE, false));
            EventHelper.Instance.RunEvent(new EventContainer(LandOfDeath, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("HORN_DISAPEAR"), 0)));
            EventHelper.Instance.RunEvent(new EventContainer(LandOfDeath, EventActionType.UNSPAWNMONSTERS, 443));

            if (IsOpen)
            {
                IsOpen = false;

                ChangePortalEffect(0);
            }
        }

        private void endLOD()
        {
            foreach (var fam in ServerManager.Instance.FamilyList.GetAllItems())
            {
                if (fam?.LandOfDeath == null)
                {
                    continue;
                }

                EventHelper.Instance.RunEvent(
                    new EventContainer(
                        fam.LandOfDeath,
                        EventActionType.DISPOSEMAP,
                        null));

                fam.LandOfDeath = null;
            }

            if (ServerManager.Instance.StartedEvents.Contains(EventType.LOD))
            {
                ServerManager.Instance.StartedEvents.Remove(EventType.LOD);
            }

            IsOpen = false;
            ChangePortalEffect(0);
        }

        private void refreshLOD(int remaining)
        {
            foreach (var fam in ServerManager.Instance.FamilyList.GetAllItems())
            {
                if (fam.LandOfDeath == null)
                {
                    fam.LandOfDeath = ServerManager.GenerateMapInstance(150, MapInstanceType.LodInstance, new InstanceBag());
                }

                EventHelper.Instance.RunEvent(new EventContainer(fam.LandOfDeath, EventActionType.CLOCK, remaining * 10));
                EventHelper.Instance.RunEvent(new EventContainer(fam.LandOfDeath, EventActionType.STARTCLOCK, new Tuple<List<EventContainer>, List<EventContainer>>(new List<EventContainer>(), new List<EventContainer>())));
            }
        }

        private void spawnDH(MapInstance LandOfDeath)
        {
            EventHelper.Instance.RunEvent(new EventContainer(LandOfDeath, EventActionType.SPAWNONLASTENTRY, 443));
            EventHelper.Instance.RunEvent(new EventContainer(LandOfDeath, EventActionType.SENDPACKET, "df 2"));
            EventHelper.Instance.RunEvent(new EventContainer(LandOfDeath, EventActionType.SENDPACKET, UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("HORN_APPEAR"), 0)));
        }
    }
}