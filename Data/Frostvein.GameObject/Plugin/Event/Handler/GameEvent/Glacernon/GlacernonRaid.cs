using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Plugin.Event
{
    public static class GlacernonRaid
    {
        public static List<MapMonster> Guardians { get; set; }

        #region Methods

        public static void GenerateRaid(MapInstanceType raidType, byte faction)
        {
            Guardians = new List<MapMonster>();
            var bitoren = ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(134));
            bitoren.CreatePortal(new Portal
            {
                SourceMapId = 134,
                SourceX = 140,
                SourceY = 100,
                DestinationMapId = 0,
                DestinationX = 1,
                DestinationY = 1,
                Type = (short)(9 + faction)
            });


            #region Guardian Spawning

            Guardians.Add(new MapMonster
            {
                MonsterVNum = (short)(678 + faction),
                MapX = 147,
                MapY = 88,
                MapId = 134,
                Position = 2,
                IsMoving = false,
                MapMonsterId = bitoren.GetNextMonsterId(),
                ShouldRespawn = false,
                IsHostile = true
            });
            Guardians.Add(new MapMonster
            {
                MonsterVNum = (short)(678 + faction),
                MapX = 149,
                MapY = 94,
                MapId = 134,
                Position = 2,
                IsMoving = false,
                MapMonsterId = bitoren.GetNextMonsterId(),
                ShouldRespawn = false,
                IsHostile = true
            });
            Guardians.Add(new MapMonster
            {
                MonsterVNum = (short)(678 + faction),
                MapX = 147,
                MapY = 101,
                MapId = 134,
                Position = 2,
                IsMoving = false,
                MapMonsterId = bitoren.GetNextMonsterId(),
                ShouldRespawn = false,
                IsHostile = true
            });
            Guardians.Add(new MapMonster
            {
                MonsterVNum = (short)(678 + faction),
                MapX = 139,
                MapY = 105,
                MapId = 134,
                Position = 2,
                IsMoving = false,
                MapMonsterId = bitoren.GetNextMonsterId(),
                ShouldRespawn = false,
                IsHostile = true
            });
            Guardians.Add(new MapMonster
            {
                MonsterVNum = (short)(678 + faction),
                MapX = 132,
                MapY = 101,
                MapId = 134,
                Position = 2,
                IsMoving = false,
                MapMonsterId = bitoren.GetNextMonsterId(),
                ShouldRespawn = false,
                IsHostile = true
            });
            Guardians.Add(new MapMonster
            {
                MonsterVNum = (short)(678 + faction),
                MapX = 129,
                MapY = 94,
                MapId = 134,
                Position = 2,
                IsMoving = false,
                MapMonsterId = bitoren.GetNextMonsterId(),
                ShouldRespawn = false,
                IsHostile = true
            });
            Guardians.Add(new MapMonster
            {
                MonsterVNum = (short)(678 + faction),
                MapX = 132,
                MapY = 88,
                MapId = 134,
                Position = 2,
                IsMoving = false,
                MapMonsterId = bitoren.GetNextMonsterId(),
                ShouldRespawn = false,
                IsHostile = true
            });

            #endregion

            foreach (var monster in Guardians)
            {
                monster.Initialize(bitoren);
                bitoren.AddMonster(monster);
                bitoren.Broadcast(monster.GenerateIn());
            }

            ServerManager.Instance.Broadcast(
                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ACT4_BITOREN_RAID_OPEN"), 0));
            var raidThread = new GlacernonRaidThread();
            Observable.Timer(TimeSpan.FromMinutes(0)).Subscribe(X => raidThread.Run(raidType, faction));
        }

        #endregion
    }

    public class GlacernonRaidThread
    {
        #region Members

        private readonly List<long> _wonFamilies = new List<long>();

        private short _bossMapId = 136;

        private bool _bossMove;

        private short _bossVNum = 563;

        private short _bossX = 55;

        private short _bossY = 11;

        private short _destPortalX = 55;

        private short _destPortalY = 80;

        private byte _faction;

        private short _mapId = 135;

        private MapInstanceType _raidType;

        private short _sourcePortalX = 146;

        private short _sourcePortalY = 43;

        private int _raidTime = 3600;

        private const int _interval = 60;

        #endregion

        #region Methods

        public void Run(MapInstanceType raidType, byte faction)
        {
            _raidType = raidType;
            _faction = faction;
            switch (raidType)
            {
                // Morcos is default
                case MapInstanceType.Act4Hatus:
                    _mapId = 137;
                    _bossMapId = 138;
                    _bossVNum = 577;
                    _bossX = 36;
                    _bossY = 18;
                    _sourcePortalX = 37;
                    _sourcePortalY = 156;
                    _destPortalX = 36;
                    _destPortalY = 58;
                    _bossMove = false;
                    break;

                case MapInstanceType.Act4Calvina:
                    _mapId = 139;
                    _bossMapId = 140;
                    _bossVNum = 629;
                    _bossX = 26;
                    _bossY = 26;
                    _sourcePortalX = 194;
                    _sourcePortalY = 17;
                    _destPortalX = 9;
                    _destPortalY = 41;
                    _bossMove = true;
                    break;

                case MapInstanceType.Act4Berios:
                    _mapId = 141;
                    _bossMapId = 142;
                    _bossVNum = 624;
                    _bossX = 29;
                    _bossY = 29;
                    _sourcePortalX = 188;
                    _sourcePortalY = 96;
                    _destPortalX = 29;
                    _destPortalY = 54;
                    _bossMove = true;
                    break;
            }


            _raidTime = 3000;

            //Run once to load everything in place
            refreshRaid(_raidTime);

            ServerManager.Instance.Act4RaidStart = DateTime.Now;

            while (_raidTime > 0)
            {
                _raidTime -= _interval;
                Thread.Sleep(_interval * 1000);
                refreshRaid(_raidTime);
            }

            endRaid();
        }

        private void endRaid()
        {
            foreach (var fam in ServerManager.Instance.FamilyList.GetAllItems())
            {
                if (fam.Act4Raid != null)
                {
                    EventHelper.Instance.RunEvent(new EventContainer(fam.Act4Raid, EventActionType.DISPOSEMAP, null));
                    fam.Act4Raid = null;
                }

                if (fam.Act4RaidBossMap != null)
                {
                    EventHelper.Instance.RunEvent(new EventContainer(fam.Act4RaidBossMap, EventActionType.DISPOSEMAP,
                        null));
                    fam.Act4RaidBossMap = null;
                }
            }

            var bitoren = ServerManager.GetMapInstance(ServerManager.GetBaseMapInstanceIdByMapId(134));

            foreach (var portal in bitoren.Portals.ToList().Where(s => s.Type.Equals(10) || s.Type.Equals(11)))
            {
                portal.IsDisabled = true;
                bitoren.Broadcast(portal.GenerateGp());
                bitoren.Portals.Remove(portal);
            }

            bitoren.Portals.RemoveAll(s => s.Type.Equals(10));
            bitoren.Portals.RemoveAll(s => s.Type.Equals(11));
            switch (_faction)
            {
                case 1:
                    ServerManager.Instance.Act4AngelStat.Mode = 0;
                    ServerManager.Instance.Act4AngelStat.IsMorcos = false;
                    ServerManager.Instance.Act4AngelStat.IsHatus = false;
                    ServerManager.Instance.Act4AngelStat.IsCalvina = false;
                    ServerManager.Instance.Act4AngelStat.IsBerios = false;
                    break;

                case 2:
                    ServerManager.Instance.Act4DemonStat.Mode = 0;
                    ServerManager.Instance.Act4DemonStat.IsMorcos = false;
                    ServerManager.Instance.Act4DemonStat.IsHatus = false;
                    ServerManager.Instance.Act4DemonStat.IsCalvina = false;
                    ServerManager.Instance.Act4DemonStat.IsBerios = false;
                    break;
            }

            ServerManager.Instance.StartedEvents.Remove(EventType.GLACERNONRAID);

            foreach (var monster in GlacernonRaid.Guardians)
            {
                bitoren.Broadcast(StaticPacketHelper.Out(UserType.Monster, monster.MapMonsterId));
                bitoren.RemoveMonster(monster);
            }

            GlacernonRaid.Guardians.Clear();
        }

        private void openRaid(Family fam)
        {
            fam.Act4RaidBossMap.OnCharacterDiscoveringMapEvents.Add(new Tuple<EventContainer, List<long>>(
                new EventContainer(fam.Act4RaidBossMap, EventActionType.STARTAct4RaidWAVES, new List<long>()),
                new List<long>()));
            var onDeathEvents = new List<EventContainer>
            {
                new EventContainer(fam.Act4RaidBossMap, EventActionType.THROWITEMS,  new Tuple<int, short, byte, int, int, short>(_bossVNum, 1046, 30, 20000, 20001, 0)),
                new EventContainer(fam.Act4RaidBossMap, EventActionType.THROWITEMS,  new Tuple<int, short, byte, int, int, short>(_bossVNum, 1122, 30, 2, 2, 0)),
            };
            if (_raidType.Equals(MapInstanceType.Act4Berios))
            {
                onDeathEvents.Add(new EventContainer(fam.Act4RaidBossMap, EventActionType.THROWITEMS,
                    new Tuple<int, short, byte, int, int, short>(_bossVNum, 2395, 15, 1, 2, 0)));
                onDeathEvents.Add(new EventContainer(fam.Act4RaidBossMap, EventActionType.THROWITEMS,
                    new Tuple<int, short, byte, int, int, short>(_bossVNum, 2396, 15, 1, 2, 0)));
                onDeathEvents.Add(new EventContainer(fam.Act4RaidBossMap, EventActionType.THROWITEMS,
                    new Tuple<int, short, byte, int, int, short>(_bossVNum, 2397, 15, 1, 2, 0)));

                fam.Act4RaidBossMap.OnCharacterDiscoveringMapEvents.Add(new Tuple<EventContainer, List<long>>(
                    new EventContainer(fam.Act4RaidBossMap, EventActionType.SPAWNMONSTER,
                        new MonsterToSummon(621, fam.Act4RaidBossMap.Map.GetRandomPosition(), null, true,
                            hasDelay: 30)), new List<long>()));
                fam.Act4RaidBossMap.OnCharacterDiscoveringMapEvents.Add(new Tuple<EventContainer, List<long>>(
                    new EventContainer(fam.Act4RaidBossMap, EventActionType.SPAWNMONSTER,
                        new MonsterToSummon(622, fam.Act4RaidBossMap.Map.GetRandomPosition(), null, true,
                            hasDelay: 205)), new List<long>()));
                fam.Act4RaidBossMap.OnCharacterDiscoveringMapEvents.Add(new Tuple<EventContainer, List<long>>(
                    new EventContainer(fam.Act4RaidBossMap, EventActionType.SPAWNMONSTER,
                        new MonsterToSummon(623, fam.Act4RaidBossMap.Map.GetRandomPosition(), null, true,
                            hasDelay: 380)), new List<long>()));
            }

            onDeathEvents.Add(new EventContainer(fam.Act4RaidBossMap, EventActionType.SCRIPTEND, (byte)1));
            onDeathEvents.Add(new EventContainer(fam.Act4Raid, EventActionType.CHANGEPORTALTYPE,
                new Tuple<int, PortalType>(
                    fam.Act4Raid.Portals
                        .Find(s => s.SourceX == _sourcePortalX && s.SourceY == _sourcePortalY && !s.IsDisabled)
                        .PortalId, PortalType.Closed)));
            var bossMob = new MonsterToSummon(_bossVNum, new MapCell { X = _bossX, Y = _bossY }, null, _bossMove)
            {
                DeathEvents = onDeathEvents
            };
            EventHelper.Instance.RunEvent(
                new EventContainer(fam.Act4RaidBossMap, EventActionType.SPAWNMONSTER, bossMob));
            EventHelper.Instance.RunEvent(new EventContainer(fam.Act4Raid, EventActionType.SENDPACKET,
                UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ACT4RAID_OPEN"), 0)));

            //Observable.Timer(TimeSpan.FromSeconds(90)).Subscribe(o =>
            //{
            //TODO: Summon Monsters
            //});
        }

        private void refreshRaid(int remaining)
        {
            foreach (var fam in ServerManager.Instance.FamilyList.GetAllItems())
            {
                if (fam.Act4Raid == null)
                {
                    fam.Act4Raid = ServerManager.GenerateMapInstance(_mapId, _raidType, new InstanceBag(), true);
                }

                if (fam.Act4RaidBossMap == null)
                {
                    fam.Act4RaidBossMap = ServerManager.GenerateMapInstance(_bossMapId, _raidType, new InstanceBag());
                }

                if (remaining <= 3000 && !fam.Act4Raid.Portals.Any(s =>
                                                      s.DestinationMapInstanceId.Equals(fam.Act4RaidBossMap.MapInstanceId))) //opens at min 50
                {
                    fam.Act4Raid.CreatePortal(new Portal
                    {
                        DestinationMapInstanceId = fam.Act4RaidBossMap.MapInstanceId,
                        DestinationX = _destPortalX,
                        DestinationY = _destPortalY,
                        SourceX = _sourcePortalX,
                        SourceY = _sourcePortalY
                    });
                    openRaid(fam);
                }

                if (fam.Act4RaidBossMap.Monsters.Find(s => s.MonsterVNum == _bossVNum && s.CurrentHp / s.MaxHp < 0.5) !=
                    null
                    && fam.Act4Raid.Portals.Find(s =>
                        s.SourceX == _sourcePortalX && s.SourceY == _sourcePortalY && !s.IsDisabled) is Portal portal)
                {
                    EventHelper.Instance.RunEvent(new EventContainer(fam.Act4Raid, EventActionType.CHANGEPORTALTYPE,
                        new Tuple<int, PortalType>(
                            fam.Act4Raid.Portals.Find(s =>
                                s.SourceX == _sourcePortalX && s.SourceY == _sourcePortalY && !s.IsDisabled).PortalId,
                            PortalType.Closed)));
                    fam.Act4Raid.Broadcast(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PORTAL_CLOSED"), 0));
                    fam.Act4RaidBossMap.Broadcast(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("PORTAL_CLOSED"), 0));
                }
            }
        }

        #endregion
    }
}