
using Frostvein.Core;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject._Event;
using Frostvein.GameObject.Battle;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using Frostvein.GameObject.Service;
using Frostvein.PathFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Frostvein.Domain.BCardType;

namespace Frostvein.GameObject
{
    public class MapNpc : MapNpcDTO
    {
        #region Members

        public NpcMonster Npc;

        private int _movetime;

        private Random _random;

        #endregion

        #region Instantiation

        public MapNpc()
        {
            PVELockObject = new object();
            OnSpawnEvents = new List<EventContainer>();
        }

        public MapNpc(MapNpcDTO input) : this()
        {
            Dialog = input.Dialog;
            Effect = input.Effect;
            EffectDelay = input.EffectDelay;
            IsDisabled = input.IsDisabled;
            IsMoving = input.IsMoving;
            IsSitting = input.IsSitting;
            MapId = input.MapId;
            MapNpcId = input.MapNpcId;
            MapX = input.MapX;
            MapY = input.MapY;
            Name = input.Name;
            NpcVNum = input.NpcVNum;
            Position = input.Position;
            Say = input.Say;
            Delay = input.Delay;
            Information = input.Information;
        }

        #endregion

        #region Properties

        public int AliveTime { get; set; }

        public BattleEntity BattleEntity { get; set; }

        public EventEntity Event { get; set; }

        public Node[][] BrushFireJagged { get; set; }

        public ThreadSafeSortedList<short, Buff> Buff => BattleEntity.Buffs;

        public ThreadSafeSortedList<short, IDisposable> BuffObservables => BattleEntity.BuffObservables;

        public double CurrentHp { get; set; }

        public double CurrentMp { get; set; }

        public DateTime Death { get; set; }

        public bool EffectActivated { get; set; }

        public short FirstX { get; set; }

        public short FirstY { get; set; }

        public bool Invisible { get; private set; }

        public bool IsAlive { get; set; }

        public bool IsHostile { get; set; }

        public bool IsMate { get; set; }

        public bool IsOut { get; set; }

        public byte Score { get; set; }

        public bool IsProtected { get; set; }

        public bool IsTsReward { get; set; }

        public DateTime LastDefence { get; set; }

        public DateTime LastEffect { get; set; }

        public DateTime LastMonsterAggro { get; set; }

        public DateTime LastMove { get; private set; }

        public DateTime LastProtectedEffect { get; private set; }

        public DateTime LastSkill { get; private set; }

        public IDisposable LifeEvent { get; set; }

        public MapInstance MapInstance { get; set; }

        public double MaxHp { get; set; }

        public double MaxMp { get; set; }

        public List<EventContainer> OnDeathEvents => BattleEntity.OnDeathEvents;

        public List<EventContainer> OnSpawnEvents { get; set; }

        public BattleEntity Owner { get; set; }

        public Character OwnerZ { get; set; }

        public List<Node> Path { get; set; }

        public object PVELockObject { get; set; }

        public List<Recipe> Recipes { get; set; }

        public short RunToX { get; set; }

        public short RunToY { get; set; }

        public Shop Shop { get; set; }

        public bool? ShouldRespawn { get; set; }

        public List<NpcMonsterSkill> Skills { get; set; }

        public bool Started { get; internal set; }

        public long Target { get; set; }

        public List<TeleporterDTO> Teleporters { get; set; }

        #endregion

        #region Methods

        public void AddBuff(Buff indicator, BattleEntity battleEntity)
        {
            BattleEntity.AddBuff(indicator, battleEntity);
        }

        public void DisableBuffs(BuffType type, int level = 100)
        {
            BattleEntity.DisableBuffs(type, level);
        }

        public void DisableBuffs(List<BuffType> types, int level = 100)
        {
            BattleEntity.DisableBuffs(types, level);
        }

        public string GenerateIn(InRespawnType respawnType = InRespawnType.NoEffect)
        {
            var npcinfo = ServerManager.GetNpcMonster(NpcVNum);
            if (npcinfo == null || IsDisabled || !IsAlive)
            {
                return "";
            }

            IsOut = false;
            return StaticPacketHelper.In(UserType.Npc,
                Npc.OriginalNpcMonsterVNum > 0 ? Npc.OriginalNpcMonsterVNum : NpcVNum, MapNpcId, MapX, MapY, Position,
                100, 100, Dialog, respawnType, IsSitting, string.IsNullOrEmpty(Name) ? "-" : Name, Invisible, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        public string GenerateOut()
        {
            var npcinfo = ServerManager.GetNpcMonster(NpcVNum);
            if (npcinfo == null || IsDisabled)
            {
                return "";
            }

            IsOut = true;
            return $"out 2 {MapNpcId}";
        }

        public string GenerateSay(string message, int type) => $"say 2 {MapNpcId} 2 {message}";

        public int[] GetBuff(CardType type, byte subtype) => BattleEntity.GetBuff(type, subtype);

        public string GetNpcDialog() => $"npc_req 2 {MapNpcId} {Dialog}";

        public bool HasBuff(CardType type, byte subtype) => BattleEntity.HasBuff(type, subtype);

        public void Initialize(MapInstance currentMapInstance)
        {
            MapInstance = currentMapInstance;
            Initialize();
        }

        public void Initialize()
        {
            if (MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance &&
                ServerManager.Instance.MapBossVNums.Contains(NpcVNum))
            {
                var randomCell = MapInstance.Map.GetRandomPosition();
                if (randomCell != null)
                {
                    if (MapInstance.Portals.Any(s => Map.GetDistance(new MapCell { X = s.SourceX, Y = s.SourceY },
                                                         new MapCell { X = randomCell.X, Y = randomCell.Y }) < 5))
                    {
                        randomCell = MapInstance.Map.GetRandomPosition();
                    }

                    MapX = randomCell.X;
                    MapY = randomCell.Y;
                }
            }

            _random = new Random(MapNpcId);
            Npc = ServerManager.GetNpcMonster(NpcVNum);
            MaxHp = Npc.MaxHP;
            MaxMp = Npc.MaxMP;

            if (MapInstance?.MapInstanceType == MapInstanceType.TimeSpaceInstance)
            {
                if (IsProtected)
                {
                    MaxHp *= 2;
                    MaxMp *= 2;
                }
            }

            IsAlive = true;
            CurrentHp = MaxHp;
            CurrentMp = MaxMp;
            LastEffect = DateTime.Now;
            LastProtectedEffect = DateTime.Now;
            LastMove = DateTime.Now;
            LastSkill = DateTime.Now;
            IsHostile = Npc.IsHostile;
            ShouldRespawn = ShouldRespawn ?? true;
            FirstX = MapX;
            FirstY = MapY;
            EffectActivated = true;
            _movetime = ServerManager.RandomNumber(500, 3000);
            Path = new List<Node>();
            Recipes = ServerManager.Instance.GetRecipesByMapNpcId(MapNpcId);
            Target = -1;
            Teleporters = ServerManager.Instance.GetTeleportersByNpcVNum(MapNpcId);
            var shop = ServerManager.Instance.GetShopByMapNpcId(MapNpcId);
            if (shop != null)
            {
                shop.Initialize();
                Shop = shop;
            }

            Skills = new List<NpcMonsterSkill>();
            foreach (var ski in Npc.Skills)
            {
                Skills.Add(new NpcMonsterSkill { SkillVNum = ski.SkillVNum, Rate = ski.Rate });
            }

            BattleEntity = new BattleEntity(this);
            Event = new EventEntity(this);

            if (AliveTime > 0)
            {
                var AliveTimeThread = new Thread(() => AliveTimeCheck());
                AliveTimeThread.Start();
            }

            if (NpcVNum == 1408)
            {
                OnDeathEvents.Add(new EventContainer(MapInstance, EventActionType.SPAWNMONSTER,
                        new MonsterToSummon(621, new MapCell { X = MapX, Y = MapY }, null, true)));
            }

            if (NpcVNum == 1409)
            {
                OnDeathEvents.Add(new EventContainer(MapInstance, EventActionType.SPAWNMONSTER,
                        new MonsterToSummon(622, new MapCell { X = MapX, Y = MapY }, null, true)));
            }

            if (NpcVNum == 1410)
            {
                OnDeathEvents.Add(new EventContainer(MapInstance, EventActionType.SPAWNMONSTER,
                        new MonsterToSummon(623, new MapCell { X = MapX, Y = MapY }, null, true)));
            }

            if (OnSpawnEvents.Any())
            {
                OnSpawnEvents.ToList().ForEach(e => { EventHelper.Instance.RunEvent(e, npc: this); });
                OnSpawnEvents.Clear();
            }
        }

        /// <summary>
        ///     Check if the Monster is in the given Range.
        /// </summary>
        /// <param name="mapX">The X coordinate on the Map of the object to check.</param>
        /// <param name="mapY">The Y coordinate on the Map of the object to check.</param>
        /// <param name="distance">The maximum distance of the object to check.</param>
        /// <returns>True if the Monster is in range, False if not.</returns>
        public bool IsInRange(short mapX, short mapY, byte distance) => Map.GetDistance(new MapCell
        {
            X = mapX,
            Y = mapY
        }, new MapCell
        {
            X = MapX,
            Y = MapY
        }) <= distance;

        public void RemoveBuff(short cardId)
        {
            BattleEntity.RemoveBuff(cardId);
        }

        public void RunDeathEvent()
        {
            MapInstance.InstanceBag.NpcsKilled++;
            OnDeathEvents.ForEach(e =>
            {
                if (e.EventActionType == EventActionType.THROWITEMS)
                {
                    Tuple<int, short, byte, int, int> evt = (Tuple<int, short, byte, int, int>)e.Parameter;
                    e.Parameter = new Tuple<int, short, byte, int, int>(MapNpcId, evt.Item2, evt.Item3, evt.Item4, evt.Item5);
                }
                EventHelper.Instance.RunEvent(e);
            });

            if (OnDeathEvents.Any(s => s.EventActionType == EventActionType.SPAWNMONSTERS)
            && (List<MonsterToSummon>)OnDeathEvents.FirstOrDefault(e => e.EventActionType == EventActionType.SPAWNMONSTERS).Parameter is List<MonsterToSummon> summonParameters)
            {
                Parallel.ForEach(summonParameters, npcMonster =>
                {
                    npcMonster.SpawnCell.X = MapX;
                    npcMonster.SpawnCell.Y = MapY;
                });
            }
            if (OnDeathEvents.Any(s => s.EventActionType == EventActionType.SPAWNNPC)
            && (NpcToSummon)OnDeathEvents.FirstOrDefault(e => e.EventActionType == EventActionType.SPAWNNPC).Parameter is NpcToSummon npcMonsterToSummon)
            {
                npcMonsterToSummon.SpawnCell.X = MapX;
                npcMonsterToSummon.SpawnCell.Y = MapY;
            }

            OnDeathEvents.RemoveAll(s => s != null);
        }

        public void SetDeathStatement()
        {
            if (Npc.BCards.Any(s =>
                s.Type == (byte)CardType.NoDefeatAndNoDamage &&
                s.SubType == (byte)AdditionalTypes.NoDefeatAndNoDamage.DecreaseHPNoDeath && s.FirstData == -1))
            {
                CurrentHp = MaxHp;
                return;
            }

            IsAlive = false;
            CurrentHp = 0;
            CurrentMp = 0;
            Death = DateTime.Now;
            LastMove = DateTime.Now;
            DisableBuffs(BuffType.All);

            // Respawn
            if (ShouldRespawn != null && !ShouldRespawn.Value)
            {
                MapInstance.RemoveNpc(this);
                MapInstance.Broadcast(GenerateOut());
            }
        }

        /// <summary>
        ///     Remove the current Target from Npc.
        /// </summary>
        internal void RemoveTarget()
        {
            if (Target != -1)
            {
                Path.Clear();
                Target = -1;

                //return to origin
                Path = BestFirstSearch.FindPathJagged(new Node { X = MapX, Y = MapY }, new Node { X = FirstX, Y = FirstY },
                    MapInstance.Map.JaggedGrid);
            }
        }

        internal void StartLife()
        {
            try
            {
                if (!MapInstance.IsSleeping)
                {
                    npcLife();
                }
            }
            catch (Exception e)
            {
                //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
            }
        }

        private void AliveTimeCheck()
        {
            var PercentPerSecond = 100 / (double)AliveTime;
            for (var i = 0; i < AliveTime; i++)
            {
                if (!IsAlive || CurrentHp <= 0)
                {
                    return;
                }

                CurrentHp -= MaxHp * PercentPerSecond / 100;
                if (CurrentHp <= 0)
                {
                    break;
                }

                Thread.Sleep(1000);
            }

            MapInstance.Broadcast(StaticPacketHelper.Out(UserType.Npc, MapNpcId));
            MapInstance.RemoveNpc(this);
        }

        private void npcLife()
        {
            // Respawn
            if (CurrentHp <= 0 && ShouldRespawn != null && !ShouldRespawn.Value)
            {
                MapInstance.RemoveNpc(this);
                 MapInstance.Broadcast(GenerateOut());
            }

            if (!IsAlive && ShouldRespawn != null && ShouldRespawn.Value)
            {
                var timeDeath = (DateTime.Now - Death).TotalSeconds;
                if (timeDeath >= Npc.RespawnTime / 10d)
                {
                    Respawn();
                }
            }

            if (LastProtectedEffect.AddMilliseconds(6000) <= DateTime.Now)
            {
                LastProtectedEffect = DateTime.Now;
                if (IsMate || IsProtected)
                {
                     MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, MapNpcId, 825), MapX, MapY);
                }

                if (IsProtected && NpcVNum == 1457)
                {
                    // Show the real tower life bar
                     MapInstance.Broadcast($"ctl 2 1 0 99");
                }
            }

            var time = (DateTime.Now - LastEffect).TotalMilliseconds;

            if (EffectDelay > 0)
            {
                if (time > EffectDelay)
                {
                    if (Effect > 0 && EffectActivated)
                    {
                         MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, MapNpcId, Effect), MapX,
                                MapY);
                    }

                    LastEffect = DateTime.Now;
                }
            }

            time = (DateTime.Now - LastSay).Seconds;
            if (Delay != 0)
            {
                if (time > Delay)
                {
                    if (Say != null)
                    {
                         MapInstance.Broadcast(StaticPacketHelper.Say(2, MapNpcId, 0, $"{Say}"));
                        LastSay = DateTime.Now;
                    }
                }
            }

            time = (DateTime.Now - LastMove).TotalMilliseconds;
            if (Target == -1 && IsMoving && Npc.Speed > 0 && time > _movetime &&
                !HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible))
            {
                _movetime = ServerManager.RandomNumber(500, 3000);
                var maxindex = Path.Count > Npc.Speed / 2 && Npc.Speed > 1 ? Npc.Speed / 2 : Path.Count;
                if (maxindex < 1)
                {
                    maxindex = 1;
                }

                if (Path.Count == 0 || Path.Count >= maxindex && maxindex > 0 && Path[maxindex - 1] == null)
                {
                    var xoffset = (short)ServerManager.RandomNumber(-1, 1);
                    var yoffset = (short)ServerManager.RandomNumber(-1, 1);

                    var moveToPosition = new MapCell { X = FirstX, Y = FirstY };
                    if (RunToX != 0 || RunToY != 0)
                    {
                        moveToPosition = new MapCell { X = RunToX, Y = RunToY };
                        _movetime = ServerManager.RandomNumber(300, 1200);
                    }

                    Path = BestFirstSearch.FindPathJagged(new GridPos { X = MapX, Y = MapY },
                        new GridPos
                        {
                            X = (short)ServerManager.RandomNumber(moveToPosition.X - 3, moveToPosition.X + 3),
                            Y = (short)ServerManager.RandomNumber(moveToPosition.Y - 3, moveToPosition.Y + 3)
                        }, MapInstance.Map.JaggedGrid);
                    maxindex = Path.Count > Npc.Speed / 2 && Npc.Speed > 1 ? Npc.Speed / 2 : Path.Count;
                }

                if (DateTime.Now > LastMove && Npc.Speed > 0 && Path.Count > 0)
                {
                    var speedIndex = (byte)(Npc.Speed / 2.5 < 1 ? 1 : Npc.Speed / 2.5);
                    maxindex = Path.Count > speedIndex ? speedIndex : Path.Count;
                    var mapX = (short)ServerManager.RandomNumber(Path[maxindex - 1].X - 1, Path[maxindex - 1].X + 1);
                    var mapY = (short)_random.Next(Path[maxindex - 1].Y - 1, Path[maxindex - 1].Y + 1);

                    //short mapX = Path[maxindex - 1].X;
                    //short mapY = Path[maxindex - 1].Y;
                    var waitingtime =
                        Map.GetDistance(new MapCell { X = mapX, Y = mapY }, new MapCell { X = MapX, Y = MapY }) /
                        (double)Npc.Speed;
                     MapInstance.Broadcast(new BroadcastPacket(null,
                        PacketFactory.Serialize(StaticPacketHelper.Move(UserType.Npc, MapNpcId, mapX, mapY, Npc.Speed)),
                        ReceiverType.All, xCoordinate: mapX, yCoordinate: mapY));
                    LastMove = DateTime.Now.AddSeconds(waitingtime > 1 ? 1 : waitingtime);

                    Observable.Timer(TimeSpan.FromMilliseconds((int)((waitingtime > 1 ? 1 : waitingtime) * 1000)))
                        .Subscribe(x =>
                        {
                            MapX = mapX;
                            MapY = mapY;
                        });

                    Path.RemoveRange(0, maxindex);
                }
            }

            if (Target == -1)
            {
                if (IsHostile && Shop == null)
                {
                    var monster = MapInstance
                        .GetMonsterInRangeList(MapX, MapY,
                            (byte)(Npc.NoticeRange > 5 ? Npc.NoticeRange / 2 : Npc.NoticeRange))
                        .Where(s => BattleEntity.CanAttackEntity(s.BattleEntity)).FirstOrDefault();
                    var session = MapInstance.Sessions.FirstOrDefault(s =>
                        BattleEntity.CanAttackEntity(s.Character.BattleEntity) &&
                        MapInstance == s.Character.MapInstance && Map.GetDistance(new MapCell { X = MapX, Y = MapY },
                            new MapCell { X = s.Character.PositionX, Y = s.Character.PositionY }) < Npc.NoticeRange);

                    if (monster != null)
                    {
                        Target = monster.MapMonsterId;
                    }

                    if (session?.Character != null)
                    {
                        Target = session.Character.CharacterId;
                    }
                }
            }
            else if (Target != -1)
            {
                var monster = MapInstance.Monsters.Find(s => s.MapMonsterId == Target);
                if (monster == null || monster.CurrentHp < 1)
                {
                    Target = -1;
                    return;
                }

                NpcMonsterSkill npcMonsterSkill = null;
                if (ServerManager.RandomNumber(0, 10) > 8)
                {
                    npcMonsterSkill = Skills
                                      .Where(s => (DateTime.Now - s.LastSkillUse).TotalMilliseconds >= 100 * s.Skill.Cooldown)
                                      .OrderBy(rnd => _random.Next()).FirstOrDefault();
                }

                var hitmode = 0;
                var onyxWings = false;
                var zephyrWings = false;
                var dragonBuff = false;
                var damage = DamageHelper.Instance.CalculateDamage(new BattleEntity(this), new BattleEntity(monster),
                    npcMonsterSkill?.Skill, ref hitmode, ref onyxWings, ref zephyrWings, ref dragonBuff);
                if (monster.Monster.BCards.Find(s =>
                    s.Type == (byte)CardType.LightAndShadow &&
                    s.SubType == (byte)AdditionalTypes.LightAndShadow.InflictDamageToMP) is BCard card)
                {
                    var reduce = damage / 100 * card.FirstData;
                    if (monster.CurrentMp < reduce)
                    {
                        reduce = (int)monster.CurrentMp;
                        monster.CurrentMp = 0;
                    }
                    else
                    {
                        monster.DecreaseMp(reduce);
                    }
                }

                var distance = Map.GetDistance(new MapCell { X = MapX, Y = MapY },
                    new MapCell { X = monster.MapX, Y = monster.MapY });
                if (monster.CurrentHp > 0 &&
                    (npcMonsterSkill != null && distance < npcMonsterSkill.Skill.Range || distance <= Npc.BasicRange) &&
                    !HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.NoAttack))
                {
                    if ((DateTime.Now - LastSkill).TotalMilliseconds >= 1000 + Npc.BasicCooldown * 200 ||
                        npcMonsterSkill != null)
                    {
                        if (npcMonsterSkill != null)
                        {
                            npcMonsterSkill.LastSkillUse = DateTime.Now;
                             MapInstance.Broadcast(StaticPacketHelper.CastOnTarget(UserType.Npc, MapNpcId,
                                UserType.Monster, Target, npcMonsterSkill.Skill.CastAnimation,
                                npcMonsterSkill.Skill.CastEffect, npcMonsterSkill.Skill.SkillVNum));
                        }

                        if (npcMonsterSkill != null && npcMonsterSkill.Skill.CastEffect != 0)
                        {
                             MapInstance.Broadcast(StaticPacketHelper.GenerateEff(UserType.Npc, MapNpcId, Effect));
                        }

                        monster.BattleEntity.GetDamage(damage, BattleEntity);
                        lock (monster.DamageList)
                        {
                            if (!monster.DamageList.Any(s => s.Key.MapEntityId == MapNpcId))
                            {
                                monster.AddToAggroList(BattleEntity);
                            }
                        }

                         MapInstance.Broadcast(npcMonsterSkill != null
                            ? StaticPacketHelper.SkillUsed(UserType.Npc, MapNpcId, 3, Target, npcMonsterSkill.SkillVNum,
                                npcMonsterSkill.Skill.Cooldown, npcMonsterSkill.Skill.AttackAnimation,
                                npcMonsterSkill.Skill.Effect, 0, 0, monster.CurrentHp > 0,
                                (int)((float)monster.CurrentHp / (float)monster.MaxHp * 100), damage, hitmode, 0)
                            : StaticPacketHelper.SkillUsed(UserType.Npc, MapNpcId, 3, Target, 0, Npc.BasicCooldown, 11,
                                Npc.BasicSkill, 0, 0, monster.CurrentHp > 0,
                                (int)((float)monster.CurrentHp / (float)monster.MaxHp * 100), damage, hitmode, 0));
                        LastSkill = DateTime.Now;

                        if (npcMonsterSkill?.Skill.TargetType == 1 && npcMonsterSkill?.Skill.HitType == 2)
                        {
                            var clientSessions =
                                MapInstance.Sessions?.Where(s =>
                                    s.Character.IsInRange(MapX,
                                        MapY, npcMonsterSkill.Skill.TargetRange));
                            IEnumerable<Mate> mates =
                                MapInstance.GetListMateInRange(MapX, MapY, npcMonsterSkill.Skill.TargetRange);

                            foreach (var skillBcard in npcMonsterSkill.Skill.BCards)
                            {
                                if (skillBcard.Type == 25 && skillBcard.SubType == 11 &&
                                    new Buff((short)skillBcard.SecondData, Npc.Level)?.Card?.BuffType == BuffType.Good)
                                {
                                    if (clientSessions != null)
                                    {
                                        foreach (var clientSession in clientSessions)
                                        {
                                            if (clientSession.Character != null)
                                            {
                                                if (!BattleEntity.CanAttackEntity(clientSession.Character.BattleEntity))
                                                {
                                                    skillBcard.ApplyBCards(clientSession.Character.BattleEntity,
                                                            BattleEntity);
                                                }
                                            }
                                        }
                                    }

                                    if (mates != null)
                                    {
                                        foreach (var mate in mates)
                                        {
                                            if (!BattleEntity.CanAttackEntity(mate.BattleEntity))
                                            {
                                                skillBcard.ApplyBCards(mate.BattleEntity, BattleEntity);
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (monster.CurrentHp < 1 && monster.SetDeathStatement())
                        {
                            monster.RunDeathEvent();
                            RemoveTarget();
                        }
                    }
                }
                else
                {
                    var maxdistance = Npc.NoticeRange > 5 ? Npc.NoticeRange / 2 : Npc.NoticeRange;
                    if (IsMoving && !HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible))
                    {
                        const short maxDistance = 5;
                        var maxindex = Path.Count > Npc.Speed / 2 && Npc.Speed > 1 ? Npc.Speed / 2 : Path.Count;
                        if (maxindex < 1)
                        {
                            maxindex = 1;
                        }

                        if (Path.Count == 0 && distance >= 1 && distance < maxDistance ||
                            Path.Count >= maxindex && maxindex > 0 && Path[maxindex - 1] == null)
                        {
                            var xoffset = (short)ServerManager.RandomNumber(-1, 1);
                            var yoffset = (short)ServerManager.RandomNumber(-1, 1);

                            //go to monster
                            Path = BestFirstSearch.FindPathJagged(new GridPos { X = MapX, Y = MapY },
                                new GridPos
                                {
                                    X = (short)(monster.MapX + xoffset),
                                    Y = (short)(monster.MapY + yoffset)
                                }, MapInstance.Map.JaggedGrid);
                            maxindex = Path.Count > Npc.Speed / 2 && Npc.Speed > 1 ? Npc.Speed / 2 : Path.Count;
                        }

                        if (DateTime.Now > LastMove && Npc.Speed > 0 && Path.Count > 0)
                        {
                            var speedIndex = (byte)(Npc.Speed / 2.5 < 1 ? 1 : Npc.Speed / 2.5);
                            maxindex = Path.Count > speedIndex ? speedIndex : Path.Count;

                            //short mapX = (short)ServerManager.RandomNumber(Path[maxindex - 1].X - 1, Path[maxindex - 1].X + 1);
                            //short mapY = (short)_random.Next(Path[maxindex - 1].Y - 1, Path[maxindex - 1].Y + 1);

                            var mapX = Path[maxindex - 1].X;
                            var mapY = Path[maxindex - 1].Y;
                            var waitingtime =
                                Map.GetDistance(new MapCell { X = mapX, Y = mapY }, new MapCell { X = MapX, Y = MapY }) /
                                (double)Npc.Speed;
                             MapInstance.Broadcast(new BroadcastPacket(null,
                                PacketFactory.Serialize(StaticPacketHelper.Move(UserType.Npc, MapNpcId, mapX, mapY,
                                    Npc.Speed)), ReceiverType.All, xCoordinate: mapX, yCoordinate: mapY));
                            LastMove = DateTime.Now.AddSeconds(waitingtime > 1 ? 1 : waitingtime);

                            Observable.Timer(
                                    TimeSpan.FromMilliseconds((int)((waitingtime > 1 ? 1 : waitingtime) * 1000)))
                                .Subscribe(x =>
                                {
                                    MapX = mapX;
                                    MapY = mapY;
                                });

                            Path.RemoveRange(0, maxindex);
                        }

                        if (Target != -1 && (MapId != monster.MapId || distance > maxDistance))
                        {
                            RemoveTarget();
                        }
                    }
                }
            }
        }

        private void Respawn()
        {
            if (Npc != null)
            {
                LastSay = DateTime.Now;
                EffectDelay = (short)(EffectDelay == 0 ? 4750 : EffectDelay);
                Delay = (short)(Delay == 0 ? 4750 : Delay);
                DisableBuffs(BuffType.All);
                IsAlive = true;
                Target = -1;
                MaxHp = Npc.MaxHP;
                MaxMp = Npc.MaxMP;
                CurrentHp = MaxHp;
                CurrentMp = MaxMp;
                MapX = FirstX;
                MapY = FirstY;
                if (MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance &&
                    ServerManager.Instance.MapBossVNums.Contains(NpcVNum))
                {
                    var randomCell = MapInstance.Map.GetRandomPosition();
                    if (randomCell != null)
                    {
                        MapX = randomCell.X;
                        MapY = randomCell.Y;
                    }
                }

                Path = new List<Node>();
                MapInstance.Broadcast(GenerateIn());
                Npc.BCards.Where(s => s.Type != 25).ToList().ForEach(s => s.ApplyBCards(BattleEntity, BattleEntity));
            }
        }

        public DateTime LastSay { get; set; }

        #endregion
    }
}