

using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.Event;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.XMLModel.Events;
using Frostvein.XMLModel.Models.ScriptedInstance;
using Frostvein.XMLModel.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Frostvein.GameObject
{
    public class ScriptedInstance : ScriptedInstanceDTO
    {
        #region Members

        public readonly Dictionary<int, MapInstance> _mapInstanceDictionary = new Dictionary<int, MapInstance>();

        private IDisposable _disposable;

        #endregion

        #region Instantiation

        public ScriptedInstance()
        {
        }

        public ScriptedInstance(ScriptedInstanceDTO input)
        {
            MapId = input.MapId;
            PositionX = input.PositionX;
            PositionY = input.PositionY;
            Script = input.Script;
            ScriptedInstanceId = input.ScriptedInstanceId;
            Type = input.Type;
            QuestTimeSpaceId = input.QuestTimeSpaceId;
            Label = input.Label;
            Info = input.Info;
        }

        #endregion

        #region Properties

        public short TimespaceType { get; set; }

        public bool IsLoserMode { get; set; }

        public short DailyEntries { get; set; }

        public List<Gift> DrawItems { get; set; }

        public List<int> FamMissions { get; set; }

        public int FamExp { get; set; }

        public MapInstance FirstMap { get; set; }

        public List<Gift> GiftItems { get; set; }

        public int PartnerVnumRewards { get; set; }

        public long Gold { get; set; }

        public short Id { get; set; }

        public InstanceBag InstanceBag { get; set; } = new InstanceBag();

        public bool IsGiantTeam { get; set; }

        public bool IsMediumTeam { get; set; }

        public bool IsTeam { get; set; }


        public bool IsIndividual { get; set; }

        public string Label { get; set; }

        public byte LevelMaximum { get; set; }

        public byte LevelMinimum { get; set; }

        public byte Lives { get; set; }

        public ScriptedInstanceModel Model { get; set; }

        public int MonsterAmount { get; internal set; }

        public string Name { get; set; }

        public int NpcAmount { get; internal set; }

        public int Reputation { get; set; }

        public List<Gift> RequiredItems { get; set; }

        public int RoomAmount { get; internal set; }

        public List<Gift> SpecialItems { get; set; }

        public int[] SpNeeded { get; set; }

        public short StartX { get; set; }

        public short StartY { get; set; }

        #endregion

        #region Methods

        public bool LoserMode(Character character)
        {
            bool losedTimespace = ServerManager.Instance.TimespaceLogs.Count(s => s.CharacterId == character.CharacterId && s.ScriptedInstanceId == ScriptedInstanceId && s.IsFailed) > 2;
            bool winnedTimespace = ServerManager.Instance.TimespaceLogs.Any(s => s.CharacterId == character.CharacterId && s.ScriptedInstanceId == ScriptedInstanceId && !s.IsFailed);
            return !winnedTimespace && losedTimespace;
        }

        public void Dispose()
        {
            Observable.Timer(TimeSpan.FromSeconds(20)).SafeSubscribe(x =>
            {
                _mapInstanceDictionary.Values.ToList().ForEach(m =>
                {
                    m.StopLife();
                    m.Dispose();
                });
            });

            _disposable?.Dispose();
        }

        public async Task DisposeAsync()
        {
            Observable.Timer(TimeSpan.FromSeconds(20)).SafeSubscribe(x =>
            {
                _mapInstanceDictionary.Values.ToList().ForEach(async m =>
                {
                    m.StopLife();
                    await m.DisposeAsync();
                });
            });

            _disposable?.Dispose();
            ////await //LOGGER("[Dispose] Disposed all objects");
        }

        public async Task DisposeTokenAsync(CancellationToken token)
        {
            Observable.Timer(TimeSpan.FromSeconds(20)).SafeSubscribe(x =>
            {
                _mapInstanceDictionary.Values.ToList().ForEach(async m =>
                {
                    m.StopLife();
                    await m.DisposeAsync();
                });
            });

            _disposable?.Dispose();
            //await //LOGGER("[Dispose] Disposed all objects");
        }

        public void End()
        {
            if (InstanceBag.Lives - InstanceBag.DeadList.Count < 0)
            {
                _mapInstanceDictionary.Values.ToList().ForEach(m => EventHelper.Instance.RunEvent(new EventContainer(m, EventActionType.SCRIPTEND, (byte)3)));
                Dispose();
                _disposable.Dispose();
            }

            if (InstanceBag.Clock.SecondsRemaining > 0)
            {
                return;
            }

            foreach (var m in _mapInstanceDictionary.Values)
            {
                EventHelper.Instance.RunEvent(new EventContainer(m, EventActionType.SCRIPTEND, (byte)1));
            }

            Dispose();
            _disposable.Dispose();
        }


        public string GenerateMainInfo() => $"minfo 0 1 -1.0/0 -1.0/0 -1/0 -1.0/0 2 {InstanceBag.Lives + 1} 0";

        public List<string> GenerateMinimap() //Bug TS VIDAS
        {
            List<string> lst = new List<string> { "rsfm 0 0 4 12 0 1" };
            _mapInstanceDictionary.Values.ToList().ForEach(s => lst.Add(s.GenerateRsfn(true)));
            return lst;
        }

        public string GenerateRbr(Character character)
        {
            string drawgift = "";
            string requireditem = "";
            string bonusitems = "";
            string specialitems = "";

            for (int i = 0; i < 5; i++)
            {
                Gift gift = DrawItems?.ElementAtOrDefault(i);
                drawgift += $" {(gift == null ? "-1.0" : $"{gift.VNum}.{gift.Amount}")}";
            }
            for (int i = 0; i < 2; i++)
            {
                Gift gift = SpecialItems?.ElementAtOrDefault(i);
                specialitems += $" {(gift == null ? "-1.0" : $"{gift.VNum}.{gift.Amount}")}";
            }

            for (int i = 0; i < 3; i++)
            {
                Gift gift = GiftItems?.ElementAtOrDefault(i);
                bonusitems += (i == 0 ? "" : " ") + (gift == null ? "-1.0" : $"{gift.VNum}.{gift.Amount}");
            }
            var winner = ServerManager.Instance.TimespaceLogs.OrderByDescending(s => s.Score).FirstOrDefault(s => s.ScriptedInstanceId == ScriptedInstanceId);
            var highestScoreInfo = winner != null ? $"{winner.Score}.{(winner.Score > 0 ? DAOFactory.CharacterDAO.LoadById(winner.CharacterId).Name : "")}" : $"0.{string.Empty}";
            bool alreadyDone = ServerManager.Instance.TimespaceLogs.Any(s => s.CharacterId == character.CharacterId && s.ScriptedInstanceId == ScriptedInstanceId && !s.IsFailed);
            return $"rbr {TimespaceType}.0.0 {(alreadyDone ? 4 : 0)} {(alreadyDone ? 15 : 0)} {LevelMinimum}.{LevelMaximum} {RequiredItems?.Sum(s => s.Amount)}{drawgift} {specialitems} {bonusitems} {highestScoreInfo} 0 {Convert.ToInt16(LoserMode(character))} {Name}\n{Label}";
        }

        public string GenerateWp(WpPortalType type) => $"wp {PositionX} {PositionY} {ScriptedInstanceId} {(byte)type} {LevelMinimum} {LevelMaximum}";

        public void LoadGlobals()
        {
            if (Script != null)
            {
                var serializer = new XmlSerializer(typeof(ScriptedInstanceModel));
                using (var textReader = new StringReader(Script))
                {
                    Model = (ScriptedInstanceModel)serializer.Deserialize(textReader);
                }

                if (Model?.Globals != null)
                {
                    RequiredItems = new List<Gift>();
                    FamMissions = new List<int>();
                    DrawItems = new List<Gift>();
                    SpecialItems = new List<Gift>();
                    GiftItems = new List<Gift>();
                    SpNeeded = new[] { 0, 0, 0, 0, 0 };

                    // set the values
                    Id = Model.Globals.Id?.Value ?? 0;
                    IsIndividual = Model.Globals.IsIndividual?.Value ?? false;
                    DailyEntries = Model.Globals.DailyEntries?.Value ?? 0;
                    Gold = Model.Globals.Gold?.Value ?? 0;
                    Reputation = Model.Globals.Reputation?.Value ?? 0;
                    FamExp = Model.Globals.FamExp?.Value ?? 0;
                    StartX = Model.Globals.StartX?.Value ?? 0;
                    StartY = Model.Globals.StartY?.Value ?? 0;
                    Lives = Model.Globals.Lives?.Value ?? 0;
                    FamMissions.Add(Model.Globals.FamMission?.Mission1 ?? 0);
                    FamMissions.Add(Model.Globals.FamMission?.Mission2 ?? 0);
                    LevelMinimum = Model.Globals.LevelMinimum?.Value ?? 1;
                    LevelMaximum = Model.Globals.LevelMaximum?.Value ?? 99;
                    Name = Model.Globals.Name?.Value ?? "No Name";
                    Label = Model.Globals.Label?.Value ?? "No Description";
                    IsGiantTeam = Model.Globals.GiantTeam != null;
                    if (Model.Globals.RequiredItems != null)
                    {
                        foreach (var item in Model.Globals.RequiredItems)
                        {
                            RequiredItems.Add(new Gift(item.VNum, item.Amount, item.Design, item.IsRandomRare));
                        }
                    }

                    if (Model.Globals.DrawItems != null)
                    {
                        foreach (var item in Model.Globals.DrawItems)
                        {
                            DrawItems.Add(new Gift(item.VNum, item.Amount, item.Design, item.IsRandomRare));
                        }
                    }

                    if (Model.Globals.SpecialItems != null)
                    {
                        foreach (var item in Model.Globals.SpecialItems)
                        {
                            SpecialItems.Add(new Gift(item.VNum, item.Amount, item.Design, item.IsRandomRare));
                        }
                    }

                    if (Model.Globals.GiftItems != null)
                    {
                        foreach (var item in Model.Globals.GiftItems)
                        {
                            GiftItems.Add(new Gift(item.VNum, item.Amount, item.Design, item.IsRandomRare)
                            {
                                MinTeamSize = item.MinTeamSize,
                                MaxTeamSize = item.MaxTeamSize
                            });
                        }
                    }

                    PartnerVnumRewards = Model.Globals.PetsRewards?.MateVnum ?? 0;
                    SpNeeded[0] = Model.Globals.SpNeeded?.Adventurer ?? 0;
                    SpNeeded[1] = Model.Globals.SpNeeded?.Swordman ?? 0;
                    SpNeeded[2] = Model.Globals.SpNeeded?.Archer ?? 0;
                    SpNeeded[3] = Model.Globals.SpNeeded?.Magician ?? 0;
                    SpNeeded[4] = Model.Globals.SpNeeded?.MartialArtist ?? 0;
                }
            }
        }

        public void LoadScript(MapInstanceType mapinstancetype, Character creator)
        {
            if (Model != null)
            {
                InstanceBag = new InstanceBag();
                InstanceBag.Lives = Lives;
                if (Model.InstanceEvents?.CreateMap != null)
                {
                    foreach (var createMap in Model.InstanceEvents.CreateMap)
                    {
                        var mapInstance = ServerManager.GenerateMapInstance(createMap.VNum, mapinstancetype,
                                InstanceBag, createMap.DropAllowed, true);
                        mapInstance.XpRate = createMap.XpRate;
                        mapInstance.MapIndexX = createMap.IndexX;
                        mapInstance.MapIndexY = createMap.IndexY;
                        mapInstance.InstanceBag.CreatorId = creator.CharacterId;
                        if (!_mapInstanceDictionary.ContainsKey(createMap.Map))
                        {
                            _mapInstanceDictionary.Add(createMap.Map, mapInstance);
                        }
                    }
                }

                FirstMap = _mapInstanceDictionary.Values.FirstOrDefault();
                Observable.Timer(TimeSpan.FromMinutes(3)).Subscribe(x =>
                {
                    if (!InstanceBag.Lock)
                    {
                        _mapInstanceDictionary.Values.ToList().ForEach(m =>
                            EventHelper.Instance.RunEvent(new EventContainer(m, EventActionType.SCRIPTEND, (byte)1)));
                        Dispose();
                    }
                });
                _disposable = Observable.Interval(TimeSpan.FromMilliseconds(100)).Subscribe(x =>
                {
                    if (mapinstancetype == MapInstanceType.TimeSpaceInstance)
                    {
                        if (InstanceBag.DeadList.ToList()
                            .Any(s => InstanceBag.DeadList.Count(e => e == s) > InstanceBag.Lives))
                        {
                            _mapInstanceDictionary.Values.ToList().ForEach(m =>
                                EventHelper.Instance.RunEvent(
                                    new EventContainer(m, EventActionType.SCRIPTEND, (byte)3)));
                            Dispose();
                            _disposable.Dispose();
                        }

                        if (_mapInstanceDictionary.SelectMany(s => s.Value?.Sessions).ToList()
                            .SelectMany(s => s.Character.Mates)
                            .Any(s => s.IsTemporalMate && s.IsTsProtected && !s.IsAlive))
                        {
                            _mapInstanceDictionary.Values.ToList().ForEach(m =>
                                EventHelper.Instance.RunEvent(
                                    new EventContainer(m, EventActionType.SCRIPTEND, (byte)2)));
                            Dispose();
                            _disposable.Dispose();
                        }
                    }

                    if (InstanceBag.Lives - InstanceBag.DeadList.Count < 0)
                    {
                        _mapInstanceDictionary.Values.ToList().ForEach(m => EventHelper.Instance.RunEvent(new EventContainer(m, EventActionType.SCRIPTEND, (byte)3)));
                        Dispose();
                        _disposable.Dispose();
                    }

                    if (InstanceBag.Clock.SecondsRemaining <= 0)
                    {
                        _mapInstanceDictionary.Values.ToList().ForEach(m =>
                            EventHelper.Instance.RunEvent(new EventContainer(m, EventActionType.SCRIPTEND, (byte)2)));
                        Dispose();
                        _disposable.Dispose();
                    }
                });

                GenerateEvent(FirstMap).ForEach(e => EventHelper.Instance.RunEvent(e));
            }
        }

        private List<EventContainer> ChangePortalType(MapInstance mapInstance, ChangePortalType[] ChangePortalType)
        {
            var evts = new List<EventContainer>();

            foreach (var changePortalType in ChangePortalType)
            {
                if (changePortalType.Map != 0)
                {
                    _mapInstanceDictionary.TryGetValue(changePortalType.Map, out var destinationMap);
                    if (destinationMap != null)
                    {
                        mapInstance = destinationMap;
                    }
                }

                evts.Add(new EventContainer(mapInstance, EventActionType.CHANGEPORTALTYPE,
                    new Tuple<int, PortalType>(changePortalType.IdOnMap, (PortalType)changePortalType.Type)));
            }

            return evts;
        }

        private ThreadSafeGenericList<EventContainer> GenerateEvent(MapInstance parentMapInstance)
        {
            // Needs Optimization, look into it.
            var evts = new ThreadSafeGenericList<EventContainer>();

            if (Model.InstanceEvents.CreateMap != null)
            {
                foreach (var createMap in Model.InstanceEvents.CreateMap)
                {
                    var mapInstance = _mapInstanceDictionary.FirstOrDefault(s => s.Key == createMap.Map).Value ??
                                      parentMapInstance;

                    if (mapInstance == null)
                    {
                        continue;
                    }

                    // SummonMonster
                    evts.AddRange(SummonMonster(mapInstance, createMap.SummonMonster));

                    // SummonNpc
                    evts.AddRange(SummonNpc(mapInstance, createMap.SummonNpc));

                    // SpawnPortal
                    evts.AddRange(SpawnPortal(mapInstance, createMap.SpawnPortal));

                    // SpawnButton
                    evts.AddRange(SpawnButton(mapInstance, createMap.SpawnButton));

                    // OnCharacterDiscoveringMap
                    evts.AddRange(OnCharacterDiscoveringMap(mapInstance, createMap));

                    // GenerateClock
                    if (createMap.GenerateClock != null)
                    {
                        evts.Add(new EventContainer(mapInstance, EventActionType.CLOCK, createMap.GenerateClock.Value));
                    }

                    // StartClock
                    if (createMap.StartClock != null)
                    {
                        evts.AddRange(StartClock(mapInstance, createMap.StartClock));
                    }

                    // OnMoveOnMap
                    if (createMap.OnMoveOnMap != null)
                    {
                        foreach (var onMoveOnMap in createMap.OnMoveOnMap)
                        {
                            evts.AddRange(OnMoveOnMap(mapInstance, onMoveOnMap));
                        }
                    }

                    // OnLockerOpen
                    if (createMap.OnLockerOpen != null)
                    {
                        evts.AddRange(OnLockerOpen(mapInstance, createMap.OnLockerOpen));
                    }

                    // OnAreaEntry
                    if (createMap.OnAreaEntry != null)
                    {
                        foreach (var onAreaEntry in createMap.OnAreaEntry)
                        {
                            var onAreaEntryEvents = new List<EventContainer>();
                            if (onAreaEntry.SummonMonster != null)
                            {
                                onAreaEntryEvents.AddRange(SummonMonster(mapInstance, onAreaEntry.SummonMonster));
                            }

                            evts.Add(new EventContainer(mapInstance, EventActionType.SETAREAENTRY,
                                    new ZoneEvent
                                    {
                                        X = onAreaEntry.PositionX,
                                        Y = onAreaEntry.PositionY,
                                        Range = onAreaEntry.Range,
                                        Events = onAreaEntryEvents
                                    }));
                        }
                    }

                    // SetButtonLockers
                    if (createMap.SetButtonLockers != null)
                    {
                        evts.Add(new EventContainer(mapInstance, EventActionType.SETBUTTONLOCKERS,
                                createMap.SetButtonLockers.Value));
                    }

                    // SetMonsterLockers
                    if (createMap.SetMonsterLockers != null)
                    {
                        evts.Add(new EventContainer(mapInstance, EventActionType.SETMONSTERLOCKERS,
                                createMap.SetMonsterLockers.Value));
                    }
                }
            }

            return evts;
        }

        private List<EventContainer> OnCharacterDiscoveringMap(MapInstance mapInstance, CreateMap createMap)
        {
            var evts = new List<EventContainer>();

            // OnCharacterDiscoveringMap
            if (createMap.OnCharacterDiscoveringMap != null)
            {
                var onDiscoverEvents = new List<EventContainer>();

                // GenerateMapClock
                if (createMap.OnCharacterDiscoveringMap.GenerateMapClock != null)
                {
                    onDiscoverEvents.Add(new EventContainer(mapInstance, EventActionType.MAPCLOCK,
                            createMap.OnCharacterDiscoveringMap.GenerateMapClock.Value));
                }

                // GenerateClock
                if (createMap.OnCharacterDiscoveringMap.GenerateClock != null)
                {
                    onDiscoverEvents.Add(new EventContainer(mapInstance, EventActionType.CLOCK,
                            createMap.OnCharacterDiscoveringMap.GenerateClock.Value));
                }

                // StartClock
                if (createMap.OnCharacterDiscoveringMap.StartClock != null)
                {
                    onDiscoverEvents.AddRange(StartClock(mapInstance, createMap.OnCharacterDiscoveringMap.StartClock));
                }

                // StartMapClock
                if (createMap.OnCharacterDiscoveringMap.StartMapClock != null)
                {
                    onDiscoverEvents.AddRange(StartMapClock(mapInstance,
                            createMap.OnCharacterDiscoveringMap.StartMapClock));
                }

                // StopClock
                if (createMap.OnCharacterDiscoveringMap.StopClock != null)
                {
                    onDiscoverEvents.Add(new EventContainer(mapInstance, EventActionType.STOPCLOCK, null));
                }

                // StopMapClock
                if (createMap.OnCharacterDiscoveringMap.StopMapClock != null)
                {
                    onDiscoverEvents.Add(new EventContainer(mapInstance, EventActionType.STOPMAPCLOCK, null));
                }

                // NpcDialog
                if (createMap.OnCharacterDiscoveringMap.NpcDialog != null)
                {
                    foreach (var npcdialog in createMap.OnCharacterDiscoveringMap.NpcDialog)
                    {
                        onDiscoverEvents.Add(
                                new EventContainer(mapInstance, EventActionType.NPCDIALOG, npcdialog.Value));
                    }
                }

                // SendMessage
                if (createMap.OnCharacterDiscoveringMap.SendMessage != null)
                {
                    foreach (var SendMessage in createMap.OnCharacterDiscoveringMap.SendMessage)
                    {
                        onDiscoverEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                    }
                }

                // SendPacket
                if (createMap.OnCharacterDiscoveringMap.SendPacket != null)
                {
                    foreach (var sendpacket in createMap.OnCharacterDiscoveringMap.SendPacket)
                    {
                        onDiscoverEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                sendpacket.Value));
                    }
                }

                // Effect
                if (createMap.OnCharacterDiscoveringMap.Effect != null)
                {
                    mapInstance.OnSpawnEvents.Add(new EventContainer(mapInstance, EventActionType.EFFECT,
                            new Tuple<short, int>(createMap.OnCharacterDiscoveringMap.Effect.Value,
                                    createMap.OnCharacterDiscoveringMap.Effect.Delay)));
                }

                // SummonMonster
                onDiscoverEvents.AddRange(SummonMonster(mapInstance,
                    createMap.OnCharacterDiscoveringMap.SummonMonster));

                // SummonNpc
                onDiscoverEvents.AddRange(SummonNpc(mapInstance, createMap.OnCharacterDiscoveringMap.SummonNpc));

                // SpawnPortal
                onDiscoverEvents.AddRange(SpawnPortal(mapInstance, createMap.OnCharacterDiscoveringMap.SpawnPortal));

                // OnMoveOnMap
                if (createMap.OnCharacterDiscoveringMap.OnMoveOnMap != null)
                {
                    onDiscoverEvents.AddRange(OnMoveOnMap(mapInstance,
                            createMap.OnCharacterDiscoveringMap.OnMoveOnMap));
                }

                // Set Monster Lockers
                if (createMap.OnCharacterDiscoveringMap.SetMonsterLockers != null)
                {
                    onDiscoverEvents.Add(new EventContainer(mapInstance, EventActionType.SETMONSTERLOCKERS,
                            createMap.OnCharacterDiscoveringMap.SetMonsterLockers.Value));
                }

                // Set Button Lockers
                if (createMap.OnCharacterDiscoveringMap.SetButtonLockers != null)
                {
                    onDiscoverEvents.Add(new EventContainer(mapInstance, EventActionType.SETBUTTONLOCKERS,
                            createMap.OnCharacterDiscoveringMap.SetButtonLockers.Value));
                }

                // RefreshRaidGoals
                if (createMap.OnCharacterDiscoveringMap.RefreshRaidGoals != null)
                {
                    onDiscoverEvents.Add(new EventContainer(mapInstance, EventActionType.REFRESHRAIDGOAL, null));
                }

                // OnMapClean
                onDiscoverEvents.AddRange(OnMapClean(mapInstance, createMap.OnCharacterDiscoveringMap.OnMapClean));

                // Wave
                if (createMap.OnCharacterDiscoveringMap.Wave != null)
                {
                    onDiscoverEvents.AddRange(Wave(mapInstance, createMap.OnCharacterDiscoveringMap.Wave));
                }

                evts.Add(new EventContainer(mapInstance, EventActionType.REGISTEREVENT,
                    new Tuple<string, List<EventContainer>>(nameof(XMLModel.Events.OnCharacterDiscoveringMap),
                        onDiscoverEvents)));
            }

            return evts;
        }

        private List<EventContainer> OnLockerOpen(MapInstance mapInstance, OnLockerOpen onLockerOpen)
        {
            var evts = new List<EventContainer>();

            if (onLockerOpen != null)
            {
                var onLockerOpenEvents = new List<EventContainer>();

                // GenerateMapClock
                if (onLockerOpen.GenerateMapClock != null)
                {
                    onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.MAPCLOCK,
                            onLockerOpen.GenerateMapClock.Value));
                }

                // GenerateClock
                if (onLockerOpen.GenerateClock != null)
                {
                    onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.CLOCK,
                            onLockerOpen.GenerateClock.Value));
                }

                // StartClock
                if (onLockerOpen.StartClock != null)
                {
                    onLockerOpenEvents.AddRange(StartClock(mapInstance, onLockerOpen.StartClock));
                }

                // StartMapClock
                if (onLockerOpen.StartMapClock != null)
                {
                    onLockerOpenEvents.AddRange(StartMapClock(mapInstance, onLockerOpen.StartMapClock));
                }

                // StopClock
                if (onLockerOpen.StopClock != null)
                {
                    onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.STOPCLOCK, null));
                }

                // StopMapClock
                if (onLockerOpen.StopMapClock != null)
                {
                    onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.STOPMAPCLOCK, null));
                }

                // ChangePortalType
                if (onLockerOpen.ChangePortalType != null)
                {
                    onLockerOpenEvents.AddRange(ChangePortalType(mapInstance, new[] { onLockerOpen.ChangePortalType }));
                }

                // SendMessage
                if (onLockerOpen.SendMessage != null)
                {
                    foreach (var SendMessage in onLockerOpen.SendMessage)
                    {
                        onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                    }
                }

                // RefreshMapItems
                if (onLockerOpen.RefreshMapItems != null)
                {
                    onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.REFRESHMAPITEMS, null));
                }

                // Set Monster Lockers
                if (onLockerOpen.SetMonsterLockers != null)
                {
                    onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.SETMONSTERLOCKERS,
                            onLockerOpen.SetMonsterLockers.Value));
                }

                // Set Button Lockers
                if (onLockerOpen.SetButtonLockers != null)
                {
                    onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.SETBUTTONLOCKERS,
                            onLockerOpen.SetButtonLockers.Value));
                }

                // SummonMonster
                onLockerOpenEvents.AddRange(SummonMonster(mapInstance, onLockerOpen.SummonMonster, true));

                // ClearMapMonsters
                if (onLockerOpen.ClearMapMonsters != null)
                {
                    onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.CLEARMAPMONSTERS, null));
                }

                // StopMapWaves
                if (onLockerOpen.StopMapWaves != null)
                {
                    onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.STOPMAPWAVES, null));
                }

                // End
                if (onLockerOpen.End != null)
                {
                    onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.SCRIPTEND,
                            onLockerOpen.End.Type));
                }

                // NpcDialog
                if (onLockerOpen.NpcDialog != null)
                {
                    foreach (var npcdialog in onLockerOpen.NpcDialog)
                    {
                        onLockerOpenEvents.Add(new EventContainer(mapInstance, EventActionType.NPCDIALOG,
                                npcdialog.Value));
                    }
                }

                // RefreshOnLockerOpen
                onLockerOpenEvents.AddRange(OnLockerOpen(mapInstance, onLockerOpen.RefreshOnLockerOpen));

                evts.Add(new EventContainer(mapInstance, EventActionType.REGISTEREVENT,
                    new Tuple<string, List<EventContainer>>(nameof(XMLModel.Events.OnLockerOpen), onLockerOpenEvents)));
            }

            return evts;
        }

        private List<EventContainer> OnMapClean(MapInstance mapInstance, OnMapClean onMapClean)
        {
            var evts = new List<EventContainer>();

            // OnMapClean
            if (onMapClean != null)
            {
                var onMapCleanEvents = new List<EventContainer>();

                // AddClockTime
                if (onMapClean.AddClockTime != null)
                {
                    onMapCleanEvents.Add(new EventContainer(mapInstance, EventActionType.ADDCLOCKTIME,
                            onMapClean.AddClockTime.Seconds));
                }

                // AddMapClockTime
                if (onMapClean.AddMapClockTime != null)
                {
                    onMapCleanEvents.Add(new EventContainer(mapInstance, EventActionType.ADDMAPCLOCKTIME,
                            onMapClean.AddMapClockTime.Seconds));
                }

                // StopMapClock
                if (onMapClean.StopMapClock != null)
                {
                    onMapCleanEvents.Add(new EventContainer(mapInstance, EventActionType.STOPMAPCLOCK, null));
                }

                // StopMapWaves
                if (onMapClean.StopMapWaves != null)
                {
                    onMapCleanEvents.Add(new EventContainer(mapInstance, EventActionType.STOPMAPWAVES, null));
                }

                // ChangePortalType
                if (onMapClean.ChangePortalType != null)
                {
                    onMapCleanEvents.AddRange(ChangePortalType(mapInstance, onMapClean.ChangePortalType));
                }

                // RefreshMapItems
                if (onMapClean.RefreshMapItems != null)
                {
                    onMapCleanEvents.Add(new EventContainer(mapInstance, EventActionType.REFRESHMAPITEMS, null));
                }

                // SendMessage
                if (onMapClean.SendMessage != null)
                {
                    foreach (var SendMessage in onMapClean.SendMessage)
                    {
                        onMapCleanEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                    }
                }

                // SendPacket
                if (onMapClean.SendPacket != null)
                {
                    foreach (var sendpacket in onMapClean.SendPacket)
                    {
                        onMapCleanEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                sendpacket.Value));
                    }
                }

                // NpcDialog
                if (onMapClean.NpcDialog != null)
                {
                    foreach (var npcdialog in onMapClean.NpcDialog)
                    {
                        onMapCleanEvents.Add(
                                new EventContainer(mapInstance, EventActionType.NPCDIALOG, npcdialog.Value));
                    }
                }

                // SummonMonster
                onMapCleanEvents.AddRange(SummonMonster(mapInstance, onMapClean.SummonMonster));

                // RefreshOnMapClean
                onMapCleanEvents.AddRange(OnMapClean(mapInstance, onMapClean.RefreshOnMapClean));

                evts.Add(new EventContainer(mapInstance, EventActionType.REGISTEREVENT,
                    new Tuple<string, List<EventContainer>>(nameof(XMLModel.Events.OnMapClean), onMapCleanEvents)));
            }

            return evts;
        }

        private List<EventContainer> OnMoveOnMap(MapInstance mapInstance, OnMoveOnMap onMoveOnMap)
        {
            var evts = new List<EventContainer>();

            // OnMoveOnMap
            if (onMoveOnMap != null)
            {
                var onMoveOnMapEvents = new List<EventContainer>();

                // GenerateMapClock
                if (onMoveOnMap.GenerateMapClock != null)
                {
                    onMoveOnMapEvents.Add(new EventContainer(mapInstance, EventActionType.MAPCLOCK,
                            onMoveOnMap.GenerateMapClock.Value));
                }

                // GenerateClock
                if (onMoveOnMap.GenerateClock != null)
                {
                    onMoveOnMapEvents.Add(new EventContainer(mapInstance, EventActionType.CLOCK,
                            onMoveOnMap.GenerateClock.Value));
                }

                // StartClock
                if (onMoveOnMap.StartClock != null)
                {
                    onMoveOnMapEvents.AddRange(StartClock(mapInstance, onMoveOnMap.StartClock));
                }

                // StartMapClock
                if (onMoveOnMap.StartMapClock != null)
                {
                    onMoveOnMapEvents.AddRange(StartMapClock(mapInstance, onMoveOnMap.StartMapClock));
                }

                // StopClock
                if (onMoveOnMap.StopClock != null)
                {
                    onMoveOnMapEvents.Add(new EventContainer(mapInstance, EventActionType.STOPCLOCK, null));
                }

                // StopMapClock
                if (onMoveOnMap.StopMapClock != null)
                {
                    onMoveOnMapEvents.Add(new EventContainer(mapInstance, EventActionType.STOPMAPCLOCK, null));
                }

                // SendMessage
                if (onMoveOnMap.SendMessage != null)
                {
                    foreach (var SendMessage in onMoveOnMap.SendMessage)
                    {
                        onMoveOnMapEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                    }
                }

                // SendPacket
                if (onMoveOnMap.SendPacket != null)
                {
                    foreach (var sendpacket in onMoveOnMap.SendPacket)
                    {
                        onMoveOnMapEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                sendpacket.Value));
                    }
                }

                // Wave
                if (onMoveOnMap.Wave != null)
                {
                    onMoveOnMapEvents.AddRange(Wave(mapInstance, onMoveOnMap.Wave));
                }

                // SummonMonster
                onMoveOnMapEvents.AddRange(SummonMonster(mapInstance, onMoveOnMap.SummonMonster));

                // SummonNpc
                onMoveOnMapEvents.AddRange(SummonNpc(mapInstance, onMoveOnMap.SummonNpc));

                // OnMapClean
                onMoveOnMapEvents.AddRange(OnMapClean(mapInstance, onMoveOnMap.OnMapClean));

                // Set Monster Lockers
                if (onMoveOnMap.SetMonsterLockers != null)
                {
                    onMoveOnMapEvents.Add(new EventContainer(mapInstance, EventActionType.SETMONSTERLOCKERS,
                            onMoveOnMap.SetMonsterLockers.Value));
                }

                // Set Button Lockers
                if (onMoveOnMap.SetButtonLockers != null)
                {
                    onMoveOnMapEvents.Add(new EventContainer(mapInstance, EventActionType.SETBUTTONLOCKERS,
                            onMoveOnMap.SetButtonLockers.Value));
                }

                // RefreshRaidGoals
                if (onMoveOnMap.RefreshRaidGoals != null)
                {
                    onMoveOnMapEvents.Add(new EventContainer(mapInstance, EventActionType.REFRESHRAIDGOAL, null));
                }

                evts.Add(new EventContainer(mapInstance, EventActionType.REGISTEREVENT,
                    new Tuple<string, List<EventContainer>>(nameof(XMLModel.Events.OnMoveOnMap), onMoveOnMapEvents)));
            }

            return evts;
        }

        private List<EventContainer> OnStop(MapInstance mapInstance, OnStop OnStop)
        {
            var evts = new List<EventContainer>();

            if (OnStop.ChangePortalType != null)
            {
                evts.AddRange(ChangePortalType(mapInstance, OnStop.ChangePortalType));
            }

            if (OnStop.SendMessage != null)
            {
                foreach (var SendMessage in OnStop.SendMessage)
                {
                    evts.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                            UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                }
            }

            if (OnStop.SendPacket != null)
            {
                foreach (var sendpacket in OnStop.SendPacket)
                {
                    evts.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET, sendpacket.Value));
                }
            }

            if (OnStop.RefreshMapItems != null)
            {
                evts.Add(new EventContainer(mapInstance, EventActionType.REFRESHMAPITEMS, null));
            }

            return evts;
        }

        private List<EventContainer> OnTimeout(MapInstance mapInstance, OnTimeout OnTimeout)
        {
            var evts = new List<EventContainer>();

            if (OnTimeout.StopMapWaves != null)
            {
                evts.Add(new EventContainer(mapInstance, EventActionType.STOPMAPWAVES, null));
            }

            if (OnTimeout.ChangePortalType != null)
            {
                evts.AddRange(ChangePortalType(mapInstance, OnTimeout.ChangePortalType));
            }

            if (OnTimeout.SendMessage != null)
            {
                foreach (var SendMessage in OnTimeout.SendMessage)
                {
                    evts.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                            UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                }
            }

            if (OnTimeout.NpcDialog != null)
            {
                foreach (var npcdialog in OnTimeout.NpcDialog)
                {
                    evts.Add(new EventContainer(mapInstance, EventActionType.NPCDIALOG, npcdialog.Value));
                }
            }

            if (OnTimeout.SendPacket != null)
            {
                foreach (var sendpacket in OnTimeout.SendPacket)
                {
                    evts.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET, sendpacket.Value));
                }
            }

            if (OnTimeout.RefreshMapItems != null)
            {
                evts.Add(new EventContainer(mapInstance, EventActionType.REFRESHMAPITEMS, null));
            }

            if (OnTimeout.End != null)
            {
                evts.Add(new EventContainer(mapInstance, EventActionType.SCRIPTEND, OnTimeout.End.Type));
            }

            // ClearMapMonsters
            if (OnTimeout.ClearMapMonsters != null)
            {
                evts.Add(new EventContainer(mapInstance, EventActionType.CLEARMAPMONSTERS, null));
            }

            return evts;
        }

        private List<EventContainer> SpawnButton(MapInstance mapInstance, SpawnButton[] spawnButton)
        {
            var evts = new List<EventContainer>();

            // SpawnButton
            if (spawnButton != null)
            {
                foreach (var spawn in spawnButton)
                {
                    var positionX = spawn.PositionX;
                    var positionY = spawn.PositionY;

                    if (positionX == 0 || positionY == 0)
                    {
                        var cell = mapInstance?.Map?.GetRandomPosition();
                        if (cell != null)
                        {
                            positionX = cell.X;
                            positionY = cell.Y;
                        }
                    }

                    var button = new MapButton(spawn.Id, positionX, positionY, spawn.VNumEnabled, spawn.VNumDisabled,
                            new List<EventContainer>(), new List<EventContainer>(), new List<EventContainer>());

                    // OnFirstEnable
                    if (spawn.OnFirstEnable != null)
                    {
                        var onFirst = new List<EventContainer>();

                        // SummonMonster
                        onFirst.AddRange(SummonMonster(mapInstance, spawn.OnFirstEnable.SummonMonster));

                        // Wave
                        if (spawn.OnFirstEnable.Wave != null)
                        {
                            onFirst.AddRange(Wave(mapInstance, spawn.OnFirstEnable.Wave));
                        }

                        // GenerateClock
                        if (spawn.OnFirstEnable.GenerateClock != null)
                        {
                            onFirst.Add(new EventContainer(mapInstance, EventActionType.CLOCK, spawn.OnFirstEnable.GenerateClock.Value));
                        }

                        //SendPacket
                        if (spawn.OnFirstEnable.SendPacket != null)
                        {
                            foreach (SendPacket sendpacket in spawn.OnFirstEnable.SendPacket)
                            {
                                onFirst.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET, sendpacket.Value));
                            }
                        }

                        // Teleport
                        if (spawn.OnFirstEnable.Teleport != null)
                        {
                            onFirst.Add(new EventContainer(mapInstance, EventActionType.TELEPORT,
                                    new Tuple<short, short, short, short>(spawn.OnFirstEnable.Teleport.PositionX,
                                            spawn.OnFirstEnable.Teleport.PositionY, spawn.OnFirstEnable.Teleport.DestinationX,
                                            spawn.OnFirstEnable.Teleport.DestinationY)));
                        }

                        // RemoveButtonLocker
                        if (spawn.OnFirstEnable.RemoveButtonLocker != null)
                        {
                            onFirst.Add(new EventContainer(mapInstance, EventActionType.REMOVEBUTTONLOCKER, null));
                        }

                        // RefreshRaidGoals
                        if (spawn.OnFirstEnable.RefreshRaidGoals != null)
                        {
                            onFirst.Add(new EventContainer(mapInstance, EventActionType.REFRESHRAIDGOAL, null));
                        }

                        // SendMessage
                        if (spawn.OnFirstEnable.SendMessage != null)
                        {
                            foreach (var SendMessage in spawn.OnFirstEnable.SendMessage)
                            {
                                onFirst.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                        UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                            }
                        }

                        // OnMapClean
                        if (spawn.OnFirstEnable.OnMapClean != null)
                        {
                            onFirst.AddRange(OnMapClean(mapInstance, spawn.OnFirstEnable.OnMapClean));
                        }

                        // ChangePortalType
                        if (spawn.OnFirstEnable.ChangePortalType != null)
                        {
                            onFirst.AddRange(ChangePortalType(mapInstance, spawn.OnFirstEnable.ChangePortalType));
                        }

                        // RefreshMapItems
                        if (spawn.OnFirstEnable.RefreshMapItems != null)
                        {
                            onFirst.Add(new EventContainer(mapInstance, EventActionType.REFRESHMAPITEMS, null));
                        }

                        // StopMapWaves
                        if (spawn.OnFirstEnable.StopMapWaves != null)
                        {
                            onFirst.Add(new EventContainer(mapInstance, EventActionType.STOPMAPWAVES, null));
                        }

                        if (spawn.OnFirstEnable.NpcDialog != null)
                        {
                            foreach (var npcdialog in spawn.OnFirstEnable.NpcDialog)
                            {
                                onFirst.Add(new EventContainer(mapInstance, EventActionType.NPCDIALOG,
                                        npcdialog.Value));
                            }
                        }

                        button.FirstEnableEvents.AddRange(onFirst);
                    }

                    // OnEnable & Teleport
                    if (spawn.OnEnable?.Teleport != null)
                    {
                        button.EnableEvents.Add(new EventContainer(mapInstance, EventActionType.TELEPORT,
                                new Tuple<short, short, short, short>(spawn.OnEnable.Teleport.PositionX,
                                        spawn.OnEnable.Teleport.PositionY, spawn.OnEnable.Teleport.DestinationX,
                                        spawn.OnEnable.Teleport.DestinationY)));
                    }

                    if (spawn.OnEnable?.NpcDialog != null)
                    {
                        foreach (var npcdialog in spawn.OnEnable?.NpcDialog)
                        {
                            button.EnableEvents.Add(new EventContainer(mapInstance, EventActionType.NPCDIALOG,
                                    npcdialog.Value));
                        }
                    }

                    // OnDisable & Teleport
                    if (spawn.OnDisable?.Teleport != null)
                    {
                        button.DisableEvents.Add(new EventContainer(mapInstance, EventActionType.TELEPORT,
                                new Tuple<short, short, short, short>(spawn.OnDisable.Teleport.PositionX,
                                        spawn.OnDisable.Teleport.PositionY, spawn.OnDisable.Teleport.DestinationX,
                                        spawn.OnDisable.Teleport.DestinationY)));
                    }

                    if (spawn.OnDisable?.NpcDialog != null)
                    {
                        foreach (var npcdialog in spawn.OnDisable?.NpcDialog)
                        {
                            button.DisableEvents.Add(new EventContainer(mapInstance, EventActionType.NPCDIALOG,
                                    npcdialog.Value));
                        }
                    }

                    evts.Add(new EventContainer(mapInstance, EventActionType.SPAWNBUTTON, button));
                }
            }

            return evts;
        }

        private List<EventContainer> SpawnPortal(MapInstance mapInstance, SpawnPortal[] spawnPortal)
        {
            var evts = new List<EventContainer>();

            // SpawnPortal
            if (spawnPortal != null)
            {
                foreach (var portalEvent in spawnPortal)
                {
                    _mapInstanceDictionary.TryGetValue(portalEvent.ToMap, out var destinationMap);
                    var portal = new Portal
                    {
                        PortalId = portalEvent.IdOnMap,
                        SourceX = portalEvent.PositionX,
                        SourceY = portalEvent.PositionY,
                        Type = portalEvent.Type,
                        DestinationX = portalEvent.ToX,
                        DestinationY = portalEvent.ToY,
                        DestinationMapId = (short)(destinationMap?.MapInstanceId == default ? -1 : 0),
                        SourceMapInstanceId = mapInstance.MapInstanceId,
                        DestinationMapInstanceId = destinationMap?.MapInstanceId ?? Guid.Empty
                    };

                    // OnTraversal
                    if (portalEvent.OnTraversal?.End != null)
                    {
                        portal.OnTraversalEvents.Add(new EventContainer(mapInstance, EventActionType.SCRIPTEND,
                                portalEvent.OnTraversal.End.Type));
                    }

                    if (portalEvent.OnTraversal?.NpcDialog != null)
                    {
                        foreach (var npcdialog in portalEvent.OnTraversal.NpcDialog)
                        {
                            portal.OnTraversalEvents.Add(new EventContainer(mapInstance, EventActionType.NPCDIALOG,
                                    npcdialog.Value));
                        }
                    }

                    evts.Add(new EventContainer(mapInstance, EventActionType.SPAWNPORTAL, portal));
                }
            }

            return evts;
        }

        private List<EventContainer> StartClock(MapInstance mapInstance, StartClock StartClock)
        {
            var evts = new List<EventContainer>();

            var onStop = new List<EventContainer>();
            var onTimeout = new List<EventContainer>();

            // OnStop
            if (StartClock.OnStop != null)
            {
                onStop.AddRange(OnStop(mapInstance, StartClock.OnStop));
            }

            // OnTimeout
            if (StartClock.OnTimeout != null)
            {
                onTimeout.AddRange(OnTimeout(mapInstance, StartClock.OnTimeout));
            }

            evts.Add(new EventContainer(mapInstance, EventActionType.STARTCLOCK,
                new Tuple<List<EventContainer>, List<EventContainer>>(onStop, onTimeout)));

            return evts;
        }

        private List<EventContainer> StartMapClock(MapInstance mapInstance, StartClock StartMapClock)
        {
            var evts = new List<EventContainer>();

            var onStop = new List<EventContainer>();
            var onTimeout = new List<EventContainer>();

            // OnStop
            if (StartMapClock.OnStop != null)
            {
                onStop.AddRange(OnStop(mapInstance, StartMapClock.OnStop));
            }

            // OnTimeout
            if (StartMapClock.OnTimeout != null)
            {
                onTimeout.AddRange(OnTimeout(mapInstance, StartMapClock.OnTimeout));
            }

            evts.Add(new EventContainer(mapInstance, EventActionType.STARTMAPCLOCK,
                new Tuple<List<EventContainer>, List<EventContainer>>(onStop, onTimeout)));

            return evts;
        }

        private List<EventContainer> SummonMonster(MapInstance mapInstance, SummonMonster[] summonMonster,
            bool isChildMonster = false)
        {
            var evts = new List<EventContainer>();

            // SummonMonster
            if (summonMonster != null)
            {
                foreach (var summon in summonMonster)
                {
                    MonsterAmount++;
                    var monster = new MonsterToSummon(summon.VNum,
                            new MapCell { X = summon.PositionX, Y = summon.PositionY }, null, summon.Move, summon.IsTarget,
                            summon.IsBonus, summon.IsHostile, summon.IsBoss);

                    //NoticeRange
                    monster.NoticeRange = summon.NoticeRange;

                    //HasDelay
                    monster.HasDelay = summon.HasDelay;

                    // Meteorite
                    monster.IsMeteorite = summon.IsMeteorite;
                    monster.Damage = summon.Damage;

                    // UseSkillOnDamage
                    summon.UseSkillOnDamage?.ToList()
                          .ForEach(s =>
                          {
                              monster.UseSkillOnDamage.Add(new UseSkillOnDamage
                              {
                                  SkillVNum = s.SkillVNum,
                                  HpPercent = s.HpPercent
                              });
                          });

                    // OnDeath
                    if (summon.OnDeath != null)
                    {
                        // ThrowItem
                        if (summon.OnDeath.ThrowItem != null)
                        {
                            foreach (var throwItem in summon.OnDeath.ThrowItem)
                            {
                                monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.THROWITEMS,
                                        new Tuple<int, short, byte, int, int, short>(-1, throwItem.VNum,
                                                throwItem.PackAmount == 0 ? (byte)1 : throwItem.PackAmount,
                                                throwItem.MinAmount == 0 ? 1 : throwItem.MinAmount,
                                                throwItem.MaxAmount == 0 ? 1 : throwItem.MaxAmount, throwItem.Delay)));
                            }
                        }

                        // AddClockTime
                        if (summon.OnDeath.AddClockTime != null)
                        {
                            monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.ADDCLOCKTIME,
                                    summon.OnDeath.AddClockTime.Seconds));
                        }

                        // AddMapClockTime
                        if (summon.OnDeath.AddMapClockTime != null)
                        {
                            monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.ADDMAPCLOCKTIME,
                                    summon.OnDeath.AddMapClockTime.Seconds));
                        }

                        // RemoveButtonLocker
                        if (summon.OnDeath.RemoveButtonLocker != null)
                        {
                            monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.REMOVEBUTTONLOCKER,
                                    null));
                        }

                        // RemoveMonsterLocker
                        if (summon.OnDeath.RemoveMonsterLocker != null)
                        {
                            monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.REMOVEMONSTERLOCKER,
                                    null));
                        }

                        // ChangePortalType
                        if (summon.OnDeath.ChangePortalType != null)
                        {
                            monster.DeathEvents.AddRange(ChangePortalType(mapInstance,
                                    summon.OnDeath.ChangePortalType));
                        }

                        // SendMessage
                        if (summon.OnDeath.SendMessage != null)
                        {
                            foreach (var SendMessage in summon.OnDeath.SendMessage)
                            {
                                monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                        UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                            }
                        }

                        // NpcDialog
                        if (summon.OnDeath.NpcDialog != null)
                        {
                            foreach (var npcdialog in summon.OnDeath.NpcDialog)
                            {
                                monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.NPCDIALOG,
                                        npcdialog.Value));
                            }
                        }

                        // SendPacket
                        if (summon.OnDeath.SendPacket != null)
                        {
                            foreach (var sendpacket in summon.OnDeath.SendPacket)
                            {
                                monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                        sendpacket.Value));
                            }
                        }

                        // RefreshRaidGoals
                        if (summon.OnDeath.RefreshRaidGoals != null)
                        {
                            monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.REFRESHRAIDGOAL,
                                    null));
                        }

                        // End
                        if (summon.OnDeath.End != null)
                        {
                            monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.SCRIPTEND,
                                    summon.OnDeath.End.Type));
                        }

                        // StopMapClock
                        if (summon.OnDeath.StopMapClock != null)
                        {
                            monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.STOPMAPCLOCK,
                                    null));
                        }

                        // RefreshMapItems
                        if (summon.OnDeath.RefreshMapItems != null)
                        {
                            monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.REFRESHMAPITEMS,
                                    null));
                        }

                        // Wave
                        if (summon.OnDeath.Wave != null)
                        {
                            monster.DeathEvents.AddRange(Wave(mapInstance, summon.OnDeath.Wave));
                        }

                        // StopMapWaves
                        if (summon.OnDeath.StopMapWaves != null)
                        {
                            monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.STOPMAPWAVES,
                                    null));
                        }

                        // RemoveButtonLocker
                        if (summon.OnDeath.ClearMapMonsters != null)
                        {
                            monster.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.CLEARMAPMONSTERS,
                                    null));
                        }

                        // SummonMonster Child
                        if (!isChildMonster)
                        {
                            monster.DeathEvents.AddRange(
                                    SummonMonster(mapInstance, summon.OnDeath?.SummonMonster, true));
                        }
                    }

                    // OnNoticing
                    if (summon.OnNoticing != null)
                    {
                        // Effect
                        if (summon.OnNoticing.Effect != null)
                        {
                            monster.NoticingEvents.Add(new EventContainer(mapInstance, EventActionType.EFFECT,
                                    new Tuple<short, int>(summon.OnNoticing.Effect.Value, summon.OnNoticing.Effect.Delay)));
                        }

                        // Move
                        if (summon.OnNoticing.Move != null)
                        {
                            var events = new List<EventContainer>();

                            // Effect
                            if (summon.OnNoticing.Move.Effect != null)
                            {
                                events.Add(new EventContainer(mapInstance, EventActionType.EFFECT,
                                        new Tuple<short, int>(summon.OnNoticing.Move.Effect.Value,
                                                summon.OnNoticing.Move.Effect.Delay)));
                            }

                            monster.NoticingEvents.Add(new EventContainer(mapInstance, EventActionType.MOVE,
                                    new ZoneEvent
                                    {
                                        X = summon.OnNoticing.Move.PositionX,
                                        Y = summon.OnNoticing.Move.PositionY,
                                        Events = events
                                    }));
                        }

                        // SummonMonster Child
                        if (!isChildMonster)
                        {
                            monster.NoticingEvents.AddRange(SummonMonster(mapInstance, summon.OnDeath?.SummonMonster,
                                    true));
                        }
                    }

                    // SendMessage
                    if (summon.SendMessage != null)
                    {
                        foreach (var SendMessage in summon.SendMessage)
                        {
                            monster.SpawnEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                    UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                        }
                    }

                    // Effect
                    if (summon.Effect != null)
                    {
                        monster.SpawnEvents.Add(new EventContainer(mapInstance, EventActionType.EFFECT,
                                new Tuple<short, int>(summon.Effect.Value, summon.Effect.Delay)));
                    }

                    // RemoveAfter
                    if (summon.RemoveAfter != null)
                    {
                        monster.AfterSpawnEvents.Add(new EventContainer(mapInstance, EventActionType.REMOVEAFTER,
                                summon.RemoveAfter.Value));
                    }

                    // Move to coords on spawn
                    if (summon.Roam != null)
                    {
                        monster.SpawnEvents.Add(new EventContainer(mapInstance, EventActionType.MOVE,
                                new ZoneEvent { X = summon.Roam.FirstX, Y = summon.Roam.FirstY }));
                    }

                    evts.Add(new EventContainer(mapInstance, EventActionType.SPAWNMONSTER, monster));
                }
            }

            return evts;
        }

        private List<EventContainer> SummonNpc(MapInstance mapInstance, SummonNpc[] summonNpc, bool isChildNpc = false)
        {
            var evts = new List<EventContainer>();

            if (summonNpc != null)
            {
                foreach (var summon in summonNpc)
                {
                    var positionX = summon.PositionX;
                    var positionY = summon.PositionY;

                    if (positionX == 0 || positionY == 0)
                    {
                        var cell = mapInstance?.Map?.GetRandomPosition();
                        if (cell != null)
                        {
                            positionX = cell.X;
                            positionY = cell.Y;
                        }
                    }

                    NpcAmount++;
                    var npcToSummon = new NpcToSummon(summon.VNum, new MapCell { X = positionX, Y = positionY }, -1,
                            summon.IsProtected, summon.IsMate, summon.Move, summon.IsHostile, summon.IsTsReward)
                    {
                        Dir = summon.Dir
                    };

                    // OnDeath
                    if (summon.OnDeath != null)
                    {
                        // ThrowItem
                        if (summon.OnDeath.ThrowItem != null)
                        {
                            foreach (var throwItem in summon.OnDeath.ThrowItem)
                            {
                                npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.THROWITEMS,
                                        new Tuple<int, short, byte, int, int, short>(-1, throwItem.VNum,
                                                throwItem.PackAmount == 0 ? (byte)1 : throwItem.PackAmount,
                                                throwItem.MinAmount == 0 ? 1 : throwItem.MinAmount,
                                                throwItem.MaxAmount == 0 ? 1 : throwItem.MaxAmount, throwItem.Delay)));
                            }
                        }

                        // AddClockTime
                        if (summon.OnDeath.AddClockTime != null)
                        {
                            npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.ADDCLOCKTIME,
                                    summon.OnDeath.AddClockTime.Seconds));
                        }

                        // AddMapClockTime
                        if (summon.OnDeath.AddMapClockTime != null)
                        {
                            npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.ADDMAPCLOCKTIME,
                                    summon.OnDeath.AddMapClockTime.Seconds));
                        }

                        // RemoveButtonLocker
                        if (summon.OnDeath.RemoveButtonLocker != null)
                        {
                            npcToSummon.DeathEvents.Add(new EventContainer(mapInstance,
                                    EventActionType.REMOVEBUTTONLOCKER, null));
                        }

                        // RemoveMonsterLocker
                        if (summon.OnDeath.RemoveMonsterLocker != null)
                        {
                            npcToSummon.DeathEvents.Add(new EventContainer(mapInstance,
                                    EventActionType.REMOVEMONSTERLOCKER, null));
                        }

                        // ChangePortalType
                        if (summon.OnDeath.ChangePortalType != null)
                        {
                            npcToSummon.DeathEvents.AddRange(ChangePortalType(mapInstance,
                                    summon.OnDeath.ChangePortalType));
                        }

                        // SendMessage
                        if (summon.OnDeath.SendMessage != null)
                        {
                            foreach (var SendMessage in summon.OnDeath.SendMessage)
                            {
                                npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                        UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                            }
                        }

                        // NpcDialog
                        if (summon.OnDeath.NpcDialog != null)
                        {
                            foreach (var npcdialog in summon.OnDeath.NpcDialog)
                            {
                                npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.NPCDIALOG,
                                        npcdialog.Value));
                            }
                        }

                        // SendPacket
                        if (summon.OnDeath.SendPacket != null)
                        {
                            foreach (var sendpacket in summon.OnDeath.SendPacket)
                            {
                                npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                        sendpacket.Value));
                            }
                        }

                        // RefreshRaidGoals
                        if (summon.OnDeath.RefreshRaidGoals != null)
                        {
                            npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.REFRESHRAIDGOAL,
                                    null));
                        }

                        // End
                        if (summon.OnDeath.End != null)
                        {
                            npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.SCRIPTEND,
                                    summon.OnDeath.End.Type));
                        }

                        // StopMapClock
                        if (summon.OnDeath.StopMapClock != null)
                        {
                            npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.STOPMAPCLOCK,
                                    null));
                        }

                        // RefreshMapItems
                        if (summon.OnDeath.RefreshMapItems != null)
                        {
                            npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.REFRESHMAPITEMS,
                                    null));
                        }

                        // Wave
                        if (summon.OnDeath.Wave != null)
                        {
                            npcToSummon.DeathEvents.AddRange(Wave(mapInstance, summon.OnDeath.Wave));
                        }

                        // StopMapWaves
                        if (summon.OnDeath.StopMapWaves != null)
                        {
                            npcToSummon.DeathEvents.Add(new EventContainer(mapInstance, EventActionType.STOPMAPWAVES,
                                    null));
                        }

                        // RemoveButtonLocker
                        if (summon.OnDeath.ClearMapMonsters != null)
                        {
                            npcToSummon.DeathEvents.Add(new EventContainer(mapInstance,
                                    EventActionType.CLEARMAPMONSTERS, null));
                        }

                        // SummonNpc Child
                        if (!isChildNpc)
                        {
                            npcToSummon.DeathEvents.AddRange(SummonNpc(mapInstance, summon.OnDeath?.SummonNpc, true));
                        }
                    }

                    // Effect
                    if (summon.Effect != null)
                    {
                        npcToSummon.SpawnEvents.Add(new EventContainer(mapInstance, EventActionType.EFFECT,
                                new Tuple<short, int>(summon.Effect.Value, summon.Effect.Delay)));
                    }

                    // Move to coords on spawn
                    if (summon.Roam != null)
                    {
                        npcToSummon.SpawnEvents.Add(new EventContainer(mapInstance, EventActionType.MOVE,
                                new ZoneEvent { X = summon.Roam.FirstX, Y = summon.Roam.FirstY }));
                    }

                    evts.Add(new EventContainer(mapInstance, EventActionType.SPAWNNPC, npcToSummon));
                }
            }

            return evts;
        }

        private List<EventContainer> Wave(MapInstance mapInstance, Wave[] Wave)
        {
            var evts = new List<EventContainer>();

            foreach (var wave in Wave)
            {
                var waveEvent = new List<EventContainer>();

                // SummonMonster
                waveEvent.AddRange(SummonMonster(mapInstance, wave.SummonMonster));

                // SummonNpc
                waveEvent.AddRange(SummonNpc(mapInstance, wave.SummonNpc));

                // SendMessage
                if (wave.SendMessage != null)
                {
                    foreach (var SendMessage in wave.SendMessage)
                    {
                        waveEvent.Add(new EventContainer(mapInstance, EventActionType.SENDPACKET,
                                UserInterfaceHelper.GenerateMsg(SendMessage.Value, SendMessage.Type)));
                    }
                }

                evts.Add(new EventContainer(mapInstance, EventActionType.REGISTERWAVE,
                    new EventWave(wave.Delay, waveEvent, wave.Offset,
                        wave.RunTimes > 0 ? wave.RunTimes : short.MaxValue)));
            }

            return evts;
        }

        #endregion
    }
}