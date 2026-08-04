using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Event
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
                    Logger.Warn("[LOD] Result=Ignored Reason=AlreadyRunning");
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

            Task.Run(async () =>
            {
                try
                {
                    var lodRuntime = new LandOfDeathRuntime();
                    await lodRuntime.RunAsync(
                            lodTime * 60,
                            (hornTime + 1) * 60,
                            (hornRespawnTime + hornStayTime) * 60,
                            hornStayTime * 60)
                        .ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    Logger.Error("[LOD] Result=Failed", exception);
                }
                finally
                {
                    lock (LodLock)
                    {
                        _isRunning = false;
                    }

                    GameEventHandler.CompleteEvent(EventType.LOD);
                    Logger.Info("[LOD] Result=Completed");
                }
            });
        }
    }

    public sealed class LandOfDeathRuntime
    {
        private const int TickSeconds = 30;

        public bool IsOpen { get; private set; } = true;

        public async Task RunAsync(int lodTime, int hornTime, int hornRespawn, int hornStay)
        {
            ChangePortalEffect(855);
            int dhSpawns = 0;

            try
            {
                while (lodTime > 0)
                {
                    RefreshLod(lodTime);

                    if (lodTime == hornTime - hornRespawn * dhSpawns)
                    {
                        foreach (Family family in ServerManager.Instance.FamilyList.GetAllItems())
                        {
                            if (family?.LandOfDeath == null)
                            {
                                continue;
                            }

                            EventHelper.Instance.RunEvent(
                                new EventContainer(
                                    family.LandOfDeath,
                                    EventActionType.CHANGEXPRATE,
                                    2));
                            EventHelper.Instance.RunEvent(
                                new EventContainer(
                                    family.LandOfDeath,
                                    EventActionType.CHANGEDROPRATE,
                                    3));
                            SpawnDh(family.LandOfDeath);
                        }
                    }
                    else if (lodTime == hornTime - hornRespawn * dhSpawns - hornStay)
                    {
                        foreach (Family family in ServerManager.Instance.FamilyList.GetAllItems())
                        {
                            if (family?.LandOfDeath != null)
                            {
                                DespawnDh(family.LandOfDeath);
                            }
                        }

                        dhSpawns++;
                    }

                    lodTime -= TickSeconds;
                    await Task.Delay(TimeSpan.FromSeconds(TickSeconds)).ConfigureAwait(false);
                }
            }
            finally
            {
                EndLod();
            }
        }

        private static void ChangePortalEffect(short effectId)
        {
            ServerManager.Instance.GetMapNpcsByVNum(453)
                .ForEach(mapNpc => mapNpc.Effect = effectId);
        }

        private void DespawnDh(MapInstance landOfDeath)
        {
            EventHelper.Instance.RunEvent(
                new EventContainer(
                    ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(98)),
                    EventActionType.NPCSEFFECTCHANGESTATE,
                    false));
            EventHelper.Instance.RunEvent(
                new EventContainer(
                    landOfDeath,
                    EventActionType.SENDPACKET,
                    UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("HORN_DISAPEAR"),
                        0)));
            EventHelper.Instance.RunEvent(
                new EventContainer(landOfDeath, EventActionType.UNSPAWNMONSTERS, 443));

            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;
            ChangePortalEffect(0);
        }

        private void EndLod()
        {
            foreach (Family family in ServerManager.Instance.FamilyList.GetAllItems())
            {
                if (family?.LandOfDeath == null)
                {
                    continue;
                }

                EventHelper.Instance.RunEvent(
                    new EventContainer(
                        family.LandOfDeath,
                        EventActionType.DISPOSEMAP,
                        null));
                family.LandOfDeath = null;
            }

            IsOpen = false;
            ChangePortalEffect(0);
        }

        private static void RefreshLod(int remaining)
        {
            foreach (Family family in ServerManager.Instance.FamilyList.GetAllItems())
            {
                if (family == null)
                {
                    continue;
                }

                family.LandOfDeath ??= ServerManager.GenerateMapInstance(
                    150,
                    MapInstanceType.LodInstance,
                    new InstanceBag());

                EventHelper.Instance.RunEvent(
                    new EventContainer(
                        family.LandOfDeath,
                        EventActionType.CLOCK,
                        remaining * 10));
                EventHelper.Instance.RunEvent(
                    new EventContainer(
                        family.LandOfDeath,
                        EventActionType.STARTCLOCK,
                        new Tuple<List<EventContainer>, List<EventContainer>>(
                            new List<EventContainer>(),
                            new List<EventContainer>())));
            }
        }

        private static void SpawnDh(MapInstance landOfDeath)
        {
            EventHelper.Instance.RunEvent(
                new EventContainer(landOfDeath, EventActionType.SPAWNONLASTENTRY, 443));
            EventHelper.Instance.RunEvent(
                new EventContainer(landOfDeath, EventActionType.SENDPACKET, "df 2"));
            EventHelper.Instance.RunEvent(
                new EventContainer(
                    landOfDeath,
                    EventActionType.SENDPACKET,
                    UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("HORN_APPEAR"),
                        0)));
        }
    }
}
