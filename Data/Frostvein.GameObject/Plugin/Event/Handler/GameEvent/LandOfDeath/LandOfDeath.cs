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
        public static EventType EventType => EventType.LOD;

        public static void GenerateLandOfDeath()
        {
            const int LOD_TIME = 60;
            const int HORN_TIME = 45;
            const int HORN_STAY_TIME = 1;
            const int HORN_RESPAWN_TIME = 3;

            EventHelper.Instance.RunEvent(new EventContainer(ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(98)), EventActionType.NPCSEFFECTCHANGESTATE, true));
            var lodThread = new LandOfDeathThread();
            var compositeDisposable = new CompositeDisposable();
            var cancellationTokenSource1 = new CancellationTokenSource();
            var observable1 = EventServiceExtension.CreateRepeatingObservableSeconds(5, async () =>
            {
               lodThread.Run(LOD_TIME * 60, (HORN_TIME + 1) * 60, (HORN_RESPAWN_TIME + HORN_STAY_TIME) * 60, HORN_STAY_TIME * 60);
            }, cancellationTokenSource1.Token);
            var subscription1 = observable1.Subscribe();
            compositeDisposable.Add(subscription1);
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

                if (lodTime == hornTime || lodTime == hornTime - hornRespawn * dhspawns)
                {
                    foreach (var fam in ServerManager.Instance.FamilyList.GetAllItems())
                        if (fam.LandOfDeath != null)
                        {
                            //fam.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LOD_RATES_INCREASED"), 0)); fix this
                            EventHelper.Instance.RunEvent(new EventContainer(fam.LandOfDeath, EventActionType.CHANGEXPRATE, 2));
                            EventHelper.Instance.RunEvent(new EventContainer(fam.LandOfDeath, EventActionType.CHANGEDROPRATE, 3));
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
                if (fam.LandOfDeath != null)
                {
                    EventHelper.Instance.RunEvent(new EventContainer(fam.LandOfDeath, EventActionType.DISPOSEMAP, null));
                    fam.LandOfDeath = null;
                }

            ServerManager.Instance.StartedEvents.Remove(EventType.LOD);
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