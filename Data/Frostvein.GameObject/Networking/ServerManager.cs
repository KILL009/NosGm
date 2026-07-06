using Frostvein.Configuration;
using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Characters.Events;
using Frostvein.GameObject.Event;
using Frostvein.GameObject.Extension;
using Frostvein.GameObject.Helpers;
using Frostvein.Master.Library.Client;
using Frostvein.Master.Library.Data;
using Frostvein.XMLModel.Models.Quest;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Frostvein.Core.Threading;
using Frostvein.GameObject.Extension.Reputation;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Threads.WorkerThreads.Battle.Buff;
using Frostvein.GameObject.Plugin.Event.Handler;
using Frostvein.GameObject.Plugin.Load;
using Frostvein.GameObject._Event;
using Frostvein.GameObject.Event.ARENA;
using Frostvein.GameObject.Plugin.Event;
using static Frostvein.GameObject.Plugin.Event.RainbowBattle;

namespace Frostvein.GameObject.Networking
{
    public class ServerManager : BroadcastableBase
    {
        #region Instantiation

        private ServerManager()
        {
        }

        #endregion

        #region Members

#if DEBUG
        public bool IsDebugMode = true;
#else
        public bool IsDebugMode = false;
#endif

        public bool InShutdown;

        public bool ShutdownStop;



        public ThreadSafeSortedList<long, Group> ThreadSafeGroupList;

        public static List<Card> Cards { get; set; }

        public static readonly ConcurrentDictionary<Guid, MapInstance> _mapinstances =
            new ConcurrentDictionary<Guid, MapInstance>();

        private static readonly ConcurrentDictionary<Guid, MapInstance> _mapinstances2 = new();

        public List<FishingPositionDto> FishingPosition { get; set; }

        public ConcurrentDictionary<FishingPositionDto, List<FishingInformationsDto>> FishingSpots { get; set; }

        public static readonly List<Map> Maps = new List<Map>();
        private static readonly CryptoRandom _random = new CryptoRandom();
        public static readonly List<Skill> Skills = new List<Skill>();
        public static readonly List<Item> Items = new List<Item>();
        public static readonly List<NpcMonster> Npcs = new List<NpcMonster>();

        //Function to get a random number
        private static readonly Random random = new Random();
        private static readonly RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();

        private static readonly object syncLock = new object();

        private static ServerManager _instance;

        public ConcurrentBag<NpcMonsterSkill> _allMonsterSkills;
        public List<DropDTO> _generalDrops;

        private bool _inRelationRefreshMode;

        public long _lastGroupId;

        public Dictionary<short, List<MapNpc>> _mapNpcs;

        public Dictionary<short, List<DropDTO>> _monsterDrops;

        public Dictionary<short, List<NpcMonsterSkill>> _monsterSkills;
        public ThreadSafeSortedList<int, RecipeListDTO> _recipeLists;

        public ThreadSafeSortedList<short, Recipe> _recipes;

        public Dictionary<int, List<ShopItemDTO>> _shopItems;

        public static double RandomDouble()
        {
            lock (syncLock)
            {
                return random.NextDouble() * 100;
            }
        }

        public Dictionary<int, Shop> _shops;

        public Dictionary<int, List<ShopSkillDTO>> _shopSkills;

        public Dictionary<int, List<TeleporterDTO>> _teleporters;

        #endregion

        #region Properties

        public bool IsAct6RaidZenas { get; set; }

        public bool IsAct6RaidErenia { get; set; }
        public List<BattlePassQuestDTO> BattlePassQuests { get; set; }

        public List<BattlePassPrizeDTO> BattlePassPrizes { get; set; }

        public static DateTime DailyBpDate { get; set; }

        public DateTime WeeklyBpDate { get; set; }

        public DateTime SeasonBpDate { get; set; }

        public int DailyBpTime { get; set; }

        public int WeeklyBpTime { get; set; }

        public int SeasonBpTime { get; set; }


        public List<CharacterTimespaceLogDTO> TimespaceLogs = new();

        public List<FairyEnchantmentDTO> FairyEnchantments = new();

        public CharacterTimespaceLogDTO newTSLog = new();


        public List<ClientSession> TimespaceSession = new();

        public static ServerManager Instance => _instance ?? (_instance = new ServerManager());

        public List<RainbowBattleTeam> RainbowBattleMembers { get; set; } = new List<RainbowBattleTeam>();

        public static byte MaxCodeAttempts { get; set; }

        public static TimeSpan TimeBeforeAutoKick { get; set; }

        public static TimeSpan AutoKickInterval { get; set; }

        public static bool AntiBotEnabled { get; set; }

        public Act4Stat Act4AngelStat { get; set; }

        public Act4Stat Act4DemonStat { get; set; }

        public DateTime Act4RaidStart { get; set; }

        public Act4Stat Act6Erenia { get; set; }

        public ConcurrentBag<ScriptedInstance> Act6Raids { get; set; }

        public Act4Stat Act6Zenas { get; set; }

        public MapInstance Act7Ship { get; set; }

        public MapInstance ArenaInstance { get; set; }

        public List<ArenaMember> ArenaMembers { get; set; } = new List<ArenaMember>();

        public List<ConcurrentBag<ArenaTeamMember>> ArenaTeams { get; set; } =
            new List<ConcurrentBag<ArenaTeamMember>>();

        public List<long> BannedCharacters { get; set; } = new List<long>();

        public ThreadSafeGenericList<BazaarItemLink> BazaarList { get; set; }

        public List<short> BossVNums { get; set; }

        public int ChannelId { get; set; }

        public List<CharacterRelationDTO> CharacterRelations { get; set; }

        public ThreadSafeSortedList<long, ClientSession> CharacterScreenSessions { get; set; }

        public ConfigurationObject Configuration { get; set; }

        public bool EventInWaiting { get; set; }

        public MapInstance FamilyArenaInstance { get; set; }

        public ThreadSafeSortedList<long, Family> FamilyList { get; set; }

        public long? FlowerQuestId { get; set; }

        public ThreadSafeGenericLockedList<Group> GroupList { get; set; } = new ThreadSafeGenericLockedList<Group>();

        public List<Group> Groups => ThreadSafeGroupList.GetAllItems();

        public bool IceBreakerInWaiting { get; set; }

        public bool IsReboot { get; set; }

        public bool IsBazaarMaintenance { get; set; }

        public bool IsWorldServer => WorldId != Guid.Empty;

        public DateTime LastFCSent { get; set; }

        public MallAPIHelper MallApi { get; set; }

        public List<short> MapBossVNums { get; set; }

        public List<int> MateIds { get; internal set; } = new List<int>();

        public List<MimicRotationDTO> MimicItems { get; set; } = new List<MimicRotationDTO>();

        public List<PenaltyLogDTO> PenaltyLogs { get; set; }

        public ThreadSafeSortedList<long, QuestModel> QuestList { get; set; }

        public List<Quest> Quests { get; set; }

        public ConcurrentBag<ScriptedInstance> Raids { get; set; }

        public Task RebootTask { get; set; }

        public List<Schedule> Schedules { get; set; }

        public string ServerGroup { get; set; }

        public List<MapInstance> SpecialistGemMapInstances { get; set; } = new List<MapInstance>();

        public List<EventType> StartedEvents { get; set; } = new List<EventType>();

        public Task TaskShutdown { get; set; }

        public ConcurrentBag<ScriptedInstance> TimeSpaces { get; set; }

        public List<CharacterDTO> TopComplimented { get; set; }

        public List<CharacterDTO> TopPoints { get; set; }

        public List<CharacterDTO> TopReputation { get; set; }

        public List<CharacterDTO> TopDuel { get; set; }

        public List<CharacterDTO> TopMonster { get; set; }

        public Guid WorldId { get; set; }

        private DateTime LastMaintenanceAdvert { get; set; }

        public static bool IsUnderDebugMode => Debugger.IsAttached;

        public short FakeCount { get; set; }

        public static EventEntity GameEventPlugin { get; set; }

        #endregion

        #region Methods

        public static void StartMonster()
        {
            Parallel.ForEach(_mapinstances, map =>
            {
                Parallel.ForEach(map.Value.Npcs, npc => npc.StartLife());
                Parallel.ForEach(map.Value.Monsters, monster => monster.StartLife());
            });
        }

        public void GlacernonProcess()
        {
            if (ChannelId != 51)
            {
                return;
            }

            var angelMapInstance = GetMapInstance(GetBaseMapInstanceIdByMapId(132));
            var demonMapInstance = GetMapInstance(GetBaseMapInstanceIdByMapId(133));

            void SummonMukraju(MapInstance instance, byte faction)
            {
                var monster = new MapMonster
                {
                    MonsterVNum = 556,
                    MapY = faction == 1 ? (short)92 : (short)95,
                    MapX = faction == 1 ? (short)114 : (short)20,
                    MapId = (short)(131 + faction),
                    IsMoving = true,
                    MapMonsterId = instance.GetNextMonsterId(),
                    ShouldRespawn = false
                };
                monster.Initialize(instance);
                monster.Faction = (FactionType)faction == FactionType.Angel ? FactionType.Demon : FactionType.Angel;
                instance.AddMonster(monster);
                instance.Broadcast(monster.GenerateIn());

                Observable.Timer(TimeSpan.FromSeconds(faction == 1 ? Act4AngelStat.TotalTime : Act4DemonStat.TotalTime))
                    .Subscribe(s =>
                    {
                        if (instance.Monsters.ToList().Any(m => m.MonsterVNum == monster.MonsterVNum))
                        {
                            if (faction == 1)
                            {
                                Act4AngelStat.Mode = 0;
                            }
                            else
                            {
                                Act4DemonStat.Mode = 0;
                            }

                            instance.DespawnMonster(monster.MonsterVNum);
                            foreach (var sess in Sessions)
                            {
                                sess.SendPacket(sess.Character.GenerateFc());
                            }
                        }
                    });
            }

            int CreateRaid(byte faction)
            {
                var raidType = MapInstanceType.Act4Morcos;
                var rng = RandomNumber(1, 5);
                switch (rng)
                {
                    case 2:
                        raidType = MapInstanceType.Act4Hatus;
                        break;

                    case 3:
                        raidType = MapInstanceType.Act4Calvina;
                        break;

                    case 4:
                        raidType = MapInstanceType.Act4Berios;
                        break;
                }

                GlacernonRaid.GenerateRaid(raidType, faction);
                return rng;
            }

            if (Act4AngelStat.Percentage >= 10000)
            {
                Act4AngelStat.Mode = 1;
                Act4AngelStat.Percentage = 0;
                Act4AngelStat.TotalTime = 300;
                SummonMukraju(angelMapInstance, 1);
                foreach (var sess in Sessions)
                {
                    sess.SendPacket(sess.Character.GenerateFc());
                }
            }

            if (Act4AngelStat.Mode == 1 && !angelMapInstance.Monsters.Any(s => s.MonsterVNum == 556))
            {
                Act4AngelStat.Mode = 3;
                Act4AngelStat.TotalTime = 3600;

                switch (CreateRaid(1))
                {
                    case 1:
                        Act4AngelStat.IsMorcos = true;
                        break;

                    case 2:
                        Act4AngelStat.IsHatus = true;
                        break;

                    case 3:
                        Act4AngelStat.IsCalvina = true;
                        break;

                    case 4:
                        Act4AngelStat.IsBerios = true;
                        break;
                }

                foreach (var sess in Sessions)
                {
                    sess.SendPacket(sess.Character.GenerateFc());
                }
            }

            if (Act4DemonStat.Percentage >= 10000)
            {
                Act4DemonStat.Mode = 1;
                Act4DemonStat.Percentage = 0;
                Act4DemonStat.TotalTime = 300;
                SummonMukraju(demonMapInstance, 2);
                foreach (var sess in Sessions)
                {
                    sess.SendPacket(sess.Character.GenerateFc());
                }
            }

            if (Act4DemonStat.Mode == 1 && !demonMapInstance.Monsters.Any(s => s.MonsterVNum == 556))
            {
                Act4DemonStat.Mode = 3;
                Act4DemonStat.TotalTime = 3600;

                switch (CreateRaid(2))
                {
                    case 1:
                        Act4DemonStat.IsMorcos = true;
                        break;

                    case 2:
                        Act4DemonStat.IsHatus = true;
                        break;

                    case 3:
                        Act4DemonStat.IsCalvina = true;
                        break;

                    case 4:
                        Act4DemonStat.IsBerios = true;
                        break;
                }

                foreach (var sess in Sessions)
                {
                    sess.SendPacket(sess.Character.GenerateFc());
                }
            }

            if (DateTime.Now >= LastFCSent.AddMinutes(1))
            {
                foreach (var sess in Sessions)
                {
                    sess.SendPacket(sess.Character.GenerateFc());
                }

                LastFCSent = DateTime.Now;
            }
        }

        public static MapInstance GenerateMapInstance(short mapId, MapInstanceType type, InstanceBag mapclock,
            bool dropAllowed = false, bool isScriptedInstance = false)
        {
            var map = Maps.Find(m => m.MapId.Equals(mapId));
            if (map == null)
            {
                return null;
            }

            var guid = Guid.NewGuid();
            var mapInstance = new MapInstance(map, guid, false, type, mapclock, dropAllowed);
            if (!isScriptedInstance)
            {
                mapInstance.LoadMonsters();
                mapInstance.LoadNpcs();
                mapInstance.LoadPortals();
                foreach (var mapMonster in mapInstance.Monsters)
                {
                    mapMonster.MapInstance = mapInstance;
                    mapInstance.AddMonster(mapMonster);
                }

                foreach (var mapNpc in mapInstance.Npcs)
                {
                    mapNpc.MapInstance = mapInstance;
                    mapInstance.AddNPC(mapNpc);
                }
            }

            _mapinstances.TryAdd(guid, mapInstance);
            return mapInstance;
        }

        public static IEnumerable<Card> GetAllCard() => Cards;

        public static List<MapInstance> GetAllMapInstances() => _mapinstances.Values.ToList();

        public static IEnumerable<Skill> GetAllSkill() => Skills;

        public static Guid GetBaseMapInstanceIdByMapId(short mapId)
        {
            return _mapinstances.FirstOrDefault(s =>
                s.Value?.Map.MapId == mapId && s.Value.MapInstanceType == MapInstanceType.BaseMapInstance).Key;
        }

        public static Card GetCard(short? cardId)
        {
            return Cards.Find(m => m.CardId.Equals(cardId));
        }

        public static Item GetItem(short vnum)
        {
            return Items.FirstOrDefault(m => m.VNum.Equals(vnum));
        }

        public static MapInstance GetMapInstance(Guid id) => _mapinstances.ContainsKey(id) ? _mapinstances[id] : null;

        public static MapInstance GetMapInstanceByMapId(short mapId)
        {
            return _mapinstances.Values.FirstOrDefault(s => s.Map.MapId == mapId);
        }

        public static List<MapInstance> GetMapInstances(Func<MapInstance, bool> predicate) => _mapinstances.Values.Where(predicate).ToList();

        //AFTER Temp-Fix
        private static ThreadSafeLockedDictionary<short, NpcMonster> _npcMonsterCache = new();
        public static NpcMonster GetNpcMonster(short npcVNum)
        {
            if (_npcMonsterCache.ContainsKey(npcVNum))
                return _npcMonsterCache[npcVNum];

            var npcMonster = Npcs.FirstOrDefault(m => m.NpcMonsterVNum.Equals(npcVNum));
            _npcMonsterCache.TryAdd(npcVNum, npcMonster);
            return npcMonster;
        }

        public static Skill GetSkill(short skillVNum)
        {
            return Skills.Find(m => m.SkillVNum.Equals(skillVNum));
        }

        public void LoadTimespaceLogs()
        {
            TimespaceLogs = DAOFactory.CharacterTimeSpaceLogDAO.LoadAll().ToList();
            //LOGGER($"{TimespaceLogs.Count} TimeSpace Logs loaded");
        }

        public static MapCell MinilandRandomPos() => new MapCell { X = (short)RandomNumber(5, 16), Y = (short)RandomNumber(3, 14) };

        public static int RandomNumber(int min = 0, int max = 100)
        {
            lock (syncLock)
            {
                // synchronize
                return random.Next(min, max);
            }
        }

        public static double NextDoubleLinear(double minValue = 0.01, double maxValue = 100)
        {
            // TODO: some validation here...
            double sample = random.NextDouble();
            return (maxValue * sample) + (minValue * (1d - sample));
        }

        public static bool RandomProbabilityCheck(double probability)
        {
            if (probability == 0) return false;

            var randomNumber = TrueRandomNumber<int>(0, 100);

            if (randomNumber <= probability) return true;
            else return false;
        }

        public static T RandomNumber<T>(int min = 0, int max = 100) => (T)Convert.ChangeType(RandomNumber(min, max), typeof(T));

        public static void RemoveMapInstance(Guid mapId)
        {
            if (_mapinstances == null || mapId == null)
            {
                return;
            }

            if (_mapinstances.FirstOrDefault(s => s.Key == mapId) is KeyValuePair<Guid, MapInstance> map &&
                !map.Equals(default))
            {
                if (map.Value == null || map.Key == null) return;
                map.Value.Dispose();
                ((IDictionary)_mapinstances).Remove(map.Key);
            }
        }

        public static T TrueRandomNumber<T>(int min, int max)
        {
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue)
            {
                // Get four random bytes.
                byte[] four_bytes = new byte[4];
                rand.GetBytes(four_bytes);

                // Convert that into an uint.
                scale = BitConverter.ToUInt32(four_bytes, 0);
            }

            // Add min to the scaled difference between max and min.
            return (T)Convert.ChangeType((int)(min + (max - min) *
                (scale / (double)uint.MaxValue)), typeof(T));
        }

        public static MapInstance ResetMapInstance(MapInstance baseMapInstance)
        {
            if (baseMapInstance != null)
            {
                var mapinfo = new Map(baseMapInstance.Map.MapId, baseMapInstance.Map.GridMapId,
                    baseMapInstance.Map.Data)
                {
                    Music = baseMapInstance.Map.Music,
                    Name = baseMapInstance.Map.Name,
                    ShopAllowed = baseMapInstance.Map.ShopAllowed,
                    XpRate = baseMapInstance.Map.XpRate
                };
                var mapInstance = new MapInstance(mapinfo, baseMapInstance.MapInstanceId, baseMapInstance.ShopAllowed,
                    baseMapInstance.MapInstanceType, new InstanceBag(), baseMapInstance.DropAllowed);
                mapInstance.LoadMonsters();
                mapInstance.LoadNpcs();
                mapInstance.LoadPortals();
                foreach (var si in DAOFactory.ScriptedInstanceDAO.LoadByMap(mapInstance.Map.MapId).ToList())
                {
                    var siObj = new ScriptedInstance(si);
                    if (siObj.Type == ScriptedInstanceType.TimeSpace)
                    {
                        mapInstance.ScriptedInstances.Add(siObj);
                    }
                    else if (siObj.Type == ScriptedInstanceType.Raid)
                    {
                        var port = new Portal
                        {
                            Type = (byte)PortalType.Raid,
                            SourceMapId = siObj.MapId,
                            SourceX = siObj.PositionX,
                            SourceY = siObj.PositionY
                        };
                        mapInstance.Portals.Add(port);
                    }
                }

                foreach (var mapMonster in mapInstance.Monsters)
                {
                    mapMonster.MapInstance = mapInstance;
                    mapInstance.AddMonster(mapMonster);
                }

                foreach (var mapNpc in mapInstance.Npcs)
                {
                    mapNpc.MapInstance = mapInstance;
                    mapInstance.AddNPC(mapNpc);
                }

                RemoveMapInstance(baseMapInstance.MapInstanceId);
                _mapinstances.TryAdd(baseMapInstance.MapInstanceId, mapInstance);
                return mapInstance;
            }

            return null;
        }

        public static void Shout(string message, bool noAdminTag = false)
        {
            Instance.Broadcast(UserInterfaceHelper.GenerateSay(
                (noAdminTag ? "" : $"({Language.Instance.GetMessageFromKey("ADMINISTRATOR")})") + message, 10));
            Instance.Broadcast(UserInterfaceHelper.GenerateMsg(message, 2));
        }

        public void Act4Process()
        {
            if (ChannelId != 51)
            {
                return;
            }

            var angelMapInstance = GetMapInstance(GetBaseMapInstanceIdByMapId(132));
            var demonMapInstance = GetMapInstance(GetBaseMapInstanceIdByMapId(133));

            void SummonMukraju(MapInstance instance, byte faction)
            {
                var monster = new MapMonster
                {
                    MonsterVNum = 556,
                    MapY = faction == 1 ? (short)92 : (short)95,
                    MapX = faction == 1 ? (short)114 : (short)20,
                    MapId = (short)(131 + faction),
                    IsMoving = true,
                    MapMonsterId = instance.GetNextMonsterId(),
                    ShouldRespawn = false
                };
                monster.Initialize(instance);
                monster.Faction = (FactionType)faction == FactionType.Angel ? FactionType.Demon : FactionType.Angel;
                instance.AddMonster(monster);
                instance.Broadcast(monster.GenerateIn());

                Observable.Timer(TimeSpan.FromSeconds(faction == 1 ? Act4AngelStat.TotalTime : Act4DemonStat.TotalTime))
                    .Subscribe(s =>
                    {
                        if (instance.Monsters.ToList().Any(m => m.MonsterVNum == monster.MonsterVNum))
                        {
                            if (faction == 1)
                            {
                                Act4AngelStat.Mode = 0;
                            }
                            else
                            {
                                Act4DemonStat.Mode = 0;
                            }

                            instance.DespawnMonster(monster.MonsterVNum);
                            foreach (var sess in Sessions)
                            {
                                sess.SendPacket(sess.Character.GenerateFc());
                            }
                        }
                    });
            }

            int CreateRaid(byte faction)
            {
                var raidType = MapInstanceType.Act4Morcos;
                var rng = RandomNumber(1, 5);
                switch (rng)
                {
                    case 2:
                        raidType = MapInstanceType.Act4Hatus;
                        break;

                    case 3:
                        raidType = MapInstanceType.Act4Calvina;
                        break;

                    case 4:
                        raidType = MapInstanceType.Act4Berios;
                        break;
                }

                GlacernonRaid.GenerateRaid(raidType, faction);
                return rng;
            }

            if (Act4AngelStat.Percentage >= 10000)
            {
                Act4AngelStat.Mode = 1;
                Act4AngelStat.Percentage = 0;
                Act4AngelStat.TotalTime = 300;
                SummonMukraju(angelMapInstance, 1);
                foreach (var sess in Sessions)
                {
                    sess.SendPacket(sess.Character.GenerateFc());
                }
            }

            if (Act4AngelStat.Mode == 1 && !angelMapInstance.Monsters.Any(s => s.MonsterVNum == 556))
            {
                Act4AngelStat.Mode = 3;
                Act4AngelStat.TotalTime = 3600;

                switch (CreateRaid(1))
                {
                    case 1:
                        Act4AngelStat.IsMorcos = true;
                        break;

                    case 2:
                        Act4AngelStat.IsHatus = true;
                        break;

                    case 3:
                        Act4AngelStat.IsCalvina = true;
                        break;

                    case 4:
                        Act4AngelStat.IsBerios = true;
                        break;
                }

                foreach (var sess in Sessions)
                {
                    sess.SendPacket(sess.Character.GenerateFc());
                }
            }

            if (Act4DemonStat.Percentage >= 10000)
            {
                Act4DemonStat.Mode = 1;
                Act4DemonStat.Percentage = 0;
                Act4DemonStat.TotalTime = 300;
                SummonMukraju(demonMapInstance, 2);
                foreach (var sess in Sessions)
                {
                    sess.SendPacket(sess.Character.GenerateFc());
                }
            }

            if (Act4DemonStat.Mode == 1 && !demonMapInstance.Monsters.Any(s => s.MonsterVNum == 556))
            {
                Act4DemonStat.Mode = 3;
                Act4DemonStat.TotalTime = 3600;

                switch (CreateRaid(2))
                {
                    case 1:
                        Act4DemonStat.IsMorcos = true;
                        break;

                    case 2:
                        Act4DemonStat.IsHatus = true;
                        break;

                    case 3:
                        Act4DemonStat.IsCalvina = true;
                        break;

                    case 4:
                        Act4DemonStat.IsBerios = true;
                        break;
                }

                foreach (var sess in Sessions)
                {
                    sess.SendPacket(sess.Character.GenerateFc());
                }
            }

            if (DateTime.Now >= LastFCSent.AddMinutes(1))
            {
                foreach (var sess in Sessions)
                {
                    sess.SendPacket(sess.Character.GenerateFc());
                }

                LastFCSent = DateTime.Now;
            }
        }

        public void Act6Process()
        {
            if (Act6Zenas.Percentage >= 10000 && Act6Zenas.Mode == 0)
            {
                IsAct6RaidZenas = true;
            }

            if (Act6Erenia.Percentage >= 10000 && Act6Erenia.Mode == 0)
            {
                IsAct6RaidErenia = true;
            }

            if (Act6Erenia.CurrentTime <= 0 && Act6Erenia.Mode != 0)
            {
                IsAct6RaidErenia = false;
            }

            if (Act6Zenas.CurrentTime <= 0 && Act6Zenas.Mode != 0)
            {
                IsAct6RaidZenas = false;
            }

            Parallel.ForEach(Sessions.Where(s => s?.Character != null && s.CurrentMapInstance?.Map.MapTypes.Any(m => m.MapTypeId == (byte)MapTypeEnum.Act61) == true), sess => sess.Character.GenerateAct6Async());

        }

        public void AddGroup(Group group)
        {
            ThreadSafeGroupList[group.GroupId] = group;
        }

        public IEnumerable<ClientSession> FindSameIpAddresses(List<ClientSession> sessions)
        {
            return sessions.Where(session => sessions.Count(s => s.ParsedAddress == session.ParsedAddress) > 2);
        }

        public void AskPvpRevive(long characterId)
        {
            var session = GetSessionByCharacterId(characterId);
            if (session?.HasSelectedCharacter == true)
            {
                if (session.Character.IsVehicled)
                {
                    session.Character.RemoveVehicle();
                }

                session.Character.DisableBuffs(BuffType.All);
                session.Character.BattleEntity.AdditionalHp = 0;
                session.Character.BattleEntity.AdditionalMp = 0;
                session.SendPacket(session.Character.GenerateAdditionalHpMp());
                session.SendPacket(session.Character.GenerateStat());
                session.SendPacket(session.Character.GenerateCond());
                session.SendPackets(UserInterfaceHelper.GenerateVb());

                session.Character.BattleEntity.RemoveOwnedMonsters();

                switch (session.CurrentMapInstance.MapInstanceType)
                {
                    case MapInstanceType.TalentArenaMapInstance:
                        var team = Instance.ArenaTeams.ToList().FirstOrDefault(s => s.Any(o => o.Session == session));
                        var member = team?.FirstOrDefault(s => s.Session == session);
                        if (member != null)
                        {
                            if (member.LastSummoned == null && team.OrderBy(tm3 => tm3.Order)
                                    .FirstOrDefault(tm3 => tm3.ArenaTeamType == member.ArenaTeamType && !tm3.Dead)
                                    ?.Session == session)
                            {
                                session.CurrentMapInstance.InstanceBag.DeadList.Add(session.Character.CharacterId);
                                member.Dead = true;
                                team.ToList().Where(s => s.LastSummoned != null).ToList().ForEach(s =>
                                {
                                    s.LastSummoned = null;
                                    s.Session.Character.PositionX = s.ArenaTeamType == ArenaTeamType.ERENIA
                                        ? (short)120
                                        : (short)19;
                                    s.Session.Character.PositionY = s.ArenaTeamType == ArenaTeamType.ERENIA
                                        ? (short)39
                                        : (short)40;
                                    session.CurrentMapInstance.Broadcast(s.Session.Character.GenerateTp());
                                    s.Session.SendPacket(
                                        UserInterfaceHelper.Instance.GenerateTaSt(TalentArenaOptionType.Watch));

                                    var bufftodisable = new List<BuffType> { BuffType.Bad };
                                    s.Session.Character.DisableBuffs(bufftodisable);
                                    s.Session.Character.Hp = (int)s.Session.Character.HPLoad();
                                    s.Session.Character.Mp = (int)s.Session.Character.MPLoad();
                                });
                                var killer = team.OrderBy(s => s.Order)
                                    .FirstOrDefault(s => !s.Dead && s.ArenaTeamType != member.ArenaTeamType);
                                session.CurrentMapInstance.Broadcast(session.Character.GenerateSay(
                                    string.Format(Language.Instance.GetMessageFromKey("TEAM_WINNER_ARENA_ROUND"),
                                        killer?.Session.Character.Name, killer?.ArenaTeamType), 10));
                                session.CurrentMapInstance.Broadcast(UserInterfaceHelper.GenerateMsg(
                                    string.Format(Language.Instance.GetMessageFromKey("TEAM_WINNER_ARENA_ROUND"),
                                        killer?.Session.Character.Name, killer?.ArenaTeamType), 0));
                                session.CurrentMapInstance.Sessions
                                    .Except(team.Where(s => s.ArenaTeamType == killer?.ArenaTeamType)
                                        .Select(s => s.Session)).ToList().ForEach(o =>
                                        {
                                            if (killer?.ArenaTeamType == ArenaTeamType.ERENIA)
                                            {
                                                o.SendPacket(killer.Session.Character.GenerateTaM(2));
                                                o.SendPacket(killer.Session.Character.GenerateTaP(2, true));
                                            }
                                            else
                                            {
                                                o.SendPacket(member.Session.Character.GenerateTaM(2));
                                                o.SendPacket(member.Session.Character.GenerateTaP(2, true));
                                            }

                                            o.SendPacket($"taw_d {member.Session.Character.CharacterId}");
                                            o.SendPacket(member.Session.Character.GenerateSay(
                                                string.Format(Language.Instance.GetMessageFromKey("WINNER_ARENA_ROUND"),
                                                    killer?.Session.Character.Name /*, killer?.ArenaTeamType*/,
                                                    member.Session.Character.Name), 10));
                                            o.SendPacket(UserInterfaceHelper.GenerateMsg(
                                                string.Format(Language.Instance.GetMessageFromKey("WINNER_ARENA_ROUND"),
                                                    killer?.Session.Character.Name /*, killer?.ArenaTeamType*/,
                                                    member.Session.Character.Name), 0));
                                        });
                                team.Replace(friends => friends.ArenaTeamType == member.ArenaTeamType).ToList()
                                    .ForEach(friends =>
                                    {
                                        friends.Session.SendPacket(friends.Session.Character.GenerateTaFc(0));
                                    });
                            }
                            else
                            {
                                member.LastSummoned = null;
                                var tm = team.OrderBy(tm3 => tm3.Order).FirstOrDefault(tm3 =>
                                    tm3.ArenaTeamType == member.ArenaTeamType && !tm3.Dead);
                                team.Replace(friends => friends.ArenaTeamType == member.ArenaTeamType).ToList()
                                    .ForEach(friends =>
                                    {
                                        friends.Session.SendPacket(tm.Session.Character.GenerateTaFc(0));
                                    });
                            }

                            team.ToList().ForEach(arenauser =>
                            {
                                if (arenauser?.Session?.Character != null)
                                {
                                    arenauser.Session.SendPacket(arenauser.Session.Character.GenerateTaP(2, true));
                                    arenauser.Session.SendPacket(arenauser.Session.Character.GenerateTaM(2));
                                }
                            });

                            Observable.Timer(TimeSpan.FromSeconds(3)).Subscribe(s =>
                            {
                                if (member != null && member?.Session != null && member?.Session?.Character != null && member?.Session?.CurrentMapInstance != null)
                                {
                                    member.Session.Character.PositionX = member.ArenaTeamType == ArenaTeamType.ERENIA
                                        ? (short)120
                                        : (short)19;
                                    member.Session.Character.PositionY = member.ArenaTeamType == ArenaTeamType.ERENIA
                                        ? (short)39
                                        : (short)40;
                                    member.Session.CurrentMapInstance.Broadcast(member.Session,
                                        member.Session.Character.GenerateTp());
                                    member.Session.SendPacket(
                                        UserInterfaceHelper.Instance.GenerateTaSt(TalentArenaOptionType.Watch));
                                }
                            });

                            Observable.Timer(TimeSpan.FromSeconds(4)).Subscribe(s =>
                            {
                                if (session != null)
                                {
                                    session.Character.Hp = (int)session.Character.HPLoad();
                                    session.Character.Mp = (int)session.Character.MPLoad();
                                    session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateRevive());
                                    session.SendPacket(session.Character.GenerateStat());
                                }
                            });
                        }

                        break;

                    case MapInstanceType.RainbowBattleInstance:
                        var rbb = Instance.RainbowBattleMembers.Find(s => s.Session.Contains(session));
                        if (rbb == null) return;
                        session.Character.PositionX = rbb.TeamEntity == RainbowTeamBattleType.Red ? RandomNumber<short>(30, 34) : RandomNumber<short>(83, 87);
                        session.Character.PositionY = rbb.TeamEntity == RainbowTeamBattleType.Red ? RandomNumber<short>(73, 77) : RandomNumber<short>(2, 6);


                        Observable.Timer(TimeSpan.FromSeconds(4)).Subscribe(s =>
                        {
                            if (session != null)
                            {
                                session.CurrentMapInstance.Broadcast(session.Character.GenerateTp());
                                session.Character.Hp = (int)session.Character.HPLoad();
                                session.Character.Mp = (int)session.Character.MPLoad();
                                session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateRevive());
                                session.SendPacket(session.Character.GenerateStat());
                            }
                        });
                        break;

                    default:
                        if (session.CurrentMapInstance == ArenaInstance ||
                            session.CurrentMapInstance == FamilyArenaInstance)
                        {
                            session.Character.LeaveTalentArena(true);
                            session.SendPacket(UserInterfaceHelper.GenerateDialog(
                                $"#revival^2 #revival^1 {Language.Instance.GetMessageFromKey("ASK_REVIVE_PVP")}"));
                            Task.Factory.StartNew(async () =>
                            {
                                var revive = true;
                                for (var i = 1; i <= 30; i++)
                                {
                                    await Task.Delay(1000);
                                    if (session.Character.Hp <= 0)
                                    {
                                        continue;
                                    }

                                    revive = false;
                                    break;
                                }

                                if (revive)
                                {
                                    ReviveTask(session);
                                }
                            });
                        }
                        else
                        {
                            AskRevive(characterId);
                        }

                        break;
                }
            }
        }

        // PacketHandler -> with Callback?
        public void AskRevive(long characterId)
        {
            var session = GetSessionByCharacterId(characterId);
            if (session?.HasSelectedCharacter == true && session.HasCurrentMapInstance)
            {
                if (session.Character.IsVehicled)
                {
                    session.Character.RemoveVehicle();
                }

                session.Character.ClearLaurena();
                session.Character.DisableBuffs(BuffType.All);
                session.Character.BattleEntity.AdditionalHp = 0;
                session.Character.BattleEntity.AdditionalMp = 0;
                session.SendPacket(session.Character.GenerateAdditionalHpMp());
                session.SendPacket(session.Character.GenerateStat());
                session.SendPacket(session.Character.GenerateCond());
                session.SendPackets(UserInterfaceHelper.GenerateVb());

                switch (session.CurrentMapInstance.MapInstanceType)
                {
                    case MapInstanceType.BaseMapInstance:
                        if (session.Character.Level > 20 && ChannelId != 51)
                        {

                            session.Character.Dignity -= (short)(session.Character.Level < 50 ? session.Character.Level : 50);

                            if (session.Character.Dignity < -1000)
                            {
                                session.Character.Dignity = -1000;
                                session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("LOSE_DIGNITY"),
                                (short)(session.Character.Level < 50 ? session.Character.Level : 50)), 11));
                            }

                            session.SendPacket(session.Character.GenerateFd());
                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);

                            session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateGidx(), ReceiverType.AllExceptMe);

                        }

                        session.SendPacket(UserInterfaceHelper.GenerateDialog(
                            $"#revival^0 #revival^1 {(session.Character.Level > 20 ? Language.Instance.GetMessageFromKey("ASK_REVIVE") : Language.Instance.GetMessageFromKey("ASK_REVIVE_FREE"))}"));
                        ReviveTask(session);
                        break;

                    case MapInstanceType.TimeSpaceInstance:
                        lock (session.CurrentMapInstance.InstanceBag.DeadList)
                        {
                            if (session.CurrentMapInstance.InstanceBag.Lives - session.CurrentMapInstance.InstanceBag
                                    .DeadList.ToList().Count(s => s == session.Character.CharacterId) < 0)
                            {
                                session.Character.Hp = 1;
                                session.Character.Mp = 1;
                            }
                            else
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(
                                    string.Format(Language.Instance.GetMessageFromKey("YOU_HAVE_LIFE"),
                                        session.CurrentMapInstance.InstanceBag.Lives -
                                        session.CurrentMapInstance.InstanceBag.DeadList.Count(e =>
                                            e == session.Character.CharacterId)), 0));
                                session.SendPacket(UserInterfaceHelper.GenerateDialog(
                                    $"#revival^1 #revival^1 {Language.Instance.GetMessageFromKey("ASK_REVIVE_TS")}"));
                                ReviveTask(session);
                            }
                        }

                        break;

                    case MapInstanceType.RaidInstance:
                        var save = session.CurrentMapInstance.InstanceBag.DeadList.ToList();
                        if (session.CurrentMapInstance.InstanceBag.Lives - save.Count < 0)
                        {
                            session.Character.Hp = 1;
                            session.Character.Mp = 1;
                            session.Character.Group?.Raid.End();
                        }
                        else if (3 - save.Count(s => s == session.Character.CharacterId) > 0)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("YOU_HAVE_LIFE"), 2 - session.CurrentMapInstance.InstanceBag.DeadList.Count(s => s == session.Character.CharacterId))));

                            session.Character.Group?.Sessions.ForEach(grpSession =>
                            {
                                grpSession?.SendPacket(grpSession.Character.Group?.GeneraterRaidmbf(grpSession));
                                grpSession?.SendPacket(grpSession.Character.Group?.GenerateRdlst());
                            });
                            Task.Factory.StartNew(async () =>
                            {
                                await Task.Delay(20000).ConfigureAwait(false);
                                Instance.ReviveFirstPosition(session.Character.CharacterId);
                            });
                        }
                        else
                        {
                            var grp = session.Character?.Group;
                            session.Character.Hp = 1;
                            session.Character.Mp = 1;
                            ChangeMap(session.Character.CharacterId, session.Character.MapId, session.Character.MapX,
                                session.Character.MapY);
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("KICK_RAID"), 0));
                            if (grp != null)
                            {
                                grp.LeaveGroup(session);
                                grp.Sessions.ForEach(s =>
                                {
                                    s.SendPacket(grp.GenerateRdlst());
                                    s.SendPacket(s.Character.Group?.GeneraterRaidmbf(s));
                                    s.SendPacket(s.Character.GenerateRaid(0));
                                });
                            }

                            session.SendPacket(session.Character.GenerateRaid(1, true));
                            session.SendPacket(session.Character.GenerateRaid(2, true));
                        }

                        break;

                    case MapInstanceType.SealedVesselsMap:
                        if (session.Character.MapId == 9999)
                        {
                            Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(s =>

                            session.SendPacket(UserInterfaceHelper.GenerateInfo("INFO: You can only use sealed vessels in this map.")));

                        }
                        break;

                    case MapInstanceType.LodInstance:
                        const int saver = 1211;
                        if (session.Character.Inventory.CountItem(saver) >= 1)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateDialog($"#revival^0 #revival^1 {Language.Instance.GetMessageFromKey("ASK_REVIVE_LOD")}"));
                            ReviveTask(session);
                        }

                        else
                        {

                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NOT_ENOUGH_SAVER"), 0));
                            Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(o => Instance.ReviveFirstPosition(session.Character.CharacterId));

                        }
                        break;

                    case MapInstanceType.CustomInstance:
                        const int seedOfPower = 1012;
                        if (session.Character.Inventory.CountItem(seedOfPower) >= 5)
                        {
                            session.SendPacket(UserInterfaceHelper.GenerateDialog($"#revival^0 #revival^1 Do you want to Revive? It will cost 5 Seed of Power"));
                            ReviveTask(session);
                        }
                        else
                        {
                            session.SendPacket("msg 4 Not enough Seed of Power");
                            Observable.Timer(TimeSpan.FromSeconds(5)).Subscribe(o => Instance.ReviveFirstPosition(session.Character.CharacterId));
                        }
                        break;

                    case MapInstanceType.Act4Berios:
                    case MapInstanceType.Act4Calvina:
                    case MapInstanceType.Act4Hatus:
                    case MapInstanceType.Act4Morcos:
                        session.SendPacket(UserInterfaceHelper.GenerateDialog(
                            $"#revival^0 #revival^1 {string.Format(Language.Instance.GetMessageFromKey("ASK_REVIVE_Act4Raid"), session.Character.Level * 10)}"));
                        ReviveTask(session);
                        break;

                    case MapInstanceType.CaligorInstance:
                        session.SendPacket(UserInterfaceHelper.GenerateDialog(
                            $"#revival^0 #revival^1 {Language.Instance.GetMessageFromKey("ASK_REVIVE_CALIGOR")}"));
                        ReviveTask(session);
                        break;

                    default:
                        Instance.ReviveFirstPosition(session.Character.CharacterId);
                        break;
                }
            }
        }

        public async void AutoReboot()
        {
            foreach (var session in ServerManager.Instance.Sessions)
            {
                //MessageExtension.SendHeader(session, "Hello Frostvein Player\n\nThe Server will perform an Auto-Reboot in 5 Minutes");
                //int sleepDuration = 5 * 60 * 1000;
                //Thread.Sleep(sleepDuration);
                MessageExtension.SendModal(session, GameConfiguration.RebootMessage);
            }
            for (var i = 0; i < 30; i++)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (Instance.ShutdownStop)
                {
                    Instance.ShutdownStop = false;
                    return;
                }
            }
            foreach (var session in ServerManager.Instance.Sessions)
            {
                MessageExtension.SendModal(session, GameConfiguration.RebootShutdownMessage);
            }
            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (Instance.ShutdownStop)
                {
                    Instance.ShutdownStop = false;
                    return;
                }
            }

            InShutdown = true;
            await Instance.SaveAll();
            CommunicationServiceClient.Instance.UnregisterWorldServer(WorldId);
            if (IsReboot)
            {
                if (ChannelId == 51)
                {
                    await Task.Delay(16000).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay((ChannelId - 1) * 2000).ConfigureAwait(false);
                }

                Process.Start("Frostvein.World.exe",
                    $"--nomsg{(ChannelId == 51 ? $" --port {Convert.ToInt32(ServerConfiguration.GlacernonServerPort)}" : "")}");
            }

            Environment.Exit(0);
        }

        public void BaazarMaintenance()
        {
            if (!IsBazaarMaintenance)
            {
                IsBazaarMaintenance = true;
            }
            else
            {
                IsBazaarMaintenance = false;
            }
        }

        public void ChangeMap(long id, short? mapId = null, short? mapX = null, short? mapY = null)
        {
            var session = GetSessionByCharacterId(id);
            if (session?.Character != null)
            {
                if (mapId != null)
                {
                    session.Character.MapInstanceId = GetBaseMapInstanceIdByMapId((short)mapId);
                }

                ChangeMapInstance(id, session.Character.MapInstanceId, mapX, mapY);
            }
        }

        public void ChangeMapInstance(long characterId, Guid mapInstanceId, int? mapX = null, int? mapY = null, bool noAggroLoss = false)
        {
            var session = GetSessionByCharacterId(characterId);
            if (session?.Character != null && !session.Character.IsChangingMapInstance)
            {
                session.Character.IsChangingMapInstance = true;

                session.Character.RemoveBuff(620);

                session.Character.WalkDisposable?.Dispose();
                SpinWait.SpinUntil(
                    () => session.Character.LastSkillUse.AddMilliseconds(500) <= DateTime.Now);
                try
                {
                    var gotoMapInstance = GetMapInstance(mapInstanceId);


                    session.SendPacket(StaticPacketHelper.Cancel(2, characterId));

                    if (session.Character.InExchangeOrTrade)
                    {
                        session.Character.CloseExchangeOrTrade();
                    }

                    if (session.Character.HasShopOpened)
                    {
                        session.Character.CloseShop();
                    }

                    session.Character.BattleEntity.ClearOwnFalcon();
                    session.Character.BattleEntity.ClearEnemyFalcon();
                    if (!noAggroLoss)
                    {
                        session.CurrentMapInstance.RemoveMonstersTarget(session.Character.CharacterId);
                    }
                    session.Character.BattleEntity.RemoveOwnedMonsters();

                    if (gotoMapInstance != null)
                    {
                        if (gotoMapInstance.MapInstanceType.Equals(MapInstanceType.Act4Instance))
                        {
                            if (gotoMapInstance.Map.MapId == 151 || gotoMapInstance.Map.MapId == 152)
                            {
                                Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(s =>
                                session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("ACT4_MINING_TIP"), 0)));
                            }
                        }
                    }


                    static void RemoveAllPetInTeam(ClientSession session)
                    {
                        foreach (var mate in session.Character.Mates?.Where(s => s.IsTeamMember))
                        {
                            mate.RemoveTeamMember();
                            if (mate.Owner.UseSp)
                            {
                                mate.RemovePartnerSkills(true);
                            }
                            else
                            {
                                mate.RemovePartnerSkills();
                            }
                        }
                    }

                    if (gotoMapInstance.MapInstanceType.Equals(MapInstanceType.ArenaInstance))
                    {
                        Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(s =>
                        MessageExtension.SendBubble(session, "Your Buffs have been lifted"));

                        if (gotoMapInstance.Map.MapId == 2006 || gotoMapInstance.Map.MapId == 2106)
                        {
                            //disable a4 pills on enter
                            session.Character.RemoveBuff(170);
                            session.Character.RemoveBuff(171);
                            session.Character.RemoveBuff(172);
                            session.Character.RemoveBuff(263);
                            session.Character.RemoveBuff(264);
                            session.Character.RemoveBuff(265);
                            session.Character.RemoveBuff(266);
                            session.Character.DisableBuffs(BuffType.All);
                            session.Character.ChargeValue = 0;
                            session.Character.IsCustomSpeed = false;
                            session.Character.LoadSpeed();
                            session.SendPacket(session.Character.GenerateCond());
                        }
                    }

                    if (mapX <= 0 && mapY <= 0)
                    {
                        switch (session.Character.Faction)
                        {
                            case FactionType.Angel:
                                mapX = 58;
                                mapY = 164;
                                break;

                            case FactionType.Demon:
                                mapX = 121;
                                mapY = 164;
                                break;
                        }
                    }

                    session.CurrentMapInstance.UnregisterSession(session.Character.CharacterId);
                    LeaveMap(session.Character.CharacterId);

                    // cleanup sending queue to avoid sending uneccessary packets to it
                    session.ClearLowPriorityQueue();

                    session.Character.IsSitting = false;
                    session.Character.MapInstanceId = mapInstanceId;
                    session.CurrentMapInstance = session.Character.MapInstance;

                    if (!session.Character.MapInstance.MapInstanceType.Equals(MapInstanceType.TimeSpaceInstance) && session.Character.Timespace != null)

                    {
                        session.Character.TimespaceRewardGotten = false;
                        session.Character.RemoveTemporalMates();
                        if (session.Character.Timespace.SpNeeded?[(byte)session.Character.Class] != 0)
                        {
                            var specialist = session.Character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);


                            if (specialist != null || specialist.ItemVNum == session.Character.Timespace.SpNeeded?[(byte)session.Character.Class])

                            {
                                Observable.Timer(TimeSpan.FromMilliseconds(300)).Subscribe(s => session.Character.RemoveSp(specialist.ItemVNum, true));

                            }
                        }

                        session.Character.Timespace = null;
                    }

                    if (session.Character.Hp <= 0 && !session.Character.IsSeal)
                    {
                        session.Character.Hp = 1;
                        session.Character.Mp = 1;
                    }

                    session.Character.LeaveTalentArena();

                    if (session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                    {
                        session.Character.MapId = session.Character.MapInstance.Map.MapId;
                        if (mapX != null && mapY != null)
                        {
                            session.Character.MapX = (short)mapX.Value;
                            session.Character.MapY = (short)mapY.Value;
                        }
                    }

                    if (mapX != null && mapY != null)
                    {
                        session.Character.PositionX = (short)mapX.Value;
                        session.Character.PositionY = (short)mapY.Value;
                    }

                    foreach (var mate in session.Character.Mates?.Where(m =>
                        m.IsTeamMember && !session.Character.IsVehicled || m.IsTemporalMate))
                    {
                        mate.PositionX =
                            (short)(session.Character.PositionX +
                                     (mate.MateType == MateType.Partner ? -1 : 1));
                        mate.PositionY = (short)(session.Character.PositionY + 1);
                        if (session.Character.MapInstance.Map.IsBlockedZone(mate.PositionX, mate.PositionY))
                        {
                            mate.PositionX = session.Character.PositionX;
                            mate.PositionY = session.Character.PositionY;
                        }

                        mate.UpdateBushFire();
                    }

                    session.Character.UpdateBushFire();
                    session.CurrentMapInstance.RegisterSession(session);
                    session.Character.LoadSpeed();

                    if (gotoMapInstance.Map?.MapId != 2514)
                    {
                        session.Character.ClearLaurena();
                    }

                    session.SendPacket(session.Character.GenerateCInfo());
                    session.SendPacket(session.Character.GenerateCMode());
                    session.SendPacket(session.Character.GenerateEq());
                    session.SendPacket(session.Character.GenerateLev());
                    session.SendPacket(session.Character.GenerateStat());
                    session.SendPacket(session.Character.GenerateAt());
                    session.SendPacket(session.Character.GenerateCond());
                    session.SendPacket(session.Character.GenerateCMap());
                    session.SendPackets(session.Character.GenerateStatChar());
                    session.SendPacket(session.Character.GeneratePairy());
                    session.Character.GenerateAct6Async();
                    session.CurrentMapInstance.Broadcast(session.Character.GenerateTitInfo());
                    session.SendPacket(Character.GenerateAct());
                    session.SendPacket(session.Character.GenerateScpStc());
                    Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(s => { session.SendPacket(FamilySystemExtensions.GenerateFmp(session)); });
                    Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(s => { session.SendPacket(FamilySystemExtensions.GenerateFmi(session)); });
                    Observable.Timer(TimeSpan.FromSeconds(1)).Subscribe(s => { session.SendPacket(session.Character.GenerateEquipment()); });

                    //Recast the Status so it doesnt get lost
                    if (session.Character.SetStatus)
                    {
                        string message = session.Character.StatusMessage;
                        StatusExtension.GenerateStatus(session, message);
                    }

                    MiniPetExtension.GenerateMiniPet(session);

                    if (session.CurrentMapInstance.OnSpawnEvents.Any())
                    {
                        session.CurrentMapInstance.OnSpawnEvents.ForEach(e =>
                                EventHelper.Instance.RunEvent(e, session));
                    }

                    if (ChannelId == 51)
                    {
                        session.SendPacket(session.Character.GenerateFc());

                        if (mapInstanceId == session.Character.Family?.Act4Raid?.MapInstanceId ||
                            mapInstanceId == session.Character.Family?.Act4RaidBossMap?.MapInstanceId)
                        {
                            session.SendPacket(session.Character.GenerateDG());
                        }
                    }

                    if (session.Character.Group?.Raid?.InstanceBag?.Lock == true)
                    {
                        session.SendPacket(session.Character.Group.GeneraterRaidmbf(session));

                        if (session.CurrentMapInstance.Monsters.Any(s => s.IsBoss))
                        {
                            session.Character.Group.Sessions?.Where(s => s?.Character != null).ForEach(s =>
                            {
                                if (!s.Character.IsChangingMapInstance &&
                                    s.CurrentMapInstance != session.CurrentMapInstance)
                                {
                                    ChangeMapInstance(s.Character.CharacterId,
                                            session.CurrentMapInstance.MapInstanceId, mapX, mapY);
                                }
                            });
                        }
                    }

                    if (session.Character.MapInstance == session.Character.Family?.Act4RaidBossMap)
                    {
                        session.Character.Family.Act4Raid.Sessions
                               .Where(s => !s.Character.IsChangingMapInstance).ToList().ForEach(s =>
                               {
                                   ChangeMapInstance(s.Character.CharacterId,
                                           session.CurrentMapInstance.MapInstanceId, mapX, mapY);
                               });
                    }

                    foreach (var visibleSession in session.CurrentMapInstance.Sessions.Where(s =>
                                                                   s.Character?.InvisibleGm == false &&
                                                                   s.Character.CharacterId != session.Character.CharacterId))
                    {
                        if (ChannelId != 51 ||
                            session.Character.Faction == visibleSession.Character.Faction)
                        {
                            session.SendPacket(visibleSession.Character.GenerateIn());
                            session.SendPacket(visibleSession.Character.GenerateGidx());
                            visibleSession.Character.Mates?
                                          .Where(m => (m.IsTeamMember || m.IsTemporalMate) &&
                                                      m.CharacterId != session.Character.CharacterId)
                                          .ToList().ForEach(m => session.SendPacket(m.GenerateIn()));
                        }
                        else
                        {
                            session.SendPacket(
                                    visibleSession.Character.GenerateIn(true, session.Account.Authority));
                            visibleSession.Character.Mates?
                                          .Where(m => (m.IsTeamMember || m.IsTemporalMate) &&
                                                      m.CharacterId != session.Character.CharacterId)
                                          .ToList().ForEach(m =>
                                                  session.SendPacket(m.GenerateIn(true, ChannelId == 51,
                                                          session.Account.Authority)));
                        }
                    }

                    session.SendPacket(session.CurrentMapInstance.GenerateMapDesignObjects());
                    session.SendPackets(session.CurrentMapInstance.GetMapDesignObjectEffects());


                    foreach (var instance in session.CurrentMapInstance.ScriptedInstances.Where(x => x.Type == ScriptedInstanceType.TimeSpace))
                    {
                        var getTs = TimespaceLogs.Find(s => s.ScriptedInstanceId == instance.ScriptedInstanceId && s.CharacterId == session.Character.CharacterId && !s.IsFailed);
                        bool isTsFound = getTs != null;
                        var isHeroTimeSpace = instance.DefaultTimeSpaceType == WpPortalType.HeroTs;
                        session.SendPacket(instance.GenerateWp(isHeroTimeSpace && isTsFound ? WpPortalType.HeroTsDone : !isHeroTimeSpace && isTsFound ? WpPortalType.NormalTsDone : instance.DefaultTimeSpaceType));
                    }


                    session.SendPackets(session.CurrentMapInstance.GetMapItems());

                    MapInstancePortalHandler
                        .GenerateMinilandEntryPortals(session.CurrentMapInstance.Map.MapId,
                            session.Character.Miniland.MapInstanceId)
                        .ForEach(p => session.SendPacket(p.GenerateGp()));
                    MapInstancePortalHandler.GenerateAct4EntryPortals(session.CurrentMapInstance.Map.MapId)
                        .ForEach(p => session.SendPacket(p.GenerateGp()));

                    if (session.CurrentMapInstance.InstanceBag?.Clock?.Enabled == true)
                    {
                        session.SendPacket(session.CurrentMapInstance.InstanceBag.Clock.GetClock());
                    }

                    if (session.CurrentMapInstance.Clock.Enabled)
                    {
                        session.SendPacket(session.CurrentMapInstance.Clock.GetClock());
                    }

                    // TODO: fix this
                    if (session.Character.MapInstance.Map.MapTypes.Any(m =>
                        m.MapTypeId == (short)MapTypeEnum.CleftOfDarkness))
                    {
                        session.SendPacket("bc 0 0 0");
                    }
                    if (!session.Character.InvisibleGm)
                    {
                        foreach (var s in session.CurrentMapInstance.Sessions.Where(
                                s => s.Character != null))
                        {
                            if (ChannelId != 51 || session.Character.Faction == s.Character.Faction)
                            {
                                s.SendPacket(session.Character.GenerateIn());
                                s.SendPacket(session.Character.GenerateGidx());
                                session.Character.Mates?.Where(m => m.IsTeamMember || m.IsTemporalMate)
                                       .ToList().ForEach(m =>
                                               s.SendPacket(m.GenerateIn(false, ChannelId == 51)));
                            }
                            else
                            {
                                s.SendPacket(session.Character.GenerateIn(true, s.Account.Authority));
                                session.Character.Mates?.Where(m => m.IsTeamMember || m.IsTemporalMate)
                                       .ToList()
                                       .ForEach(m =>
                                               s.SendPacket(m.GenerateIn(true, ChannelId == 51,
                                                       s.Account.Authority)));
                            }

                            if (session.Character.GetBuff(BCardType.CardType.SpecialEffects,
                                                (byte)AdditionalTypes.SpecialEffects.ShadowAppears) is int[]
                                        EffectData && EffectData[0] != 0 &&
                                EffectData[1] != 0)
                            {
                                s.CurrentMapInstance.Broadcast(
                                        $"guri 0 {(short)UserType.Player} {session.Character.CharacterId} {EffectData[0]} {EffectData[1]}");
                            }

                            session.Character.Mates?.Where(m => m.IsTeamMember || m.IsTemporalMate).ToList()
                                   .ForEach(m =>
                                   {
                                       if (session.Character.IsVehicled)
                                       {
                                           m.PositionX = session.Character.PositionX;
                                           m.PositionY = session.Character.PositionY;
                                       }

                                       if (m.GetBuff(BCardType.CardType.SpecialEffects,
                                                           (byte)AdditionalTypes.SpecialEffects.ShadowAppears) is int[]
                                                   MateEffectData && MateEffectData[0] != 0 &&
                                           MateEffectData[1] != 0)
                                       {
                                           s.CurrentMapInstance.Broadcast(
                                                   $"guri 0 {(short)UserType.Monster} {m.MateTransportId} {MateEffectData[0]} {MateEffectData[1]}");
                                       }
                                   });
                        }
                    }

                    session.SendPacket(session.Character.GeneratePinit());

                    if (session.Character.Mates?.FirstOrDefault(s =>
                        (s.IsTeamMember || s.IsTemporalMate) && s.MateType == MateType.Partner &&
                        s.IsUsingSp) is Mate partner)
                    {
                        session.SendPacket(partner.Sp.GeneratePski());
                    }

                    session.Character.Mates?.ForEach(s => session.SendPacket(s.GenerateScPacket()));
                    session.SendPackets(session.Character.GeneratePst());

                    if (session.Character.Size != 10)
                    {
                        session.SendPacket(session.Character.GenerateScal());
                    }

                    if (session.CurrentMapInstance?.IsDancing == true && !session.Character.IsDancing)
                    {
                        session.CurrentMapInstance?.Broadcast("dance 2");
                    }
                    else if (session.CurrentMapInstance?.IsDancing == false && session.Character.IsDancing)
                    {
                        session.Character.IsDancing = false;
                        session.CurrentMapInstance?.Broadcast("dance");
                    }

                    if (Groups != null)
                    {
                        foreach (var group in Groups)
                            foreach (var groupSession in @group.Sessions.GetAllItems())
                            {
                                var groupCharacterSession = Sessions.FirstOrDefault(s =>
                                        s.Character != null &&
                                        s.Character.CharacterId == groupSession.Character.CharacterId &&
                                        s.CurrentMapInstance == groupSession.CurrentMapInstance);

                                if (groupCharacterSession == null)
                                {
                                    continue;
                                }

                                groupSession.SendPacket(groupSession.Character.GeneratePinit());
                                groupSession.SendPackets(groupSession.Character.GeneratePst());
                            }
                    }

                    if (session.Character.Group?.GroupType == GroupType.Group)
                    {
                        session.CurrentMapInstance?.Broadcast(session, session.Character.GeneratePidx(), ReceiverType.AllExceptMe);

                    }

                    session.SendPacket(session.Character.GenerateMinimapPosition());
                    session.CurrentMapInstance.OnCharacterDiscoveringMapEvents.ForEach(e =>
                    {
                        if (!e.Item2.Contains(session.Character.CharacterId))
                        {
                            e.Item2.Add(session.Character.CharacterId);
                            EventHelper.Instance.RunEvent(e.Item1, session);
                        }
                    });
                    session.CurrentMapInstance.OnCharacterDiscoveringMapEvents = session.CurrentMapInstance
                        .OnCharacterDiscoveringMapEvents
                        .Where(s => s.Item1.EventActionType == EventActionType.SENDPACKET).ToList();
                    session.Character.LeaveIceBreaker();

                    session.Character.IsChangingMapInstance = false;
                }
                catch (Exception ex)
                {
                    Logger.Warn("Character changed while changing map. Do not abuse " +
                        "Commands.", ex);
                    session.Character.IsChangingMapInstance = false;
                }
            }
        }

        public void FamilyRefresh(long familyId, bool changeFaction = false)
        {
            CommunicationServiceClient.Instance.UpdateFamily(ServerGroup, familyId, changeFaction);
        }

        public List<Recipe> GetAllRecipes() => _recipes.GetAllItems();

        public Family GetBestFamily(bool isLevel)
        {
            if (isLevel)
            {
                return FamilyList.GetAllItems().OrderByDescending(
                        s => s.FamilyLevel).ToList().FirstOrDefault();
            }

            return FamilyList.GetAllItems().OrderByDescending(
                                      s => s.FamilyExperience).ToList().FirstOrDefault();
        }

        public Card GetCardByCardId(short cardId)
        {
            return Cards.Find(s => s.CardId == cardId);
        }

        public List<DropDTO> GetDropsByMonsterVNum(short monsterVNum) => _monsterDrops.ContainsKey(monsterVNum)
                ? _generalDrops.Concat(_monsterDrops[monsterVNum]).ToList()
                : _generalDrops.ToList();

        public Group GetGroupByCharacterId(long characterId)
        {
            return Groups?.SingleOrDefault(g => g.IsMemberOfGroup(characterId));
        }

        public List<MapNpc> GetMapNpcsByVNum(short npcVNum)
        {
            return GetAllMapInstances().Where(mapInstance =>
                    mapInstance != null && !mapInstance.IsScriptedInstance)
                .SelectMany(mapInstance => mapInstance.Npcs.Where(mapNpc => mapNpc?.NpcVNum == npcVNum))
                .ToList();
        }

        public long GetNextGroupId() => ++_lastGroupId;

        public int GetNextMobId()
        {
            var maxMobId = 0;
            foreach (var map in _mapinstances.Values.ToList())
            {
                if (map.Monsters.Count > 0 && maxMobId < map.Monsters.Max(m => m.MapMonsterId))
                {
                    maxMobId = map.Monsters.Max(m => m.MapMonsterId);
                }
            }

            return ++maxMobId;
        }

        public int GetNextNpcId()
        {
            var mapNpcId = 0;
            foreach (var map in _mapinstances.Values.ToList())
            {
                if (map.Npcs.Count > 0 && mapNpcId < map.Npcs.Max(m => m.MapNpcId))
                {
                    mapNpcId = map.Npcs.Max(m => m.MapNpcId);
                }
            }

            return ++mapNpcId;
        }

        public NpcMonsterSkill GetNpcMonsterSkill(short skillVnum)
        {
            return _allMonsterSkills.FirstOrDefault(s => s.SkillVNum == skillVnum);
        }

        public Quest GetQuest(long questId)
        {
            return Quests.FirstOrDefault(m => m.QuestId.Equals(questId));
        }

        public List<Recipe> GetRecipesByItemVNum(short itemVNum)
        {
            var recipes = new List<Recipe>();
            foreach (var recipeList in _recipeLists.Where(r => r.ItemVNum == itemVNum))
            {
                recipes.Add(_recipes[recipeList.RecipeId]);
            }

            return recipes;
        }

        public List<Recipe> GetRecipesByMapNpcId(int mapNpcId)
        {
            var recipes = new List<Recipe>();
            foreach (var recipeList in _recipeLists.Where(r => r.MapNpcId == mapNpcId))
            {
                recipes.Add(_recipes[recipeList.RecipeId]);
            }

            return recipes;
        }

        public ClientSession GetSessionByCharacterName(string name)
        {
            return Sessions.SingleOrDefault(s => s.Character.Name == name);
        }

        public ClientSession GetSessionBySessionId(int sessionId)
        {
            return Sessions.SingleOrDefault(s => s.SessionId == sessionId);
        }

        public async void GroupLeave(ClientSession session)
        {
            if (Groups != null)
            {
                var grp = Instance.Groups.Find(s => s.IsMemberOfGroup(session.Character.CharacterId));
                if (grp != null)
                {
                    switch (grp.GroupType)
                    {
                        case GroupType.BigTeam:
                        case GroupType.GiantTeam:
                        case GroupType.Team:
                            if (grp.Raid?.InstanceBag.Lock == true)
                            {
                                grp.Raid.InstanceBag.DeadList.Add(session.Character.CharacterId);
                            }

                            if (grp.Sessions.ElementAt(0) == session && grp.SessionCount > 1)
                            {
                                Broadcast(session,
                                        UserInterfaceHelper.GenerateInfo(
                                                Language.Instance.GetMessageFromKey("NEW_LEADER")),
                                        ReceiverType.OnlySomeone, "",
                                        grp.Sessions.ElementAt(1)?.Character.CharacterId ?? 0);
                            }

                            grp.LeaveGroup(session);
                            session.SendPacket(session.Character.GenerateRaid(1, true));
                            session.SendPacket(session.Character.GenerateRaid(2, true));
                            foreach (var groupSession in grp.Sessions.GetAllItems())
                            {
                                groupSession.SendPacket(grp.GenerateRdlst());
                                groupSession.SendPacket(grp.GeneraterRaidmbf(groupSession));
                                groupSession.SendPacket(groupSession.Character.GenerateRaid(0));
                            }

                            if (session.CurrentMapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
                            {
                                ChangeMap(session.Character.CharacterId, session.Character.MapId,
                                        session.Character.MapX, session.Character.MapY);
                            }

                            session.SendPacket(
                                UserInterfaceHelper.GenerateMsg(
                                    Language.Instance.GetMessageFromKey("LEFT_RAID"), 0));
                            break;

                        /*case GroupType.GiantTeam:
                            ClientSession[] grpmembers = new ClientSession[40];
                            grp.Sessions.CopyTo(grpmembers);
                            foreach (ClientSession targetSession in grpmembers)
                            {
                                if (targetSession != null)
                                {
                                    targetSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GROUP_CLOSED"), 0));
                                    Broadcast(targetSession.Character.GeneratePidx(true));
                                    grp.LeaveGroup(targetSession);
                                    targetSession.SendPacket(targetSession.Character.GeneratePinit());
                                    targetSession.SendPackets(targetSession.Character.GeneratePst());
                                }
                            }
                            GroupList.RemoveAll(s => s.GroupId == grp.GroupId);
                            ThreadSafeGroupList.Remove(grp.GroupId);
                            break;*/

                        case GroupType.Group:
                            if (grp.Sessions.ElementAt(0) == session && grp.SessionCount > 1)
                            {
                                Broadcast(session, UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NEW_LEADER")),
                                        ReceiverType.OnlySomeone, "", grp.Sessions.ElementAt(1).Character.CharacterId);
                            }

                            grp.LeaveGroup(session);
                            if (grp.SessionCount == 1)
                            {
                                var targetSession = grp.Sessions.ElementAt(0);
                                if (targetSession != null)
                                {
                                    targetSession.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GROUP_CLOSED"), 0));
                                    Broadcast(targetSession.Character.GeneratePidx(true));
                                    grp.LeaveGroup(targetSession);
                                    targetSession.SendPacket(targetSession.Character.GeneratePinit());
                                    targetSession.SendPackets(targetSession.Character.GeneratePst());
                                    targetSession.Character.RemoveBuff(1247);
                                    session.Character.RemoveBuff(1247);
                                }
                            }
                            if (grp.SessionCount == 0)
                            {
                                session.Character.RemoveBuff(1247);
                                session.Character.RemoveBuff(1248);
                            }
                            else
                            {
                                foreach (var groupSession in grp.Sessions.GetAllItems())
                                {
                                    groupSession.SendPacket(groupSession.Character.GeneratePinit());
                                    groupSession.SendPackets(session.Character.GeneratePst());
                                    groupSession.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("LEAVE_GROUP"), session.Character.Name), 0));
                                    groupSession.Character.RemoveBuff(1248);
                                }
                            }

                            session.SendPacket(session.Character.GeneratePinit());
                            session.SendPackets(session.Character.GeneratePst());
                            Broadcast(session.Character.GeneratePidx(true));
                            session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("GROUP_LEFT"), 0));
                            BuffThread.RemoveGroupBuff(session);
                            break;

                        default:
                            return;
                    }

                    session.Character.Group = null;
                }
            }
        }



        public List<FishingInformationsDto> GetFishingSpot(short mapId, short mapX, short mapY)
        {
            var kvp = FishingSpots.FirstOrDefault(s => s.Key.MapId == mapId && s.Key.MapX == mapX && s.Key.MapY == mapY);

            if (kvp.Equals(default(KeyValuePair<FishingPositionDto, List<FishingInformationsDto>>)))
            {
                return null;
            }

            return kvp.Value;
        }

        public void Initialize()
        {
            LoadService.Load();
            WorldId = Guid.NewGuid();
        }

        public void SendItemToMail(long id, short vnum, short amount, sbyte rare, byte upgrade)
        {
            Item it = GetItem(vnum);

            if (it != null)
            {
                if (it.ItemType != ItemType.Weapon && it.ItemType != ItemType.Armor && it.ItemType != ItemType.Specialist)
                {
                    upgrade = 0;
                }
                else if (it.ItemType != ItemType.Weapon && it.ItemType != ItemType.Armor)
                {
                    rare = 0;
                }
                if (rare > 8 || rare < -2)
                {
                    rare = 0;
                }
                if (upgrade > 10 && it.ItemType != ItemType.Specialist)
                {
                    upgrade = 0;
                }
                else if (it.ItemType == ItemType.Specialist && upgrade > 15)
                {
                    upgrade = 0;
                }

                // maximum size of the amount is 99
                if (amount > 999)
                {
                    amount = 999;
                }

                MailDTO mail = new MailDTO
                {
                    AttachmentAmount = it.Type == InventoryType.Etc || it.Type == InventoryType.Main ? amount : (byte)1,
                    IsOpened = false,
                    Date = DateTime.UtcNow,
                    ReceiverId = id,
                    SenderId = id,
                    AttachmentRarity = (byte)rare,
                    AttachmentUpgrade = upgrade,
                    IsSenderCopy = false,
                    Title = "RankingReward",
                    AttachmentVNum = vnum,
                    SenderClass = ClassType.Adventurer,
                    SenderGender = GenderType.Male,
                    SenderHairColor = HairColorType.Black,
                    SenderHairStyle = HairStyleType.NoHair,
                    EqPacket = string.Empty,
                    SenderMorphId = 0
                };
                MailServiceClient.Instance.SendMail(mail);
            }
        }

        public bool IsAct4Online() => CommunicationServiceClient.Instance.IsAct4Online();

        public bool IsChannel1Online() => CommunicationServiceClient.Instance.IsChannel1Online(ServerGroup);
        public bool IsChannel2Online() => CommunicationServiceClient.Instance.IsChannel2Online(ServerGroup);
        public bool IsChannel3Online() => CommunicationServiceClient.Instance.IsChannel3Online(ServerGroup);
        public bool IsChannel4Online() => CommunicationServiceClient.Instance.IsChannel4Online(ServerGroup);
        public bool IsChannel5Online() => CommunicationServiceClient.Instance.IsChannel5Online(ServerGroup);
        public bool IsChannel6Online() => CommunicationServiceClient.Instance.IsChannel6Online(ServerGroup);
        public bool IsChannel7Online() => CommunicationServiceClient.Instance.IsChannel7Online(ServerGroup);

        public bool IsCharacterMemberOfGroup(long characterId)
        {
            return Groups?.Any(g => g.IsMemberOfGroup(characterId)) == true;
        }

        public bool IsCharactersGroupFull(long characterId)
        {
            return Groups?.Any(g =>
                       g.IsMemberOfGroup(characterId) &&
                       (g.SessionCount == (byte)g.GroupType || g.GroupType == GroupType.TalentArena)) ==
                   true;
        }

        public bool ItemHasRecipe(short itemVNum)
        {
            return _recipeLists.Any(r => r.ItemVNum == itemVNum);
        }

        public void JoinMiniland(ClientSession session, ClientSession minilandOwner)
        {
            if (session.Character.Miniland.MapInstanceId == minilandOwner.Character.Miniland.MapInstanceId)
            {
                foreach (var mate in session.Character.Mates)
                {
                    if (mate != null)
                    {
                        if (session.Character.Miniland.Map.IsBlockedZone(mate.PositionX, mate.PositionY))
                        {
                            var newPos = MinilandRandomPos();
                            mate.MapX = newPos.X;
                            mate.MapY = newPos.Y;
                            mate.PositionX = mate.MapX;
                            mate.PositionY = mate.MapY;
                        }

                        if (!mate.IsAlive || mate.Hp <= 0)
                        {
                            mate.Hp = mate.MaxHp / 2;
                            mate.Mp = mate.MaxMp / 2;
                            mate.IsAlive = true;
                            mate.ReviveDisposable?.Dispose();
                        }
                    }
                }
            }

            ChangeMapInstance(session.Character.CharacterId, minilandOwner.Character.Miniland.MapInstanceId,
                5, 8);
            if (session.Character.Miniland.MapInstanceId != minilandOwner.Character.Miniland.MapInstanceId)
            {
                session.SendPacket(UserInterfaceHelper.GenerateMsg(minilandOwner.Character.MinilandMessage,
                    0));
                session.SendPacket(minilandOwner.Character.GenerateMlinfobr());
                session.SendPacket(minilandOwner.Character.GenerateMinilandObjectForFriends());
            }
            else
            {
                session.SendPacket(session.Character.GenerateMlinfo());
                session.SendPacket(minilandOwner.Character.GetMinilandObjectList());
            }

            minilandOwner.Character.Mates.Where(s => !s.IsTeamMember).ToList()
                .ForEach(s => session.SendPacket(s.GenerateIn()));
            session.SendPackets(minilandOwner.Character.GetMinilandEffects());
        }

        // Server
        public void Kick(string characterName)
        {
            var session = Sessions.FirstOrDefault(s => s.Character?.Name.Equals(characterName) == true);
            session?.Disconnect();
        }

        // Map
        public void LeaveMap(long id)
        {
            var session = GetSessionByCharacterId(id);
            if (session == null)
            {
                return;
            }

            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.CustomInstance && session.CurrentMapInstance.Sessions.Count() == 0)
            {
                if (session.Character.Group?.Sessions.Any(a => a.Character.MapInstance == session.Character.CustomInstance) == false)
                {
                    RemoveMapInstance(session.Character.CustomInstance.MapInstanceId);
                }
                session.Character.CustomInstance = null;
                session.CurrentMapInstance.Dispose();
            }

            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.LodInstance && session.CurrentMapInstance.Sessions.Count() == 0)
            {
                if (session.Character.Group?.Sessions.Any(a => a.Character.MapInstance == session.Character.LodInstance) == false)
                {
                    RemoveMapInstance(session.Character.LodInstance.MapInstanceId);
                }
                session.Character.LodInstance = null;
                session.CurrentMapInstance.Dispose();
            }

            if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.CelestialSpire && session.CurrentMapInstance.Sessions.Count() == 0)
            {
                session.CurrentMapInstance.Dispose();
            }

            session.SendPacket(UserInterfaceHelper.GenerateMapOut());
            if (!session.Character.InvisibleGm)
            {
                session.Character.Mates?.Where(s => s.IsTeamMember).ToList().ForEach(s =>
                    session.CurrentMapInstance?.Broadcast(session,
                        StaticPacketHelper.Out(UserType.Npc, s.MateTransportId), ReceiverType.AllExceptMe));
                session.CurrentMapInstance?.Broadcast(session,
                    StaticPacketHelper.Out(UserType.Player, session.Character.CharacterId),
                    ReceiverType.AllExceptMe);
            }
        }

        public void CharacterSynchronizingAtSaveProcess(long characterId, bool lockOrUnlock)
        {
            CommunicationServiceClient.Instance.AddOrRemoveSavingCharacters(characterId, lockOrUnlock);
        }

        public bool IsCharacterSaving(long characterId)
        {
            return CommunicationServiceClient.Instance.IsCharacterSaving(characterId);
        }

        public bool MapNpcHasRecipe(int mapNpcId)
        {
            return _recipeLists.Any(r => r.MapNpcId == mapNpcId);
        }

        public void RefreshRanking()
        {
            TopComplimented = DAOFactory.CharacterDAO.GetTopCompliment();
            TopPoints = DAOFactory.CharacterDAO.GetTopPoints();
            TopReputation = DAOFactory.CharacterDAO.GetTopReputation();
            TopDuel = DAOFactory.CharacterDAO.GetTopDuel();
            TopMonster = DAOFactory.CharacterDAO.GetTopMonster();
        }

        public void RefreshDailyMissions()
        {
            foreach (var fsm in DAOFactory.FamilySkillMissionDAO.LoadAll())
            {
                if (!FamilySystemHelper.IsDaily(fsm.ItemVNum) && fsm.ItemVNum > 9603) continue;

                DAOFactory.FamilySkillMissionDAO.DailyReset(fsm);
            }
        }

        public void RelationRefresh(long relationId)
        {
            _inRelationRefreshMode = true;
            CommunicationServiceClient.Instance.UpdateRelation(ServerGroup, relationId);
            SpinWait.SpinUntil(() => !_inRelationRefreshMode);
        }

        // Map
        public void ReviveFirstPosition(long characterId)
        {
            var session = GetSessionByCharacterId(characterId);
            if (session?.Character.Hp <= 0)
            {
                if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance ||
                    session.CurrentMapInstance.MapInstanceType == MapInstanceType.RaidInstance)
                {
                    session.Character.Hp = (int)session.Character.HPLoad();
                    session.Character.Mp = (int)session.Character.MPLoad();
                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateRevive());
                    session.SendPacket(session.Character.GenerateStat());
                }
                else
                {
                    if (ChannelId == 51)
                    {
                        if (session.CurrentMapInstance.MapInstanceId ==
                            session.Character.Family?.Act4RaidBossMap?.MapInstanceId)
                        {
                            session.Character.Hp = 1;
                            session.Character.Mp = 1;

                            switch (session.Character.Family.Act4Raid.MapInstanceType)
                            {
                                case MapInstanceType.Act4Morcos:
                                    Instance.ChangeMapInstance(session.Character.CharacterId,
                                        session.Character.Family.Act4Raid.MapInstanceId, 43, 179);
                                    break;

                                case MapInstanceType.Act4Hatus:
                                    Instance.ChangeMapInstance(session.Character.CharacterId,
                                        session.Character.Family.Act4Raid.MapInstanceId, 15, 9);
                                    break;

                                case MapInstanceType.Act4Calvina:
                                    Instance.ChangeMapInstance(session.Character.CharacterId,
                                        session.Character.Family.Act4Raid.MapInstanceId, 24, 6);
                                    break;

                                case MapInstanceType.Act4Berios:
                                    Instance.ChangeMapInstance(session.Character.CharacterId,
                                        session.Character.Family.Act4Raid.MapInstanceId, 20, 20);
                                    break;
                            }
                        }
                        else
                        {
                            session.Character.Hp = (int)session.Character.HPLoad();
                            session.Character.Mp = (int)session.Character.MPLoad();
                            var x = (short)(39 + RandomNumber(-2, 3));
                            var y = (short)(42 + RandomNumber(-2, 3));
                            if (session.Character.Faction == FactionType.Angel)
                            {
                                ChangeMap(session.Character.CharacterId, 130, x, y);
                            }
                            else if (session.Character.Faction == FactionType.Demon)
                            {
                                ChangeMap(session.Character.CharacterId, 131, x, y);
                            }
                        }
                    }
                    else
                    {
                        session.Character.Hp = 1;
                        session.Character.Mp = 1;
                        if (session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                        {
                            var resp = session.Character.Respawn;
                            var x = (short)(resp.DefaultX + RandomNumber(-3, 3));
                            var y = (short)(resp.DefaultY + RandomNumber(-3, 3));
                            ChangeMap(session.Character.CharacterId, resp.DefaultMapId, x, y);
                        }
                        else
                        {
                            Instance.ChangeMap(session.Character.CharacterId, session.Character.MapId,
                                session.Character.MapX, session.Character.MapY);
                        }
                    }

                    session.CurrentMapInstance?.Broadcast(session, session.Character.GenerateTp());
                    session.CurrentMapInstance?.Broadcast(session.Character.GenerateRevive());
                    session.SendPacket(session.Character.GenerateStat());
                }
            }
        }

        public async Task SaveAll()
        {
            await Task.WhenAll(Sessions.Select(async sess =>
            {
                await sess.Character.Event.EmitEventAsync(new CharacterSaveEvent());
            }));
            DAOFactory.BazaarItemDAO.RemoveOutDated();

            //LOGGER($"[SAVE] SaveAll finished succesfully. Saved {Sessions.Count()} Sessions");
        }

        public async Task ShutdownTaskAsync(int Time = 5)
        {
            Shout(string.Format(Language.Instance.GetMessageFromKey("SHUTDOWN_MIN"), Time));
            if (Time > 1)
            {
                for (var i = 0; i < 60 * (Time - 1); i++)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    if (Instance.ShutdownStop)
                    {
                        Instance.ShutdownStop = false;
                        return;
                    }
                }

                Shout(string.Format(Language.Instance.GetMessageFromKey("SHUTDOWN_MIN"), 1));
            }

            for (var i = 0; i < 30; i++)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (Instance.ShutdownStop)
                {
                    Instance.ShutdownStop = false;
                    return;
                }
            }

            Shout(string.Format(Language.Instance.GetMessageFromKey("SHUTDOWN_SEC"), 30));
            for (var i = 0; i < 30; i++)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (Instance.ShutdownStop)
                {
                    Instance.ShutdownStop = false;
                    return;
                }
            }

            Shout(string.Format(Language.Instance.GetMessageFromKey("SHUTDOWN_SEC"), 10));
            for (var i = 0; i < 10; i++)
            {
                await Task.Delay(1000).ConfigureAwait(false);
                if (Instance.ShutdownStop)
                {
                    Instance.ShutdownStop = false;
                    return;
                }
            }

            InShutdown = true;
            await Instance.SaveAll();
            CommunicationServiceClient.Instance.UnregisterWorldServer(WorldId);
            if (IsReboot)
            {
                if (ChannelId != 51)
                {
                    await Task.Delay(ChannelId * 2000).ConfigureAwait(false);
                }
                Process.Start("Frostvein.World.exe",
                    $"--nomsg{(ChannelId == 51 ? $" --port {Convert.ToInt32(ServerConfiguration.GlacernonServerPort)}" : "")}");
            }

            Environment.Exit(0);
        }

        public void SynchronizeSheduling()
        {
            if (Schedules.FirstOrDefault(s => s.Event == EventType.TALENTARENA)?.Time is TimeSpan arenaOfTalentsTime && IsTimeBetween(DateTime.Now, arenaOfTalentsTime, arenaOfTalentsTime.Add(new TimeSpan(4, 0, 0))))
            {
                GameEventHandler.GenerateEvent(EventType.TALENTARENA);
            }
            Schedules.Where(s => s.Event == EventType.LOD).ToList().ForEach(lodSchedule =>
            {
                if (IsTimeBetween(DateTime.Now, lodSchedule.Time, lodSchedule.Time.Add(new TimeSpan(2, 0, 0))))
                {
                    GameEventHandler.GenerateEvent(EventType.LOD);
                }
            });
        }

        public void TeleportOnRandomPlaceInMap(ClientSession session, Guid guid)
        {
            var map = GetMapInstance(guid);
            if (guid != default)
            {
                var pos = map.Map.GetRandomPosition();
                if (pos == null)
                {
                    return;
                }

                if (map != null)
                {
                    bool blocked = map.Map.IsBlockedZone(pos.X, pos.Y);
                    if (blocked)
                    {
                        return;
                    }
                }

                ChangeMapInstance(session.Character.CharacterId, guid, pos.X, pos.Y);
            }
        }

        // Server
        public void UpdateGroup(long charId)
        {
            try
            {
                if (Groups != null)
                {
                    var myGroup = Groups.Find(s => s.IsMemberOfGroup(charId));
                    if (myGroup == null)
                    {
                        return;
                    }

                    var groupMembers = Groups.Find(s => s.IsMemberOfGroup(charId))?.Sessions;
                    if (groupMembers != null)
                    {
                        foreach (var session in groupMembers.GetAllItems())
                        {
                            session.SendPacket(session.Character.GeneratePinit());
                            session.SendPackets(session.Character.GeneratePst());
                            session.SendPacket(session.Character.GenerateStat());
                        }
                    }
                }
            }
            catch (Exception e)
            {
                //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
            }
        }

        internal static void StopServer()
        {
            Instance.ShutdownStop = true;
            Instance.TaskShutdown = null;
        }

        internal List<NpcMonsterSkill> GetNpcMonsterSkillsByMonsterVNum(short npcMonsterVNum) => _monsterSkills.ContainsKey(npcMonsterVNum)
                ? _monsterSkills[npcMonsterVNum]
                : new List<NpcMonsterSkill>();

        internal Shop GetShopByMapNpcId(int mapNpcId) => _shops.ContainsKey(mapNpcId) ? _shops[mapNpcId] : null;

        internal List<ShopItemDTO> GetShopItemsByShopId(int shopId) => _shopItems.ContainsKey(shopId) ? _shopItems[shopId] : new List<ShopItemDTO>();

        internal List<ShopSkillDTO> GetShopSkillsByShopId(int shopId) => _shopSkills.ContainsKey(shopId) ? _shopSkills[shopId] : new List<ShopSkillDTO>();

        internal List<TeleporterDTO> GetTeleportersByNpcVNum(int npcMonsterVNum)
        {
            if (_teleporters?.ContainsKey(npcMonsterVNum) == true)
            {
                return _teleporters[npcMonsterVNum];
            }

            return new List<TeleporterDTO>();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //_monsterDrops.Dispose();
                ThreadSafeGroupList.Dispose();

                //_monsterSkills.Dispose();
                //_shopSkills.Dispose();
                //_shopItems.Dispose();
                //_shops.Dispose();
                _recipes.Dispose();
                //_mapNpcs.Dispose();
                //_teleporters.Dispose();
                GC.SuppressFinalize(this);
            }
        }

        private static void Act4StatProcess()
        {
            if (Instance.ChannelId != 51)
            {
                return;
            }

            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = null,
                SourceCharacterId = 0,
                SourceWorldId = Instance.WorldId,
                Message =
                            $"[A4 Status] Angels: {Instance.Act4AngelStat.Percentage / 100} % Demons: {Instance.Act4DemonStat.Percentage / 100} %",
                Type = MessageType.Shout
            });
        }

        private static void LoadNpcMonsters()
        {
            var bcards = DAOFactory.BCardDAO.LoadAll().ToArray().Where(s => s.NpcMonsterVNum.HasValue);
            foreach (var npcMonster in DAOFactory.NpcMonsterDAO.LoadAll().ToArray())
            {
                var tmp = new NpcMonster(npcMonster);

                if (!(tmp is NpcMonster monster))
                {
                    continue;
                }

                // TODO: remove that after
                monster.Initialize();
                monster.BCards = new List<BCard>();

                foreach (var s in bcards.Where(s =>
                    s.NpcMonsterVNum == (monster.OriginalNpcMonsterVNum > 0
                        ? npcMonster.OriginalNpcMonsterVNum
                        : monster.NpcMonsterVNum)))
                {
                    monster.BCards.Add(new BCard(s));
                }

                Npcs.Add(monster);
            }

            Logger.Info(
                string.Format(Language.Instance.GetMessageFromKey("NPCMONSTERS_LOADED"), Npcs.Count));
        }

        public static void OnGlobalEvent(object sender, EventArgs e)
        {
            var tuple = (Tuple<EventType, byte>)sender;
            GameEventHandler.GenerateEvent(tuple.Item1, value: tuple.Item2);
        }

        public static void OnRestart(object sender, EventArgs e)
        {
            if (Instance.TaskShutdown != null)
            {
                Instance.IsReboot = false;
                Instance.ShutdownStop = true;
                Instance.TaskShutdown = null;
            }
            else
            {
                Instance.IsReboot = true;
                Instance.TaskShutdown = Instance.ShutdownTaskAsync();
                Instance.TaskShutdown.Start();
            }
        }

        public static void OnShutdown(object sender, EventArgs e)
        {
            if (Instance.TaskShutdown != null)
            {
                Instance.ShutdownStop = true;
                Instance.TaskShutdown = null;
            }
            else
            {
                Instance.TaskShutdown = Instance.ShutdownTaskAsync();
                Instance.TaskShutdown.Start();
            }
        }

        private static void ReviveTask(ClientSession session)
        {
            Task.Factory.StartNew(async () =>
            {
                var revive = true;
                for (var i = 1; i <= 30; i++)
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                    if (session.Character.Hp > 0)
                    {
                        revive = false;
                        break;
                    }
                }

                if (revive)
                {
                    Instance.ReviveFirstPosition(session.Character.CharacterId);
                }
            });
        }

        private void Act4FlowerProcess()
        {
            foreach (var map in GetAllMapInstances().Where(s =>
                s.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.Act4) &&
                s.Npcs.Count(o => o.NpcVNum == 2004 && o.IsOut) < s.Npcs.Count(n => n.NpcVNum == 2004)))
                foreach (var i in map.Npcs.Where(s => s.IsOut && s.NpcVNum == 2004))
                {
                    var randomPos = map.Map.GetRandomPosition();
                    i.MapX = randomPos.X;
                    i.MapY = randomPos.Y;
                    i.MapInstance.Broadcast(i.GenerateIn());
                }
        }

        private void GroupProcess()
        {
            try
            {
                if (Groups != null)
                {
                    foreach (var grp in Groups)
                        foreach (var session in grp.Sessions.GetAllItems())
                        {
                            if (grp.GroupType == GroupType.Group)
                            {
                                session.SendPackets(grp.GeneratePst(session));
                            }
                            else if (grp.GroupType == GroupType.Team || grp.GroupType == GroupType.BigTeam || grp.GroupType == GroupType.GiantTeam)
                            {
                                session.SendPacket(grp.GenerateRdlst());
                            }
                            else if (grp.GroupType == GroupType.RBBBlue || grp.GroupType == GroupType.RBBRed)
                            {
                                session.SendPacket(RainbowThread.GenerateFbList(session));
                            }
                        }
                }
            }
            catch (Exception e)
            {
                //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
            }
        }

        private void InitAllProperty()
        {
            Act4RaidStart = DateTime.Now;
            Act4AngelStat = new Act4Stat();
            Act4DemonStat = new Act4Stat();
            Act6Erenia = new Act4Stat();
            Act6Zenas = new Act4Stat();
            LastFCSent = DateTime.Now;
            CharacterScreenSessions = new ThreadSafeSortedList<long, ClientSession>();
        }

        private bool IsTimeBetween(DateTime dateTime, TimeSpan start, TimeSpan end)
        {
            var now = dateTime.TimeOfDay;

            return start < end ? start <= now && now <= end : !(end < now && now < start);
        }

        public void CheckForStuckAccountsAtSaving()
        {
            CommunicationServiceClient.Instance.CheckForStuckAccountsAtSaving();
        }

        public void OnConfiguratinEvent(object sender, EventArgs e)
        {
            Configuration = (ConfigurationObject)sender;
        }

        public void OnFamilyRefresh(object sender, EventArgs e)
        {
            var tuple = (Tuple<long, bool>)sender;
            var familyId = tuple.Item1;
            var famdto = DAOFactory.FamilyDAO.LoadById(familyId);
            var fam = FamilyList[familyId];
            lock (FamilyList)
            {
                if (famdto != null)
                {
                    var newFam = new Family(famdto);
                    if (fam != null)
                    {
                        newFam.FamilyRoom = fam.FamilyRoom;
                        newFam.LandOfDeath = fam.LandOfDeath;
                        newFam.FamilyTower = fam.FamilyTower;
                        newFam.Act4Raid = fam.Act4Raid;
                        newFam.Act4RaidBossMap = fam.Act4RaidBossMap;
                        newFam.NewEvent = fam.NewEvent;
                    }

                    newFam.FamilyCharacters = new List<FamilyCharacter>();
                    foreach (var famchar in DAOFactory.FamilyCharacterDAO.LoadByFamilyId(famdto.FamilyId)
                        .ToList())
                    {
                        newFam.FamilyCharacters.Add(new FamilyCharacter(famchar));
                    }
                    foreach (FamilySkillMissionDTO famskill in DAOFactory.FamilySkillMissionDAO.LoadByFamilyId(famdto.FamilyId).ToList())
                    {
                        newFam.FamilySkillMissions.Add(new FamilySkillMission(famskill));
                    }

                    var familyHead = newFam.FamilyCharacters.Find(s => s.Authority == FamilyAuthority.Head);
                    if (familyHead != null)
                    {
                        newFam.Warehouse = new Inventory(new Character(familyHead.Character));
                        foreach (var inventory in DAOFactory.ItemInstanceDAO
                            .LoadByCharacterId(familyHead.CharacterId)
                            .Where(s => s.Type == InventoryType.FamilyWareHouse).ToList())
                        {
                            inventory.CharacterId = familyHead.CharacterId;
                            newFam.Warehouse[inventory.Id] = new ItemInstance(inventory);
                        }
                    }

                    newFam.FamilyLogs = DAOFactory.FamilyLogDAO.LoadByFamilyId(famdto.FamilyId).ToList();
                    FamilyList[familyId] = newFam;

                    foreach (var session in Sessions.Where(s =>
                        newFam.FamilyCharacters.Any(m => m.CharacterId == s.Character.CharacterId)))
                    {
                        if (session.Character.LastFamilyLeave < DateTime.Now.AddDays(-1).Ticks)
                        {
                            session.Character.Family = newFam;

                            if (tuple.Item2)
                            {
                                session.Character.ChangeFaction((FactionType)newFam.FamilyFaction);
                            }
                            session?.CurrentMapInstance?.Broadcast(session?.Character?.GenerateGidx());
                        }
                        session.Character.Family = newFam;

                        if (tuple.Item2)
                        {
                            session.Character.ChangeFaction((FactionType)newFam.FamilyFaction);
                        }

                        session?.CurrentMapInstance?.Broadcast(session?.Character?.GenerateGidx());
                        session?.SendPacket(FamilySystemExtensions.GenerateFmi(session));
                        session?.SendPacket(FamilySystemExtensions.GenerateFmp(session));
                    }
                }
                else if (fam != null)
                {
                    lock (FamilyList)
                    {
                        FamilyList.Remove(fam.FamilyId);
                    }

                    foreach (var sess in Sessions.Where(s =>
                        fam.FamilyCharacters.Any(f => f.CharacterId.Equals(s.Character.CharacterId))))
                    {
                        sess.Character.Family = null;
                        sess.SendPacket(sess.Character.GenerateGidx());
                        sess?.SendPacket(FamilySystemExtensions.GenerateFmi(sess));
                        sess?.SendPacket(FamilySystemExtensions.GenerateFmp(sess));

                    }
                }
            }
        }

        public void OnMailSent(object sender, EventArgs e)
        {
            var mail = (MailDTO)sender;

            var session = GetSessionByCharacterId(mail.IsSenderCopy ? mail.SenderId : mail.ReceiverId);
            if (session != null)
            {
                if (mail.AttachmentVNum != null)
                {
                    session.Character.MailList.Add(
                        (session.Character.MailList.Count > 0
                            ? session.Character.MailList.OrderBy(s => s.Key).Last().Key
                            : 0) + 1, mail);
                    session.SendPacket(session.Character.GenerateParcel(mail));

                    //session.SendPacket(session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ITEM_GIFTED"), GetItem(mail.AttachmentVNum.Value)?.Name, mail.AttachmentAmount), 12));
                }
                else
                {
                    session.Character.MailList.Add(
                        (session.Character.MailList.Count > 0
                            ? session.Character.MailList.OrderBy(s => s.Key).Last().Key
                            : 0) + 1, mail);
                    session.SendPacket(session.Character.GeneratePost(mail,
                        mail.IsSenderCopy ? (byte)2 : (byte)1));
                }
            }
        }

        public void OnMessageSentToCharacter(object sender, EventArgs e)
        {
            if (sender != null)
            {
                var message = (SCSCharacterMessage)sender;

                var targetSession = Sessions.SingleOrDefault(s =>
                    s.Character.CharacterId == message.DestinationCharacterId);
                switch (message.Type)
                {
                    case MessageType.WhisperGM:
                    case MessageType.Whisper:
                        if (targetSession == null)
                        {
                            return;
                        }

                        if (targetSession.Character.GmPvtBlock)
                        {
                            if (message.DestinationCharacterId != null)
                            {
                                CommunicationServiceClient.Instance.SendMessageToCharacter(
                                        new SCSCharacterMessage
                                        {
                                            DestinationCharacterId = message.SourceCharacterId,
                                            SourceCharacterId = message.DestinationCharacterId.Value,
                                            SourceWorldId = WorldId,
                                            Message = targetSession.Character.GenerateSay(
                                                        Language.Instance.GetMessageFromKey("GM_CHAT_BLOCKED"), 10),
                                            Type = MessageType.Other
                                        });
                            }
                        }
                        else if (targetSession.Character.WhisperBlocked && DAOFactory.AccountDAO.LoadById(DAOFactory.CharacterDAO.LoadById(message.SourceCharacterId).AccountId).Authority < AuthorityType.GM)
                        {
                            if (message.DestinationCharacterId != null)
                            {
                                CommunicationServiceClient.Instance.SendMessageToCharacter(
                                        new SCSCharacterMessage
                                        {
                                            DestinationCharacterId = message.SourceCharacterId,
                                            SourceCharacterId = message.DestinationCharacterId.Value,
                                            SourceWorldId = WorldId,
                                            Message = UserInterfaceHelper.GenerateMsg(
                                                        Language.Instance.GetMessageFromKey("USER_WHISPER_BLOCKED"), 0),
                                            Type = MessageType.Other
                                        });
                            }
                        }
                        else
                        {
                            if (message.SourceWorldId != WorldId)
                            {
                                if (message.DestinationCharacterId != null)
                                {
                                    CommunicationServiceClient.Instance.SendMessageToCharacter(
                                            new SCSCharacterMessage
                                            {
                                                DestinationCharacterId = message.SourceCharacterId,
                                                SourceCharacterId = message.DestinationCharacterId.Value,
                                                SourceWorldId = WorldId,
                                                Message = targetSession.Character.GenerateSay(
                                                            string.Format(
                                                                    Language.Instance.GetMessageFromKey(
                                                                            "MESSAGE_SENT_TO_CHARACTER"),
                                                                    targetSession.Character.Name, ChannelId), 11),
                                                Type = MessageType.Other
                                            });
                                }

                                targetSession.SendPacket(
                                    $"{message.Message} <{Language.Instance.GetMessageFromKey("CHANNEL")}: {CommunicationServiceClient.Instance.GetChannelIdByWorldId(message.SourceWorldId)}>");
                            }
                            else
                            {
                                targetSession.SendPacket(message.Message);
                            }
                        }

                        break;

                    case MessageType.Shout:
                        Shout(message.Message);
                        break;

                    case MessageType.PrivateChat:
                        targetSession?.SendPacket(message.Message);
                        break;

                    case MessageType.FamilyChat:
                        if (message.DestinationCharacterId.HasValue && message.SourceWorldId != WorldId)
                        {
                            foreach (var session in Instance.Sessions)
                            {
                                if (session.HasSelectedCharacter && session.Character.Family != null &&
                                    session.Character.Family.FamilyId == message.DestinationCharacterId)
                                {
                                    session.SendPacket($"sayi2 1 -1 6 1081 20 {CommunicationServiceClient.Instance.GetChannelIdByWorldId(message.SourceWorldId)} {message.Name} {message.Message}");
                                }
                            }
                        }

                        break;

                    case MessageType.Family:
                        if (message.DestinationCharacterId.HasValue)
                        {
                            foreach (var session in Instance.Sessions)
                            {
                                if (session.HasSelectedCharacter && session.Character.Family != null &&
                                    session.Character.Family.FamilyId == message.DestinationCharacterId)
                                {
                                    session.SendPacket(message.Message);
                                }
                            }
                        }

                        break;

                    case MessageType.Other:
                        targetSession?.SendPacket(message.Message);
                        break;

                    case MessageType.Broadcast:
                        foreach (var session in Instance.Sessions)
                        {
                            session.SendPacket(message.Message);
                        }

                        break;

                    case MessageType.UpdateExploit:
                        if (!message.DestinationCharacterId.HasValue)
                        {
                            return;
                        }

                        var target = Sessions.FirstOrDefault(s =>
                            s.Character?.CharacterId == message.DestinationCharacterId.Value);

                        if (target == null || !target.HasSelectedCharacter)
                        {
                            return;
                        }

                        var split = message.Message.Split(' ');

                        if (split.Length != 2)
                        {
                            return;
                        }

                        var exploitType = (CharacterExploitType)Enum.Parse(typeof(CharacterExploitType), split[0]);
                        var value = long.Parse(split[1]);

                        var exploit =
                            target.Character.Exploit.FirstOrDefault(s => s.CharacterExploitType == exploitType);

                        if (exploit == null)
                        {
                            return;
                        }

                        exploit.Stat = value;
                        target.SendPacket(target.Character.GenerateSay("Exploit restored", 12));
                        break;
                }
            }
        }

        public void OnPenaltyLogRefresh(object sender, EventArgs e)
        {
            var relId = (int)sender;
            var reldto = DAOFactory.PenaltyLogDAO.LoadById(relId);
            var rel = PenaltyLogs.Find(s => s.PenaltyLogId == relId);
            if (reldto != null)
            {
                if (rel != null)
                {
                }
                else
                {
                    PenaltyLogs.Add(reldto);
                }
            }
            else if (rel != null)
            {
                PenaltyLogs.Remove(rel);
            }
        }

        public void OnRelationRefresh(object sender, EventArgs e)
        {
            _inRelationRefreshMode = true;
            var relId = (long)sender;
            lock (CharacterRelations)
            {
                var reldto = DAOFactory.CharacterRelationDAO.LoadById(relId);
                var rel = CharacterRelations.Find(s => s.CharacterRelationId == relId);
                if (reldto != null)
                {
                    if (rel != null)
                    {
                        CharacterRelations.Find(s => s.CharacterRelationId == rel.CharacterRelationId)
                                          .RelationType = reldto.RelationType;
                    }
                    else
                    {
                        CharacterRelations.Add(reldto);
                    }
                }
                else if (rel != null)
                {
                    CharacterRelations.Remove(rel);
                }
            }

            _inRelationRefreshMode = false;
        }

        public void OnSessionKicked(object sender, EventArgs e)
        {
            if (sender != null)
            {
                var kickedSession = (Tuple<long?, long?>)sender;
                if (!kickedSession.Item1.HasValue && !kickedSession.Item2.HasValue)
                {
                    return;
                }

                var accId = kickedSession.Item1;
                var sessId = kickedSession.Item2;

                var targetSession = CharacterScreenSessions.FirstOrDefault(s =>
                    s.SessionId == sessId || s.Account.AccountId == accId);
                targetSession?.Disconnect();
                targetSession = Sessions.FirstOrDefault(s =>
                    s.SessionId == sessId || s.Account.AccountId == accId);
                targetSession?.Disconnect();
            }
        }

        public void OnStaticBonusRefresh(object sender, EventArgs e)
        {
            var characterId = (long)sender;

            var sess = GetSessionByCharacterId(characterId);
            if (sess != null)
            {
                sess.Character.StaticBonusList = DAOFactory.StaticBonusDAO.LoadByCharacterId(characterId).ToList();
            }
        }


        public static short GetChangeItem(byte classType, byte level, byte jobLevel, bool isHero, EquipmentType equipmentSlot, byte reput, ItemType type, byte sub, short morph, long itemValid)
        {
            if (type == ItemType.Fashion && sub == 6 && classType != 16)
                return Items.FirstOrDefault(i => i.Class == classType && i.LevelMinimum == level && i.LevelJobMinimum == jobLevel && i.IsHeroic == isHero
                        && i.EquipmentSlot == equipmentSlot && i.ReputationMinimum == reput && i.ItemType == type && i.ItemSubType == sub && i.Morph == morph && i.ItemValidTime == itemValid)?.VNum ?? 0;
            else
                return Items.LastOrDefault(i => i.Class == classType && i.LevelMinimum == level && i.LevelJobMinimum == jobLevel && i.IsHeroic == isHero
                    && i.EquipmentSlot == equipmentSlot && i.ReputationMinimum == reput)?.VNum ?? 0;
        }
        #endregion
    }
}