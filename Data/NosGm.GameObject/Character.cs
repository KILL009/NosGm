using NosGm.Configuration;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Packets.Packets.ServerPackets;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject._Event;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Characters.Events;
using NosGm.GameObject.Event;
using NosGm.GameObject.EventArguments;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Service;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;
using NosGm.PathFinder;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using static NosGm.Domain.BCardType;
using NosGm.GameObject.Extension.Message;
using NosGm.Core.Extensions;
using NosGm.GameObject.Extension.Reputation;
using NosGm.GameObject.ItemThread;
using NosGm.GameObject.Threads.WorkerThreads.Battle.Buff;
using NosGm.GameObject.Plugin.Event;
using NosGm.GameObject.Plugin.Load.Handler;

namespace NosGm.GameObject
{
    public class Character : CharacterDTO
    {
        #region Members

        public bool _isStaticBuffListInitial;

        public int OriginalFaction = -1;
        public int slhpbonus;
        private readonly object _syncObj = new object();

        private Random _random;
        private byte _speed;
        public Guid worldId;
        List<string> lastPackets;

        public List<short> npcMonstersSkillsInCd = new List<short>();

        public DateTime LastMessage = DateTime.Now;

        public DateTime LastDrop = DateTime.Now;

        public DateTime LastPdtse = DateTime.Now;

        #endregion

        #region Events

        public event EventHandler<MoveEventArgs> Move;

        public event EventHandler<HitEventArgs> ReceiveHit;

        // ReSharper disable once EventNeverSubscribedTo.Global
        public event EventHandler<HitEventArgs> LandHit;

        public event EventHandler<KillEventArgs> Kill;

        public event EventHandler<CaptureEventArgs> Capture;

        public event EventHandler<DieEventArgs> Die;

        public event EventHandler<CraftRecipeEventArgs> CraftRecipe;

        public event EventHandler<PickupItemEventArgs> PickupItem;

        public event EventHandler<TalkEventArgs> Talk;

        public event EventHandler<FinishScriptedInstanceEventArgs> FinishScriptedInstance;


        #endregion

        #region Instantiation

        public Character()
        {
            GroupSentRequestCharacterIds = new ThreadSafeGenericList<long>();
            FamilyInviteCharacters = new ThreadSafeGenericList<long>();
            TradeRequests = new ThreadSafeGenericList<long>();
            FriendRequestCharacters = new ThreadSafeGenericList<long>();
            MarryRequestCharacters = new ThreadSafeGenericList<long>();
            StaticBonusList = new List<StaticBonusDTO>();
            MinilandObjects = new List<MinilandObject>();
            Mates = new List<Mate>();
            LastMonsterAggro = DateTime.Now;
            LastPulse = DateTime.Now;
            LastFreeze = DateTime.Now;
            MTListTargetQueue = new ConcurrentStack<MTListHitTarget>();
            MeditationDictionary = new Dictionary<short, DateTime>();
            PVELockObject = new object();
            SpeedLockObject = new object();
            ShellEffectArmor = new ConcurrentBag<ShellEffectDTO>();
            ShellEffectMain = new ConcurrentBag<ShellEffectDTO>();
            RuneEffectMain = new ConcurrentBag<RuneEffectDTO>();
            FairyEnchantments = new ConcurrentBag<FairyEnchantmentDTO>();
            ShellEffectSecondary = new ConcurrentBag<ShellEffectDTO>();
            Exploit = new List<CharacterExploitDTO>();
            Quests = new ConcurrentBag<CharacterQuest>();
            DamageList = new Dictionary<long, long>();
            Title = new List<CharacterTitleDTO>();
            TimespaceLog = new List<CharacterTimespaceLogDTO>();
            EffectFromTitle = new ThreadSafeGenericList<BCard>();
            FishingLogs = new List<CharacterFishDto>();
            BazaarItems = new ConcurrentDictionary<long, BazaarItemDTO>();
            BazaarActionTimer = new BazaarActionTimer();
            BattlePassQuestProgresses = new List<BattlePassQuestProgressDTO>();
            BattlePassAccountLogs = new List<BattlePassAccountLogDTO>();
        }

        public Character(CharacterDTO input) : this()
        {
            AccountId = input.AccountId;
            Act4Dead = input.Act4Dead;
            Act4Kill = input.Act4Kill;
            Act4Points = input.Act4Points;
            ArenaWinner = input.ArenaWinner;
            Biography = input.Biography;
            BuffBlocked = input.BuffBlocked;
            CharacterId = input.CharacterId;
            Class = input.Class;
            Compliment = input.Compliment;
            Dignity = input.Dignity;
            EmoticonsBlocked = input.EmoticonsBlocked;
            ExchangeBlocked = input.ExchangeBlocked;
            Faction = input.Faction;
            FamilyRequestBlocked = input.FamilyRequestBlocked;
            FriendRequestBlocked = input.FriendRequestBlocked;
            Gender = input.Gender;
            Gold = input.Gold;
            GoldBank = input.GoldBank;
            GroupRequestBlocked = input.GroupRequestBlocked;
            HairColor = input.HairColor;
            HairStyle = input.HairStyle;
            HeroChatBlocked = input.HeroChatBlocked;
            HeroLevel = input.HeroLevel;
            HeroXp = input.HeroXp;
            Hp = input.Hp;
            HpBlocked = input.HpBlocked;
            IsPetAutoRelive = input.IsPetAutoRelive;
            IsPartnerAutoRelive = input.IsPartnerAutoRelive;
            IsSeal = input.IsSeal;
            JobLevel = input.JobLevel;
            JobLevelXp = input.JobLevelXp;
            LastFamilyLeave = input.LastFamilyLeave;
            Level = input.Level;
            LevelXp = input.LevelXp;
            MapId = input.MapId;
            MapX = input.MapX;
            MapY = input.MapY;
            MasterPoints = input.MasterPoints;
            MasterTicket = input.MasterTicket;
            MaxMateCount = input.MaxMateCount;
            MaxPartnerCount = input.MaxPartnerCount;
            MinilandInviteBlocked = input.MinilandInviteBlocked;
            MinilandMessage = input.MinilandMessage;
            MinilandPoint = input.MinilandPoint;
            MinilandState = input.MinilandState;
            MouseAimLock = input.MouseAimLock;
            Mp = input.Mp;
            Name = input.Name;
            QuickGetUp = input.QuickGetUp;
            RagePoint = input.RagePoint;
            Reputation = input.Reputation;
            Slot = input.Slot;
            SpAdditionPoint = input.SpAdditionPoint;
            SpPoint = input.SpPoint;
            State = input.State;
            TalentLose = input.TalentLose;
            TalentSurrender = input.TalentSurrender;
            TalentWin = input.TalentWin;
            ArenaDeath = input.ArenaDeath;
            ArenaKill = input.ArenaKill;
            WhisperBlocked = input.WhisperBlocked;
            lastPackets = new List<string>();
            TrophyCount = input.TrophyCount;
            Trophy1 = input.Trophy1;
            Trophy2 = input.Trophy2;
            Trophy3 = input.Trophy3;
            Trophy4 = input.Trophy4;
            Trophy5 = input.Trophy5;
            Trophy6 = input.Trophy6;
            Trophy7 = input.Trophy7;
            Trophy8 = input.Trophy8;
            Trophy9 = input.Trophy9;
            Trophy10 = input.Trophy10;
            Trophy11 = input.Trophy11;
            Trophy12 = input.Trophy12;
            Trophy13 = input.Trophy13;
            Trophy14 = input.Trophy14;
            Trophy15 = input.Trophy15;
            LegendaryTrophy = input.LegendaryTrophy;
            RaidCount = input.RaidCount;
            MonsterCount = input.MonsterCount;
            MysteryBoxCount = input.MysteryBoxCount;
            BattlePassPoints = input.BattlePassPoints;
            HasPremiumBattlePass = input.HasPremiumBattlePass;
            UnlockedBattlePassMultiplicator = input.UnlockedBattlePassMultiplicator;
            BuffCharge = input.BuffCharge;
            LimitedBuffCharge = input.LimitedBuffCharge;
            Stage = input.Stage;
            PrimalCharacterQuest = input.PrimalCharacterQuest;
            PrimalRaidQuest = input.PrimalRaidQuest;
            PrimalFamilyQuest = input.PrimalFamilyQuest;
            PrimalCharacterQuestProgress = input.PrimalCharacterQuestProgress;
            PrimalRaidQuestProgress = input.PrimalRaidQuestProgress;
            PrimalFamilyQuestProgress = input.PrimalFamilyQuestProgress;
            PrimalQuestCount = input.PrimalQuestCount;
            DailyRewardChest = input.DailyRewardChest;
            AutoLoot = input.AutoLoot;
            SafeBet = input.SafeBet;
            DuelWon = input.DuelWon;
            DuelLost = input.DuelLost;
            DuelCount = input.DuelCount;
            CurrentIp = input.CurrentIp;
            StarterBoxUsed = input.StarterBoxUsed;
            InstanceMapId = input.InstanceMapId;
            InstanceMapX = input.InstanceMapX;
            InstanceMapY = input.InstanceMapY;
            PityCount = input.PityCount;
            Icon = input.Icon;
            MiniPet = input.MiniPet;
            PetSkill1 = input.PetSkill1;
            PetSkill2 = input.PetSkill2;
        }

        #endregion

        #region Properties

        public MapInstance CustomInstance = null;

        public MapInstance LodInstance = null;

        public bool HasDoneIceFlowerQuest { get; set; }

        public bool UsedTranslatorCommand { get; set; }

        public bool IsOnMapInstance { get; set; }

        public int WaterfallBerserkerRage { get; set; }

        public int Sharpness { get; set; }     

        public int Heat { get; set; }

        public byte Gravitation { get; set; }

        public byte AntiGravitation { get; set; }

        public byte Fuel { get; set; }

        public bool ClockEnabled { get; set; }

        public DateTime LastInstanceCreated { get; set; }

        public DateTime LastRageIncrease { get; set; }

        public DateTime LastClockUpdate { get; set; }

        public DateTime LastDuelInvite { get; set; }

        public DateTime LastGroupEffect { get; set; }

        public bool IsIn1v1PrivateQueue { get; set; }

        public bool IsIn1v1Queue { get; set; }

        public bool IsCurrentlyIn1v1Private { get; set; }

        public bool IsCurrentlyIn1v1 { get; set; }

        public bool IsCurrentlyOnCustomMapInstance { get; set; }

        public bool AutomaticRarify { get; set; }

        public bool AutomaticPerfection { get; set; }

        public bool AutomaticSpecialistUpgrade { get; set; }

        public bool AutomaticEquipmentUpgrade { get; set; }

        public DateTime LastTeleport { get; set; }

        public byte BazaarRequests { get; set; }

        public short BazaarRequest { get; set; }

        public short FishingSpotsMapY { get; set; } = ServerManager.RandomNumber<short>(114, 118);

        public short FishingSpotsMapX { get; set; } = ServerManager.RandomNumber<short>(78, 81);

        public short FishingSpotsMapId { get; set; } = 1;

        public bool IsFishing { get; set; }

        public bool IsBiting { get; set; }


        public DateTime LastFishBite { get; set; }

        public DateTime LastFishCycle { get; set; }

        public List<CharacterFishDto> FishingLogs { get; set; }

        public List<BattlePassAccountLogDTO> BattlePassAccountLogs { get; set; }

        public List<BattlePassQuestProgressDTO> BattlePassQuestProgresses { get; set; }

        public DateTime BattlePassTime { get; set; }

        public AuthorityType Authority { get; set; }

        public byte AntiBotCount { get; set; }

        public short AntiBotIdentificator { get; set; }

        public IDisposable AntiBotMessageInterval { get; set; }

        public IDisposable AntiBotObservable { get; set; }

        public BattleEntity BattleEntity { get; set; }

        public EventEntity Event { get; set; }

        public int DamageInRaid { get; set; }

        public bool Answer { get; set; }

        public BazaarActionTimer BazaarActionTimer { get; set; }

        public ConcurrentDictionary<long, BazaarItemDTO> BazaarItems { get; set; }

        public byte BeforeDirection { get; set; }

        public Node[][] BrushFireJagged { get; set; }
        public int SheepScore1 { get; set; }

        public bool IsWaitingForGift { get; set; }
        public int SheepScore2 { get; set; }

        public int SheepScore3 { get; set; }

        public byte gameLifes = 3;

        public string BubbleMessage { get; set; }

        public DateTime BubbleMessageEnd { get; set; }

        public DateTime LastISort { get; set; }

        public byte SnackRequests { get; set; }

        public byte PotionRequests { get; set; }

        public byte BazarRequests { get; set; }

        public byte SayRequests { get; set; }

        public byte LastDropRequests { get; set; }

        public byte LastPdtseRequests { get; set; }

        public ThreadSafeSortedList<short, Buff> Buff => BattleEntity.Buffs;

        public ThreadSafeSortedList<short, IDisposable> BuffObservables => BattleEntity.BuffObservables;

        public bool CanFight => !IsSitting && ExchangeInfo == null;

        public ThreadSafeGenericList<CellonOptionDTO> CellonOptions => BattleEntity.CellonOptions;

        public ServerManager Channel { get; set; }

        public List<CharacterRelationDTO> CharacterRelations
        {
            get
            {
                lock (ServerManager.Instance.CharacterRelations)
                {
                    return ServerManager.Instance.CharacterRelations == null
                        ? new List<CharacterRelationDTO>()
                        : ServerManager.Instance.CharacterRelations.Where(s =>
                            s.CharacterId == CharacterId || s.RelatedCharacterId == CharacterId).ToList();
                }
            }
        }

        public string Orb { get; set; }

        public int ChargeValue { get; set; }

        public int ConvertedDamageToHP { get; set; }

        public short CurrentMinigame { get; set; }

        public IDictionary<long, long> DamageList { get; set; }

        public int DarkResistance { get; set; }

        public int Defence { get; set; }

        public int DefenceRate { get; set; }

        public byte Direction { get; set; }

        public int DistanceDefence { get; set; }

        public int DistanceDefenceRate { get; set; }

        public IDisposable DragonModeObservable { get; set; }

        public ThreadSafeGenericList<BCard> EffectFromTitle { get; set; }

        public byte Element { get; set; }

        public int ElementRate { get; set; }

        public int ElementRateSP { get; private set; }

        public ThreadSafeGenericLockedList<BCard> EquipmentBCards => BattleEntity.BCards;

        public ExchangeInfo ExchangeInfo { get; set; }

        public List<CharacterExploitDTO> Exploit { get; set; }

        public IDisposable ExploitInterval { get; set; }

        public Family Family { get; set; }

        public FamilyCharacterDTO FamilyCharacter => Family?.FamilyCharacters.Find(s => s.CharacterId == CharacterId);

        public ThreadSafeGenericList<long> FamilyInviteCharacters { get; set; }

        public int FireResistance { get; set; }

        public int FoodAmount { get; set; }

        public int FoodHp { get; set; }

        public int FoodMp { get; set; }

        public ThreadSafeGenericList<long> FriendRequestCharacters { get; set; }

        public ThreadSafeGenericList<GeneralLogDTO> GeneralLogs { get; set; }

        public bool GmPvtBlock { get; set; }

        public Group Group { get; set; }

        public ThreadSafeGenericList<long> GroupSentRequestCharacterIds { get; set; }

        public bool HasGodMode { get; set; }

        public bool HasMagicalFetters => HasBuff(608);

        public bool HasMagicSpellCombo => HasBuff(617) && (LastComboCastId >= 11 && LastComboCastId <= 15);

        public bool HasShopOpened { get; set; }

        public int HitCriticalChance { get; set; }

        public int HitCriticalRate { get; set; }

        public int HitRate { get; set; }

        public int HPMax { get; set; }

        public int MPMax { get; set; }

        public bool InExchangeOrTrade => ExchangeInfo != null || Speed == 0;

        public Inventory Inventory { get; set; }

        public bool Invisible { get; set; }

        public bool InvisibleGm { get; set; }

        public int CurrentArenaKill { get; set; }

        public int CurrentArenaDeath { get; set; }

        public bool IsChangingMapInstance { get; set; }

        public bool IsCustomSpeed { get; set; }

        public bool IsDancing { get; set; }

        public bool IsDisposed { get; private set; }

        public DateTime LastBazaarBuy { get; set; }

        /// <summary>
        /// Defines if the Character Is currently sending or getting items thru exchange.
        /// </summary>
        public bool IsExchanging { get; set; }

        public bool IsMarried => CharacterRelations.Any(c => c.RelationType == CharacterRelationType.Spouse);

        public bool IsMorphed { get; set; }

        public bool IsShopping { get; set; }

        public bool IsSitting { get; set; }

        public bool IsUsingFairyBooster => _isStaticBuffListInitial ? Buff.ContainsKey(131) : DAOFactory.StaticBuffDAO.LoadByCharacterId(CharacterId).Any(s => s.CardId.Equals(131));

        public bool IsVehicled { get; set; }

        public bool IsWaitingForEvent { get; set; }

        public bool IsOnGlacernonShip { get; set; }

        public DateTime LastFamilyAction { get; set; }

        public DateTime LastNoteSent { get; set; }

        public DateTime LastRaidOpened { get; set; }

        public DateTime LastDropPacket { get; set; }

        public DateTime LastCMD { get; set; }

        public int LastComboCastId { get; set; }

        public DateTime LastCommand { get; set; }

        public DateTime LastDefence { get; set; }

        public DateTime LastDelay { get; set; }

        public DateTime LastDelayRecovery { get; set; }

        public DateTime LastDeposit { get; set; }

        public DateTime LastEffect { get; set; }

        public DateTime LastFreeze { get; set; }

        public DateTime LastFunnelUse { get; set; }

        public DateTime LastPotionUse { get; set; }

        public DateTime LastHealth { get; set; }

        public short LastItemVNum { get; set; }
        public int LastSpecialItemVNum { get; set; }
        public DateTime LastBazaarInsert { get; set; }

        public DateTime LastBazaarModeration { get; set; }

        public DateTime FamilyBuff { get; set; }

        public DateTime LastLoyalty { get; set; }

        public DateTime LastMapObject { get; set; }

        public DateTime LastMonsterAggro { get; set; }

        public DateTime LastMove { get; set; }

        public int LastNpcMonsterId { get; set; }

        public int LastNRunId { get; set; }

        public DateTime LastPermBuffRefresh { get; set; }

        public double LastPortal { get; set; }

        public DateTime LastPotion { get; set; }

        public DateTime LastPulse { get; set; }

        public ClientSession LastPvPKiller { get; set; }

        public DateTime LastPVPRevive { get; set; }

        public DateTime LastQuest { get; set; }

        public DateTime LastQuestSummon { get; set; }

        public DateTime LastRepos { get; set; }

        public DateTime LastRoll { get; set; }

        public DateTime LastSchedule { get; set; }

        public DateTime LastEffectDelay { get; set; }

        public DateTime LastSpeaker { get; set; }

        public DateTime LastSkillComboUse { get; set; }

        public DateTime LastSkillUse { get; set; }

        public Skill LastSkillType { get; set; }

        public double LastSp { get; set; }

        public DateTime LastSpeedChange { get; set; }

        public DateTime LastSpGaugeRemove { get; set; }

        public DateTime LastTransform { get; set; }

        public DateTime LastUnstuck { get; set; }

        public DateTime LastVessel { get; set; }

        public DateTime LastWarp { get; set; }

        public DateTime LastSort { get; set; }

        public DateTime LastWithdraw { get; set; }

        public bool IsUsingFamilyWarehouse { get; set; }

        public DateTime LastWareHouseMove { get; set; }

        public DateTime LastShopBuyItem { get; set; }

        public IDisposable Life { get; set; }

        public int LightResistance { get; set; }

        public int MagicalDefence { get; set; }

        public IDictionary<int, MailDTO> MailList { get; set; }

        public MapInstance MapInstance => ServerManager.GetMapInstance(MapInstanceId);

        public Guid MapInstanceId { get; set; }

        public ThreadSafeGenericList<long> MarryRequestCharacters { get; set; }

        public List<Mate> Mates { get; set; }

        public int MaxFood { get; set; }

        public int MaxHit { get; set; }

        public int MaxSnack { get; set; }

        public Dictionary<short, DateTime> MeditationDictionary { get; set; }

        public int MessageCounter { get; set; }

        public int MinHit { get; set; }

        public MinigameLogDTO MinigameLog { get; set; }

        public MapInstance Miniland { get; private set; }

        public List<MinilandObject> MinilandObjects { get; set; }

        public int Morph { get; set; }

        public int MorphUpgrade { get; set; }

        public int MorphUpgrade2 { get; set; }

        public ConcurrentStack<MTListHitTarget> MTListTargetQueue { get; set; }

        public bool NoAttack { get; set; }

        public bool NoMove { get; set; }

        public List<EventContainer> OnDeathEvents => BattleEntity.OnDeathEvents;

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public int PreviousMorph { get; set; }

        public object PVELockObject { get; set; }

        public bool PyjamaDead { get; set; }

        public ConcurrentBag<CharacterQuest> Quests { get; internal set; }

        public List<QuicklistEntryDTO> QuicklistEntries { get; set; }

        public double MaxHp => HPLoad();

        public double MaxMp => MPLoad();

        public RespawnMapTypeDTO Respawn
        {
            get
            {
                RespawnMapTypeDTO respawn = new RespawnMapTypeDTO
                {
                    DefaultX = 79,
                    DefaultY = 116,
                    DefaultMapId = 1,
                    RespawnMapTypeId = -1
                };

                if (Session.HasCurrentMapInstance && Session.CurrentMapInstance.Map.MapTypes.Count > 0)
                {
                    long? respawnmaptype = Session.CurrentMapInstance.Map.MapTypes[0].RespawnMapTypeId;
                    if (respawnmaptype != null)
                    {
                        RespawnDTO resp = Respawns.Find(s => s.RespawnMapTypeId == respawnmaptype);
                        if (resp == null)
                        {
                            RespawnMapTypeDTO defaultresp = Session.CurrentMapInstance.Map.DefaultRespawn;
                            if (defaultresp != null)
                            {
                                respawn.DefaultX = defaultresp.DefaultX;
                                respawn.DefaultY = defaultresp.DefaultY;
                                respawn.DefaultMapId = defaultresp.DefaultMapId;
                                respawn.RespawnMapTypeId = (long)respawnmaptype;
                            }
                        }
                        else
                        {
                            respawn.DefaultX = resp.X;
                            respawn.DefaultY = resp.Y;
                            respawn.DefaultMapId = resp.MapId;
                            respawn.RespawnMapTypeId = (long)respawnmaptype;
                        }
                    }
                }
                else if (Session.HasCurrentMapInstance)
                {
                    RespawnDTO resp = Respawns.Find(s => s.RespawnMapTypeId == 0);
                    if (resp != null)
                    {
                        respawn.DefaultX = resp.X;
                        respawn.DefaultY = resp.Y;
                        respawn.DefaultMapId = resp.MapId;
                        respawn.RespawnMapTypeId = (long)1;
                    }
                }

                return respawn;
            }
        }

        public List<RespawnDTO> Respawns { get; set; }

        public RespawnMapTypeDTO Return
        {
            get
            {
                RespawnMapTypeDTO respawn = new RespawnMapTypeDTO();
                if (Session.HasCurrentMapInstance && Session.CurrentMapInstance.Map.MapTypes.Count > 0)
                {
                    long? respawnmaptype = Session.CurrentMapInstance.Map.MapTypes[0].ReturnMapTypeId;
                    if (respawnmaptype != null)
                    {
                        RespawnDTO resp = Respawns.Find(s => s.RespawnMapTypeId == respawnmaptype);
                        if (resp == null)
                        {
                            RespawnMapTypeDTO defaultresp = Session.CurrentMapInstance.Map.DefaultReturn;
                            if (defaultresp != null)
                            {
                                respawn.DefaultX = defaultresp.DefaultX;
                                respawn.DefaultY = defaultresp.DefaultY;
                                respawn.DefaultMapId = defaultresp.DefaultMapId;
                                respawn.RespawnMapTypeId = (long)respawnmaptype;
                            }
                        }
                        else
                        {
                            respawn.DefaultX = resp.X;
                            respawn.DefaultY = resp.Y;
                            respawn.DefaultMapId = resp.MapId;
                            respawn.RespawnMapTypeId = (long)respawnmaptype;
                        }
                    }
                }
                else if (Session.HasCurrentMapInstance && Session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    RespawnDTO resp = Respawns.Find(s => s.RespawnMapTypeId == 1);
                    if (resp != null)
                    {
                        respawn.DefaultX = resp.X;
                        respawn.DefaultY = resp.Y;
                        respawn.DefaultMapId = resp.MapId;
                        respawn.RespawnMapTypeId = 1;
                    }
                }

                return respawn;
            }
        }

        public ConcurrentBag<RuneEffectDTO> RuneEffectMain { get; set; }

        public ConcurrentBag<FairyEnchantmentDTO> FairyEnchantments { get; set; }

        public RaidType RaidType { get; set; }

        public MapCell SavedLocation { get; set; }

        public IDisposable SaveObs { get; set; }

        public short SaveX { get; set; }

        public short SaveY { get; set; }

        public byte ScPage { get; set; }

        public IDisposable SealDisposable { get; set; }

        public DateTime LastBugSkill = DateTime.Now;

        public DateTime LastSkillUseNew = DateTime.Now;

        public DateTime LastWarehouse = DateTime.Now;

        public int SecondWeaponCriticalChance { get; set; }

        public int SecondWeaponCriticalRate { get; set; }

        public int SecondWeaponHitRate { get; set; }

        public int SecondWeaponMaxHit { get; set; }

        public int SecondWeaponMinHit { get; set; }

        public ClientSession Session { get; private set; }

        public ConcurrentBag<ShellEffectDTO> ShellEffectArmor { get; set; }

        public ConcurrentBag<ShellEffectDTO> ShellEffectMain { get; set; }

        public ConcurrentBag<ShellEffectDTO> ShellEffectSecondary { get; set; }

        /// <summary>
        /// Tamaño predeterminado compatible con el cliente 0.9.3.3254.
        /// </summary>
        public static int DefaultCharacterSize => 106;

        public int Size { get; set; } = DefaultCharacterSize;

        public byte SkillComboCount { get; set; }

        public ThreadSafeSortedList<int, CharacterSkill> Skills { get; private set; }

        public ThreadSafeSortedList<int, CharacterSkill> SkillsSp { get; set; }

        public int SnackAmount { get; set; }

        public int SnackHp { get; set; }

        public int SnackMp { get; set; }

        public int SpCooldown { get; set; }

        public byte Speed
        {
            get
            {
                if (_speed > 59)
                {
                    return 59;
                }

                return _speed;
            }

            set
            {
                LastSpeedChange = DateTime.Now;
                _speed = value > 59 ? (byte)59 : value;
            }
        }

        public object SpeedLockObject { get; set; }

        public ItemInstance SpInstance => Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);

        public List<StaticBonusDTO> StaticBonusList { get; set; }

        public ScriptedInstance Timespace { get; set; }

        public bool TimespaceRewardGotten { get; set; }

        public int TimesUsed { get; set; }

        public List<CharacterTitleDTO> Title { get; set; }

        public List<CharacterTimespaceLogDTO> TimespaceLog { get; set; }

        public ThreadSafeGenericList<long> TradeRequests { get; set; }

        public bool TriggerAmbush { get; set; }

        public int UltimatePoints { get; set; }

        public bool Undercover { get; set; }

        public bool UseSp { get; set; }

        public Item VehicleItem { get; set; }

        public byte VehicleSpeed { private get; set; }

        public IDisposable WalkDisposable { get; set; }

        public int WareHouseSize
        {
            get
            {
                MinilandObject mp = MinilandObjects
                    .Where(s => s.ItemInstance.Item.ItemType == ItemType.House && s.ItemInstance.Item.ItemSubType == 2)
                    .OrderByDescending(s => s.ItemInstance.Item.MinilandObjectPoint).FirstOrDefault();
                if (mp != null)
                {
                    return mp.ItemInstance.Item.MinilandObjectPoint;
                }

                return 0;
            }
        }

        public int RBBWin { get; internal set; }
        public int RBBLose { get; internal set; }

        public int WaterResistance { get; set; }
        public int RbbKill { get; set; }
        public int RbbDead { get; set; }

        public int MandraCount { get; set; }
        public bool isFreezed { get; set; }

        public bool SetStatus { get; set; }

        public string StatusMessage { get; set; }

        public bool EditorMode { get; set; }

        #endregion

        #region Methods

        public void IncreaseBattlePassPoints(int amount)
        {
            Session.Character.BattlePassPoints += amount;
        }

        public void GenerateBattlePassPoints(int amount)
        {
            if (Session.Character.UnlockedBattlePassMultiplicator)
            {
                Session.Character.BattlePassPoints += amount * 2;
            }
            else
            {
                Session.Character.BattlePassPoints += amount;
            }
        }

        public string DisplayAllPrimalQuest()
        {
            string CharacterQuest = "";
            string CharacterQuestProgress = "";
            string RaidQuest = "";
            string RaidQuestProgress = "";
            string FamilyQuest = "";
            string FamilyQuestProgress = "";

            switch (PrimalCharacterQuest)
            {
                case 0:
                    CharacterQuest = "None";
                    CharacterQuestProgress = $"None";
                    break;

                case 1:
                    CharacterQuest = "Hunt Dusi Fox";
                    CharacterQuestProgress = $"{PrimalCharacterQuestProgress}/50";
                    break;

                case 2:
                    CharacterQuest = "Hunt Kenko Raider";
                    CharacterQuestProgress = $"{PrimalCharacterQuestProgress}/50";
                    break;

                case 3:
                    CharacterQuest = "Hunt Revenant Skeleton";
                    CharacterQuestProgress = $"{PrimalCharacterQuestProgress}/100";
                    break;

                case 4:
                    CharacterQuest = "Hunt Magmaros";
                    CharacterQuestProgress = $"{PrimalCharacterQuestProgress}/250";
                    break;

                case 5:
                    CharacterQuest = "Hunt Tallion";
                    CharacterQuestProgress = $"{PrimalCharacterQuestProgress}/250";
                    break;

                case 6:
                    CharacterQuest = "Hunt Sentinel";
                    CharacterQuestProgress = $"{PrimalCharacterQuestProgress}/100";
                    break;

                case 7:
                    CharacterQuest = "Hunt Unknown Spirit Mage";
                    CharacterQuestProgress = $"{PrimalCharacterQuestProgress}/200";
                    break;

                case 8:
                    CharacterQuest = "Hunt Twisted Goblin";
                    CharacterQuestProgress = $"{PrimalCharacterQuestProgress}/300";
                    break;

                case 9:
                    CharacterQuest = "Hunt Gryphon";
                    CharacterQuestProgress = $"{PrimalCharacterQuestProgress}/200";
                    break;
            }

            switch (PrimalRaidQuest)
            {
                case 0:
                    RaidQuest = "None";
                    RaidQuestProgress = $"None";
                    break;

                case 1:
                    RaidQuest = "Complete 'Mother Cuby'";
                    RaidQuestProgress = $"{PrimalRaidQuestProgress}/20";
                    break;
            }

            switch (PrimalFamilyQuest)
            {
                case 0:
                    FamilyQuest = "None";
                    FamilyQuestProgress = $"None";
                    break;

                case 1:
                    FamilyQuest = "Finished Raids";
                    FamilyQuestProgress = $"{PrimalFamilyQuestProgress}/5";
                    break;

                case 2:
                    FamilyQuest = "Finished Raids";
                    FamilyQuestProgress = $"{PrimalFamilyQuestProgress}/10";
                    break;

                case 3:
                    FamilyQuest = "Finished Raids";
                    FamilyQuestProgress = $"{PrimalFamilyQuestProgress}/50";
                    break;

                case 4:
                    FamilyQuest = "Finished Raids";
                    FamilyQuestProgress = $"{PrimalFamilyQuestProgress}/100";
                    break;

                case 5:
                    FamilyQuest = "Finished Cuby Raids";
                    FamilyQuestProgress = $"{PrimalFamilyQuestProgress}/5";
                    break;

                case 6:
                    FamilyQuest = "Finished Ibrahim Raids";
                    FamilyQuestProgress = $"{PrimalFamilyQuestProgress}/5";
                    break;

                case 7:
                    FamilyQuest = "Finished Draco Raids";
                    FamilyQuestProgress = $"{PrimalFamilyQuestProgress}/5";
                    break;

                case 8:
                    FamilyQuest = "Finished Glacerus Raids";
                    FamilyQuestProgress = $"{PrimalFamilyQuestProgress}/5";
                    break;

                case 9:
                    FamilyQuest = "Finished Erenia Raids";
                    FamilyQuestProgress = $"{PrimalFamilyQuestProgress}/5";
                    break;

                case 10:
                    FamilyQuest = "Finished Zenas Raids";
                    FamilyQuestProgress = $"{PrimalFamilyQuestProgress}/5";
                    break;
            }

            return $"modal 1 [Primal Quest System]\n\nCharacter Quest: {CharacterQuest} | Progress: {CharacterQuestProgress}\nRaid Quest: {RaidQuest} | Progress: {RaidQuestProgress}\nFamily Quest: {FamilyQuest} | Progress: {FamilyQuestProgress}";
        }

        public static string GenerateAct() => "act 6";

        public static string GenerateRaidBf(byte type) => $"raidbf 0 {type} 25 ";

        public static void InsertOrUpdatePenalty(PenaltyLogDTO log)
        {
            DAOFactory.PenaltyLogDAO.InsertOrUpdate(ref log);
            CommunicationServiceClient.Instance.RefreshPenalty(log.PenaltyLogId);
        }

        public void AddBuff(Buff indicator, BattleEntity sender, bool noMessage = false, short x = 0, short y = 0) =>
            BattleEntity.AddBuff(indicator, sender, noMessage, x, y);

        public bool HasDoneQuestId(int questId)
        {
            return DAOFactory.QuestLogDAO.LoadByCharacterId(this.CharacterId).FirstOrDefault(x => x.QuestId == questId) != null;
        }

        public string GenerateNowTime()
        {
            DateTime now = DateTime.Now;

            return $"nowtime {now.Hour} {now.Minute} {now.Second}";
        }



        public bool AddPet(Mate mate)
        {
            if (CanAddMate(mate) || mate.IsTemporalMate)
            {
                Mates.Add(mate);
                MapInstance.Broadcast(mate.GenerateIn());
                if (!mate.IsTemporalMate)
                {
                    Session.SendPacket(
                        GenerateSay(string.Format(Language.Instance.GetMessageFromKey("YOU_GET_PET"), mate.Name), 12));
                }

                Session.SendPacket(UserInterfaceHelper.GeneratePClear());
                Session.SendPackets(GenerateScP());
                Session.SendPackets(GenerateScN());
                InventoryType newMateInventory = (InventoryType)(13 + mate.PetId);
                switch (mate.Monster.AttackClass)
                {
                    case 0:

                        // Partner Basic Weapon
                        mate.WeaponInstance = Inventory.AddNewToInventory(990, 1, newMateInventory).FirstOrDefault();

                        // Partner Basic Armor
                        mate.ArmorInstance = Inventory.AddNewToInventory(997, 1, newMateInventory).FirstOrDefault();
                        break;

                    case 1:

                        // Partner Basic Weapon
                        mate.WeaponInstance = Inventory.AddNewToInventory(991, 1, newMateInventory).FirstOrDefault();

                        // Partner Basic Armor
                        mate.ArmorInstance = Inventory.AddNewToInventory(996, 1, newMateInventory).FirstOrDefault();
                        break;

                    case 2:

                        // Partner Basic Weapon
                        mate.WeaponInstance = Inventory.AddNewToInventory(992, 1, newMateInventory).FirstOrDefault();

                        // Partner Basic Armor
                        mate.ArmorInstance = Inventory.AddNewToInventory(995, 1, newMateInventory).FirstOrDefault();
                        break;
                }

                Session.SendPackets(GenerateScN());
                mate.RefreshStats();
                return true;
            }

            return false;
        }

        public void AddQuest(long questId, bool isMain = false)
        {
            if (Session.Character.Authority == AuthorityType.ADMIN)
            {
                Session.SendPacket(Session.Character.GenerateSay($"[HELPER] QuestId: {questId}", 10));
            }

            var characterQuest = new CharacterQuest(questId, CharacterId);
            if (Quests.Any(q => q.QuestId == questId) || characterQuest.Quest == null ||
                (isMain && Quests.Any(q => q.IsMainQuest))
                || (Quests.Where(q => q.Quest.QuestType != (byte)QuestType.WinRaid).ToList().Count >= 5 &&
                    characterQuest.Quest.QuestType != (byte)QuestType.WinRaid && !isMain)
                || ((QuestType)characterQuest.Quest.QuestType == QuestType.FlowerQuest &&
                    Quests.Any(s => (QuestType)s.Quest.QuestType == QuestType.FlowerQuest)))
            {
                return;
            }

            //if (characterQuest.Quest.LevelMin > Level)
            //{
            //    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TOO_LOW_LVL"), 0));
            //    return;
            //}
            //if (/*ServerManager.Instance.Configuration.MaxLevel == 99 &&*/ characterQuest.Quest.LevelMax < Level)
            //{
            //    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TOO_HIGH_LVL"), 0));
            //    return;
            //}

            #region GlacernonQuest          

            bool GlaceQuest = false;

            if ((questId >= 7525 && questId <= 7526)) // Kill Character Missions Glacernon
            {
                GlaceQuest = true;
            }

            if (GlaceQuest)
            {
                if (Session.Character.Faction == FactionType.Angel && questId == 7526)
                {
                    return;
                }
                if (Session.Character.Faction == FactionType.Demon && questId == 7525)
                {
                    return;
                }
                if (!characterQuest.Quest.IsDaily && !characterQuest.IsMainQuest)
                {
                    if (DAOFactory.QuestLogDAO.LoadByCharacterId(CharacterId).Any(s =>
                   s.QuestId == questId && s.LastDaily != null && s.LastDaily.Value.AddHours(24) >= DateTime.Now))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("QUEST_ALREADY_DONE"), 0));
                        return;
                    }
                }
                else if (characterQuest.Quest.IsDaily)
                {
                    if (DAOFactory.QuestLogDAO.LoadByCharacterId(CharacterId).Any(s => s.QuestId == questId && s.LastDaily != null && s.LastDaily.Value.Date == DateTime.Today))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("QUEST_ALREADY_DONE_TODAY"), 0));
                        return;
                    }
                }
            }

            #endregion GlacernonQuest

            #region SPQuest

            bool isSpQuest = false;

            if ((questId >= 2000 && questId <= 2007) // Pajama
                || (questId >= 2008 && questId <= 2013) // SP 1
                || (questId >= 2014 && questId <= 2020) // SP 2
                || (questId >= 2060 && questId <= 2095) // SP 3
                || (questId >= 2100 && questId <= 2134) // SP 4
                )
            {
                isSpQuest = true;
            }

            #endregion SPQuest

            if (!isSpQuest)
            {
                if (!characterQuest.Quest.IsDaily && !characterQuest.IsMainQuest && (QuestType)characterQuest.Quest.QuestType != QuestType.FlowerQuest)
                {
                    if (DAOFactory.QuestLogDAO.LoadByCharacterId(CharacterId).Any(s => s.QuestId == questId))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("QUEST_ALREADY_DONE"), 0));
                        return;
                    }
                }
                else if (characterQuest.Quest.IsDaily && (QuestType)characterQuest.Quest.QuestType != QuestType.FlowerQuest)
                {
                    if (DAOFactory.QuestLogDAO.LoadByCharacterId(CharacterId).Any(s => s.QuestId == questId && s.LastDaily != null && s.LastDaily.Value.Date == DateTime.Today))
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("QUEST_ALREADY_DONE_TODAY"), 0));
                        return;
                    }
                }
                else if (characterQuest.Quest.CanBeDoneOnlyOnce == true) //1-time quests
                {
                    if (DAOFactory.QuestLogDAO.LoadByCharacterId(CharacterId).Any(s =>
                        s.QuestId == questId && s.LastDaily != null && s.LastDaily.Value.AddYears(2) >= DateTime.Now)
                    ) //what im doing lol
                    {
                        Session.SendPacket(
                            UserInterfaceHelper.GenerateMsg(
                                Language.Instance.GetMessageFromKey("ONE_TIME_QUEST_COMPLETED"), 0));
                        return;
                    }
                }
            }

            if (GameConfiguration.TimeSpaceQuestEnabled)
            {
                if (characterQuest.Quest.QuestType == (int)QuestType.Product
               || characterQuest.Quest.QuestType == (int)QuestType.Collect3
               || characterQuest.Quest.QuestType == (int)QuestType.TransmitGold
               || characterQuest.Quest.QuestType == (int)QuestType.TsPoint
               || characterQuest.Quest.QuestType == (int)QuestType.NumberOfKill
               || characterQuest.Quest.QuestType == (int)QuestType.TargetReput
               || characterQuest.Quest.QuestType == (int)QuestType.Inspect
               || characterQuest.Quest.QuestType == (int)QuestType.Needed
               || characterQuest.Quest.QuestType == (int)QuestType.Collect5
               || QuestHelper.Instance.SkipQuests.Any(q => q == characterQuest.QuestId))
                {
                    Session.SendPacket(characterQuest.Quest.GetRewardPacket(this, true));
                    AddQuest(characterQuest.Quest.NextQuestId ?? -1, isMain);
                    return;
                }
            }
            else
            {
                if (characterQuest.Quest.QuestType == (int)QuestType.TimesSpace && ServerManager.Instance.TimeSpaces.All(si => si.QuestTimeSpaceId != (characterQuest.Quest.QuestObjectives.FirstOrDefault()?.SpecialData ?? -1))
               || characterQuest.Quest.QuestType == (int)QuestType.Product
               || characterQuest.Quest.QuestType == (int)QuestType.Collect3
               || characterQuest.Quest.QuestType == (int)QuestType.TransmitGold
               || characterQuest.Quest.QuestType == (int)QuestType.TsPoint
               || characterQuest.Quest.QuestType == (int)QuestType.NumberOfKill
               || characterQuest.Quest.QuestType == (int)QuestType.TargetReput
               || characterQuest.Quest.QuestType == (int)QuestType.Inspect
               || characterQuest.Quest.QuestType == (int)QuestType.Needed
               || characterQuest.Quest.QuestType == (int)QuestType.Collect5
               || QuestHelper.Instance.SkipQuests.Any(q => q == characterQuest.QuestId))
                {
                    Session.SendPacket(characterQuest.Quest.GetRewardPacket(this, true));
                    AddQuest(characterQuest.Quest.NextQuestId ?? -1, isMain);
                    return;
                }
            }

            if (characterQuest.Quest.TargetMap != null)
            {
                Session.SendPacket(characterQuest.Quest.TargetPacket());
            }

            characterQuest.IsMainQuest = isMain;
            Quests.Add(characterQuest);
            Session.SendPacket(GenerateQuestsPacket(questId));
            if (characterQuest.Quest.QuestType == (int)QuestType.UnKnow)
            {
                Session.Character.IncrementObjective(characterQuest, isOver: true);
            }
            Session.SendPacket(GetSqst());
        }

        public void AddRelation(long characterId, CharacterRelationType Relation)
        {
            if (characterId == CharacterId)
            {
                Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("CANT_RELATION_YOURSELF"), 11));
            }

            CharacterRelationDTO addRelation = new CharacterRelationDTO
            {
                CharacterId = CharacterId,
                RelatedCharacterId = characterId,
                RelationType = Relation
            };

            DAOFactory.CharacterRelationDAO.InsertOrUpdate(ref addRelation);
            ServerManager.Instance.RelationRefresh(addRelation.CharacterRelationId);
            Session.SendPacket(GenerateFinit());
            ClientSession target = ServerManager.Instance.Sessions.FirstOrDefault(s => s.Character?.CharacterId == characterId);
            target?.SendPacket(target?.Character.GenerateFinit());
        }

        public void AddPetWithSkill(Mate mate)
        {
            if (mate == null)
            {
                return;
            }
            bool isUsingMate = true;
            if (!Mates.ToList().Any(s => s.IsTeamMember && s.MateType == mate.MateType))
            {
                isUsingMate = false;
                mate.IsTeamMember = true;
            }
            else
            {
                mate?.BackToMiniland();
            }

            Session.SendPacket($"ctl 2 {mate.MateTransportId} 3");
            Mates.Add(mate);
            Session.SendPacket(UserInterfaceHelper.GeneratePClear());
            Session.SendPackets(GenerateScP());
            Session.SendPackets(GenerateScN());
            if (!isUsingMate)
            {
                Parallel.ForEach(Session.CurrentMapInstance.Sessions.Where(s => s.Character != null), s =>
                {
                    if (ServerManager.Instance.ChannelId != 51 || Session.Character.Faction == s.Character.Faction)
                    {
                        s.SendPacket(mate.GenerateIn(false, ServerManager.Instance.ChannelId == 51));
                    }
                    else
                    {
                        s.SendPacket(mate.GenerateIn(true, ServerManager.Instance.ChannelId == 51, s.Account.Authority));
                    }
                });

                Session.SendPacket(GeneratePinit());
                Session.SendPacket(UserInterfaceHelper.GeneratePClear());
                Session.SendPackets(GenerateScP());
                Session.SendPackets(GenerateScN());
                Session.SendPackets(GeneratePst());
            }
        }

        public void ChangeClass(ClassType characterClass, bool fromCommand)
        {
            if (!fromCommand)
            {
                JobLevel = 80;
                JobLevelXp = 0;
            }

            Session.SendPacket("npinfo 0");
            Session.SendPacket(UserInterfaceHelper.GeneratePClear());

            if (characterClass == (byte)ClassType.Adventurer)
            {
                HairStyle = (byte)HairStyle > 1 ? 0 : HairStyle;
                if (JobLevel > 20)
                {
                    JobLevel = 20;
                }
            }

            LoadSpeed();
            Class = characterClass;
            Hp = (int)HPLoad();
            Mp = (int)MPLoad();
            Session.SendPacket(GenerateTit());
            Session.SendPacket(GenerateStat());
            Session.CurrentMapInstance?.Broadcast(Session, GenerateEq());
            Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 8),
                PositionX, PositionY);
            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CLASS_CHANGED"),
                0));
            Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 196),
                PositionX, PositionY);

            ChangeFaction(Session.Character.Family == null ? (FactionType)ServerManager.RandomNumber(1, 2) : (FactionType)Session.Character.Family.FamilyFaction);

            Session.SendPacket(GenerateCond());
            Session.SendPacket(GenerateLev());
            Session.CurrentMapInstance?.Broadcast(Session, GenerateCMode());
            Session.CurrentMapInstance?.Broadcast(Session, GenerateIn(), ReceiverType.AllExceptMe);
            Session.CurrentMapInstance?.Broadcast(Session, GenerateGidx(), ReceiverType.AllExceptMe);
            Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 6),
                PositionX, PositionY);
            Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 198),
                PositionX, PositionY);
            Session.Character.ResetSkills();

            //TODO: Test this, might cause an issue
            if (fromCommand)
            {
                foreach (QuicklistEntryDTO quicklists in DAOFactory.QuicklistEntryDAO.LoadByCharacterId(CharacterId)
                .Where(quicklists => QuicklistEntries.Any(qle => qle.Id == quicklists.Id)))
                {
                    DAOFactory.QuicklistEntryDAO.Delete(quicklists.Id);
                }
            }

            QuicklistEntries = new List<QuicklistEntryDTO>
            {
                new QuicklistEntryDTO
                {
                    CharacterId = CharacterId,
                    Q1 = 0,
                    Q2 = 9,
                    Type = 1,
                    Slot = 3,
                    Pos = 1
                }
            };

            Session.SendPackets(GenerateQuicklist());
            LoadPartnerSkills();

            if (ServerManager.Instance.Groups.Any(s => s.IsMemberOfGroup(Session) && s.GroupType == GroupType.Group))
            {
                Session.CurrentMapInstance?.Broadcast(Session, $"pidx 1 1.{CharacterId}", ReceiverType.AllExceptMe);
            }
        }

        public void ChangeFaction(FactionType faction)
        {
            if (Faction == faction)
            {
                return;
            }

            if (Channel.ChannelId == 51)
            {
                string connection = CommunicationServiceClient.Instance.RetrieveOriginWorld(AccountId);
                if (string.IsNullOrWhiteSpace(connection))
                {
                    return;
                }

                MapId = 145;
                MapX = 51;
                MapY = 41;
                int port = Convert.ToInt32(connection.Split(':')[1]);
                Session.Character.Event.EmitEvent(new PlayerChangeChannelEvent(connection.Split(':')[0], port, 3));
            }

            Faction = faction;
            Act4Kill = 0;
            Act4Dead = 0;
            Act4Points = 0;
            Session.SendPacket("scr 0 0 0 0 0 0");
            Session.SendPacket(GenerateFaction());
            Session.SendPackets(GenerateStatChar());

            if (faction != FactionType.None)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                    Language.Instance.GetMessageFromKey($"GET_PROTECTION_POWER_{(int)Faction}"), 0));
                var effectId = 4799 + (int)faction;
                if (Family != null)
                {
                    effectId += 2;
                }

                Session.CurrentMapInstance?.Broadcast(GenerateEff(effectId));
            }

            Session.SendPacket(GenerateCond());
            Session.SendPacket(GenerateLev());
        }

        public bool AddSkill(short skillVNum)
        {
            Skill skillinfo = ServerManager.GetSkill(skillVNum);
            if (skillinfo == null)
            {
                Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("SKILL_DOES_NOT_EXIST"), 11));
                return false;
            }

            if (skillinfo.SkillVNum < 200)
            {
                if (Skills.GetAllItems()
                    .Any(s => skillinfo.CastId == s.Skill.CastId && s.Skill.SkillVNum < 200 &&
                              s.Skill.UpgradeSkill > skillinfo.UpgradeSkill))
                {
                    // Character already has a better passive skill of the same type.
                    return false;
                }

                foreach (CharacterSkill skill in Skills.GetAllItems()
                    .Where(s => skillinfo.CastId == s.Skill.CastId && s.Skill.SkillVNum < 200))
                {
                    Skills.Remove(skill.SkillVNum);
                }
            }
            else
            {
                if (Skills.ContainsKey(skillVNum))
                {
                    Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("SKILL_ALREADY_EXIST"), 11));
                    return false;
                }

                if (skillinfo.UpgradeSkill != 0)
                {
                    CharacterSkill oldupgrade = Skills.FirstOrDefault(s =>
                        s.Skill.UpgradeSkill == skillinfo.UpgradeSkill
                        && s.Skill.UpgradeType == skillinfo.UpgradeType && s.Skill.UpgradeSkill != 0);
                    if (oldupgrade != null)
                    {
                        Skills.Remove(oldupgrade.SkillVNum);
                    }
                }
            }

            Skills[skillVNum] = new CharacterSkill
            {
                SkillVNum = skillVNum,
                CharacterId = CharacterId
            };

            Session.SendPackets(GenerateQuicklist());
            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SKILL_LEARNED"),
                0));
            return true;
        }

        public void AddStaticBuff(StaticBuffDTO staticBuff, bool isPermaBuff = false)
        {
            Buff bf = new Buff(staticBuff.CardId, Level, isPermaBuff)
            {
                Start = DateTime.Now,
                StaticBuff = true
            };
            Buff oldbuff = Buff[staticBuff.CardId];
            if (oldbuff != null)
            {
                oldbuff.Card.BCards.Where(s => BattleEntity.BCardDisposables[s.BCardId] != null).ToList().ForEach(b => BattleEntity.BCardDisposables[b.BCardId].Dispose());
                oldbuff.StaticVisualEffect?.Dispose();
            }
            if (staticBuff.RemainingTime <= 0)
            {
                bf.RemainingTime = (int)(bf.Card.Duration * 0.6);
                Buff[bf.Card.CardId] = bf;
            }
            else if (staticBuff.RemainingTime > 0)
            {
                bf.RemainingTime = staticBuff.CardId == 340 ? staticBuff.RemainingTime + (oldbuff == null ? 0 : (int)(oldbuff.RemainingTime - (DateTime.Now - oldbuff.Start).TotalSeconds)) : staticBuff.RemainingTime;
                Buff[bf.Card.CardId] = bf;
            }
            else if (oldbuff != null)
            {
                Buff.Remove(bf.Card.CardId);
                int time = (int)((oldbuff.Start.AddSeconds(oldbuff.Card.Duration * 6 / 10) - DateTime.Now).TotalSeconds / 10 * 6);
                bf.RemainingTime = (bf.Card.Duration * 6 / 10) + (time > 0 ? time : 0);
                Buff[bf.Card.CardId] = bf;
            }
            else
            {
                bf.RemainingTime = bf.Card.Duration * 6 / 10;
                Buff[bf.Card.CardId] = bf;
            }
            bf.Card.BCards.ForEach(c => c.ApplyBCards(BattleEntity, BattleEntity));
            if (BuffObservables.ContainsKey(bf.Card.CardId))
            {
                BuffObservables[bf.Card.CardId].Dispose();
                BuffObservables.Remove(bf.Card.CardId);
            }
            if (bf.RemainingTime > 0)
            {
                BuffObservables[bf.Card.CardId] = Observable.Timer(TimeSpan.FromSeconds(bf.RemainingTime)).Subscribe(o =>
                {
                    RemoveBuff(bf.Card.CardId);
                    if (bf.Card.TimeoutBuff != 0 && ServerManager.RandomNumber() < bf.Card.TimeoutBuffChance)
                    {
                        AddBuff(new Buff(bf.Card.TimeoutBuff, Level), BattleEntity);
                    }
                });
            }
            if (!_isStaticBuffListInitial)
            {
                _isStaticBuffListInitial = true;
            }

            Session.SendPacket($"vb {bf.Card.CardId} 1 {(bf.RemainingTime <= 0 ? -1 : bf.RemainingTime * 10)}");
            Session.SendPacket(GenerateSay(string.Format(Language.Instance.GetMessageFromKey("UNDER_EFFECT"), bf.Card.Name), 12));

            // Visual Effects (eff packet)
            if (bf.Card.CardId == 319)
            {
                bf.StaticVisualEffect = Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(o =>
                {
                    if (!Invisible)
                    {
                        Session?.CurrentMapInstance?.Broadcast(GenerateEff(881));
                    }
                });
            }
            if (bf.Card.CardId == 244)
            {
                bf.StaticVisualEffect = Observable.Interval(TimeSpan.FromSeconds(3)).Subscribe(o =>
                {
                    if (!Invisible)
                    {
                        Session?.CurrentMapInstance?.Broadcast(GenerateEff(883));
                    }
                });
            }
        }

        public async Task AddStaticBuffAsync(StaticBuffDTO staticBuff, bool isPermaBuff = false)
        {
            Buff bf = new Buff(staticBuff.CardId, Level, isPermaBuff)
            {
                Start = DateTime.Now,
                StaticBuff = true
            };

            if (bf.Card == null)
            {
                return;
            }

            Buff oldbuff = Buff[staticBuff.CardId];
            if (oldbuff != null)
            {
                oldbuff.Card.BCards.Where(s => BattleEntity.BCardDisposables[s.BCardId] != null).ToList()
                    .ForEach(b => BattleEntity.BCardDisposables[b.BCardId].Dispose());
                oldbuff.StaticVisualEffect?.Dispose();
            }

            if (staticBuff.RemainingTime <= 0)
            {
                bf.RemainingTime = (int)(bf.Card.Duration * 0.6);
                Buff[bf.Card.CardId] = bf;
            }
            else if (staticBuff.RemainingTime > 0)
            {
                bf.RemainingTime = staticBuff.CardId == 340
                    ? staticBuff.RemainingTime + (oldbuff == null
                        ? 0
                        : (int)(oldbuff.RemainingTime - (DateTime.Now - oldbuff.Start).TotalSeconds))
                    : staticBuff.RemainingTime;
                Buff[bf.Card.CardId] = bf;
            }
            else if (staticBuff.RemainingTime > 0)
            {
                bf.RemainingTime = staticBuff.CardId == 1145
                    ? staticBuff.RemainingTime + (oldbuff == null
                        ? 0
                        : (int)(oldbuff.RemainingTime - (DateTime.Now - oldbuff.Start).TotalSeconds))
                    : staticBuff.RemainingTime;
                Buff[bf.Card.CardId] = bf;
            }
            else if (oldbuff != null)
            {
                Buff.Remove(bf.Card.CardId);
                int time =
                    (int)((oldbuff.Start.AddSeconds(oldbuff.Card.Duration * 6 / 10) - DateTime.Now).TotalSeconds / 10 *
                           6);
                bf.RemainingTime = (bf.Card.Duration * 6 / 10) + (time > 0 ? time : 0);
                Buff[bf.Card.CardId] = bf;
            }
            else
            {
                bf.RemainingTime = bf.Card.Duration * 6 / 10;
                Buff[bf.Card.CardId] = bf;
            }

            bf.Card.BCards.ForEach(c => c.ApplyBCards(BattleEntity, BattleEntity));
            if (BuffObservables.ContainsKey(bf.Card.CardId))
            {
                BuffObservables[bf.Card.CardId].Dispose();
                BuffObservables.Remove(bf.Card.CardId);
            }

            if (bf.RemainingTime > 0)
            {
                BuffObservables[bf.Card.CardId] = Observable.Timer(TimeSpan.FromSeconds(bf.RemainingTime)).Subscribe(
                    o =>
                    {
                        RemoveBuff(bf.Card.CardId);
                        if (bf.Card.TimeoutBuff != 0 && ServerManager.RandomNumber() < bf.Card.TimeoutBuffChance)
                        {
                            AddBuff(new Buff(bf.Card.TimeoutBuff, Level), BattleEntity);
                        }
                    });
            }

            if (!_isStaticBuffListInitial)
            {
                _isStaticBuffListInitial = true;
            }

            Session.SendPacket($"vb {bf.Card.CardId} 1 {(bf.RemainingTime <= 0 ? -1 : bf.RemainingTime * 10)}");
            Session.SendPacket(
                GenerateSay(string.Format(Language.Instance.GetMessageFromKey("UNDER_EFFECT"), bf.Card.Name), 12));

            // Visual Effects (eff packet)
            if (bf.Card.CardId == 319)
            {
                bf.StaticVisualEffect = Observable.Interval(TimeSpan.FromSeconds(2)).Subscribe(o =>
                {
                    if (!Invisible)
                    {
                        Session?.CurrentMapInstance?.Broadcast(GenerateEff(881));
                    }
                });
            }

            if (bf.Card.CardId == 4035) // Custom Buff
            {
                bf.StaticVisualEffect = Observable.Interval(TimeSpan.FromSeconds(4)).Subscribe(o =>
                {
                    if (!Invisible)
                    {
                        Session?.CurrentMapInstance?.Broadcast(GenerateEff(3039));
                    }
                });
            }

            if (bf.Card.CardId == 4036) // Last Breaths
            {
                bf.StaticVisualEffect = Observable.Interval(TimeSpan.FromSeconds(3)).Subscribe(o =>
                {
                    if (!Invisible)
                    {
                        Session?.CurrentMapInstance?.Broadcast(GenerateEff(6007));
                    }
                });
            }

            if (bf.Card.CardId == 244)
            {
                bf.StaticVisualEffect = Observable.Interval(TimeSpan.FromSeconds(3)).Subscribe(o =>
                {
                    if (!Invisible)
                    {
                        Session?.CurrentMapInstance?.Broadcast(GenerateEff(883));
                    }
                });
            }
        }

        public void AddUltimatePoints(short points)
        {
            UltimatePoints += points;

            if (UltimatePoints > 3000)
            {
                UltimatePoints = 3000;
            }

            Session.SendPacket(GenerateFtPtPacket());
            Session.SendPackets(GenerateQuicklist());
        }

        public void AddWolfBuffs()
        {
            if (UltimatePoints >= 1000 &&
                !Buff.Any(s => s.Card.CardId == 727 || s.Card.CardId == 728 || s.Card.CardId == 729))
            {
                AddBuff(new Buff(727, 10, false), BattleEntity);
                RemoveBuff(728);
                RemoveBuff(729);
            }

            if (UltimatePoints >= 2000 && !Buff.Any(s => s.Card.CardId == 728 || s.Card.CardId == 729))
            {
                AddBuff(new Buff(728, 10, false), BattleEntity);
                RemoveBuff(727);
                RemoveBuff(729);
            }

            if (UltimatePoints >= 3000 && !Buff.Any(s => s.Card.CardId == 729))
            {
                AddBuff(new Buff(729, 10, false), BattleEntity);
                RemoveBuff(727);
                RemoveBuff(728);
            }
        }

        public bool CanAddMate(Mate mate) => mate.MateType == MateType.Pet ? MaxMateCount > Mates.Count(s => s.MateType == MateType.Pet) : MaxPartnerCount > Mates.Count(s => s.MateType == MateType.Partner);

        public bool CanAttack() => !NoAttack && !HasBuff(CardType.SpecialAttack, (byte)AdditionalTypes.SpecialAttack.NoAttack) && !HasBuff(CardType.FrozenDebuff, (byte)AdditionalTypes.FrozenDebuff.EternalIce);

        public bool CanMove() => !NoMove && !HasBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible) && !HasBuff(CardType.FrozenDebuff, (byte)AdditionalTypes.FrozenDebuff.EternalIce);

        public bool CanUseNosBazaar()
        {
            if (MapInstance == null)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
                return false;
            }

            if (ServerManager.Instance.IsBazaarMaintenance && Authority < AuthorityType.GM)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("BAZAAR_MAINTENANCE"), 0));
                return false;
            }

            StaticBonusDTO medal = Session.Character.StaticBonusList.Find(s => s.StaticBonusType == StaticBonusType.BazaarMedalGold || s.StaticBonusType == StaticBonusType.BazaarMedalSilver);
            if (medal == null)
            {
                // Check if there is NosBazaar in Map
                if (!Session.Character.MapInstance.Npcs.Any(s => s.Dialog == 460) && !Session.Character.MapInstance.Npcs.Any(s => s.Dialog == 11030))
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("INFO_BAZAAR")));
                    return false;
                }
            }



            return true;
        }

        public void ChangeChannel(string ip, int port, byte mode)
        {
            Session.SendPacket($"mz {ip} {port} {Slot}");
            Session.SendPacket($"it {mode}");
            Session.IsDisposing = true;
            CommunicationServiceClient.Instance.RegisterCrossServerAccountLogin(Session.Account.AccountId, Session.SessionId);

            //explictly save data before disconnecting to prevent data loss

            Session.Character.Event.EmitEvent(new CharacterSaveEvent());

            Session.Disconnect();
        }

        public void SendWorldInformation()
        {
            if (GameConfiguration.SendWorldInformation)
            {
                int xpRate = GameConfiguration.XPRate;
                int cxpRate = GameConfiguration.HeroXPRate;
                int fairyRate = GameConfiguration.FairyXPRate;
                int gold = GameConfiguration.GoldRate;
                Session.SendPacket($"msg 3 ========== NosGm ==========");
                Session.SendPacket($"msg 3 EXP: {xpRate} - CXP: {cxpRate} - Fairy: {fairyRate} - Gold: {gold}");
                Session.SendPacket($"msg 3 =============================");
            }
        }

        //public void GenerateBp()
        //{
        //    Session.SendPacket("bp_open");
        //    Session.SendPacket("bpm 70 2 1800 22031000 22042100 0 14 0 0 2 0 5 -999 1 17 0 0 5 0 5 -999 2 34 0 0 2 5911 5 -999 3 18 0 0 3 0 5 -999 4 13 0 0 5 0 5 -999 5 26 0 0 2 0 5 -999 6 1 0 0 1 0 5 -999 7 29 0 0 2 7 10 -999 8 32 0 0 25 1 5 -999 9 9 0 0 15 0 5 -999 10 8 0 0 3 0 5 -999 11 7 0 0 2 0 5 -999 12 2 0 0 2 0 5 -999 13 21 0 0 1 0 5 -999 14 20 0 0 60 0 5 -999 15 33 0 0 4 3128 5 -999 16 15 0 0 1 0 10 -999 17 13 0 0 2 0 5 -999 18 25 0 0 2 0 5 -999 19 17 0 0 5 0 5 -999 20 19 0 0 5555 0 5 -999 21 33 0 0 4 3125 5 -999 22 33 0 0 4 3126 5 -999 23 33 0 0 4 3127 5 -999 24 16 0 0 5 0 5 -999 25 29 0 0 1 3 10 -999 26 12 0 0 3 0 10 -999 27 14 0 0 1 0 5 -999 28 20 0 0 45 0 5 -999 29 3 0 0 2 0 5 -999 30 21 0 0 1 0 5 -999 31 2 0 0 2 0 5 -999 32 18 0 0 3 0 5 -999 33 27 0 0 5 0 10 -999 34 15 0 0 1 0 10 -999 35 30 0 0 15 384 5 -999 36 9 0 0 15 0 5 -999 37 26 0 0 1 0 5 -999 38 7 0 0 2 0 5 -999 39 20 0 23 60 0 5 564 40 29 0 0 2 11 5 564 41 17 0 0 5 0 5 564 42 19 0 0 40000 0 10 564 43 21 0 0 1 0 5 -997 44 13 0 0 2 0 5 -997 45 8 0 0 2 0 5 -997 46 31 0 0 1 1242 5 -997 47 2 0 0 2 0 5 -997 48 4 0 0 69 0 5 -997 49 1 0 0 1 0 5 -997 50 24 0 0 999999 0 10 -997 51 34 0 0 2 5911 5 -997 52 20 0 0 45 0 5 -997 53 30 0 0 15 283 5 -997 54 3 0 0 2 0 5 -997 55 5 1 0 5 0 30 -999 56 6 1 0 5 0 30 -999 57 5 1 0 5 0 30 -999 58 6 1 0 5 0 30 -999 59 5 1 0 5 0 30 6324 60 6 1 0 5 0 30 -997 61 23 1 0 3 0 30 -999 62 3 1 0 7 0 40 -999 63 28 1 0 3 0 40 -999 64 25 1 0 7 0 40 -999 65 23 1 0 3 0 30 -999 66 2 1 0 9 0 30 -999 67 16 1 0 22 0 40 -999 68 1 1 0 5 0 30 -999 69 23 1 1 3 0 30 6324");
        //}

        public void GenerateMastery()
        {
            Session.SendPacket($"food {MasteryXp} 1");
        }

        public void GenerateFtpt()
        {
            ItemInstance specialist = Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            if (specialist == null)
            {
                return;
            }

            switch (specialist.ItemVNum)
            {
                //Dragon Knight
                case 8521:
                    Session.SendPacket($"ftpt 5 {Sharpness} 300");
                    break;

                //Blaster
                case 8522:
                    Session.SendPacket($"ftpt 2 {Heat} 100");
                    break;

                //Gravity
                case 8523:
                    Session.SendPacket($"ftpt 3 {Gravitation} {AntiGravitation}");
                    break;

                //Hydraulic Fist
                case 8524:
                    Session.SendPacket($"ftpt 4 {Fuel} 100");
                    break;
            }
        }

        public void GenerateEquipmentShine()
        {
            var now = DateTime.UtcNow;
            if ((now - LastEffect).TotalSeconds < 3)
                return; // sale sin tocar el inventario

            var inventory = Session.Character.Inventory;
            var weapon = inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
            var armor = inventory.LoadBySlotAndType((byte)EquipmentType.Armor, InventoryType.Wear);
            if (weapon == null || armor == null)
                return;

            int effect = weapon.Upgrade switch
            {
                13 => 4945,
                12 => 4459,
                11 => 4358,
                _ => armor.Upgrade switch
                {
                    13 => 4950,
                    12 => 4946,
                    11 => 4947,
                    _ => 0
                }
            };

            if (effect == 0)
                return;

            Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateEff(effect));
            LastEffect = now;
        }

        public void GenerateWaterfallBerserkerRage()
        {
            Session.SendPacket($"ftpt 1 {Session.Character.WaterfallBerserkerRage} 100");
        }

        public void ResetState()
        {
            //Make sure that ftpt is being released even though it's removed by Client
            Session.SendPacket("ftpt -1");

            Session.Character.Heat = 0;
            Session.Character.WaterfallBerserkerRage = 0;
        }

        public void CharacterLife()
        {
            //Load Equipment Shine
            Session.Character.GenerateEquipmentShine();

            if (Session == null)
            {
                return;
            }
            if (Session.Character.IsFishing)
            {
                if (LastFishCycle.AddSeconds(1) < DateTime.Now)
                {
                    LastFishCycle = DateTime.Now;

                    if (LastFishBite.AddSeconds(2) < DateTime.Now)
                    {
                        LastFishBite = DateTime.Now;

                        var rnd = ServerManager.RandomNumber();

                        if (rnd < 50)
                        {
                            if (!IsBiting)
                            {
                                IsBiting = true;
                                Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 5104));
                                Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(6, 1, Session.Character.CharacterId, 30));
                                Session.SendPacket(UserInterfaceHelper.GenerateGuri(6, 1, Session.Character.CharacterId, 30));
                            }
                        }
                        else
                        {
                            if (IsBiting)
                            {
                                IsBiting = false;
                                IsFishing = false;
                                Session.CurrentMapInstance?.Broadcast(UserInterfaceHelper.GenerateGuri(6, 26, Session.Character.CharacterId, 30));
                                Session.SendPacket(this.GenerateSay("Ugh, the fish ate the bait!", 0));
                            }
                        }
                    }
                }
            }

            if (LastGroupEffect.AddSeconds(5) <= DateTime.Now)
            {
                BuffThread.AddGroupBuff(Session);
            }

            if (LastClockUpdate.AddSeconds(10) <= DateTime.Now)
            {
                Session.SendPacket($"lf 1 {DateTime.Now.ToString("HH:mm")}");
                LastClockUpdate = DateTime.Now;
                BuffThread.RemoveGroupBuff(Session);
            }
            ItemInstance Specialist = Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            if (Specialist != null)
            {
                if (Specialist.ItemVNum == 4581 && WaterfallBerserkerRage > 0)
                {
                    //if (LastSkillUse.AddSeconds(10) > DateTime.Now || LastDefence.AddSeconds(100) > DateTime.Now)
                    //{
                    //    WaterfallBerserkerRage = 0;
                    //}
                }
                if (Specialist.ItemVNum == 4498)
                {
                    Session.SendPacket("ob_ar");
                }
            }
            if (HasBuff(900) && LastRageIncrease > DateTime.Now)
            {
                WaterfallBerserkerRage += 4;
                LastRageIncrease = DateTime.Now.AddSeconds(4);

                Session.Character.GenerateWaterfallBerserkerRage();
            }
            if (Hp == 0 && LastHealth.AddSeconds(2) <= DateTime.Now)
            {
                Mp = 0;
                Session.SendPacket(GenerateStat());
                LastHealth = DateTime.Now;
            }
            else
            {
                if (Level >= 1 && Level < 81)
                {
                    if (!HasBuff(684))
                    {
                        AddBuff(new Buff(684, Level), BattleEntity);
                    }
                }
                else
                {
                    if (HasBuff(684))
                    {
                        RemoveBuff(684);
                    }
                }
                if (Session.Character.LastEffectDelay > DateTime.Now)

                {
                    // nothing
                }
                else
                {
                    if (HasBuff(260))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7206), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    //if (HasBuff(664))
                    //{
                    //    Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7451), Session.Character.PositionX, Session.Character.PositionY);
                    //    Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(10);
                    //}
                    if (HasBuff(888))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 3808), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(251))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7210), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(1124))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7432), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(892))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 4706), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(893))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 3501), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(896))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 277), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(899))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 610), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(902))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 4471), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(913))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7240), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(915))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 4763), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(922))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 3701), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(923))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7559), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(924))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7206), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(925))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 4683), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(926))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7276), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(10);
                    }
                    if (HasBuff(927))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7179), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(10);
                    }
                    if (HasBuff(928))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7304), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(929))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 7674), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(5);
                    }
                    if (HasBuff(934))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 651), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(3);
                    }
                    if (HasBuff(935))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 614), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(1142))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 4649), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                    if (HasBuff(1143))
                    {
                        Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 4807), Session.Character.PositionX, Session.Character.PositionY);
                        Session.Character.LastEffectDelay = DateTime.Now.AddSeconds(1);
                    }
                }

                if (BubbleMessage != null && BubbleMessageEnd <= DateTime.Now)
                {
                    BubbleMessage = null;
                }

                if (CurrentMinigame != 0 && LastEffect.AddSeconds(3) <= DateTime.Now)
                {
                    Session.CurrentMapInstance?.Broadcast(
                        StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, CurrentMinigame));
                    LastEffect = DateTime.Now;
                }

                if (LastEffect.AddMilliseconds(400) <= DateTime.Now && MessageCounter > 0)
                {
                    MessageCounter--;
                }

                if (MapInstance != null
                    && HasBuff(CardType.FrozenDebuff, (byte)AdditionalTypes.FrozenDebuff.EternalIce)
                    && LastFreeze.AddSeconds(1) <= DateTime.Now)
                {
                    LastFreeze = DateTime.Now;
                    MapInstance.Broadcast(GenerateEff(35));
                }

                if (MapInstance == Miniland && LastLoyalty.AddSeconds(10) <= DateTime.Now)
                {
                    LastLoyalty = DateTime.Now;
                    Mates.ForEach(m =>
                    {
                        m.Loyalty += 100;
                        if (m.Loyalty > 1000) m.Loyalty = 1000;
                    });
                    Session.SendPackets(GenerateScP());
                    Session.SendPackets(GenerateScN());
                }
                if (Session.CurrentMapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
                {
                    Session.SendPacket("ob_ar");
                }
                if (LastEffect.AddSeconds(5) <= DateTime.Now)
                {
                    if (Session.CurrentMapInstance?.MapInstanceType == MapInstanceType.RaidInstance)
                    {
                        Session.SendPacket(GenerateRaid(3));
                    }

                    ItemInstance ring = Inventory.LoadBySlotAndType((byte)EquipmentType.Ring, InventoryType.Wear);
                    ItemInstance bracelet =
                        Inventory.LoadBySlotAndType((byte)EquipmentType.Bracelet, InventoryType.Wear);
                    ItemInstance necklace =
                        Inventory.LoadBySlotAndType((byte)EquipmentType.Necklace, InventoryType.Wear);
                    ItemInstance orb = 
                        Inventory.LoadBySlotAndType((byte)EquipmentType.Orb, InventoryType.Wear);

                    CellonOptions.Clear();
                    if (ring != null)
                    {
                        CellonOptions.AddRange(ring.CellonOptions);
                    }

                    if (bracelet != null)
                    {
                        CellonOptions.AddRange(bracelet.CellonOptions);
                    }

                    if (necklace != null)
                    {
                        CellonOptions.AddRange(necklace.CellonOptions);
                    }


                    if (!Invisible)
                    {
                        ItemInstance amulet =
                            Inventory.LoadBySlotAndType((byte)EquipmentType.Amulet, InventoryType.Wear);
                        if (amulet != null)
                        {
                            if (amulet.ItemVNum == 4503 || amulet.ItemVNum == 4504)
                            {
                                Session.CurrentMapInstance?.Broadcast(
                                    StaticPacketHelper.GenerateEff(UserType.Player, CharacterId,
                                        amulet.Item.EffectValue +
                                        (Class == ClassType.Adventurer ? 0 : (byte)Class - 1)), PositionX, PositionY);
                            }
                            else
                            {
                                Session.CurrentMapInstance?.Broadcast(
                                    StaticPacketHelper.GenerateEff(UserType.Player, CharacterId,
                                        amulet.Item.EffectValue), PositionX, PositionY);
                            }
                        }

                        if (Group != null && (Group.GroupType == GroupType.Team ||
                                              Group.GroupType == GroupType.BigTeam ||
                                              Group.GroupType == GroupType.GiantTeam))
                        {
                            try
                            {
                                Session.CurrentMapInstance?.Broadcast(Session,
                                    StaticPacketHelper.GenerateEff(UserType.Player, CharacterId,
                                        828 + (Group.IsLeader(Session) ? 1 : 0)), ReceiverType.AllExceptGroup);
                                Session.CurrentMapInstance?.Broadcast(Session,
                                    StaticPacketHelper.GenerateEff(UserType.Player, CharacterId,
                                        830 + (Group.IsLeader(Session) ? 1 : 0)), ReceiverType.Group);
                            }
                            catch (Exception ex)
                            {
                                //LOGGER
                                ////LOGGER(2, "", $"{ex.ToString()}", LogType.ServerError);
                            }
                        }

                        Mates.Where(s => s.CanPickUp).ToList().ForEach(s =>
                            Session.CurrentMapInstance?.Broadcast(
                                StaticPacketHelper.GenerateEff(UserType.Npc, s.MateTransportId, 3007)));
                        Mates.Where(s => s.IsTsProtected).ToList().ForEach(s =>
                            Session.CurrentMapInstance?.Broadcast(
                                StaticPacketHelper.GenerateEff(UserType.Npc, s.MateTransportId, 825)));
                        Mates.Where(s => s.MateType == MateType.Pet && s.Loyalty <= 0).ToList().ForEach(s =>
                            Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Npc, s.MateTransportId, 5003)));
                    }

                    LastEffect = DateTime.Now;
                }

                foreach (Mate mate in Mates?.Where(m => m.IsTeamMember))
                {
                    if (mate != null && mate.LastHealth.AddSeconds(mate.IsSitting ? 1.5 : 2) <= DateTime.Now)
                    {
                        mate.LastHealth = DateTime.Now;
                        if (mate.LastDefence.AddSeconds(4) <= DateTime.Now &&
                            mate.LastSkillUse.AddSeconds(2) <= DateTime.Now && mate.Hp > 0)
                        {
                            mate.Hp += mate.Hp + mate.HealthHpLoad() < mate.HpLoad() ? mate.HealthHpLoad() : mate.HpLoad() - mate.Hp;
                            mate.Mp += mate.Mp + mate.HealthMpLoad() < mate.MpLoad() ? mate.HealthMpLoad() : mate.MpLoad() - mate.Mp;
                        }
                        Session.SendPackets(GeneratePst());
                    }
                }

                if (LastHealth.AddSeconds(2) <= DateTime.Now ||
                    (IsSitting && LastHealth.AddSeconds(1.5) <= DateTime.Now))
                {
                    LastHealth = DateTime.Now;

                    if (Session.HealthStop)
                    {
                        Session.HealthStop = false;
                        return;
                    }

                    if (LastDefence.AddSeconds(4) <= DateTime.Now && LastSkillUse.AddSeconds(2) <= DateTime.Now &&
                        Hp > 0)
                    {
                        bool change = false;

                        if (Hp + HealthHPLoad() < HPLoad())
                        {
                            change = true;
                            Hp += HealthHPLoad();
                        }
                        else
                        {
                            change |= Hp != (int)HPLoad();
                            Hp = (int)HPLoad();
                        }

                        if (Mp + HealthMPLoad() < MPLoad())
                        {
                            Mp += HealthMPLoad();
                            change = true;
                        }
                        else
                        {
                            change |= Mp != (int)MPLoad();
                            Mp = (int)MPLoad();
                        }

                        if (change)
                        {
                            Session.SendPacket(this.GenerateStat());
                        }
                    }
                }

                if (Session.Character.LastQuestSummon.AddSeconds(7) < DateTime.Now
                ) // Quest in which you make monster spawn
                {
                    Session.Character.CheckHuntQuest();
                    Session.Character.LastQuestSummon = DateTime.Now;
                }

                if (MeditationDictionary.Count != 0)
                {
                    try
                    {

                        if (MeditationDictionary.Count != 0)
                        {
                            if (MeditationDictionary.ContainsKey(891) && MeditationDictionary[891] < DateTime.Now)
                            {
                                Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 7220));
                                AddBuff(new Buff(891, Level), BattleEntity);
                                if (BuffObservables.ContainsKey(890))
                                {
                                    BuffObservables[890].Dispose();
                                    BuffObservables.Remove(890);
                                }

                                MeditationDictionary.Remove(891);
                            }
                            else if (MeditationDictionary.ContainsKey(890) && MeditationDictionary[890] < DateTime.Now)
                            {
                                Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 7223));
                                AddBuff(new Buff(890, Level), BattleEntity);
                                if (BuffObservables.ContainsKey(889))
                                {
                                    BuffObservables[889].Dispose();
                                    BuffObservables.Remove(889);
                                }

                                MeditationDictionary.Remove(890);
                            }
                            else if (MeditationDictionary.ContainsKey(889) && MeditationDictionary[889] < DateTime.Now)
                            {
                                Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 7223));
                                AddBuff(new Buff(889, Level), BattleEntity);
                                if (BuffObservables.ContainsKey(891))
                                {
                                    BuffObservables[891].Dispose();
                                    BuffObservables.Remove(891);
                                }

                                MeditationDictionary.Remove(889);
                            }
                        }
                    }
                    catch
                    {
                    }
                }



                if (HasMagicSpellCombo)
                {
                    Session.SendPacket($"mslot {LastComboCastId} 0");
                }
                else if (SkillComboCount > 0 && LastSkillComboUse.AddSeconds(5) < DateTime.Now)
                {
                    SkillComboCount = 0;
                    Session.SendPackets(this.GenerateQuicklist());
                    Session.SendPacket($"mslot {LastComboCastId} 0");
                }
                if (LastPermBuffRefresh.AddSeconds(2) <= DateTime.Now)
                {
                    LastPermBuffRefresh = DateTime.Now;

                    foreach (BCard bcard in EquipmentBCards.Where(b =>
                        b.Type.Equals(CardType.Buff) &&
                        new Buff((short)b.CardId, Level).Card?.BuffType == BuffType.Good))
                    {
                        bcard.ApplyBCards(BattleEntity, BattleEntity);
                    }

                    if (UseSp)
                    {
                        GenerateFtpt();

                        ItemInstance specialist2 = Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);

                        if (specialist2 == null)
                        {
                            return;
                        }
                        if (specialist2.Upgrade == 20)
                        {
                            switch (specialist2.Plus20Buff)
                            {
                                case 0:
                                    if (!Buff.ContainsKey(942))
                                    {
                                        AddBuff(new Buff(942, Level), BattleEntity, true);
                                    }
                                    break;

                                case 1:
                                    if (!Buff.ContainsKey(943))
                                    {
                                        AddBuff(new Buff(943, Level), BattleEntity, true);
                                    }
                                    break;

                                case 2:
                                    if (!Buff.ContainsKey(944))
                                    {
                                        AddBuff(new Buff(944, Level), BattleEntity, true);
                                    }
                                    break;

                                case 3:
                                    if (!Buff.ContainsKey(945))
                                    {
                                        AddBuff(new Buff(945, Level), BattleEntity, true);
                                    }
                                    break;

                                case 4:
                                    if (!Buff.ContainsKey(946))
                                    {
                                        AddBuff(new Buff(946, Level), BattleEntity, true);
                                    }
                                    break;
                            }
                        }
                    }
                }

                

                if (UseSp)
                {

                    ItemInstance specialist = Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                    if (specialist == null)
                    {
                        return;
                    }

                    if (LastSpGaugeRemove <= new DateTime(0001, 01, 01, 00, 00, 00))
                    {
                        LastSpGaugeRemove = DateTime.Now;
                    }

                    if (LastSkillUse.AddSeconds(15) >= DateTime.Now && LastSpGaugeRemove.AddSeconds(1) <= DateTime.Now)
                    {
                        byte spType = 0;

                        if ((specialist.Item.Morph > 1 && specialist.Item.Morph < 8) ||
                            (specialist.Item.Morph > 9 && specialist.Item.Morph < 16))
                        {
                            spType = 3;
                        }
                        else if (specialist.Item.Morph > 16 && specialist.Item.Morph < 60)
                        {
                            spType = 2;
                        }
                        else if (specialist.Item.Morph == 9)
                        {
                            spType = 1;
                        }

                        if (SpPoint >= spType)
                        {
                            SpPoint -= spType;
                        }
                        else if (SpPoint < spType && SpPoint != 0)
                        {
                            spType -= (byte)SpPoint;
                            SpPoint = 0;
                            SpAdditionPoint -= spType;
                        }
                        else if (SpPoint == 0 && SpAdditionPoint >= spType)
                        {
                            SpAdditionPoint -= spType;
                        }
                        else if (SpPoint == 0 && SpAdditionPoint < spType)
                        {
                            SpAdditionPoint = 0;

                            double currentRunningSeconds =
                                (DateTime.Now - Process.GetCurrentProcess().StartTime.AddSeconds(-50)).TotalSeconds;

                            if (UseSp)
                            {
                                LastSp = currentRunningSeconds;
                                if (Session?.HasSession == true)
                                {
                                    if (IsVehicled)
                                    {
                                        return;
                                    }

                                    UseSp = false;
                                    WingsThread.RemoveBuff(Session);
                                    CharacterHelper.RemoveDragonBuff(Session);
                                    LoadSpeed();
                                    Session.SendPacket(GenerateCond());
                                    Session.SendPacket(GenerateLev());
                                    SpCooldown = 30;
                                    if (SkillsSp != null)
                                    {
                                        foreach (CharacterSkill ski in SkillsSp.Where(s => !s.CanBeUsed()))
                                        {
                                            short time = ski.Skill.Cooldown;
                                            double temp = (ski.LastUse - DateTime.Now).TotalMilliseconds + (time * 100);
                                            temp /= 1000;
                                            SpCooldown = temp > SpCooldown ? (int)temp : SpCooldown;
                                        }
                                    }

                                    Session.SendPacket(this.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("STAY_TIME"), SpCooldown), 11));
                                    Session.SendPacket($"sd {SpCooldown}");
                                    Session.CurrentMapInstance?.Broadcast(this.GenerateCMode());
                                    Session.CurrentMapInstance?.Broadcast(
                                        UserInterfaceHelper.GenerateGuri(6, 1, CharacterId), PositionX, PositionY);

                                    // ms_c
                                    Session.SendPacket(GenerateSki());
                                    Session.SendPackets(GenerateQuicklist());
                                    Session.SendPacket(GenerateStat());
                                    Session.SendPackets(GenerateStatChar());




                                    Observable.Timer(TimeSpan.FromMilliseconds(SpCooldown * 1000)).Subscribe(o =>
                                    {
                                        if (Session == null)
                                        {
                                            return;
                                        }
                                        Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("TRANSFORM_DISAPPEAR"), 11));
                                        Session.SendPacket("sd 0");
                                    });
                                }
                            }
                        }

                        Session.SendPacket(GenerateSpPoint());
                        LastSpGaugeRemove = DateTime.Now;
                    }
                }
            }
        }

        public void CheckHuntQuest()
        {
            CharacterQuest quest = Quests?.FirstOrDefault(q =>
                q?.Quest?.QuestType == (int)QuestType.Hunt && q.Quest?.TargetMap == MapInstance?.Map?.MapId &&
                Math.Abs(PositionX - q.Quest?.TargetX ?? 0) < 2 && Math.Abs(PositionY - q.Quest?.TargetY ?? 0) < 2);
            if (quest == null)
            {
                return;
            }

            if (MapInstance == null || MapInstance.Monsters == null || MapInstance.Monsters.Where(a => a != null).Any(
                s =>
                    s?.MonsterVNum == (short)(quest?.GetObjectiveByIndex(1)?.Data ?? -1) &&
                    Math.Abs(s?.MapX - quest?.Quest?.TargetX ?? 0) < 4 &&
                    Math.Abs(s?.MapY - quest?.Quest?.TargetY ?? 0) < 4))
            {
                return;
            }

            ConcurrentBag<MonsterToSummon> monsters = new ConcurrentBag<MonsterToSummon>();
            var monstersToSpawn = quest.GetObjectiveByIndex(1)?.Objective / 2 + 1;

            if (monstersToSpawn > 4)
            {
                monstersToSpawn = 4;
            }

            for (var a = 0; a < monstersToSpawn; a++)
            {
                monsters.Add(new MonsterToSummon((short)(quest.GetObjectiveByIndex(1)?.Data ?? -1),
                    new MapCell
                    {
                        X = (short)(PositionX + ServerManager.RandomNumber<int>(-2, 3)),
                        Y = (short)(PositionY + ServerManager.RandomNumber<int>(-2, 3))
                    }, this.BattleEntity, true));
            }

            EventHelper.Instance.RunEvent(new EventContainer(MapInstance, EventActionType.SPAWNMONSTERS,
                monsters.ToList()));
        }

        public void ClearLaurena()
        {
            if (IsLaurenaMorph())
            {
                IsMorphed = false;
                Morph = PreviousMorph;
                PreviousMorph = 0;
                MapInstance?.Broadcast(GenerateCMode());
            }

            RemoveBuff(477, true);
            RemoveBuff(478, true);
        }

        public void CloseExchangeOrTrade()
        {
            if (InExchangeOrTrade)
            {
                long? targetSessionId = ExchangeInfo?.TargetCharacterId;

                if (targetSessionId.HasValue && Session.HasCurrentMapInstance)
                {
                    ClientSession targetSession =
                        Session.CurrentMapInstance.GetSessionByCharacterId(targetSessionId.Value);

                    if (targetSession == null)
                    {
                        return;
                    }

                    Session.SendPacket("exc_close 0");
                    targetSession.SendPacket("exc_close 0");
                    ExchangeInfo = null;
                    targetSession.Character.ExchangeInfo = null;
                }
            }
        }

        public void CloseShop()
        {
            if (HasShopOpened && Session.HasCurrentMapInstance)
            {
                KeyValuePair<long, MapShop> shop =
                    Session.CurrentMapInstance.UserShops.FirstOrDefault(mapshop =>
                        mapshop.Value.OwnerId.Equals(CharacterId));
                if (!shop.Equals(default))
                {
                    Session.CurrentMapInstance.UserShops.Remove(shop.Key);

                    // declare that the shop cannot be closed
                    HasShopOpened = false;

                    Session.CurrentMapInstance?.Broadcast(GenerateShopEnd());
                    Session.CurrentMapInstance?.Broadcast(Session, GeneratePlayerFlag(0), ReceiverType.AllExceptMe);
                    IsSitting = false;
                    IsShopping = false; // close shop by character will always completely close the shop

                    LoadSpeed();
                    Session.SendPacket(GenerateCond());
                    Session.CurrentMapInstance?.Broadcast(GenerateRest());
                }
            }
        }

        public bool CustomQuestRewards(QuestType type, long questId)
        {
            switch (type)
            {
                case QuestType.FlowerQuest:
                    GetDignity(100);
                    AddBuff(new Buff(378, Level), BattleEntity);
                    return true;
            }

            switch (questId)
            {
                case 2255:
                    short[] possibleRewards = new short[] { 1894, 1895, 1896, 1897, 1898, 1899, 1900, 1901, 1902, 1903 };
                    GiftAdd(possibleRewards[ServerManager.RandomNumber(0, possibleRewards.Length - 1)], 1);
                    return true;
            }

            return false;
        }

        public void Dance() => IsDancing = !IsDancing;

        public void DecreaseMp(int amount) => BattleEntity.DecreaseMp(amount);

        public Character DeepCopy() => (Character)MemberwiseClone();

        public void DeleteBlackList(long characterId)
        {
            CharacterRelationDTO chara = CharacterRelations.Find(s => s.RelatedCharacterId == characterId);
            if (chara != null)
            {
                long id = chara.CharacterRelationId;
                DAOFactory.CharacterRelationDAO.Delete(id);
                ServerManager.Instance.RelationRefresh(id);
                Session.SendPacket(GenerateBlinit());
            }
        }

        public void DeleteItem(InventoryType type, short slot)
        {
            if (Inventory != null)
            {
                Inventory.DeleteFromSlotAndType(slot, type);
                Session.SendPacket(UserInterfaceHelper.Instance.GenerateInventoryRemove(type, slot));
            }
        }

        public void DeleteItemByItemInstanceId(Guid id)
        {
            if (Inventory != null)
            {
                Tuple<short, InventoryType> result = Inventory.DeleteById(id);
                Session.SendPacket(UserInterfaceHelper.Instance.GenerateInventoryRemove(result.Item2, result.Item1));
            }
        }

        public void DeleteRelation(long characterId, CharacterRelationType relationType)
        {
            CharacterRelationDTO chara = CharacterRelations.Find(s => (s.RelatedCharacterId == characterId || s.CharacterId == characterId) && s.RelationType == relationType);
            if (chara != null)
            {
                long id = chara.CharacterRelationId;
                CharacterDTO charac = DAOFactory.CharacterDAO.LoadById(characterId);
                DAOFactory.CharacterRelationDAO.Delete(id);
                ServerManager.Instance.RelationRefresh(id);

                Session.SendPacket(GenerateFinit());
                if (charac != null)
                {
                    List<CharacterRelationDTO> lst = ServerManager.Instance.CharacterRelations.Where(s => s.CharacterId == characterId || s.RelatedCharacterId == characterId).ToList();
                    string result = "finit";
                    foreach (CharacterRelationDTO relation in lst.Where(c => c.RelationType == CharacterRelationType.Friend || c.RelationType == CharacterRelationType.Spouse))
                    {
                        long id2 = relation.RelatedCharacterId == charac.CharacterId ? relation.CharacterId : relation.RelatedCharacterId;
                        bool isOnline = CommunicationServiceClient.Instance.IsCharacterConnected(ServerManager.Instance.ServerGroup, id2);
                        result += $" {id2}|{(short)relation.RelationType}|{(isOnline ? 1 : 0)}|{DAOFactory.CharacterDAO.LoadById(id2).Name}";
                    }

                    int? sentChannelId = CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = charac.CharacterId,
                        SourceCharacterId = CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = result,
                        Type = MessageType.PrivateChat
                    });
                }
            }
        }

        public void DeleteTimeout()
        {
            if (Inventory == null)
            {
                return;
            }

            foreach (ItemInstance item in Inventory.GetAllItems())
            {
                if ((item.IsBound || item.Item.ItemType == ItemType.Box) && item.ItemDeleteTime != null &&
                    item.ItemDeleteTime < DateTime.Now)
                {
                    Inventory.DeleteById(item.Id);

                    EquipmentBCards.RemoveAll(o => o.ItemVNum == item.ItemVNum);

                    if (item.Type == InventoryType.Wear)
                    {
                        Session.SendPacket(GenerateEquipment());
                    }
                    else
                    {
                        Session.SendPacket(UserInterfaceHelper.Instance.GenerateInventoryRemove(item.Type, item.Slot));
                    }

                    Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("ITEM_TIMEOUT"), 10));
                }
            }
        }

        public void DisableBuffs(BuffType type, int level = 100) => BattleEntity.DisableBuffs(type, level);

        public void DisableBuffs(List<BuffType> types, int level = 100) => BattleEntity.DisableBuffs(types, level);

        public void DisposeMap()
        {
            CloseShop();
            CloseExchangeOrTrade();
            GroupSentRequestCharacterIds.Clear();
            FamilyInviteCharacters.Clear();
            FriendRequestCharacters.Clear();
            WalkDisposable?.Dispose();
            SealDisposable?.Dispose();
            MarryRequestCharacters?.Clear();
            BattleEntity?.ClearOwnFalcon();
            BattleEntity?.ClearEnemyFalcon();
            BattleEntity?.ClearSacrificeBuff();
            BattleEntity?.RemoveOwnedMonsters();
            BattleEntity?.RemoveOwnedNpcs();
            RemoveTemporalMates();
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                Miniland?.StopLife();

                if (OriginalFaction != -1)
                {
                    Faction = (FactionType)OriginalFaction;
                }

                DisposeShopAndExchange();
                GroupSentRequestCharacterIds?.Clear();
                FamilyInviteCharacters?.Clear();
                FriendRequestCharacters?.Clear();
                Life?.Dispose();
                WalkDisposable?.Dispose();
                SealDisposable?.Dispose();
                ExploitInterval?.Dispose();
                MarryRequestCharacters?.Clear();
                BazaarItems = null;

                Mates.Where(s => s.IsTeamMember).ToList().ForEach(s =>
                {
                    Session.CurrentMapInstance?.Broadcast(Session, s.GenerateOut(), ReceiverType.AllExceptMe);
                    s.ReviveDisposable?.Dispose();
                    s.StopLife();
                });
                Session.CurrentMapInstance?.Broadcast(Session, StaticPacketHelper.Out(UserType.Player, CharacterId),
                    ReceiverType.AllExceptMe);

                if (Hp < 1)
                {
                    Hp = 1;
                }

                if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.RainbowBattleInstance)
                {
                    CharacterDTO characterToMute = DAOFactory.CharacterDAO.LoadByName(Session.Character.Name);
                    if (Session.Character.IsMuted() == false)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateInfo(string.Format(Language.Instance.GetMessageFromKey("MUTED_PLURAL"), "RBB DISCONNECT", "120")));
                    }

                    PenaltyLogDTO log = new PenaltyLogDTO
                    {
                        AccountId = characterToMute.AccountId,
                        Reason = "RBB DISCONNECT",
                        Penalty = PenaltyType.Muted,
                        DateStart = DateTime.Now,
                        DateEnd = DateTime.Now.AddMinutes(120),
                        AdminName = "SYSTEM"
                    };
                    InsertOrUpdatePenalty(log);
                }

                if (ServerManager.Instance.Groups != null)
                {
                    if (ServerManager.Instance.Groups.Any(s => s.IsMemberOfGroup(CharacterId)))
                    {
                        ServerManager.Instance.GroupLeave(Session);
                    }
                }

                LeaveTalentArena(true);
                LeaveIceBreaker();
                BattleEntity?.DisableBuffs(BuffType.All);
                BattleEntity?.RemoveOwnedMonsters();
                BattleEntity?.RemoveOwnedNpcs();
                RemoveTemporalMates();

                BattleEntity?.ClearOwnFalcon();
                BattleEntity?.ClearEnemyFalcon();
                BattleEntity?.ClearSacrificeBuff();

                if (MapInstance != null)
                {
                    if (MapInstance.MapInstanceId == Family?.Act4RaidBossMap?.MapInstanceId
                        || MapInstance.MapInstanceId == Family?.Act4Raid?.MapInstanceId)
                    {
                        short x = (short)(39 + ServerManager.RandomNumber(-2, 3));
                        short y = (short)(42 + ServerManager.RandomNumber(-2, 3));
                        if (Faction == FactionType.Angel)
                        {
                            MapId = 130;
                            MapX = x;
                            MapY = y;
                        }
                        else if (Faction == FactionType.Demon)
                        {
                            MapId = 131;
                            MapX = x;
                            MapY = y;
                        }
                    }

                    if (MapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance ||
                        MapInstance.MapInstanceType == MapInstanceType.RaidInstance)
                    {
                        MapInstance.InstanceBag.DeadList.Add(CharacterId);
                        if (MapInstance.MapInstanceType == MapInstanceType.RaidInstance)
                        {
                            Group?.Sessions.ForEach(s =>
                            {
                                if (s != null)
                                {
                                    s.SendPacket(s.Character.Group.GeneraterRaidmbf(s));
                                    s.SendPacket(s.Character.Group.GenerateRdlst());
                                }
                            });
                        }
                    }

                    if (Miniland != null)
                    {
                        ServerManager.RemoveMapInstance(Miniland.MapInstanceId);
                    }
                }
                SaveObs?.Dispose();
            }
        }

        public void DisposeShopAndExchange()
        {
            CloseShop();
            CloseExchangeOrTrade();
        }

        public void EnterInstance(ScriptedInstance input)
        {
            ScriptedInstance instance = input.Copy();
            instance.LoadScript(MapInstanceType.TimeSpaceInstance, this);
            if (instance.FirstMap == null)
            {
                return;
            }

            if (Session.Character.Level < instance.LevelMinimum)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TOO_LOW_LVL"), 0));
                return;
            }

            if (Session.Character.Level > instance.LevelMaximum)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TOO_HIGH_LVL"), 0));
                return;
            }

          
            if (instance.DailyEntries == 0)
            {
                foreach (Gift requiredItem in instance.RequiredItems)
                {
                    if (Session.Character.Inventory.CountItem(requiredItem.VNum) < requiredItem.Amount)
                    {
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("NO_ITEM_REQUIRED"), ServerManager.GetItem(requiredItem.VNum).Name), 0));
                        return;
                    }

                    Session.Character.Inventory.RemoveItemAmount(requiredItem.VNum, requiredItem.Amount);
                }

                Session?.SendPackets(instance.GenerateMinimap());
                Session?.SendPacket(instance.GenerateMainInfo());
                Session?.SendPacket(instance.FirstMap.InstanceBag.GenerateScore());
                if (instance.StartX != 0 || instance.StartY != 0)
                {
                    ServerManager.Instance.ChangeMapInstance(Session.Character.CharacterId, instance.FirstMap.MapInstanceId, instance.StartX, instance.StartY);
                }
                else
                {
                    ServerManager.Instance.TeleportOnRandomPlaceInMap(Session, instance.FirstMap.MapInstanceId);
                }

                instance.InstanceBag.CreatorId = Session.Character.CharacterId;
                Session.Character.Timespace = instance;
            }
            else
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("TS_NO_MORE_ENTRIES"), 0));
                Session.SendPacket(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("TS_NO_MORE_ENTRIES"), 10));
            }
        }

        public void GenerateAct6Async()
        {
            if (MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (short)MapTypeEnum.Act61 || m.MapTypeId == (short)MapTypeEnum.Act61a || m.MapTypeId == (short)MapTypeEnum.Act61d))
            {
                Session.SendPacket($"act6 1 0 {ServerManager.Instance.Act6Zenas.Percentage / 100} " +
                       $"{Convert.ToByte(ServerManager.Instance.Act6Zenas.Mode)} " +
                       $"{ServerManager.Instance.Act6Zenas.CurrentTime} " +
                       $"{ServerManager.Instance.Act6Zenas.TotalTime} " +
                       $"{ServerManager.Instance.Act6Erenia.Percentage / 100} " +
                       $"{Convert.ToByte(ServerManager.Instance.Act6Erenia.Mode)} " +
                       $"{ServerManager.Instance.Act6Erenia.CurrentTime} " +
                       $"{ServerManager.Instance.Act6Erenia.TotalTime}");
            }
            else
            {
                Session.SendPacket("act6");
            }
        }

        public string GenerateAdditionalHpMp()
        {
            return $"guri 4 {Math.Round(BattleEntity.AdditionalHp)} {Math.Round(BattleEntity.AdditionalMp)}";
        }

        public string GenerateAt() =>
            $"at {CharacterId} {MapInstance.Map.GridMapId} {PositionX} {PositionY} {Direction} 0 {MapInstance?.InstanceMusic ?? 0} 2 -1";

        public string GenerateBfePacket(short effect, short time) => $"bf_e 1 {CharacterId} {effect} {time}";

        public string GenerateBlinit()
        {
            string result = "blinit";

            foreach (CharacterRelationDTO relation in CharacterRelations.Where(s =>
                s.CharacterId == CharacterId && s.RelationType == CharacterRelationType.Blocked))
            {
                result +=
                    $" {relation.RelatedCharacterId}|{DAOFactory.CharacterDAO.LoadById(relation.RelatedCharacterId)?.Name}";
            }

            return result;
        }

        public string GenerateBubbleMessagePacket()
        {
            return $"csp {CharacterId} {BubbleMessage}";
        }

        public string GenerateCharge() => $"bf 1 {CharacterId} {ChargeValue}.0.{ChargeValue} {Level}";

        public string GenerateCInfo()
        {
            var morph =
                UseSp && !IsVehicled && SpInstance?.HasSkin == true
                    ? SpInstance.Item.VNum == 903 ? 102
                    : SpInstance.Item.VNum == 913 ? 101
                    : SpInstance.Item.VNum == 902 ? 100
                    : (UseSp || IsVehicled || IsMorphed ? Morph : 0)
                    : (UseSp || IsVehicled || IsMorphed ? Morph : 0);

            string packet =
                $"c_info {Name} - -1 " +
                $"{(Family != null && FamilyCharacter != null && !Undercover
                    ? $"{Family.FamilyId}.{CharacterExtension.GetFamilyNameType(Session)} {Family.Name}"
                    : "-1 -")} " +
                $"{CharacterId} {(Invisible && Authority >= AuthorityType.GM ? 6 : 0)} " +
                $"{(byte)Gender} {(byte)HairStyle} {(byte)HairColor} {(byte)Class} " +
                $"{(GetDignityIco() == 1 ? GetReputationIco() : -GetDignityIco())} {Compliment} " +
                $"{morph} {(Invisible ? 1 : 0)} " +
                $"{Family?.FamilyLevel ?? 0} {(UseSp ? MorphUpgrade : 0)} " +
                $"{ArenaWinner} 0 -1";

            Logger.Info($"C_INFO: {packet}");

            return packet;
        }
        public string GenerateCMap() =>
            $"c_map 0 {MapInstance.Map.MapId} {(MapInstance.MapInstanceType != MapInstanceType.BaseMapInstance ? 1 : 0)}";

        public string GenerateAlternativeCMode(int morph) => $"c_mode 1 {CharacterId} {morph} 0 0 0 {Size} 0";

        public string GenerateCMode()
        {
            var morph = (UseSp && !IsVehicled && SpInstance.HasSkin
                ? SpInstance.Item.VNum == 903 ? 102
                : SpInstance.Item.VNum == 913 ? 101
                : SpInstance.Item.VNum == 902 ? 100
                : UseSp || IsVehicled || IsMorphed ? Morph : 0
                : UseSp || IsVehicled || IsMorphed ? Morph : 0);

            ItemInstance item = Inventory.LoadBySlotAndType(
                (byte)EquipmentType.Wings,
                InventoryType.Wear);

            string packet = !IsSeal
                ? $"c_mode 1 {CharacterId} {morph} " +
                  $"{(!IsLaurenaMorph() && UseSp ? MorphUpgrade : 0)} " +
                  $"{(!IsLaurenaMorph() && UseSp ? MorphUpgrade2 : 0)} " +
                  $"{ArenaWinner} {Size} {item?.Item.Morph ?? 0}"
                : "";

            Logger.Info($"C_MODE: {packet}");

            return packet;
        }

        public string GenerateCond() =>
            $"cond 1 {CharacterId} {(!IsLaurenaMorph() && !CanAttack() ? 1 : 0)} {(!CanMove() ? 1 : 0)} {Speed}";

        public string GenerateDG()
        {
            byte raidType = 0;

            if (ServerManager.Instance.Act4RaidStart.AddMinutes(60) < DateTime.Now)
            {
                ServerManager.Instance.Act4RaidStart = DateTime.Now;
            }

            double seconds = (ServerManager.Instance.Act4RaidStart.AddMinutes(60) - DateTime.Now).TotalSeconds;

            switch (Family?.Act4Raid?.MapInstanceType)
            {
                case MapInstanceType.Act4Morcos:
                    raidType = 1;
                    break;

                case MapInstanceType.Act4Hatus:
                    raidType = 2;
                    break;

                case MapInstanceType.Act4Calvina:
                    raidType = 3;
                    break;

                case MapInstanceType.Act4Berios:
                    raidType = 4;
                    break;
            }

            return $"dg {raidType} {(seconds > 1800 ? 1 : 2)} {(int)seconds} 0";
        }

        public void GenerateDignity(NpcMonster monsterinfo)
        {
            if (Level < monsterinfo.Level && Dignity < 100 && Level > 20)
            {
                Dignity += (float)0.5;

                if (Dignity == (int)Dignity)
                {
                    Session.SendPacket(GenerateFd());
                    Session.CurrentMapInstance?.Broadcast(Session, GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
                    Session.CurrentMapInstance?.Broadcast(Session, GenerateGidx(), ReceiverType.AllExceptMe);
                    Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("RESTORE_DIGNITY"), 11));
                }
            }
        }

        public string GenerateDir() => $"dir 1 {CharacterId} {Direction}";

        public string GenerateDm(int dmg) => BattleEntity.GenerateDm(dmg);

        public EffectPacket GenerateEff(int effectid)
        {
            return new EffectPacket
            {
                EffectType = UserType.Player,
                CallerId = CharacterId,
                EffectId = effectid
            };
        }

        public string GenerateEq()
        {
            int color = (byte)HairColor;

            ItemInstance head = Inventory?.LoadBySlotAndType((byte)EquipmentType.Hat, InventoryType.Wear);

            if (head?.Item.IsColored == true)
            {
                color = head.Design;
            }

            return
                $"eq {CharacterId} {(Invisible && Authority >= AuthorityType.GM ? 6 : Undercover ? (byte)AuthorityType.User : Authority < AuthorityType.User ? (byte)AuthorityType.User : Authority >= AuthorityType.GM ? 2 : (byte)Authority)} {(byte)Gender} {(byte)HairStyle} {color} {(byte)Class} {GenerateEqListForPacket()} {(!InvisibleGm ? GenerateEqRareUpgradeForPacket() : null)}";

            //return $"eq {CharacterId} {(Invisible ? 6 : 0)} {(byte)Gender} {(byte)HairStyle} {color} {(byte)Class} {GenerateEqListForPacket()} {(!InvisibleGm ? GenerateEqRareUpgradeForPacket() : null)}";
        }

        public string GenerateEqListForPacket()
        {
            string[] invarray = new string[18];

            if (Inventory != null)
            {
                for (short i = 0; i < invarray.Length; i++)
                {
                    ItemInstance item = Inventory.LoadBySlotAndType(i, InventoryType.Wear);

                    if (item != null)
                    {
                        invarray[i] = item.ItemVNum.ToString();
                    }
                    else
                    {
                        invarray[i] = "-1";
                    }
                }
            }

            return
                $"{(!HideHat ? invarray[(byte)EquipmentType.Hat] : 0)}.{invarray[(byte)EquipmentType.Armor]}.{invarray[(byte)EquipmentType.MainWeapon]}.{invarray[(byte)EquipmentType.SecondaryWeapon]}.{invarray[(byte)EquipmentType.Mask]}.{invarray[(byte)EquipmentType.Fairy]}.{invarray[(byte)EquipmentType.CostumeSuit]}.{(!HideHat ? invarray[(byte)EquipmentType.CostumeHat] : 0)}.{invarray[(byte)EquipmentType.WeaponSkin]}.{invarray[(byte)EquipmentType.Wings]}.{invarray[(byte)EquipmentType.Orb]}";
        }

        public string GenerateEqRareUpgradeForPacket()
        {
            sbyte weaponRare = 0;
            byte weaponUpgrade = 0;
            sbyte armorRare = 0;
            byte armorUpgrade = 0;

            if (Inventory != null)
            {
                for (short i = 0; i < 17; i++)
                {
                    ItemInstance wearable = Inventory.LoadBySlotAndType(i, InventoryType.Wear);

                    if (wearable != null)
                    {
                        switch (wearable.Item.EquipmentSlot)
                        {
                            case EquipmentType.MainWeapon:
                                weaponRare = wearable.Rare;
                                weaponUpgrade = wearable.Upgrade;
                                break;

                            case EquipmentType.Armor:
                                armorRare = wearable.Rare;
                                armorUpgrade = wearable.Upgrade;
                                break;
                        }
                    }
                }
            }

            return $"{weaponUpgrade}{weaponRare} {armorUpgrade}{armorRare}";
        }

        public string GenerateEquipment()
        {
            string eqlist = "";

            EquipmentBCards.Lock(() =>
            {
                EquipmentBCards.Clear();
                ShellEffectArmor.Clear();
                ShellEffectMain.Clear();
                RuneEffectMain.Clear();
                FairyEnchantments.Clear();
                ShellEffectSecondary.Clear();
                CellonOptions.Clear();

                if (Inventory != null)
                {
                    EquipmentBCards.AddRange(GetRunesInEquipment());
                    EquipmentBCards.AddRange(GetFairyEnchantments());

                    for (short i = 0; i < 17; i++)
                    {
                        ItemInstance item = Inventory.LoadBySlotAndType(i, InventoryType.Wear);
                        if (item != null)
                        {
                            if (item.Item.EquipmentSlot != EquipmentType.Sp)
                            {
                                EquipmentBCards.AddRange(item.Item.BCards);
                                switch (item.Item.ItemType)
                                {
                                    case ItemType.Armor:
                                        foreach (ShellEffectDTO dto in item.ShellEffects)
                                        {
                                            ShellEffectArmor.Add(dto);
                                        }

                                        break;
                                    case ItemType.Weapon:
                                        switch (item.Item.EquipmentSlot)
                                        {
                                            case EquipmentType.MainWeapon:
                                                foreach (ShellEffectDTO dto in item.ShellEffects.Where(s => !s.IsRune))
                                                {
                                                    ShellEffectMain.Add(dto);
                                                }

                                                foreach (RuneEffectDTO dto in item.RuneEffects)
                                                {
                                                    RuneEffectMain.Add(dto);
                                                }

                                                break;

                                            case EquipmentType.SecondaryWeapon:
                                                foreach (ShellEffectDTO dto in item.ShellEffects)
                                                {
                                                    ShellEffectSecondary.Add(dto);
                                                }

                                                break;
                                        }
                                        break;

                                    case ItemType.Jewelery:
                                        switch (item.Item.EquipmentSlot)
                                        {
                                            case EquipmentType.Necklace:
                                            case EquipmentType.Bracelet:
                                            case EquipmentType.Ring:
                                                foreach (CellonOptionDTO dto in item.CellonOptions)
                                                {
                                                    CellonOptions.Add(dto);
                                                }
                                                break;

                                            case EquipmentType.Fairy:
                                                foreach (FairyEnchantmentDTO dto in item.FairyEnchantments)
                                                {
                                                    FairyEnchantments.Add(dto);
                                                }
                                                break;
                                        }
                                        break;
                                }
                                switch (item.Item.ItemSubType)
                                {
                                    case 5:
                                        eqlist += $" {i}.{(item.HoldingVNum == 0 ? item.Item.VNum : item.HoldingVNum)}.0.0.0";
                                        break;
                                }
                            }
                            if (item.Item.ItemType == ItemType.Fashion)
                            {
                                eqlist += $" {i}.{(item.HoldingVNum == 0 ? item.Item.VNum : item.HoldingVNum)}.{item.Rare}.{(item.Item.IsColored ? item.Design : item.Upgrade)}.0.{item.RuneAmount}";
                            }
                            else
                                eqlist += $" {i}.{item.Item.VNum}.{item.Rare}.{(item.Item.IsColored ? item.Design : item.Upgrade)}.0.{item.RuneAmount}";
                        }
                    }
                }

                if (Family != null)
                {

                    foreach (FamilySkillMission famskill in Family.FamilySkillMissions)
                    {
                        Item effect = ServerManager.GetItem(famskill.ItemVNum);
                        EquipmentBCards.AddRange(effect.BCards);
                    }
                }
            });

            return $"equip {GenerateEqRareUpgradeForPacket()}{eqlist}";
        }



        public string GenerateExts()
        {
            var haveback = (HaveBackpack() ? 1 : 0) * 12;
            var haveext = (HaveExtension() ? 1 : 0) * 60;

            string tropDrole = string.Empty;

            for (byte i = 0; i != 3; i++)
            {
                tropDrole += $"{48 + haveback + haveext} ";
            }

            return $"exts 0 {tropDrole}";
        }

        public string GenerateFaction() => $"fs {(byte)Faction}";

        public string GenerateFamilyMember()
        {
            string str = "gmbr 0";
            try
            {
                if (Family?.FamilyCharacters != null)
                {
                    foreach (FamilyCharacter TargetCharacter in Family?.FamilyCharacters)
                    {
                        bool isOnline = CommunicationServiceClient.Instance.IsCharacterConnected(ServerManager.Instance.ServerGroup, TargetCharacter.CharacterId);
                        str += $" {TargetCharacter.Character.CharacterId}|{Family.FamilyId}|{TargetCharacter.Character.Name}|{TargetCharacter.Character.Level}|{(byte)TargetCharacter.Character.Class}|{(byte)TargetCharacter.Authority}|{(byte)TargetCharacter.Rank}|{(isOnline ? 1 : 0)}|{TargetCharacter.Character.HeroLevel}";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return str;
        }

        public string GenerateFamilyMemberExp()
        {
            string str = "gexp";
            try
            {
                if (Family?.FamilyCharacters != null)
                {
                    foreach (FamilyCharacter TargetCharacter in Family?.FamilyCharacters)
                    {
                        str += $" {TargetCharacter.CharacterId}|{TargetCharacter.Experience}";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return str;
        }

        public string GenerateFamilyMemberMessage()
        {
            string str = "gmsg";
            try
            {
                if (Family?.FamilyCharacters != null)
                {
                    foreach (FamilyCharacter TargetCharacter in Family?.FamilyCharacters)
                    {
                        str += $" {TargetCharacter.CharacterId}|{TargetCharacter.DailyMessage}";
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return str;
        }

        public List<string> GenerateFamilyWarehouseHist()
        {
            if (Family != null)
            {
                List<string> packetList = new List<string>();
                string packet = "";
                int i = 0;
                int amount = -1;
                List<FamilyLogDTO> warehouseLogs = Family.FamilyLogs
                    .Where(s => s.FamilyLogType == FamilyLogType.WareHouseAdded ||
                                s.FamilyLogType == FamilyLogType.WareHouseRemoved).OrderByDescending(s => s.Timestamp)
                    .Take(100).ToList();
                foreach (FamilyLogDTO log in warehouseLogs)
                {
                    packet +=
                        $" {(log.FamilyLogType == FamilyLogType.WareHouseAdded ? 0 : 1)}|{log.FamilyLogData}|{(int)(DateTime.Now - log.Timestamp).TotalHours}";
                    i++;
                    if (i == 50)
                    {
                        i = 0;
                        packetList.Add($"fslog_stc {amount}{packet}");
                        amount++;
                    }
                    else if (i == warehouseLogs.Count)
                    {
                        packetList.Add($"fslog_stc {amount}{packet}");
                    }
                }

                return packetList;
            }

            return new List<string>();
        }

        public bool GenerateFamilyXp(int FXP, short InstanceId = -1)
        {
            if (!Session.Account.PenaltyLogs.Any(s => s.Penalty == PenaltyType.BlockFExp && s.DateEnd > DateTime.Now) && Family != null && FamilyCharacter != null && (InstanceId == -1))
            {
                FamilyCharacterDTO famchar = FamilyCharacter;
                FamilyDTO fam = Family;
                fam.FamilyExperience += FXP;
                famchar.Experience += FXP;
                Session.SendPacket(GenerateSay(string.Format("You won {0} family xp!", FXP), 10));
                if (CharacterHelper.LoadFamilyXPData(Family.FamilyLevel) <= fam.FamilyExperience)
                {
                    fam.FamilyExperience -= CharacterHelper.LoadFamilyXPData(Family.FamilyLevel);
                    fam.FamilyLevel++;
                    Family.AddMissionProgress((short)(9616 + fam.FamilyLevel), 1);
                    Family.InsertFamilyLog(FamilyLogType.FamilyLevelUp, level: fam.FamilyLevel);
                    CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                    {
                        DestinationCharacterId = Family.FamilyId,
                        SourceCharacterId = CharacterId,
                        SourceWorldId = ServerManager.Instance.WorldId,
                        Message = UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("FAMILY_UP"), 0),
                        Type = MessageType.Family
                    });
                }

                DAOFactory.FamilyCharacterDAO.InsertOrUpdate(ref famchar);
                DAOFactory.FamilyDAO.InsertOrUpdate(ref fam);
                ServerManager.Instance.FamilyRefresh(Family.FamilyId);
                CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
                {
                    DestinationCharacterId = Family.FamilyId,
                    SourceCharacterId = CharacterId,
                    SourceWorldId = ServerManager.Instance.WorldId,
                    Message = "fhis_stc",
                    Type = MessageType.Family
                });
                if (FXP > 1000)
                {
                    int value = FXP - FXP % 1000;
                    Session.Character.Family.InsertFamilyLog(FamilyLogType.FamilyXP, Session.Character.Name,
                        experience: value);
                }
                else if (famchar.Experience % 1000 == 0)
                {
                    Session.Character.Family.InsertFamilyLog(FamilyLogType.FamilyXP, Session.Character.Name,
                        experience: 1000);
                }

                return true;
            }

            return false;
        }

        public string GenerateFc()
        {
            return
                $"fc {(byte)Faction} {ServerManager.Instance.Act4AngelStat.MinutesUntilReset} {ServerManager.Instance.Act4AngelStat.Percentage / 100} {ServerManager.Instance.Act4AngelStat.Mode}" +
                $" {ServerManager.Instance.Act4AngelStat.CurrentTime} {ServerManager.Instance.Act4AngelStat.TotalTime} {Convert.ToByte(ServerManager.Instance.Act4AngelStat.IsMorcos)}" +
                $" {Convert.ToByte(ServerManager.Instance.Act4AngelStat.IsHatus)} {Convert.ToByte(ServerManager.Instance.Act4AngelStat.IsCalvina)} {Convert.ToByte(ServerManager.Instance.Act4AngelStat.IsBerios)}" +
                $" 0 {ServerManager.Instance.Act4DemonStat.Percentage / 100} {ServerManager.Instance.Act4DemonStat.Mode} {ServerManager.Instance.Act4DemonStat.CurrentTime} {ServerManager.Instance.Act4DemonStat.TotalTime}" +
                $" {Convert.ToByte(ServerManager.Instance.Act4DemonStat.IsMorcos)} {Convert.ToByte(ServerManager.Instance.Act4DemonStat.IsHatus)} {Convert.ToByte(ServerManager.Instance.Act4DemonStat.IsCalvina)} " +
                $"{Convert.ToByte(ServerManager.Instance.Act4DemonStat.IsBerios)} 0";

            //return $"fc {Faction} 0 69 0 0 0 1 1 1 1 0 34 0 0 0 1 1 1 1 0";
        }

        public string GenerateFd() =>
            $"fd {Reputation} {GetReputationIco()} {(int)Dignity} {Math.Abs(GetDignityIco())}";

        public string GenerateFinfo(long? relatedCharacterLoggedId, bool isConnected)
        {
            string result = "finfo";
            foreach (CharacterRelationDTO relation in CharacterRelations.Where(c =>
                c.RelationType == CharacterRelationType.Friend || c.RelationType == CharacterRelationType.Spouse))
            {
                if (relatedCharacterLoggedId.HasValue &&
                    (relatedCharacterLoggedId.Value == relation.RelatedCharacterId ||
                     relatedCharacterLoggedId.Value == relation.CharacterId))
                {
                    if (DAOFactory.CharacterDAO.LoadById(relatedCharacterLoggedId.Value) is CharacterDTO character)
                    {
                        result += $" {relatedCharacterLoggedId}.{(isConnected ? 1 : 0)}.{character.Name}";
                    }
                }
            }

            return result;
        }

        public string GenerateFinit()
        {
            string result = "finit";
            foreach (CharacterRelationDTO relation in CharacterRelations.ToList().Where(c => c.RelationType == CharacterRelationType.Friend || c.RelationType == CharacterRelationType.Spouse))
            {
                long id = relation.RelatedCharacterId == CharacterId ? relation.CharacterId : relation.RelatedCharacterId;
                if (DAOFactory.CharacterDAO.LoadById(id) is CharacterDTO character)
                {
                    bool isOnline = CommunicationServiceClient.Instance.IsCharacterConnected(ServerManager.Instance.ServerGroup, id);
                    result += $" {id}|{(short)relation.RelationType}|{(isOnline ? 1 : 0)}|{character.Name}";
                }
            }

            return result;
        }

        public static string GenerateFrank(byte type, ClientSession session)
        {
            string packet = "frank_stc";
            int rank = 0;
            long savecount = 0;

            if (type >= 0 && type <= 3)
            {
                packet += " 0";
            }

            if (type >= 4 && type <= 7)
            {
                packet += " 1";
            }

            if (type >= 8)
            {
                packet += " 2";
            }

            List<Family> familyordered = ServerManager.Instance.FamilyList.Where(s => DAOFactory.FamilyCharacterDAO.LoadByFamilyId(s.FamilyId).FirstOrDefault(c => c.Authority == FamilyAuthority.Head) is FamilyCharacterDTO famChar && DAOFactory.CharacterDAO.LoadById(famChar.CharacterId) is CharacterDTO character && DAOFactory.AccountDAO.LoadById(character.AccountId).Authority <= AuthorityType.ADMIN);

            switch (type)
            {
                case 0:
                    familyordered = familyordered.OrderByDescending(s => s.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.FamilyXP && l.Timestamp.AddDays(30) > DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]))).ToList();//use month instead log
                    break;

                case 1:
                    familyordered = familyordered.OrderByDescending(s => s.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.FamilyXP && l.Timestamp.AddDays(30) > DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]))).ToList();//use month instead log
                    break;

                case 2:

                    // use month instead log
                    familyordered = familyordered.OrderByDescending(s => s.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.RaidWon && l.Timestamp.AddDays(30) > DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]))).ToList();//use month instead log/*familyordered.OrderByDescending(s => s.FamilyCharacters.Sum(c => c.Character.Act4Points)).ToList();*/
                    break;

                case 3:
                    familyordered = familyordered.OrderByDescending(s => s.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.RaidWon && l.Timestamp.AddDays(30) > DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]))).ToList();
                    break;

                case 4:
                    familyordered = familyordered.OrderByDescending(s => s.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.FamilyXP && l.Timestamp.AddDays(60) > DateTime.Now && l.Timestamp.AddDays(30) < DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]))).ToList();//use month instead log
                    break;

                case 6:
                    familyordered = familyordered.OrderByDescending(s => s.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.RaidWon && l.Timestamp.AddDays(60) > DateTime.Now && l.Timestamp.AddDays(30) < DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]))).ToList();//use month instead log
                    break;

                case 7:
                    familyordered = familyordered.OrderByDescending(s => s.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.RaidWon && l.Timestamp.AddDays(60) > DateTime.Now && l.Timestamp.AddDays(30) < DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]))).ToList();//use month instead log
                    break;

                case 8:
                    familyordered = familyordered.OrderByDescending(s => s.FamilyExperience).ToList();
                    break;

                case 9:
                    familyordered = familyordered.OrderByDescending(s => s.FamilyCharacters.Sum(c => c.Character.Reputation)).ToList();
                    break;
            }
            int i = 0;
            if (familyordered != null)
            {
                foreach (Family fam in familyordered.Take(100))
                {
                    i++;
                    long sum = 0;
                    switch (type)
                    {
                        case 0:
                            sum = fam.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.FamilyXP && l.Timestamp.AddDays(30) > DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]));
                            if (savecount != fam.FamilyExperience)
                            {
                                rank++;
                            }
                            else
                            {
                                rank = i;
                            }
                            savecount = sum;
                            packet += $" {rank}|{fam.Name}|{fam.FamilyLevel}|{sum}";
                            if (session.Character.Family != null && session.Character.Family.Name == fam.Name)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateFmRank((byte)0, rank, fam.Name, fam.FamilyLevel, sum, fam.FamilyExperience, CharacterHelper.LoadFamilyXPData(fam.FamilyLevel)));
                            }
                            break;

                        case 1:
                            sum = fam.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.FamilyXP && l.Timestamp.AddDays(30) > DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]));
                            if (savecount != fam.FamilyExperience)
                            {
                                rank++;
                            }
                            else
                            {
                                rank = i;
                            }
                            savecount = sum;
                            packet += $" {rank}|{fam.Name}|{fam.FamilyLevel}|{sum}";
                            if (session.Character.Family != null && session.Character.Family.Name == fam.Name)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateFmRank((byte)0, rank, fam.Name, fam.FamilyLevel, sum, fam.FamilyExperience, CharacterHelper.LoadFamilyXPData(fam.FamilyLevel)));
                            }
                            break;

                        case 2:
                            if (fam.FamilyFaction == 1)
                            {
                                sum = fam.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.RaidWon && l.Timestamp.AddDays(30) > DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]));
                                if (savecount != sum)
                                {
                                    rank++;
                                }
                                else
                                {
                                    rank = i;
                                }
                                savecount = sum;//replace by month log
                                packet += $" {rank}|{fam.Name}|{fam.FamilyLevel}|{savecount}";
                                if (session.Character.Family != null && session.Character.Family.Name == fam.Name)
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateFmRank((byte)0, rank, fam.Name, fam.FamilyLevel, sum, fam.FamilyExperience, CharacterHelper.LoadFamilyXPData(fam.FamilyLevel)));
                                }
                            }
                            break;

                        case 3:
                            if (fam.FamilyFaction == 2)
                            {
                                sum = fam.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.RaidWon && l.Timestamp.AddDays(30) > DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]));
                                if (savecount != sum)
                                {
                                    rank++;
                                }
                                else
                                {
                                    rank = i;
                                }
                                savecount = sum;
                                packet += $" {rank}|{fam.Name}|{fam.FamilyLevel}|{savecount}";
                                if (session.Character.Family != null && session.Character.Family.Name == fam.Name)
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateFmRank((byte)0, rank, fam.Name, fam.FamilyLevel, sum, fam.FamilyExperience, CharacterHelper.LoadFamilyXPData(fam.FamilyLevel)));
                                }
                            }
                            break;

                        case 4:
                            sum = fam.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.FamilyXP && l.Timestamp.AddDays(60) > DateTime.Now && l.Timestamp.AddDays(30) < DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]));
                            if (savecount != fam.FamilyExperience)
                            {
                                rank++;
                            }
                            else
                            {
                                rank = i;
                            }
                            if (rank == 1)
                            {
                                fam.IconTopOne = 1;
                            }
                            else if (rank != 1)
                            {
                                fam.IconTopOne = 0;
                            }
                            savecount = sum;
                            packet += $" {rank}|{fam.Name}|{fam.FamilyLevel}|{sum}";
                            if (session.Character.Family != null && session.Character.Family.Name == fam.Name)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateFmRank((byte)1, rank, fam.Name, fam.FamilyLevel, sum, fam.FamilyExperience, CharacterHelper.LoadFamilyXPData(fam.FamilyLevel)));
                            }
                            break;

                        case 6:
                            if (fam.FamilyFaction == 1)
                            {
                                sum = fam.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.RaidWon && l.Timestamp.AddDays(60) > DateTime.Now && l.Timestamp.AddDays(30) < DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]));
                                if (savecount != sum)
                                {
                                    rank++;
                                }
                                else
                                {
                                    rank = i;
                                }
                                if (rank == 1)
                                {
                                    fam.IconTopRaid = 1;
                                }
                                else if (rank != 1)
                                {
                                    fam.IconTopRaid = 0;
                                }
                                savecount = sum;//replace by month log
                                packet += $" {rank}|{fam.Name}|{fam.FamilyLevel}|{savecount}";
                                if (session.Character.Family != null && session.Character.Family.Name == fam.Name)
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateFmRank((byte)1, rank, fam.Name, fam.FamilyLevel, sum, fam.FamilyExperience, CharacterHelper.LoadFamilyXPData(fam.FamilyLevel)));
                                }
                            }
                            break;

                        case 7:
                            if (fam.FamilyFaction == 2)
                            {
                                sum = fam.FamilyLogs.Where(l => l.FamilyLogType == FamilyLogType.RaidWon && l.Timestamp.AddDays(60) > DateTime.Now && l.Timestamp.AddDays(30) < DateTime.Now).ToList().Sum(c => long.Parse(c.FamilyLogData.Split('|')[1]));
                                if (savecount != sum)
                                {
                                    rank++;
                                }
                                else
                                {
                                    rank = i;
                                }
                                if (rank == 1)
                                {
                                    fam.IconTopRaid = 2;
                                }
                                else if (rank != 1)
                                {
                                    fam.IconTopRaid = 0;
                                }
                                savecount = sum;//replace by month log
                                packet += $" {rank}|{fam.Name}|{fam.FamilyLevel}|{savecount}";
                                if (session.Character.Family != null && session.Character.Family.Name == fam.Name)
                                {
                                    session.SendPacket(UserInterfaceHelper.GenerateFmRank((byte)1, rank, fam.Name, fam.FamilyLevel, sum, fam.FamilyExperience, CharacterHelper.LoadFamilyXPData(fam.FamilyLevel)));
                                }
                            }
                            break;

                        case 8:
                            sum = fam.FamilyExperience;
                            for (byte x = 1; x < fam.FamilyLevel; x++)
                            {
                                sum += CharacterHelper.LoadFamilyXPData(x);
                            }
                            if (savecount != sum)
                            {
                                rank++;
                            }
                            else
                            {
                                rank = i;
                            }
                            savecount = sum;
                            packet += $" {rank}|{fam.Name}|{fam.FamilyLevel}|{savecount} ";
                            if (session.Character.Family != null && session.Character.Family.Name == fam.Name)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateFmRank((byte)2, rank, fam.Name, fam.FamilyLevel, savecount, fam.FamilyExperience, CharacterHelper.LoadFamilyXPData(fam.FamilyLevel)));
                            }
                            break;

                        case 9:
                            sum = fam.FamilyCharacters.Sum(c => c.Character.Reputation);
                            if (savecount != sum)
                            {
                                rank++;
                            }
                            else
                            {
                                rank = i;
                            }
                            savecount = sum;
                            packet += $" {rank}|{fam.Name}|{fam.FamilyLevel}|{savecount}";
                            if (session.Character.Family != null && session.Character.Family.Name == fam.Name)
                            {
                                session.SendPacket(UserInterfaceHelper.GenerateFmRank((byte)2, rank, fam.Name, fam.FamilyLevel, sum, fam.FamilyExperience, CharacterHelper.LoadFamilyXPData(fam.FamilyLevel)));
                            }
                            break;
                    }
                }
            }
            return packet;
        }


        public void RestartFExp()
        {

            Session.Character.FamilyCharacter.Experience = 0;
        }

        public string GenerateFStashAll()
        {
            string stash = $"f_stash_all {Family.WarehouseSize}";
            foreach (ItemInstance item in Family.Warehouse.GetAllItems())
            {
                stash += $" {item.GenerateStashPacket()}";
            }

            return stash;
        }

        public string GenerateFtPtPacket() => $"ftpt {UltimatePoints} 3000";

        public string GenerateGb(byte type) => $"gb {type} {Session.Character.GoldBank / 1000} {Gold} 0 0";

        public string GenerateGender() => $"p_sex {(byte)Gender}";

        public string GenerateGExp()
        {
            string str = "gexp";
            foreach (FamilyCharacter familyCharacter in Family.FamilyCharacters)
            {
                str += $" {familyCharacter.CharacterId}|{familyCharacter.Experience}";
            }

            return str;
        }

        public string GenerateGidx() //Left Faction
        {
            if (Family != null && FamilyCharacter != null && Family.FamilySkillMissions != null)
            {
                var faction = (Family.FamilyLevel >= 5 ? Family.FamilyFaction == 0 ? "1" : Family.FamilyFaction.ToString() : "0");

                return
                    $"gidx 1 " +
                    $"{CharacterId} " +
                    $"{Family.FamilyId}.{CharacterExtension.GetFamilyNameType(Session)} " +
                    $"{Family.Name} " +
                    $"{Family.FamilyLevel} " +
                    $"{(Family.FamilySkillMissions.Any(s => s.ItemVNum == 9600) ? 1 : 0)}|" +
                    $"{(Family.FamilySkillMissions.Any(s => s.ItemVNum == 9601) ? 1 : 0)}|" +
                    $"{Family.IconTopOne}|0|{Family.IconTopRaid}";
            }

            return
                $"gidx 1 " +
                $"{CharacterId} " +
                $"-1 " +
                $"- 0 " +
                $"0|0|0";
        }

        public string GenerateGInfo()
        {
            if (Family != null)
            {
                try
                {
                    FamilyCharacter familyCharacter = Family.FamilyCharacters.Find(s => s.Authority == FamilyAuthority.Head);
                    if (familyCharacter != null)
                    {
                        return $"ginfo {Family.Name} {familyCharacter.Character.Name} {(byte)Family.FamilyHeadGender} {Family.FamilyLevel} {Family.FamilyExperience} {CharacterHelper.LoadFamilyXPData(Family.FamilyLevel)} {Family.FamilyCharacters.Count} {Family.MaxSize} {(byte)FamilyCharacter.Authority} {(Family.ManagerCanInvite ? 1 : 0)} {(Family.ManagerCanNotice ? 1 : 0)} {(Family.ManagerCanShout ? 1 : 0)} {(Family.ManagerCanGetHistory ? 1 : 0)} {(byte)Family.ManagerAuthorityType} {(Family.MemberCanGetHistory ? 1 : 0)} {(byte)Family.MemberAuthorityType} {Family.FamilyMessage.Replace(' ', '^')}";
                    }
                }
                catch (Exception)
                {
                    return "";
                }
            }
            return "";
        }

        public string GenerateAscr() => $"ascr {ArenaKill} {ArenaDeath} 0 {CurrentArenaKill} {CurrentArenaDeath} 0 0 0 0 0";

        public string GenerateFood()
        {
            return $"food 0";
        }

        public string GenerateGold() => $"gold {Gold} {Session.Character.GoldBank / 1000}";

        public string GenerateIcon(int type, int value, short itemVNum) =>
            $"icon {type} {CharacterId} {value} {itemVNum}";

        public string GenerateIdentity() => $"Character: {Name}";

        public string GenerateIn(bool foe = false, AuthorityType receiverAuthority = AuthorityType.User, int InEffect = 0)
        {
            string name = Name;

            if (receiverAuthority >= AuthorityType.GM)
            {
                foe = false;
                name = $"[{Faction}]{name}";
            }

            if (foe && Authority < AuthorityType.GM)
            {
                name = "!§$%&/()=?*+~#";
            }

            int faction = 0;

            if (ServerManager.Instance.ChannelId == 51)
            {
                faction = (byte)Faction + 2;
            }

            int color = HairStyle == HairStyleType.Hair8 ? 0 : (byte)HairColor;

            ItemInstance fairy = null;

            if (Inventory != null)
            {
                ItemInstance headWearable = Inventory.LoadBySlotAndType((byte)EquipmentType.Hat, InventoryType.Wear);

                if (headWearable?.Item.IsColored == true)
                {
                    color = headWearable.Design;
                }

                fairy = Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            }

            long tit = 0;
            if (Title.Find(s => s.Stat.Equals(3)) != null)
            {
                tit = Title.Find(s => s.Stat.Equals(3)).TitleVnum;
            }

            if (Title.Find(s => s.Stat.Equals(7)) != null)
            {
                tit = Title.Find(s => s.Stat.Equals(7)).TitleVnum;
            }

            var fLvl = (Family != null
                ? Family.FamilyLevel >= 5 ? Family.FamilyFaction == 0 ? "1" : Family.FamilyFaction.ToString() : "0"
                : "0");
            if (Session.Character.Authority == AuthorityType.GS)
            {
                return $"in 1 " +
                   $"{(Authority > AuthorityType.User && !Undercover ? $"[GS]{name}" : name)} " +
                   $"- {CharacterId} {PositionX} {PositionY} {Direction} " +
                   $"{(Undercover ? (byte)AuthorityType.User : (byte)Authority)} {(byte)Gender} {(byte)HairStyle} {color} {(byte)Class} " +
                   $"{GenerateEqListForPacket()} {Math.Ceiling(Hp / HPLoad() * 100)} {Math.Ceiling(Mp / MPLoad() * 100)} {(IsSitting ? 1 : 0)} " +
                   $"{(Group?.GroupType == GroupType.Group ? (Group?.GroupId ?? -1) : -1)} {(fairy != null && !Undercover ? 4 : 0)} " +
                   $"{fairy?.Item.Element ?? 0} 0 {fairy?.Item.Morph ?? 0} {InEffect} {(UseSp || IsVehicled || IsMorphed ? Morph : 0)} " +
                   $"{GenerateEqRareUpgradeForPacket()} {(!Undercover ? (foe ? -1 : Family?.FamilyId ?? -1) : -1)} {(!Undercover ? (foe ? name : Family?.Name ?? "-") : "-")} " +
                   $"{(GetDignityIco() == 1 ? GetReputationIco() : -GetDignityIco())} {(Invisible ? 1 : 0)} {(UseSp ? MorphUpgrade : 0)} {faction} " +
                   $"{(UseSp ? MorphUpgrade2 : 0)} {Level} {Family?.FamilyLevel ?? 0} " +
                   $"{Family?.IconTopOne ?? 0}|0|{Family?.IconTopRaid ?? 0} " +
                   $"{ArenaWinner} " +
                   $"{Compliment} {Size} {HeroLevel} {tit}";
            }
            return $"in 1 " +
                   $"{(Authority > AuthorityType.User && !Undercover ? name : name)} " +
                   $"- {CharacterId} {PositionX} {PositionY} {Direction} " +
                   $"{(Undercover ? (byte)AuthorityType.User : ((byte)Authority > 2 ? 2 : Authority))} {(byte)Gender} {(byte)HairStyle} {color} {(byte)Class} " +
                   $"{GenerateEqListForPacket()} {Math.Ceiling(Hp / HPLoad() * 100)} {Math.Ceiling(Mp / MPLoad() * 100)} {(IsSitting ? 1 : 0)} " +
                   $"{(Group?.GroupType == GroupType.Group ? (Group?.GroupId ?? -1) : -1)} {(fairy != null && !Undercover ? 4 : 0)} " +
                   $"{fairy?.Item.Element ?? 0} 0 {fairy?.Item.Morph ?? 0} {InEffect} {(UseSp || IsVehicled || IsMorphed ? Morph : 0)} " +
                   $"{GenerateEqRareUpgradeForPacket()} {(!Undercover ? (foe ? -1 : Family?.FamilyId ?? -1) : -1)} {(!Undercover ? (foe ? name : Family?.Name ?? "-") : "-")} " +
                   $"{(GetDignityIco() == 1 ? GetReputationIco() : -GetDignityIco())} {(Invisible ? 1 : 0)} {(UseSp ? MorphUpgrade : 0)} {faction} " +
                   $"{(UseSp ? MorphUpgrade2 : 0)} {Level} {Family?.FamilyLevel ?? 0} " +
                   $"{Family?.IconTopOne ?? 0}|0|{Family?.IconTopRaid ?? 0} " +
                   $"{ArenaWinner} " +
                   $"{Compliment} {Size} {HeroLevel} {tit}";
        }


        public string GenerateInvisible() => $"cl {CharacterId} {(Invisible ? 1 : 0)} {(InvisibleGm ? 1 : 0)}";

        public void GenerateKillBonusAsync(MapMonster monsterToAttack, BattleEntity Killer)
        {
            Session.Character.MonsterCount += 1;

            #region Battle Pass
            if (GameConfiguration.BattlePassEnabled)
            {
                int bppChance = ServerManager.RandomNumber(0, 100);
                if (monsterToAttack.Monster.Level >= Session.Character.Level + 5 || monsterToAttack.Monster.Level <= Session.Character.Level - 5)
                {
                    if (bppChance < 4)
                    {
                        if (Session.Character.UnlockedBattlePassMultiplicator)
                        {
                            Session.Character.BattlePassPoints += 2;
                            MessageExtension.SendGreen(Session, "You received 2 BattlePass Point");
                        }
                        else
                        {
                            Session.Character.BattlePassPoints += 1;
                            MessageExtension.SendGreen(Session, "You received 1 BattlePass Point");
                        }
                    }
                }
            }
            #endregion 

            #region Rainbow Battle
            if (Session?.CurrentMapInstance?.MapInstanceType == MapInstanceType.RainbowBattleInstance && monsterToAttack?.MonsterVNum == 2558)
            {
                var rbb = ServerManager.Instance.RainbowBattleMembers.Find(s => s.Session.Contains(Session));

                rbb.Score += 5;
                MandraCount += 1;

                // Give buff mandra
                if (ServerManager.RandomNumber() < 90)
                {
                    Session.Character.AddBuff(new Buff(4, Level), BattleEntity, true);
                }
                else
                {
                    Session.Character.AddBuff(new Buff(5, Level), BattleEntity, true);
                }

                Session.CurrentMapInstance.Broadcast($"msg 0 {Session.Character.Name} killed the mandra and won 5 points !");
                RainbowBattleManager.SendFbs(Session.CurrentMapInstance);
            }
            #endregion

            void _handleGoldDrop(DropDTO drop, long maxGold, long? dropOwner, short posX, short posY)
            {
                Observable.Timer(TimeSpan.FromMilliseconds(500)).Subscribe(async o =>
                {
                    if (Session == null)
                    {
                        return;
                    }
                    if (Session.HasCurrentMapInstance)
                    {
                        if (CharacterId == dropOwner && Session.Character.HasBuff(5003) || Session.Character.HasBuff(5004) || Session.Character.HasBuff(5005))
                        {
                            double multiplier = 1 + (Session.Character.GetBuff(CardType.Item, (byte)AdditionalTypes.Item.IncreaseEarnedGold)[0] / 100D);
                            multiplier += (Session.Character.ShellEffectMain.FirstOrDefault(s => s.Effect == (byte)ShellWeaponEffectType.GainMoreGold)?.Value ?? 0) / 100D;
                            multiplier += (HasEreniaMedal() ? 1.2d : 0);

                            Gold += (int)(drop.Amount * multiplier);

                            if (Gold > maxGold)
                            {
                                Gold = maxGold;
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MAX_GOLD"), 0));
                            }
                            Session.SendPacket(GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {ServerManager.GetItem(drop.ItemVNum).Name} x{drop.Amount}{(multiplier > 1 ? $" + {(int)(drop.Amount * multiplier) - drop.Amount}" : "")}", 12));
                            Session.SendPacket(GenerateGold());
                        }
                        else
                        {
                            Session.CurrentMapInstance.DropItemByMonster(dropOwner, drop, monsterToAttack.MapX, monsterToAttack.MapY);
                        }
                    }
                });
            }

            void _handleItemDrop(DropDTO drop, long? owner, short posX, short posY)
            {
                Observable.Timer(TimeSpan.FromMilliseconds(500)).Subscribe(o =>
                {
                    if (Session == null)
                    {
                        return;
                    }
                    if (Session.HasCurrentMapInstance)
                    {

                        if (CharacterId == owner && Session.Character.HasBuff(5003) || Session.Character.HasBuff(5004) || Session.Character.HasBuff(5005))
                        {
                            if (Quests.Any(q => (q.Quest.QuestType == (int)QuestType.Collect4 || q.Quest.QuestType == (int)QuestType.Collect2 || (q.Quest?.QuestType == (int)QuestType.Collect1 && MapInstance.Map.MapTypes.Any(s => s.MapTypeId != (short)MapTypeEnum.Act4))) && q.Quest.QuestObjectives.Any(qst => qst.Data == drop.ItemVNum)) || drop.ItemVNum == 1086 || drop.ItemVNum == 1114)
                            {
                                Session.CurrentMapInstance.DropItemByMonster(owner, drop, monsterToAttack.MapX, monsterToAttack.MapY, Quests.Any(q => (q.Quest.QuestType == (int)QuestType.Collect4 || q.Quest.QuestType == (int)QuestType.Collect2 || (q.Quest?.QuestType == (int)QuestType.Collect1 && MapInstance.Map.MapTypes.Any(s => s.MapTypeId != (short)MapTypeEnum.Act4))) && q.Quest.QuestObjectives.Any(qst => qst.Data == drop.ItemVNum)));
                            }
                            else
                            {
                                if (drop != null)
                                {
                                    GiftAdd(drop.ItemVNum, (byte)drop.Amount);
                                    SealedVesselEventExtension.GenerateDrop(Session, monsterToAttack, owner);
                                }
                                else
                                {
                                    Console.WriteLine("This maybe caused the issue. Something here went wrong.");
                                    Console.WriteLine($"{drop.ItemVNum}");
                                }
                            }
                        }
                        else
                        {
                            if (monsterToAttack.MonsterVNum == 161)
                            {
                                RewardExtension.HandleMultipleItemDrops(Session, monsterToAttack, owner, drop);
                                return;
                            }
                            SealedVesselEventExtension.GenerateDrop(Session, monsterToAttack, owner);
                            Session.CurrentMapInstance.DropItemByMonster(owner, drop, monsterToAttack.MapX, monsterToAttack.MapY, Quests.Any(q => (q.Quest.QuestType == (int)QuestType.Collect4 || q.Quest.QuestType == (int)QuestType.Collect2 || (q.Quest?.QuestType == (int)QuestType.Collect1 && MapInstance.Map.MapTypes.Any(s => s.MapTypeId != (short)MapTypeEnum.Act4))) && q.Quest.QuestObjectives.Any(qst => qst.Data == drop.ItemVNum)));
                        }
                    }
                });
            }

            lock (_syncObj)
            {
                if (monsterToAttack == null || monsterToAttack.IsAlive)
                {
                    return;
                }

                monsterToAttack.RunDeathEvent();


                if (monsterToAttack.GetBuff(CardType.SpecialEffects,
                    (byte)AdditionalTypes.SpecialEffects.DecreaseKillerHP) is int[] DecreaseKillerHp)
                {
                    bool EffectResistance = false;
                    if (Killer.MapEntityId != CharacterId)
                    {
                        if (Killer.HasBuff(CardType.Buff, (byte)AdditionalTypes.Buff.EffectResistance))
                        {
                            if (ServerManager.RandomNumber() < 90)
                            {
                                EffectResistance = true;
                            }
                        }

                        if (!EffectResistance)
                        {
                            if (DecreaseKillerHp[0] > 0)
                            {
                                if (!HasGodMode)
                                {
                                    int DecreasedHp = 0;
                                    if (Killer.Hp - Killer.Hp * DecreaseKillerHp[0] / 100 > 1)
                                    {
                                        DecreasedHp = Killer.Hp * DecreaseKillerHp[0] / 100;
                                    }
                                    else
                                    {
                                        DecreasedHp = Killer.Hp - 1;
                                    }

                                    Killer.GetDamage(DecreasedHp, monsterToAttack.BattleEntity, true);
                                    Session.SendPacket(Killer.GenerateDm(DecreasedHp));
                                    if (Killer.Mate != null)
                                    {
                                        Session.SendPacket(Killer.Mate.GenerateStatInfo());
                                    }
                                    Session.SendPacket(new EffectPacket { EffectType = Killer.UserType, CallerId = Killer.MapEntityId, EffectId = 6007 });
                                }
                            }
                        }
                    }
                    else
                    {
                        if (HasBuff(CardType.Buff, (byte)AdditionalTypes.Buff.EffectResistance))
                        {
                            if (ServerManager.RandomNumber() < 90)
                            {
                                EffectResistance = true;
                            }
                        }

                        if (!EffectResistance)
                        {
                            if (DecreaseKillerHp[0] > 0)
                            {
                                if (!HasGodMode)
                                {
                                    int DecreasedHp = 0;
                                    if (Hp - Hp * DecreaseKillerHp[0] / 100 > 1)
                                    {
                                        DecreasedHp = Hp * DecreaseKillerHp[0] / 100;
                                    }
                                    else
                                    {
                                        DecreasedHp = Hp - 1;
                                    }

                                    GetDamage(DecreasedHp, monsterToAttack.BattleEntity, true);
                                    Session.SendPacket(GenerateDm(DecreasedHp));
                                    Session.SendPacket(GenerateStat());
                                    Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, Session.Character.CharacterId, 6007));
                                }
                            }
                        }
                    }
                }

                Random random = new Random(DateTime.Now.Millisecond & monsterToAttack.MapMonsterId);

                long? dropOwner;

                lock (monsterToAttack.DamageList)
                {
                    dropOwner = monsterToAttack.DamageList.FirstOrDefault(s => s.Value > 0).Key?.MapEntityId ?? null;
                }

                Group group = null;
                if (dropOwner != null)
                {
                    group = ServerManager.Instance.Groups.Find(g =>
                        g.IsMemberOfGroup((long)dropOwner) && g.GroupType == GroupType.Group);
                }

                IncrementQuests(QuestType.Hunt, monsterToAttack.MonsterVNum);

                if (ServerManager.Instance.ChannelId == 51)
                {
                    if (ServerManager.Instance.Act4DemonStat.Mode == 0 &&
                        ServerManager.Instance.Act4AngelStat.Mode == 0 && !CaligorRaid.IsRunning)
                    {
                        if (Faction == FactionType.Angel)
                        {
                            ServerManager.Instance.Act4AngelStat.Percentage++;
                        }
                        else if (Faction == FactionType.Demon)
                        {
                            ServerManager.Instance.Act4DemonStat.Percentage++;
                        }
                    }

                    if (monsterToAttack.MonsterVNum == 556)
                    {
                        if (ServerManager.Instance.Act4AngelStat.Mode == 1 && Faction != FactionType.Angel)
                        {
                            ServerManager.Instance.Act4AngelStat.Mode = 0;
                        }

                        if (ServerManager.Instance.Act4DemonStat.Mode == 1 && Faction != FactionType.Demon)
                        {
                            ServerManager.Instance.Act4DemonStat.Mode = 0;
                        }
                    }
                }


                // end owner set
                if (Session.HasCurrentMapInstance &&
                    ((MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance || MapInstance.MapInstanceType == MapInstanceType.CustomInstance ||
                      MapInstance.MapInstanceType == MapInstanceType.LodInstance) || MapInstance.DropAllowed))
                {
                    short[] explodeMonsters = new short[] { 1348, 1906 };

                    List<DropDTO> droplist = monsterToAttack.Monster.Drops.Where(s =>
                        (!explodeMonsters.Contains(monsterToAttack.MonsterVNum) &&
                         Session.CurrentMapInstance.Map.MapTypes.Any(m => m.MapTypeId == s.MapTypeId)) ||
                        s.MapTypeId == null).ToList();

                    int levelDifference = Session.Character.Level - monsterToAttack.Monster.Level;

                    #region Quest

                    Quests.Where(q =>
                            (q.Quest?.QuestType == (int)QuestType.Collect4 ||
                             q.Quest?.QuestType == (int)QuestType.Collect2 ||
                             (q.Quest?.QuestType == (int)QuestType.Collect1 &&
                              MapInstance.Map.MapTypes.Any(s => s.MapTypeId != (short)MapTypeEnum.Act4)))).ToList()
                        .ForEach(qst =>
                        {
                            qst.Quest.QuestObjectives.ForEach(d =>
                            {
                                if (d.SpecialData == monsterToAttack.MonsterVNum || d.SpecialData == null)
                                {
                                    droplist.Add(new DropDTO()
                                    {
                                        ItemVNum = (short)d.Data,
                                        Amount = 1,
                                        MonsterVNum = monsterToAttack.MonsterVNum,
                                        DropChance = (int)((d.DropRate ?? 100) * 100 * GameConfiguration.QuestDropRate) // Approx
                                    });
                                }
                            });
                        });

                    IncrementQuests(QuestType.FlowerQuest, monsterToAttack.Monster.Level);

                    #endregion

                    if (explodeMonsters.Contains(monsterToAttack.MonsterVNum) && ServerManager.RandomNumber() < 50)
                    {
                        MapInstance.Broadcast($"eff 3 {monsterToAttack.MapMonsterId} 3619");
                        if (Killer.MapEntityId != CharacterId)
                        {
                            if (!HasGodMode)
                            {
                                int DecreasedHp = 0;
                                if (Killer.Hp - Killer.Hp * 50 / 100 > 1)
                                {
                                    DecreasedHp = Killer.Hp * 50 / 100;
                                }
                                else
                                {
                                    DecreasedHp = Killer.Hp - 1;
                                }

                                Killer.GetDamage(DecreasedHp, monsterToAttack.BattleEntity, true);
                                if (Killer.Mate != null)
                                {
                                    Session.SendPacket(Killer.Mate.GenerateStatInfo());
                                }
                            }
                        }
                        else
                        {
                            if (!HasGodMode)
                            {
                                int DecreasedHp = 0;
                                if (Hp - Hp * 50 / 100 > 1)
                                {
                                    DecreasedHp = Hp * 50 / 100;
                                }
                                else
                                {
                                    DecreasedHp = Hp - 1;
                                }

                                GetDamage(DecreasedHp, monsterToAttack.BattleEntity, true);
                                Session.SendPacket(GenerateStat());
                            }
                        }

                        return;
                    }

                    if (monsterToAttack.Monster.MonsterType != MonsterType.Special)
                    {
                        #region item drop

                        int dropRate = (GameConfiguration.DropRate + MapInstance.DropRate);
                        int x = 0;
                        double rndamount = ServerManager.NextDoubleLinear(0.01, 100);


                        foreach (DropDTO drop in droplist.OrderBy(s => random.Next()))
                        {
                            if (x < 4)
                            {
                                if (!explodeMonsters.Contains(monsterToAttack.MonsterVNum))
                                {
                                    rndamount = ServerManager.NextDoubleLinear(0.01, 100);
                                }

                                bool divideRate = true;
                                if (MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (byte)MapTypeEnum.Act4)
                                    || MapInstance.Map.MapId == 20001 // Miniland
                                    || MapInstance.Map.MapId == 103
                                    || explodeMonsters.Contains(monsterToAttack.MonsterVNum))
                                {
                                    divideRate = false;
                                }

                                double divider = !divideRate ? 1 : levelDifference >= 40 ? 0.25 : levelDifference <= -40 ? 0.25 : levelDifference >= 20 ? 0.5 : levelDifference <= -20 ? 0.5 : 1;
                                if (rndamount <= (((double)drop.DropChance) * dropRate / 1000) * divider)
                                {
                                    x++;
                                    if (Session.CurrentMapInstance != null)
                                    {
                                        if (monsterToAttack.Monster.MonsterType == MonsterType.Elite)
                                        {
                                            List<long> alreadyGifted = new List<long>();
                                            List<BattleEntity> damagers;

                                            lock (monsterToAttack.DamageList)
                                            {
                                                damagers = monsterToAttack.DamageList.Keys.ToList();
                                            }

                                            foreach (BattleEntity damager in damagers)
                                            {
                                                if (!alreadyGifted.Contains(damager.MapEntityId))
                                                {
                                                    ClientSession giftsession =
                                                        ServerManager.Instance.GetSessionByCharacterId(
                                                            damager.MapEntityId);
                                                    giftsession?.Character.GiftAdd(drop.ItemVNum, (byte)drop.Amount);
                                                    alreadyGifted.Add(damager.MapEntityId);
                                                }
                                            }
                                        }
                                        else if (Session.CurrentMapInstance.Map.MapTypes.Any(s =>
                                            s.MapTypeId == (short)MapTypeEnum.Act4))
                                        {
                                            List<long> alreadyGifted = new List<long>();
                                            List<Character> hitters;

                                            lock (monsterToAttack.DamageList)
                                            {
                                                hitters = monsterToAttack.DamageList
                                                    .Where(s => s.Key?.Character != null &&
                                                                s.Key.Character.MapInstance ==
                                                                monsterToAttack.MapInstance && s.Value > 0)
                                                    .Select(s => s.Key.Character).ToList();
                                            }

                                            foreach (Character hitter in hitters)
                                            {
                                                if (!alreadyGifted.Contains(hitter.CharacterId))
                                                {
                                                    hitter.GiftAdd(drop.ItemVNum, (byte)drop.Amount);
                                                    alreadyGifted.Add(hitter.CharacterId);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            if (group?.GroupType == GroupType.Group)
                                            {
                                                if (group.SharingMode == (byte)GroupSharingType.ByOrder)
                                                {
                                                    dropOwner = group.GetNextOrderedCharacterId(this);
                                                    if (dropOwner.HasValue)
                                                    {
                                                        group.Sessions.ForEach(s =>
                                                            s.SendPacket(s.Character.GenerateSay(
                                                                string.Format(
                                                                    Language.Instance
                                                                        .GetMessageFromKey("ITEM_BOUND_TO"),
                                                                    ServerManager.GetItem(drop.ItemVNum).Name,
                                                                    group.Sessions.Single(c =>
                                                                            c.Character.CharacterId == (long)dropOwner)
                                                                        .Character.Name, drop.Amount), 10)));
                                                    }
                                                }
                                                else
                                                {
                                                    group.Sessions.ForEach(s =>
                                                        s.SendPacket(s.Character.GenerateSay(
                                                            string.Format(
                                                                Language.Instance.GetMessageFromKey("DROPPED_ITEM"),
                                                                ServerManager.GetItem(drop.ItemVNum).Name, drop.Amount),
                                                            10)));
                                                }
                                            }

                                            _handleItemDrop(drop, dropOwner, monsterToAttack.MapX,
                                                monsterToAttack.MapY);
                                        }
                                    }

                                    if (explodeMonsters.Contains(monsterToAttack.MonsterVNum))
                                    {
                                        break;
                                    }
                                }
                                else if (explodeMonsters.Contains(monsterToAttack.MonsterVNum))
                                {
                                    rndamount -= (double)drop.DropChance * dropRate / 1000.000 / divider;
                                }

                            }
                        }

                        #endregion

                        #region gold drop

                        // gold calculation
                        int gold = GetGold(monsterToAttack);
                        gold *= GameConfiguration.GoldRate;
                        long maxGold = GameConfiguration.MaxGold;
                        gold = gold > maxGold ? (int)maxGold : gold;
                        double randChance = ServerManager.RandomNumber() * random.NextDouble();

                        if (Session.CurrentMapInstance.MapInstanceType != MapInstanceType.LodInstance && gold > 0 && randChance <= (int)(GameConfiguration.GoldDropRate * 10 *
                            (Session.CurrentMapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4) ? 1 : CharacterHelper.GoldPenalty(Level, (byte)monsterToAttack.Monster.Level))))
                        {
                            DropDTO drop2 = new DropDTO
                            {
                                Amount = gold,
                                ItemVNum = 1046
                            };



                            if (Session.CurrentMapInstance != null)
                            {
                                if (Session.CurrentMapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4) || monsterToAttack.Monster.MonsterType == MonsterType.Elite)
                                {
                                    List<long> alreadyGifted = new List<long>();
                                    List<BattleEntity> damagers;

                                    lock (monsterToAttack.DamageList)
                                    {
                                        damagers = monsterToAttack.DamageList.Keys.ToList();
                                    }

                                    foreach (BattleEntity damager in damagers)
                                    {
                                        if (!alreadyGifted.Contains(damager.MapEntityId))
                                        {
                                            ClientSession session = ServerManager.Instance.GetSessionByCharacterId(damager.MapEntityId);
                                            if (session != null)
                                            {
                                                double multiplier = 1 + (GetBuff(CardType.Item, (byte)AdditionalTypes.Item.IncreaseEarnedGold)[0] / 100D);
                                                multiplier += (ShellEffectMain.FirstOrDefault(s => s.Effect == (byte)ShellWeaponEffectType.GainMoreGold)?.Value ?? 0) / 100D;

                                                session.Character.Gold += (int)(drop2.Amount * multiplier);
                                                if (session.Character.Gold > maxGold)
                                                {
                                                    session.Character.Gold = maxGold;
                                                    session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MAX_GOLD"), 0));
                                                }
                                                session.SendPacket(session.Character.GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {ServerManager.GetItem(drop2.ItemVNum).Name} x {drop2.Amount}{(multiplier > 1 ? $" + {(int)(drop2.Amount * multiplier) - drop2.Amount}" : "")}", 10));
                                                session.SendPacket(session.Character.GenerateGold());
                                            }
                                            alreadyGifted.Add(damager.MapEntityId);
                                        }
                                    }
                                }
                                else
                                {
                                    if (group != null && MapInstance.MapInstanceType != MapInstanceType.LodInstance)
                                    {
                                        if (group.SharingMode == (byte)GroupSharingType.ByOrder)
                                        {
                                            dropOwner = group.GetNextOrderedCharacterId(this);

                                            if (dropOwner.HasValue)
                                            {
                                                group.Sessions.ForEach(s => s.SendPacket(s.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("ITEM_BOUND_TO"), ServerManager.GetItem(drop2.ItemVNum).Name, group.Sessions.Single(c => c.Character.CharacterId == (long)dropOwner).Character.Name, drop2.Amount), 10)));
                                            }
                                        }
                                        else
                                        {
                                            group.Sessions.ForEach(s => s.SendPacket(s.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("DROPPED_ITEM"), ServerManager.GetItem(drop2.ItemVNum).Name, drop2.Amount), 10)));
                                        }
                                    }

                                    _handleGoldDrop(drop2, maxGold, dropOwner, monsterToAttack.MapX, monsterToAttack.MapY);
                                }
                            }
                        }

                        #endregion
                    }
                }

                #region act.4 % from monsters

                //if (monsterToAttack.MapInstance?.MapInstanceType == MapInstanceType.Act4Instance)
                if (monsterToAttack.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (byte)MapTypeEnum.Act4 == true))
                {
                    if (ServerManager.Instance.Act4AngelStat.Mode == 0 &&
                        ServerManager.Instance.Act4DemonStat.Mode == 0 && ServerManager.Instance.ChannelId == 51)
                    {
                        switch (Faction)
                        {
                            case FactionType.Angel:
                                ServerManager.Instance.Act4AngelStat.Percentage +=
                                   10000 / (GlacernonConfigurationExtension.GlacernonRatePVM * 500);
                                break;

                            case FactionType.Demon:
                                ServerManager.Instance.Act4DemonStat.Percentage +=
                                    10000 / (GlacernonConfigurationExtension.GlacernonRatePVM * 500);
                                break;
                        }

                        ServerManager.Instance.Act4Process();
                    }
                }

                #endregion

                #region Act6 Stats

                if (monsterToAttack.MapInstance.Map.MapTypes.Any(m => m.MapTypeId == (byte)MapTypeEnum.Act61))
                {
                    if (((MapId >= 229 && MapId <= 232) || MapId == 237 || MapId == 238))
                    {
                        ServerManager.Instance.Act6Zenas.Percentage += 5 * GameConfiguration.Act6ZenasRaidMultiplier;
                        ServerManager.Instance.Act6Process();
                    }

                    if (((MapId >= 233 && MapId <= 236) || MapId == 239 || MapId == 240))
                    {
                        ServerManager.Instance.Act6Erenia.Percentage += 5 * GameConfiguration.Act6EreniaRaidMultiplier;
                        ServerManager.Instance.Act6Process();
                    }
                }

                #endregion

                #region EXP, Reputation and Dignity

                if (Hp > 0 && !monsterToAttack.BattleEntity.IsMateTrainer(monsterToAttack.MonsterVNum))
                {
                    GenerateXp(monsterToAttack);
                    GenerateDignity(monsterToAttack.Monster);
                    GenerateReputation(monsterToAttack);
                }

                #endregion

                PrimalQuestRewardExtension.GenerateCharacterReward(Session, monsterToAttack);
            }
        }


        public void HandleMultipleDrops(DropDTO Drop, long? Owner, MapMonster Monster)
        {
            short destX;
            short destY;
            destX = (short)(Monster.MapX + ServerManager.RandomNumber(-10, 10));
            destY = (short)(Monster.MapY + ServerManager.RandomNumber(-10, 10));
            Session.CurrentMapInstance.DropItemByMonster(Owner, Drop, destX, destY);
        }

        #region BattlePass

        public void IncreaseBpQuest(BpQuestType questType, int value = 1)
        {
            if (!GameConfiguration.BattlePassEnabled)
            {
                return;
            }

            if (ServerManager.Instance.ChannelId == 51)
            {
                return;
            }

            List<BattlePassQuestDTO> bpQuests = new();

            bpQuests.AddRange(ServerManager.Instance.BattlePassQuests.Where(b => b.BpQuestType == questType && b.IsPremium == false));

            if (Session.Character.HasPremiumBattlePass)
            {
                bpQuests.AddRange(ServerManager.Instance.BattlePassQuests.Where(b => b.BpQuestType == questType && b.IsPremium == true));
            }

            foreach (var quest in bpQuests.OrderBy(b => b.BpQuestId))
            {
                var questProgress = BattlePassQuestProgresses.FirstOrDefault(q => q.BpQuestId == quest.BpQuestId);

                if (questProgress == null)
                {
                    BattlePassQuestProgresses.Add(new BattlePassQuestProgressDTO { AccountId = AccountId, Amount = value, BpQuestId = quest.BpQuestId, Completed = false });
                    break;
                }
                else if (questProgress.Completed == false && questProgress.Amount < quest.Amount)
                {
                    questProgress.Amount = questProgress.Amount + value <= quest.Amount ? questProgress.Amount + value : quest.Amount;
                    break;
                }
            }
            Session.SendPacket(GenerateBpQuest());
            Session.SendPacket(GenerateBp2Quest());
        }

        public string GenerateBppPacket()
        {
            if (!GameConfiguration.BattlePassEnabled)
            {
                return null;
            }

            if (ServerManager.Instance.ChannelId == 51)
            {
                return null;
            }

            string packet = $"bpp {ServerManager.Instance.BattlePassPrizes.OrderBy(b => b.Level).LastOrDefault().Level} {Session.Character.BattlePassPoints} {(Session.Character.HasPremiumBattlePass ? 1 : 0)}";

            foreach (var prize in ServerManager.Instance.BattlePassPrizes.OrderBy(b => b.Level))
            {
                byte prizeState = 0;
                byte premiumPrizeStat = 0;

                if (prize.Level * 50 <= Session.Character.BattlePassPoints)
                {
                    var bpLog = BattlePassAccountLogs.Where(b => b.AccountId == AccountId && b.Level == prize.Level);

                    if (!bpLog.Any())
                    {
                        prizeState = 1;
                        premiumPrizeStat = (byte)(Session.Character.HasPremiumBattlePass ? 1 : 0);
                    }
                    else
                    {
                        foreach (var bp in bpLog)
                        {
                            if (!bp.IsPremium)
                            {
                                prizeState = 2;
                            }
                            else
                            {
                                premiumPrizeStat = (byte)(Session.Character.HasPremiumBattlePass ? 2 : 0);
                            }
                        }
                    }
                }

                packet += $" {prize.Level} {prize.ItemVNum} {prize.Amount} {prize.ItemVNumPremium} {prize.AmountPremium} {prizeState} {premiumPrizeStat} {(prize.IsSpecial ? 1 : 0)}";
            }
            return packet;
        }

        public string GenerateBpQuest()
        {
            if (!GameConfiguration.BattlePassEnabled)
            {
                return null;
            }

            if (ServerManager.Instance.ChannelId == 51)
            {
                return null;
            }

            string bpQuest = $"bpm 47 4 {GameConfiguration.MaxBattlePassPoints} 01031200 01040900";

            bool isPremium = Session.Character.HasPremiumBattlePass;

            IEnumerable<BattlePassQuestProgressDTO> completedBpQuest = BattlePassQuestProgresses.Where(x => x.Completed);

            IEnumerable<BattlePassQuestDTO> characterBpQuest = ServerManager.Instance.BattlePassQuests.Where(x => !completedBpQuest.Any(y => y.BpQuestId == x.BpQuestId));

            List<BpQuestType> addedQuestType = new();

            foreach (var battlePass in characterBpQuest.OrderBy(x => x.BpQuestId))
            {
                if (battlePass == null)
                {
                    continue;
                }

                if (addedQuestType.Contains(battlePass.BpQuestType))
                {
                    continue;
                }

                addedQuestType.Add(battlePass.BpQuestType);

                string time = isPremium switch
                {
                    true => battlePass.BpTimeType == BpTimeType.Daily ? ServerManager.Instance.DailyBpTime.ToString()
                            : battlePass.BpTimeType == BpTimeType.Weekly ? ServerManager.Instance.WeeklyBpTime.ToString() : ServerManager.Instance.SeasonBpTime.ToString(),
                    false when battlePass.IsPremium => "-997",
                    false when !battlePass.IsPremium => battlePass.BpTimeType == BpTimeType.Daily ? ServerManager.Instance.DailyBpTime.ToString()
                            : battlePass.BpTimeType == BpTimeType.Weekly ? ServerManager.Instance.WeeklyBpTime.ToString() : ServerManager.Instance.SeasonBpTime.ToString()
                };

                var bpQuestProgress = BattlePassQuestProgresses.FirstOrDefault(q => q.BpQuestId == battlePass.BpQuestId);

                int amount = bpQuestProgress?.Amount ?? 0;

                bpQuest += $" {battlePass.BpQuestId} {(byte)battlePass.BpQuestType} {(byte)battlePass.BpTimeType} {amount} {battlePass.Amount} 0 {battlePass.Points}";
            }

            foreach (var completedQuest in completedBpQuest)
            {
                var quest = ServerManager.Instance.BattlePassQuests.FirstOrDefault(x => x.BpQuestId == completedQuest.BpQuestId);

                bpQuest += $" {completedQuest.BpQuestId} {(byte)quest.BpQuestType} {(byte)quest.BpTimeType} -1 {quest.Amount} 0 {quest.Points} 0";
            }

            return bpQuest;
        }

        public string GenerateBp2Quest()
        {
            string bpQuest = "bpm2 70 10 1 0 11 0 50 -997";

            return bpQuest;
        }

        public string GenerateBptPacket()
        {
            if (!GameConfiguration.BattlePassEnabled)
            {
                return null;
            }

            if (ServerManager.Instance.ChannelId == 51)
            {
                return null;
            }

            string bptPacket = "bpt " + ServerManager.Instance.SeasonBpTime.ToString();
            bool isPremium = Session.Character.HasPremiumBattlePass;
            foreach (var battlePass in ServerManager.Instance.BattlePassQuests.OrderBy(q => q.BpQuestId))
            {
                string time = isPremium switch
                {
                    true => battlePass.BpTimeType == BpTimeType.Daily ? ServerManager.Instance.DailyBpTime.ToString() : battlePass.BpTimeType == BpTimeType.Weekly ? ServerManager.Instance.WeeklyBpTime.ToString() : ServerManager.Instance.SeasonBpTime.ToString(),
                    false when battlePass.IsPremium => "-997",
                    false when !battlePass.IsPremium => battlePass.BpTimeType == BpTimeType.Daily ? ServerManager.Instance.DailyBpTime.ToString() : battlePass.BpTimeType == BpTimeType.Weekly ? ServerManager.Instance.WeeklyBpTime.ToString() : ServerManager.Instance.SeasonBpTime.ToString()
                };

                bptPacket += $" {time}";
            }

            return bptPacket;
        }

        public void LoadBattlePass()
        {
            if (!GameConfiguration.BattlePassEnabled)
            {
                return;
            }

            if (ServerManager.Instance.ChannelId == 51)
            {
                return;
            }

            BattlePassAccountLogs = DAOFactory.BattlePassAccountLogDAO.LoadAllById(AccountId).ToList();
            BattlePassQuestProgresses = DAOFactory.BattlePassQuestProgressDAO.LoadByAccountId(AccountId).ToList();
        }

        public void BattlePassQuestReset(bool force = false)
        {
            if (!GameConfiguration.BattlePassEnabled)
            {
                return;
            }

            if (ServerManager.Instance.ChannelId == 51)
            {
                return;
            }

            var bpQuestLog = DAOFactory.GeneralLogDAO.LoadByAccount(AccountId).LastOrDefault(x => x.Timestamp.Date == DateTime.Now.Date && x.LogData == "BP_RESET");

            if (!force && bpQuestLog != null)
            {
                return;
            }

            var questProgress = BattlePassQuestProgresses;

            if (!questProgress.Any())
            {
                return;
            }

            int days = (ServerManager.Instance.SeasonBpDate.Date - DateTime.Now.Date).Days;

            bool weeklyReset = days % 7 == 0;
            bool seasonReset = days % 35 == 0;

            for (int i = questProgress.Count - 1; i >= 0; i--)
            {
                BattlePassQuestDTO quest = ServerManager.Instance.BattlePassQuests.FirstOrDefault(x => x.BpQuestId == questProgress[i].BpQuestId);

                if (quest == null)
                {
                    continue;
                }

                if (quest.BpTimeType == BpTimeType.Daily && !force)
                {
                    DAOFactory.BattlePassQuestProgressDAO.Delete(questProgress[i].BpQuestProgressId);
                    BattlePassQuestProgresses.Remove(questProgress[i]);
                    continue;
                }

                if (quest.BpTimeType == BpTimeType.Weekly && weeklyReset && !force)
                {
                    DAOFactory.BattlePassQuestProgressDAO.Delete(questProgress[i].BpQuestProgressId);
                    BattlePassQuestProgresses.Remove(questProgress[i]);
                    continue;
                }

                if (quest.BpTimeType == BpTimeType.Seasonal && seasonReset || force)
                {
                    DAOFactory.BattlePassQuestProgressDAO.Delete(questProgress[i].BpQuestProgressId);
                    BattlePassQuestProgresses.Remove(questProgress[i]);
                }
            }

            if (seasonReset || force)
            {
                Session.Character.HasPremiumBattlePass = false;
                Session.Character.BattlePassPoints = 0;
            }

            Session.SendPacket(GenerateBpQuest());
            Session.SendPacket(GenerateBp2Quest());
            Session.SendPacket(GenerateBptPacket());
            Session.SendPacket(GenerateBppPacket());


            DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
            {
                AccountId = AccountId,
                CharacterId = CharacterId,
                Timestamp = DateTime.Now,
                LogData = "BP_RESET"
            });

            
        }

       

        public void DailyBattlePassRefresh()
        {
            if (!GameConfiguration.BattlePassEnabled)
            {
                return;
            }

            if (ServerManager.Instance.ChannelId == 51)
            {
                return;
            }

            var dailyLogin = DAOFactory.GeneralLogDAO.LoadByAccount(Session.Account.AccountId).LastOrDefault(x => x.LogData == "DAILY_LOG");

            if (dailyLogin == null)
            {
                DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
                {
                    AccountId = AccountId,
                    CharacterId = CharacterId,
                    Timestamp = DateTime.Now,
                    LogData = "DAILY_LOG"
                });
                IncreaseBpQuest(BpQuestType.LoginRow);
            }
            else
            {
                if (dailyLogin.Timestamp.Date == DateTime.Now.Date)
                {
                    return;
                }

                DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
                {
                    AccountId = AccountId,
                    CharacterId = CharacterId,
                    Timestamp = DateTime.Now,
                    LogData = "DAILY_LOG"
                });
                IncreaseBpQuest(BpQuestType.LoginRow);
            }
        }

        #endregion BattlePass

        public void SaveBattlePass()
        {
            DAOFactory.BattlePassAccountLogDAO.InsertOrUpdateFromList(BattlePassAccountLogs);

            DAOFactory.BattlePassQuestProgressDAO.InsertOrUpdateFromList(BattlePassQuestProgresses);
        }

        public void GenerateReputation(MapMonster monsterToAttack)
        {
            var reputationPerKill = monsterToAttack.Monster.Level;
            if (Group?.GroupType == GroupType.Group)
            {
                foreach (ClientSession targetSession in Group.Sessions.Where(s =>
                    s.Character.MapInstanceId == MapInstanceId))
                {
                    targetSession.Character.GetReputation(reputationPerKill / GameConfiguration.ReputationDevidedByInGroup);
                }
            }
            else
            {
                GetReputation(reputationPerKill / GameConfiguration.ReputationDevidedBy);
            }
        }

        public string GenerateLev()
        {
            ItemInstance specialist = null;
            if (Inventory != null)
            {
                specialist = Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            }

            return
                $"lev {Level} {(int)(Level < 100 ? LevelXp : LevelXp / 100)} {(!UseSp || specialist == null ? JobLevel : specialist.SpLevel)} {(!UseSp || specialist == null ? JobLevelXp : specialist.XP)} {(int)(Level < 100 ? XpLoad() : XpLoad() / 100)} {(!UseSp || specialist == null ? JobXPLoad() : SpXpLoad())} {Reputation} {GetCP()} {(int)(HeroLevel < 100 ? HeroXp : HeroXp / 100)} {HeroLevel} {(int)(HeroLevel < 100 ? HeroXPLoad() : HeroXPLoad() / 100)} 0";
        }

        public string GenerateLevelUp()
        {
            //LOGGER
            ////LOGGER($"[LEVEL] LevelUp | {Session.GenerateIdentity()} | Level: {Level} | JobLevel: {JobLevel} | SPLevel: {Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear)?.SpLevel} | HeroLevel: {HeroLevel}");
            return $"levelup {CharacterId}";
        }

        public void GenerateMiniland()
        {
            if (Miniland == null)
            {
                Miniland = ServerManager.GenerateMapInstance(20001, MapInstanceType.NormalInstance, new InstanceBag(),
                    true);
                foreach (MinilandObjectDTO obj in DAOFactory.MinilandObjectDAO.LoadByCharacterId(CharacterId))
                {
                    MinilandObject mapobj = new MinilandObject(obj);
                    if (mapobj.ItemInstanceId != null)
                    {
                        ItemInstance item = Inventory.GetItemInstanceById((Guid)mapobj.ItemInstanceId);
                        if (item != null)
                        {
                            mapobj.ItemInstance = item;
                            MinilandObjects.Add(mapobj);
                        }
                    }
                }
            }
        }

        public string GenerateMinilandObjectForFriends()
        {
            string mlobjstring = "mltobj";
            int i = 0;
            foreach (MinilandObject mp in MinilandObjects)
            {
                mlobjstring += $" {mp.ItemInstance.ItemVNum}.{i}.{mp.MapX}.{mp.MapY}";
                i++;
            }

            return mlobjstring;
        }

        public string GenerateMinilandPoint() => $"mlpt {MinilandPoint} 100";

        public string GenerateMinimapPosition() => MapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance
                                                   || MapInstance.MapInstanceType == MapInstanceType.RaidInstance
            ? $"rsfp {MapInstance.MapIndexX} {MapInstance.MapIndexY}"
            : "rsfp 0 -1";

        public string GenerateMlinfo() =>
            $"mlinfo 3800 {MinilandPoint} 100 0 0 10 {(byte)MinilandState} {Language.Instance.GetMessageFromKey("WELCOME_MUSIC_INFO")} {MinilandMessage.Replace(' ', '^')}";

        public string GenerateMlinfobr() =>
            $"mlinfobr 3800 {Name} 0 0 25 {MinilandMessage.Replace(' ', '^')}";

        public string GenerateMloMg(MinilandObject mlobj, MinigamePacket packet) =>
            $"mlo_mg {packet.MinigameVNum} {MinilandPoint} 0 0 {mlobj.ItemInstance.DurabilityPoint} {mlobj.ItemInstance.Item.MinilandObjectPoint}";

        public string GenerateNpcDialog(int value) => $"npc_req 1 {CharacterId} {value}";

        public string GeneratePairy()
        {
            ItemInstance fairy = null;
            if (Inventory != null)
            {
                fairy = Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            }
            ElementRate = 0;
            Element = 0;
            bool shouldChangeMorph = false;

            if (fairy != null)
            {
                shouldChangeMorph = IsUsingFairyBooster && (fairy.Item.Morph > 4 && fairy.Item.Morph != 9 && fairy.Item.Morph != 14);
                ElementRate += fairy.ElementRate + (IsUsingFairyBooster ? 30 : 0) + GetStuffBuff(CardType.PixieCostumeWings, (byte)AdditionalTypes.PixieCostumeWings.IncreaseFairyElement)[0];
                Element = fairy.Item.Element;
            }

            return fairy != null
                ? $"pairy 1 {CharacterId} 4 {fairy.Item.Element} {fairy.ElementRate} {fairy.Item.Morph + (shouldChangeMorph ? 5 : 0)}"
                : $"pairy 1 {CharacterId} 0 0 0 0";
        }

        public string GenerateParcel(MailDTO mail) => mail.AttachmentVNum != null
            ? $"parcel 1 1 {MailList.First(s => s.Value.MailId == mail.MailId).Key} {(mail.Title == "NOSTALE" ? 1 : 4)} 0 {mail.Date.ToString("yyMMddHHmm")} {mail.Title} {mail.AttachmentVNum} {mail.AttachmentAmount} {(byte)ServerManager.GetItem((short)mail.AttachmentVNum).Type}"
            : "";

        public string GeneratePetskill(int VNum = -1, int VNum2 = -1, int VNum3 = -1) => $"petski {VNum} {VNum2} {VNum3}";

        public string GenerateSMemo(int type, string msg)
        {
            return $"s_memo {type} {msg}";
        }

        public string GeneratePidx(bool isLeaveGroup = false)
        {
            if (!isLeaveGroup && Group != null)
            {
                string result = $"pidx {Group.GroupId}";
                foreach (ClientSession session in Group.Sessions.GetAllItems()
                    .Where(s => s.Character.CharacterId != CharacterId))
                {
                    if (session.Character != null)
                    {
                        result += $" {(Group.IsMemberOfGroup(CharacterId) ? 1 : 0)}.{session.Character.CharacterId} ";
                    }
                }

                foreach (ClientSession session in Group.Sessions.GetAllItems()
                    .Where(s => s.Character.CharacterId == CharacterId))
                {
                    if (session.Character != null)
                    {
                        result += $" {(Group.IsMemberOfGroup(CharacterId) ? 1 : 0)}.{session.Character.CharacterId} ";
                    }
                }

                return result;
            }

            return $"pidx -1 1.{CharacterId}";
        }

        public string GeneratePinit()
        {
            Group grp = ServerManager.Instance.Groups.Find(s =>
                s.IsMemberOfGroup(CharacterId) && s.GroupType == GroupType.Group);

            List<Mate> mates = Mates.ToList();

            int count = 0;

            string str = "";

            if (mates != null)
            {
                foreach (Mate mate in mates.Where(s => s.IsTeamMember).OrderByDescending(s => s.MateType))
                {
                    if ((byte)mate.MateType == 1)
                    {
                        count++;
                    }

                    str +=
                        $" 2|{mate.MateTransportId}|{(mate.MateType == MateType.Pet ? "1" : "0")}|{mate.Level}|{(mate.IsUsingSp ? mate.Sp.GetName() : mate.Name.Replace(' ', '^'))}|-1|{(mate.IsUsingSp && mate.Sp != null ? mate.Sp.Instance.Item.Morph : mate.Monster.NpcMonsterVNum)}|0|0|1";
                }
            }

            if (grp != null)
            {
                foreach (ClientSession groupSessionForId in grp.Sessions.GetAllItems()
                    .Where(s => s.Character.CharacterId != CharacterId))
                {
                    count++;
                    str +=
                        $" 1|{groupSessionForId.Character.CharacterId}|{count}|{groupSessionForId.Character.Level}|{groupSessionForId.Character.Name}|0|{(byte)groupSessionForId.Character.Gender}|{(byte)groupSessionForId.Character.Class}|{(groupSessionForId.Character.UseSp || groupSessionForId.Character.IsVehicled || groupSessionForId.Character.IsMorphed ? groupSessionForId.Character.Morph : 0)}|{groupSessionForId.Character.HeroLevel}";
                }

                foreach (ClientSession groupSessionForId in grp.Sessions.GetAllItems()
                    .Where(s => s.Character.CharacterId == CharacterId))
                {
                    count++;
                    str +=
                        $" 1|{groupSessionForId.Character.CharacterId}|{count}|{groupSessionForId.Character.Level}|{groupSessionForId.Character.Name}|0|{(byte)groupSessionForId.Character.Gender}|{(byte)groupSessionForId.Character.Class}|{(groupSessionForId.Character.UseSp || groupSessionForId.Character.IsVehicled || groupSessionForId.Character.IsMorphed ? groupSessionForId.Character.Morph : 0)}|{groupSessionForId.Character.HeroLevel}";
                }
            }

            return $"pinit {(grp != null ? count : mates.Count(s => s.IsTeamMember))}{str}";
        }

        public string GeneratePlayerFlag(long pflag) => $"pflag 1 {CharacterId} {pflag}";

        public string GeneratePost(MailDTO mail, byte type)
        {
            if (mail != null)
            {
                return
                    $"post 1 {type} {(MailList?.FirstOrDefault(s => s.Value?.MailId == mail?.MailId))?.Key} 0 {(mail.IsOpened ? 1 : 0)} {mail.Date.ToString("yyMMddHHmm")} {(type == 2 ? DAOFactory.CharacterDAO.LoadById(mail.ReceiverId).Name : DAOFactory.CharacterDAO.LoadById(mail.SenderId).Name)} {mail.Title}";
            }

            return "";
        }

        public string GeneratePostMessage(MailDTO mailDTO, byte type)
        {
            CharacterDTO sender = DAOFactory.CharacterDAO.LoadById(mailDTO.SenderId);

            return
                $"post 5 {type} {MailList.First(s => s.Value == mailDTO).Key} 0 0 {(byte)mailDTO.SenderClass} {(byte)mailDTO.SenderGender} {mailDTO.SenderMorphId} {(byte)mailDTO.SenderHairStyle} {(byte)mailDTO.SenderHairColor} {mailDTO.EqPacket} {sender.Name} {mailDTO.Title} {mailDTO.Message}";
        }

        public List<string> GeneratePst() => Mates.Where(s => s.IsTeamMember).OrderByDescending(s => s.MateType).Select(
                mate =>
                    $"pst 2 {mate.MateTransportId} {(mate.MateType == MateType.Partner ? "0" : "1")} {(int)(mate.Hp / mate.MaxHp * 100)} {(int)(mate.Mp / mate.MaxMp * 100)} {mate.Hp} {mate.Mp} 0 0 0 {mate.Buff.GetAllItems().Aggregate("", (current, buff) => current + $" {buff.Card.CardId}")}").ToList();

        public string GeneratePStashAll()
        {
            string stash =
                $"pstash_all {(StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.PetBackPack) ? 50 : 0)}";
            return Inventory.Where(s => s.Type == InventoryType.PetWarehouse).Aggregate(stash,
                (current, item) => current + $" {item.GenerateStashPacket()}");
        }

        public string GenerateQuestsPacket(long newQuestId = -1)
        {
            short a = 0;
            short b = 6;
            Quests.ToList().ForEach(qst =>
            {
                qst.QuestNumber = qst.IsMainQuest
                    ? (short)5
                    : (!qst.IsMainQuest && !qst.Quest.IsDaily || qst.Quest.QuestId >= 5000 ? b++ : a++);
            });
            return
                $"qstlist {Quests.Aggregate("", (current, quest) => current + $" {quest.GetInfoPacket(quest.QuestId == newQuestId)}")}";
        }

        public IEnumerable<string> GenerateQuicklist()
        {
            string[] pktQs = { "qslot 0", "qslot 1" };
            var morph = Morph;
            if (Class == ClassType.MartialArtist && Morph == 29 || Morph == 30)
            {
                morph = 30;
            }


            switch (Class)
            {
                case ClassType.MartialArtist when Morph == 31 && UseSp && SpInstance != null &&
                                                  SpInstance.SpLevel >= 20 && HasBuff(CardType.LotusSkills,
                                                      (byte)AdditionalTypes.LotusSkills.ChangeLotusSkills):
                    GenerateQuickListSp2Am(ref pktQs);
                    break;

                case ClassType.MartialArtist when Morph == 33 && UseSp && SpInstance != null &&
                                                  SpInstance.SpLevel >= 20 && HasBuff(CardType.WolfMaster,
                                                      (byte)AdditionalTypes.WolfMaster.CanExecuteUltimateSkills):
                    GenerateQuickListSp3Am(ref pktQs);
                    break;

                default:
                    {
                        for (var i = 0; i < 30; i++)
                        {
                            for (var j = 0; j < 2; j++)
                            {
                                QuicklistEntryDTO qi = QuicklistEntries.Find(n =>
                                    n.Q1 == j && n.Q2 == i && n.Morph == (UseSp ? SpInstance.Item.Morph : 0));
                                pktQs[j] += $" {qi?.Type ?? 7}.{qi?.Slot ?? 7}.{qi?.Pos.ToString() ?? "-1.-1"}";
                            }
                        }

                        break;
                    }
            }

            return pktQs;
        }

        public string GenerateRaid(int Type, bool exit = false)
        {
            string result = "";
            switch (Type)
            {
                case 0:
                    result = "raid 0";
                    Group?.Sessions?.ForEach(s => result += $" {s.Character?.CharacterId}");
                    break;

                case 2:
                    result = $"raid 2 {(exit ? "-1" : $"{Group?.Sessions?.FirstOrDefault().Character.CharacterId}")}";
                    break;

                case 1:
                    result = $"raid 1 {(exit ? 0 : 1)}";
                    break;

                case 3:
                    result = "raid 3";
                    Group?.Sessions?.ForEach(s =>
                        result +=
                            $" {s.Character?.CharacterId}.{Math.Ceiling(s.Character.Hp / s.Character.HPLoad() * 100)}.{Math.Ceiling(s.Character.Mp / s.Character.MPLoad() * 100)}");
                    break;

                case 4:
                    result = "raid 4";
                    break;

                case 5:
                    result = "raid 5 1";
                    break;
            }

            return result;
        }

        public string GenerateRc(int characterHealth) => BattleEntity.GenerateRc(characterHealth);

        public string GenerateReqInfo()
        {
            ItemInstance fairy = null;
            ItemInstance armor = null;
            ItemInstance weapon2 = null;
            ItemInstance weapon = null;

            if (Inventory != null)
            {
                fairy = Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
                armor = Inventory.LoadBySlotAndType((byte)EquipmentType.Armor, InventoryType.Wear);
                weapon2 = Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
                weapon = Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
            }

            bool isPvpPrimary = false;
            bool isPvpSecondary = false;
            bool isPvpArmor = false;

            if (weapon != null && !string.IsNullOrEmpty(weapon.Item.Name) && weapon.Item.Name.Contains(": "))
            {
                isPvpPrimary = true;
            }

            isPvpSecondary |= weapon2?.Item.Name.Contains(": ") == true;
            isPvpArmor |= armor?.Item.Name.Contains(": ") == true;

            string biography = string.IsNullOrWhiteSpace(Biography)
                ? Language.Instance.GetMessageFromKey("NO_PREZ_MESSAGE")
                : Biography.Replace('\r', ' ').Replace('\n', ' ');

            return $"tc_info {Level} {Name} {fairy?.Item.Element ?? 0} {ElementRate} {(byte)Class} " +
                   $"{(byte)Gender} {(Family != null ? $"{Family.FamilyId}.{CharacterExtension.GetFamilyNameType(Session)} {Family.Name}" : "-1 -")} " +
                   $"{GetReputationIco()} {GetDignityIco()} {(weapon != null ? 1 : 0)} {weapon?.Rare ?? 0} {weapon?.Upgrade ?? 0} {(weapon2 != null ? 1 : 0)} " +
                   $"{weapon2?.Rare ?? 0} {weapon2?.Upgrade ?? 0} " +
                   $"{(armor != null ? 1 : 0)} " +
                   $"{armor?.Rare ?? 0} " +
                   $"{armor?.Upgrade ?? 0} " +
                   $"{Act4Kill} {Act4Dead} " +
                   $"{Reputation} 0 0 0 {(UseSp ? Morph : -1)} {TalentWin} {TalentLose} {TalentSurrender} 0 {MasterPoints} {Compliment} {Act4Points} " +
                   $"{(isPvpPrimary ? 1 : 0)} {(isPvpSecondary ? 1 : 0)} {(isPvpArmor ? 1 : 0)} {HeroLevel} {(fairy != null ? fairy.FairyLevel : 0)} " +
                   biography;
        }

        public string GenerateRest() => $"rest 1 {CharacterId} {(IsSitting ? 1 : 0)}";



        public string GenerateRevive()
        {
            int lives = MapInstance.InstanceBag.Lives - MapInstance.InstanceBag.DeadList.Count + 1;
            if (MapInstance.MapInstanceType == MapInstanceType.TimeSpaceInstance)
            {
                lives = MapInstance.InstanceBag.Lives -
                    MapInstance.InstanceBag.DeadList.ToList().Count(s => s == CharacterId) + 1;
            }

            return $"revive 1 {CharacterId} {(lives > 0 ? lives : 0)}";
        }

        public string GenerateSay(string message, int type, bool ignoreNickname = false) => $"say {(ignoreNickname ? 2 : 1)} {CharacterId} {type} {message}";

        public string GenerateSayi(int type, long characterId, GameConstString gameConst, SayColorType sayColorType, short firstArgument = 0, short secondArgument = 0, short thirdArgument = 0, short fourthArgument = 0)
           => $"sayi {type} {characterId} {sayColorType} {gameConst} {firstArgument} {secondArgument} {thirdArgument} {fourthArgument}";

        //public string GenerateSayi2(EntityType type, long id, SayColorType sayColorType, GameConstString gameConstString, string firstArgument = null, string secondArgument = null)
        // => $"sayi2 {type} {id} {sayColorType} {gameConstString} 99 {firstArgument} {secondArgument}";

        public string GenerateSayItem(string message, int type, byte itemInventory, short itemSlot,
            bool ignoreNickname = false)
        {
            if (Inventory.LoadBySlotAndType(itemSlot, (InventoryType)itemInventory) is ItemInstance item)
            {
                return
                    $"sayitem {(ignoreNickname ? 2 : 1)} {CharacterId} {type} {message.Replace(' ', '|')} {(item.Item.EquipmentSlot == EquipmentType.Sp ? item.GenerateSlInfo() : item.GenerateEInfo())}";
            }

            return "";
        }

        public string GenerateScal()
        {
            string packet = $"char_sc 1 {CharacterId} {Size}";

            Logger.Info($"CHAR_SC: {packet}");

            return packet;
        }

        public List<string> GenerateScN()
        {
            List<string> list = new List<string>();
            byte i = 0;
            var partners = Mates.Where(s => s.MateType == MateType.Partner).ToList();

            foreach (var partner in partners)
            {
                partner.PetId = i;
                partner.LoadInventory();
                list.Add(partner.GenerateScPacket());
                i++;
            }

            return list;
        }

        public List<string> GenerateScP(byte page = 0)
        {
            List<string> list = new List<string>();

            byte i = 0;

            Mates.Where(s => s.MateType == MateType.Pet).Skip(page * 10).Take(10).ToList().ForEach(s =>
            {
                s.PetId = (byte)(page * 10 + i);
                list.Add(s.GenerateScPacket());
                i++;
            });

            return list;
        }

        public string GenerateScpStc() => $"sc_p_stc {(MaxMateCount - 10) / 10} {MaxPartnerCount - 3}";

        public string GenerateShop(string shopname) => $"shop 1 {CharacterId} 1 3 0 {shopname}";

        public string GenerateShopEnd() => $"shop 1 {CharacterId} 0 0";

        public string GenerateSki()
        {
            string ski = "ski 0";

            List<CharacterSkill> skills = GetSkills().OrderBy(s => s.Skill.CastId).OrderBy(s => s.SkillVNum < 200).ToList();

            if (skills.Count >= 2)
            {
                if (UseSp)
                {
                    ski += $" {skills[0].SkillVNum} {skills[0].SkillVNum}";
                }
                else
                {
                    ski += $" {skills[0].SkillVNum} {skills[1].SkillVNum}";
                }

                ski = skills.Aggregate(ski, (packet, characterSKill) => $"{packet} {(characterSKill.IsTattoo ? $"{characterSKill.SkillVNum}|{characterSKill.TattooLevel}" : $"{characterSKill.SkillVNum}")}");
            }

            return ski;
        }

        public string GenerateSpk(object message, int type) => $"spk 1 {CharacterId} {type} {Name} {message}";

        public string GenerateSpPoint() => $"sp {SpAdditionPoint} 1000000 {SpPoint} 10000";

        public void GenerateStartupInventory()
        {
            string inv0 = "inv 0",
                inv1 = "inv 1",
                inv2 = "inv 2",
                inv3 = "inv 3",
                inv6 = "inv 6",
                inv7 = "inv 7"; // inv 3 used for miniland objects
            if (Inventory != null)
            {
                foreach (ItemInstance inv in Inventory.GetAllItems())
                {
                    switch (inv.Type)
                    {
                        case InventoryType.Equipment:
                            if (inv.Item.EquipmentSlot == EquipmentType.Sp)
                            {
                                inv0 += $" {inv.Slot}.{inv.ItemVNum}.{inv.Rare}.{inv.Upgrade}.{inv.SpStoneUpgrade}.0";
                            }
                            else
                            {
                                inv0 +=
                                    $" {inv.Slot}.{inv.ItemVNum}.{inv.Rare}.{(inv.Item.IsColored ? inv.Design : inv.Upgrade)}.0.{inv.RuneAmount}";
                            }

                            break;

                        case InventoryType.Main:
                            inv1 += $" {inv.Slot}.{inv.ItemVNum}.{inv.Amount}.0";
                            break;

                        case InventoryType.Etc:
                            inv2 += $" {inv.Slot}.{inv.ItemVNum}.{inv.Amount}.0";
                            break;

                        case InventoryType.Miniland:
                            inv3 += $" {inv.Slot}.{inv.ItemVNum}.{inv.Amount}";
                            break;

                        case InventoryType.Specialist:
                            inv6 += $" {inv.Slot}.{inv.ItemVNum}.{inv.Rare}.{inv.Upgrade}.{inv.SpStoneUpgrade}";
                            break;

                        case InventoryType.Costume:
                            inv7 += $" {inv.Slot}.{inv.ItemVNum}.{inv.Rare}.{inv.Upgrade}.0";
                            break;
                    }
                }
            }

            Session.SendPacket(inv0);
            Session.SendPacket(inv1);
            Session.SendPacket(inv2);
            Session.SendPacket(inv3);
            Session.SendPacket(inv6);
            Session.SendPacket(inv7);
            Session.SendPacket(GetMinilandObjectList());
        }

        public string GenerateStashAll()
        {
            string stash = $"stash_all {WareHouseSize}";
            foreach (ItemInstance item in Inventory.Where(s => s.Type == InventoryType.Warehouse))
            {
                stash += $" {item.GenerateStashPacket()}";
            }

            return stash;
        }

        public int GetTitleEffectValue(CardType type, byte subtype)
        {
            return EffectFromTitle?.Where(x => x.Type == (byte)type && x.SubType == subtype)
                ?.Sum(x => x.FirstData) ?? 0;
        }

        public string GenerateStat()
        {
            double option =
                (WhisperBlocked ? Math.Pow(2, (int)CharacterOption.WhisperBlocked - 1) : 0)
                + (FamilyRequestBlocked ? Math.Pow(2, (int)CharacterOption.FamilyRequestBlocked - 1) : 0)
                + (!MouseAimLock ? Math.Pow(2, (int)CharacterOption.MouseAimLock - 1) : 0)
                + (MinilandInviteBlocked ? Math.Pow(2, (int)CharacterOption.MinilandInviteBlocked - 1) : 0)
                + (ExchangeBlocked ? Math.Pow(2, (int)CharacterOption.ExchangeBlocked - 1) : 0)
                + (FriendRequestBlocked ? Math.Pow(2, (int)CharacterOption.FriendRequestBlocked - 1) : 0)
                + (EmoticonsBlocked ? Math.Pow(2, (int)CharacterOption.EmoticonsBlocked - 1) : 0)
                + (HpBlocked ? Math.Pow(2, (int)CharacterOption.HpBlocked - 1) : 0)
                + (BuffBlocked ? Math.Pow(2, (int)CharacterOption.BuffBlocked - 1) : 0)
                + (GroupRequestBlocked ? Math.Pow(2, (int)CharacterOption.GroupRequestBlocked - 1) : 0)
                + (HeroChatBlocked ? Math.Pow(2, (int)CharacterOption.HeroChatBlocked - 1) : 0)
                + (QuickGetUp ? Math.Pow(2, (int)CharacterOption.QuickGetUp - 1) : 0)
                + (HideHat ? Math.Pow(2, (int)CharacterOption.HideHat - 1) : 0)
                + (UiBlocked ? Math.Pow(2, (int)CharacterOption.UiBlocked - 1) : 0)
                + (!IsPetAutoRelive ? 64 : 0)
                + (!IsPartnerAutoRelive ? 128 : 0);
            return $"stat {Hp} {HPLoad()} {Mp} {MPLoad()} 0 {option}";
        }

        public List<string> GenerateStatChar()
        {
            int weaponUpgrade = 0;
            int secondaryUpgrade = 0;
            int armorUpgrade = 0;
            MinHit = (int)CharacterHelper.MinHit(Class, Level);
            MaxHit = (int)CharacterHelper.MaxHit(Class, Level);
            HitRate = (int)CharacterHelper.HitRate(Class, Level);
            HitCriticalChance = CharacterHelper.HitCriticalRate(Class, Level);
            HitCriticalRate = CharacterHelper.HitCritical(Class, Level);
            SecondWeaponMinHit = (int)CharacterHelper.MinDistance(Class, Level);
            SecondWeaponMaxHit = (int)CharacterHelper.MaxDistance(Class, Level);
            SecondWeaponHitRate = (int)CharacterHelper.DistanceRate(Class, Level);
            SecondWeaponCriticalChance = CharacterHelper.DistCriticalRate(Class, Level);
            SecondWeaponCriticalRate = CharacterHelper.DistCritical(Class, Level);
            FireResistance = 0;
            LightResistance = 0;
            WaterResistance = 0;
            DarkResistance = 0;
            Defence = (int)CharacterHelper.Defence(Class, Level);
            DefenceRate = (int)CharacterHelper.DefenceRate(Class, Level);
            ElementRate = 0;
            ElementRateSP = 0;
            DistanceDefence = (int)CharacterHelper.DistanceDefence(Class, Level);
            DistanceDefenceRate = (int)CharacterHelper.DistanceDefenceRate(Class, Level);
            MagicalDefence = (int)CharacterHelper.MagicalDefence(Class, Level);
            if (UseSp)
            {
                // handle specialist
                ItemInstance specialist = Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                if (specialist != null)
                {
                    MinHit += specialist.DamageMinimum + (specialist.SpDamage * 13);
                    MaxHit += specialist.DamageMaximum + (specialist.SpDamage * 13);
                    SecondWeaponMinHit += specialist.DamageMinimum + (specialist.SpDamage * 13);
                    SecondWeaponMaxHit += specialist.DamageMaximum + (specialist.SpDamage * 13);
                    HitCriticalChance += specialist.CriticalLuckRate;
                    HitCriticalRate += specialist.CriticalRate;
                    SecondWeaponCriticalChance += specialist.CriticalLuckRate;
                    SecondWeaponCriticalRate += specialist.CriticalRate;
                    HitRate += specialist.HitRate;
                    SecondWeaponHitRate += specialist.HitRate;
                    DefenceRate += specialist.DefenceDodge;
                    DistanceDefenceRate += specialist.DistanceDefenceDodge;
                    FireResistance += specialist.Item.FireResistance + specialist.SpFire;
                    WaterResistance += specialist.Item.WaterResistance + specialist.SpWater;
                    LightResistance += specialist.Item.LightResistance + specialist.SpLight;
                    DarkResistance += specialist.Item.DarkResistance + specialist.SpDark;
                    ElementRateSP += specialist.ElementRate + specialist.SpElement * 5;
                    Defence += specialist.CloseDefence + (specialist.SpDefence * 10);
                    DistanceDefence += specialist.DistanceDefence + (specialist.SpDefence * 10);
                    MagicalDefence += specialist.MagicDefence + (specialist.SpDefence * 10);

                    ItemInstance mainWeapon =
                        Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
                    ItemInstance secondaryWeapon =
                        Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
                    List<ShellEffectDTO> effects = new List<ShellEffectDTO>();
                    if (mainWeapon?.ShellEffects != null)
                    {
                        effects.AddRange(mainWeapon.ShellEffects);
                    }

                    if (secondaryWeapon?.ShellEffects != null)
                    {
                        effects.AddRange(secondaryWeapon.ShellEffects);
                    }

                    int GetShellWeaponEffectValue(ShellWeaponEffectType effectType)
                    {
                        return effects?.Where(s => s.Effect == (byte)effectType)?.OrderByDescending(s => s.Value)
                            ?.FirstOrDefault()?.Value ?? 0;
                    }


                    int point = CharacterHelper.SlPoint(specialist.SlDamage, 0)
                                + GetShellWeaponEffectValue(ShellWeaponEffectType.SLDamage)
                                + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal)
                                + GetTitleEffectValue(CardType.IncreaseSlPoint,
                                    (byte)AdditionalTypes.IncreaseSlPoint.IncreaseDamage);

                    if (point > 120) { point = 120; };

                    int p = 0;
                    int cc = 0;
                    int cr = 0;
                    int hp = 0;
                    int mana = 0;
                    if (point <= 10)
                    {
                        p = 5 + point * 5;
                    }
                    else if (point <= 20)
                    {
                        p = 50 + ((point - 10) * 6);
                        cc = 2;
                    }
                    else if (point <= 30)
                    {
                        p = 110 + ((point - 20) * 7);
                        cc = 2;
                    }
                    else if (point <= 40)
                    {
                        p = 190 + ((point - 30) * 8);
                        cc = 2;
                    }
                    else if (point <= 50)
                    {
                        p = 270 + ((point - 40) * 9);
                        cr = 10;
                        cc = 2;
                    }
                    else if (point <= 60)
                    {
                        p = 360 + ((point - 50) * 10);
                        cr = 10;
                        cc = 2;
                        hp = 200;
                        mana = 200;
                    }
                    else if (point <= 70)
                    {
                        p = 460 + ((point - 60) * 11);
                        cr = 10;
                        cc = 2;
                        hp = 200;
                        mana = 200;
                    }
                    else if (point <= 80)
                    {
                        p = 575 + ((point - 70) * 13);
                        cr = 10;
                        cc = 2;
                        hp = 200;
                        mana = 200;
                    }
                    else if (point <= 90)
                    {
                        p = 705 + ((point - 80) * 14);
                        cr = 10;
                        cc = 5;
                        hp = 200;
                        mana = 200;
                    }
                    else if (point <= 94)
                    {
                        p = 845 + ((point - 90) * 15);
                        cr = 30;
                        cc = 5;
                        hp = 200;
                        mana = 200;
                    }
                    else if (point <= 95)
                    {
                        p = 905 + 16;
                        cr = 30;
                        cc = 5;
                        hp = 200;
                        mana = 200;
                    }
                    else if (point <= 97)
                    {
                        p = 921 + ((point - 95) * 17);
                        cr = 30;
                        cc = 5;
                        hp = 200;
                        mana = 200;
                    }
                    else if (point > 97 && point <= 100)
                    {
                        p = 955 + ((point - 97) * 20);
                        cr = 50;
                        cc = 8;
                        hp = 400;
                        mana = 400;
                    }
                    else if (point > 100 && point < 110)
                    {
                        p = 1020 + ((point - 100) * 15);
                        cr = 50;
                        cc = 8;
                        hp = 400;
                        mana = 400;
                    }
                    else if (point >= 110 && point < 120)
                    {
                        p = 1180 + ((point - 110) * 20);
                        cr = 80;
                        cc = 8;
                        hp = 400;
                        mana = 400;
                    }
                    else if (point >= 120)
                    {
                        p = 1390;
                        cr = 90;
                        cc = 10;
                        hp = 600;
                        mana = 600;
                    }



                    MaxHit += p;
                    MinHit += p;
                    SecondWeaponMaxHit += p;
                    SecondWeaponMinHit += p;
                    HitCriticalChance += cc;
                    HitCriticalRate += cr;
                    SecondWeaponCriticalRate += cr;
                    SecondWeaponCriticalChance += cc;
                    Hp += hp;
                    Mp += mana;

                    point = CharacterHelper.SlPoint(specialist.SlDefence, 1)
                            + GetShellWeaponEffectValue(ShellWeaponEffectType.SLDefence)
                            + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal)
                            + GetTitleEffectValue(CardType.IncreaseSlPoint,
                                (byte)AdditionalTypes.IncreaseSlPoint.IncreaseDefence);

                    if (point > 120) { point = 120; };



                    p = 0;
                    int evadeCloseLong = 0;
                    int decreaseDeathblow = 0;
                    hp = 0;
                    int allRes = 0;

                    if (point <= 10)
                    {
                        p = point;
                    }
                    else if (point <= 20)
                    {
                        p = 10 + ((point - 10) * 2);
                        evadeCloseLong = 5;
                    }
                    else if (point <= 30)
                    {
                        p = 30 + ((point - 20) * 3);
                        decreaseDeathblow = 2;
                        evadeCloseLong = 5;
                    }
                    else if (point <= 40)
                    {
                        p = 60 + ((point - 30) * 4);
                        decreaseDeathblow = 2;
                        evadeCloseLong = 5;
                        hp = 100;
                    }
                    else if (point <= 50)
                    {
                        p = 100 + ((point - 40) * 5);
                        decreaseDeathblow = 4;
                        evadeCloseLong = 5;
                        hp = 100;
                    }
                    else if (point <= 60)
                    {
                        p = 150 + ((point - 50) * 6);
                        decreaseDeathblow = 4;
                        evadeCloseLong = 10;
                        hp = 100;
                    }
                    else if (point <= 70)
                    {
                        p = 210 + ((point - 60) * 7);
                        decreaseDeathblow = 4;
                        evadeCloseLong = 10;
                        hp = 300;
                    }
                    else if (point < 75)
                    {
                        p = 280 + ((point - 70) * 8);
                        decreaseDeathblow = 7;
                        evadeCloseLong = 10;
                        hp = 300;
                    }
                    else if (point >= 75 && point < 80)
                    {
                        p = 320 + ((point - 75) * 8);
                        decreaseDeathblow = 7;
                        evadeCloseLong = 10;
                        hp = 300;
                        allRes = 2;
                    }
                    else if (point <= 90)
                    {
                        p = 360 + ((point - 80) * 9);
                        decreaseDeathblow = 10;
                        evadeCloseLong = 20;
                        hp = 300;
                        allRes = 2;
                    }
                    else if (point > 90 && point <= 95)
                    {
                        p = 450 + ((point - 90) * 10);
                        decreaseDeathblow = 10;
                        evadeCloseLong = 20;
                        hp = 300;
                        allRes = 5;
                    }
                    else if (point >= 95 && point < 100)
                    {
                        p = 500 + ((point - 95) * 10);
                        decreaseDeathblow = 10;
                        evadeCloseLong = 20;
                        hp = 600;
                        allRes = 5;
                    }
                    else if (point >= 100 && point < 110)
                    {
                        p = 550 + ((point - 90) * 10);
                        decreaseDeathblow = 10;
                        evadeCloseLong = 40;
                        hp = 600;
                        allRes = 10;
                    }
                    else if (point >= 110 && point < 115)
                    {
                        p = 660 + ((point - 110) * 14);
                        decreaseDeathblow = 14;
                        evadeCloseLong = 50;
                        hp = 600;
                        allRes = 10;
                    }
                    else if (point >= 115 && point < 120)
                    {
                        p = 730 + ((point - 115) * 14);
                        decreaseDeathblow = 14;
                        evadeCloseLong = 50;
                        hp = 1000;
                        allRes = 10;
                    }
                    else if (point >= 120)
                    {
                        p = 810;
                        decreaseDeathblow = 16;
                        evadeCloseLong = 50;
                        hp = 1000;
                        allRes = 15;
                    }

                    Defence += p;
                    MagicalDefence += evadeCloseLong;
                    DistanceDefence += evadeCloseLong;
                    DistanceDefence += evadeCloseLong;
                    FireResistance += allRes;
                    WaterResistance += allRes;
                    DarkResistance += allRes;
                    LightResistance += allRes;
                    Hp += hp;

                    point = CharacterHelper.SlPoint(specialist.SlElement, 2)
                            + GetShellWeaponEffectValue(ShellWeaponEffectType.SLElement)
                            + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal)
                            + GetTitleEffectValue(CardType.IncreaseSlPoint,
                                (byte)AdditionalTypes.IncreaseSlPoint.IncreaseEllement);

                    if (point > 120) { point = 120; };

                    p = 0;
                    mana = 0;
                    int dmd = 0;
                    int rs = 0;
                    int ele = 0;

                    if (point <= 10)
                    {
                        p = 50 + ((point - 85) * 2);
                        mana = 100;
                        ele = 2;
                    }
                    else if (point <= 20)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 5;
                        mana = 100;
                        ele = 2;
                    }
                    else if (point <= 30)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 5;
                        rs = 2;
                        mana = 100;
                        ele = 4;
                    }
                    else if (point <= 40)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 5;
                        rs = 2;
                        mana = 200;
                        ele = 4;
                    }
                    else if (point <= 50)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 10;
                        rs = 2;
                        mana = 200;
                        ele = 4;
                    }
                    else if (point <= 60)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 10;
                        rs = 5;
                        mana = 200;
                        ele = 6;
                    }
                    else if (point <= 70)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 10;
                        rs = 5;
                        mana = 300;
                        ele = 6;
                    }
                    else if (point <= 80)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 15;
                        rs = 5;
                        mana = 300;
                        ele = 6;
                    }
                    else if (point <= 90)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 15;
                        rs = 9;
                        mana = 300;
                        ele = 8;
                    }
                    else if (point <= 100)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 20;
                        rs = 15;
                        mana = 500;
                        ele = 10;
                    }
                    else if (point <= 110)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 25;
                        rs = 15;
                        mana = 500;
                        ele = 12;
                    }
                    else if (point <= 115)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 25;
                        rs = 15;
                        mana = 700;
                        ele = 12;
                    }
                    else if (point >= 120)
                    {
                        p = 50 + ((point - 85) * 2);
                        dmd = 25;
                        rs = 20;
                        mana = 700;
                        ele = 14;
                    }

                    FireResistance += rs;
                    WaterResistance += rs;
                    LightResistance += rs;
                    DarkResistance += rs;
                    Mp += mana;
                    ElementRateSP += p;

                    slhpbonus = GetShellWeaponEffectValue(ShellWeaponEffectType.SLHP)
                                + GetShellWeaponEffectValue(ShellWeaponEffectType.SLGlobal)
                                + GetTitleEffectValue(CardType.IncreaseSlPoint,
                                    (byte)AdditionalTypes.IncreaseSlPoint.IncreaseHPMP);
                }
            }

            // TODO: add base stats
            ItemInstance weapon = Inventory?.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
            if (weapon != null)
            {
                weaponUpgrade = weapon.Upgrade;
                MinHit += weapon.DamageMinimum + weapon.Item.DamageMinimum;
                MaxHit += weapon.DamageMaximum + weapon.Item.DamageMaximum;
                HitRate += weapon.HitRate + weapon.Item.HitRate;
                HitCriticalChance += weapon.CriticalLuckRate + weapon.Item.CriticalLuckRate;
                HitCriticalRate += weapon.CriticalRate + weapon.Item.CriticalRate;

                // maxhp-mp
            }

            ItemInstance weapon2 = Inventory?.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
            if (weapon2 != null)
            {
                secondaryUpgrade = weapon2.Upgrade;
                SecondWeaponMinHit += weapon2.DamageMinimum + weapon2.Item.DamageMinimum;
                SecondWeaponMaxHit += weapon2.DamageMaximum + weapon2.Item.DamageMaximum;
                SecondWeaponHitRate += weapon2.HitRate + weapon2.Item.HitRate;
                SecondWeaponCriticalChance += weapon2.CriticalLuckRate + weapon2.Item.CriticalLuckRate;
                SecondWeaponCriticalRate += weapon2.CriticalRate + weapon2.Item.CriticalRate;

                // maxhp-mp
            }

            ItemInstance armor = Inventory?.LoadBySlotAndType((byte)EquipmentType.Armor, InventoryType.Wear);
            if (armor != null)
            {
                armorUpgrade = armor.Upgrade;
                Defence += armor.CloseDefence + armor.Item.CloseDefence;
                DefenceRate += armor.DefenceDodge + armor.Item.DefenceDodge;
                MagicalDefence += armor.MagicDefence + armor.Item.MagicDefence;
                DistanceDefence += armor.DistanceDefence + armor.Item.DistanceDefence;
                DistanceDefenceRate += armor.DistanceDefenceDodge + armor.Item.DistanceDefenceDodge;
            }

            //TODO: Rework 
            ItemInstance fairy = Inventory?.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            if (fairy != null)
            {
                ElementRate += fairy.ElementRate + (IsUsingFairyBooster ? 30 : 0)
                    + GetStuffBuff(CardType.PixieCostumeWings, (byte)AdditionalTypes.PixieCostumeWings.IncreaseFairyElement)[0];
            }

            for (short i = 1; i < 14; i++)
            {
                ItemInstance item = Inventory?.LoadBySlotAndType(i, InventoryType.Wear);
                if (item != null && item.Item.EquipmentSlot != EquipmentType.MainWeapon
                                 && item.Item.EquipmentSlot != EquipmentType.SecondaryWeapon
                                 && item.Item.EquipmentSlot != EquipmentType.Armor
                                 && item.Item.EquipmentSlot != EquipmentType.Sp)
                {
                    FireResistance += item.FireResistance + item.Item.FireResistance;
                    LightResistance += item.LightResistance + item.Item.LightResistance;
                    WaterResistance += item.WaterResistance + item.Item.WaterResistance;
                    DarkResistance += item.DarkResistance + item.Item.DarkResistance;
                    Defence += item.CloseDefence + item.Item.CloseDefence;
                    DefenceRate += item.DefenceDodge + item.Item.DefenceDodge;
                    MagicalDefence += item.MagicDefence + item.Item.MagicDefence;
                    DistanceDefence += item.DistanceDefence + item.Item.DistanceDefence;
                    DistanceDefenceRate += item.DistanceDefenceDodge + item.Item.DistanceDefenceDodge;
                }
            }

            //BCards
            int BCardFireResistance =
                GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.FireIncreased)[0] +
                GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
            int BCardLightResistance =
                GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.LightIncreased)[0] +
                GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
            int BCardWaterResistance =
                GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.WaterIncreased)[0] +
                GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];
            int BCardDarkResistance =
                GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.DarkIncreased)[0] +
                GetStuffBuff(CardType.ElementResistance, (byte)AdditionalTypes.ElementResistance.AllIncreased)[0];

            int BCardHitCritical = GetStuffBuff(CardType.Critical, (byte)AdditionalTypes.Critical.DamageIncreased)[0] +
                                   GetStuffBuff(CardType.Critical,
                                       (byte)AdditionalTypes.Critical.DamageFromCriticalIncreased)[0];
            int BCardHitCriticalRate =
                GetStuffBuff(CardType.Critical, (byte)AdditionalTypes.Critical.InflictingIncreased)[0];

            int BCardHit =
                GetStuffBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.AllAttacksIncreased)[0];
            int BCardSecondHit =
                GetStuffBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.AllAttacksIncreased)[0];

            int BCardHitRate = GetStuffBuff(CardType.Target, (byte)AdditionalTypes.Target.AllHitRateIncreased)[0];
            int BCardSecondHitRate =
                GetStuffBuff(CardType.Target, (byte)AdditionalTypes.Target.AllHitRateIncreased)[0];

            int BCardMeleeDodge = GetStuffBuff(CardType.DodgeAndDefencePercent,
                (byte)AdditionalTypes.Target.AllHitRateIncreased)[0];
            int BCardRangeDodge = GetStuffBuff(CardType.DodgeAndDefencePercent,
                (byte)AdditionalTypes.Target.AllHitRateIncreased)[0];

            int BCardMeleeDefence = GetStuffBuff(CardType.Defence, (byte)AdditionalTypes.Defence.AllIncreased)[0] +
                                    GetStuffBuff(CardType.Defence, (byte)AdditionalTypes.Defence.MeleeIncreased)[0];

            int BCardRangeDefence = GetStuffBuff(CardType.Defence, (byte)AdditionalTypes.Defence.AllIncreased)[0] +
                                    GetStuffBuff(CardType.Defence, (byte)AdditionalTypes.Defence.RangedIncreased)[0];

            int BCardMagicDefence = GetStuffBuff(CardType.Defence, (byte)AdditionalTypes.Defence.AllIncreased)[0] +
                                    GetStuffBuff(CardType.Defence, (byte)AdditionalTypes.Defence.MagicalIncreased)[0];

            switch (Class)
            {
                case ClassType.Adventurer:
                case ClassType.Swordsman:
                    BCardHit += GetStuffBuff(CardType.AttackPower,
                        (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased)[0];
                    BCardSecondHit += GetStuffBuff(CardType.AttackPower,
                        (byte)AdditionalTypes.AttackPower.RangedAttacksIncreased)[0];
                    BCardHitRate +=
                        GetStuffBuff(CardType.Target, (byte)AdditionalTypes.Target.MeleeHitRateIncreased)[0];
                    BCardSecondHitRate +=
                        GetStuffBuff(CardType.Target, (byte)AdditionalTypes.Target.RangedHitRateIncreased)[0];
                    break;

                case ClassType.Archer:
                    BCardHit += GetStuffBuff(CardType.AttackPower,
                        (byte)AdditionalTypes.AttackPower.RangedAttacksIncreased)[0];
                    BCardSecondHit += GetStuffBuff(CardType.AttackPower,
                        (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased)[0];
                    BCardHitRate +=
                        GetStuffBuff(CardType.Target, (byte)AdditionalTypes.Target.RangedHitRateIncreased)[0];
                    BCardSecondHitRate +=
                        GetStuffBuff(CardType.Target, (byte)AdditionalTypes.Target.MeleeHitRateIncreased)[0];
                    break;

                case ClassType.Magician:
                    BCardHit += GetStuffBuff(CardType.AttackPower,
                        (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased)[0];
                    BCardSecondHit += GetStuffBuff(CardType.AttackPower,
                        (byte)AdditionalTypes.AttackPower.RangedAttacksIncreased)[0];
                    BCardHitRate += GetStuffBuff(CardType.Target,
                        (byte)AdditionalTypes.Target.MagicalConcentrationIncreased)[0];
                    BCardSecondHitRate +=
                        GetStuffBuff(CardType.Target, (byte)AdditionalTypes.Target.RangedHitRateIncreased)[0];
                    break;
            }

            //Fuego
            BCardFireResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedFireResistence) + GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
            //Agua
            BCardWaterResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedWaterResistence) + GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
            //Luz
            BCardLightResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedLightResistence) + GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);
            //Tini
            BCardDarkResistance += GetShellArmorEffectValue(ShellArmorEffectType.IncreasedDarkResistence) + GetShellArmorEffectValue(ShellArmorEffectType.IncreasedAllResistence);

            byte type = Class == ClassType.Adventurer ? (byte)0 : (byte)(Class - 1);

            List<string> packets = new List<string>();
            packets.Add(
                $"sc {type} {(weaponUpgrade == 13 ? weaponUpgrade : weaponUpgrade + GetBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.AttackLevelIncreased)[0])} {MinHit + BCardHit} {MaxHit + BCardHit} {HitRate + BCardHitRate} {HitCriticalChance + BCardHitCriticalRate} {HitCriticalRate + BCardHitCritical} {(Class == ClassType.Archer ? 1 : 0)} {(secondaryUpgrade == 13 ? secondaryUpgrade : secondaryUpgrade + GetBuff(CardType.AttackPower, (byte)AdditionalTypes.AttackPower.AttackLevelIncreased)[0])} {SecondWeaponMinHit + BCardSecondHit} {SecondWeaponMaxHit + BCardSecondHit} {SecondWeaponHitRate + BCardSecondHitRate} {SecondWeaponCriticalChance + BCardHitCriticalRate} {SecondWeaponCriticalRate + BCardHitCritical} {(armorUpgrade == 13 ? armorUpgrade : armorUpgrade + GetBuff(CardType.Defence, (byte)AdditionalTypes.Defence.DefenceLevelIncreased)[0])} {Defence + BCardMeleeDefence} {DefenceRate + BCardMeleeDodge} {DistanceDefence + BCardRangeDefence} {DistanceDefenceRate + BCardRangeDodge} {MagicalDefence + BCardMagicDefence} {FireResistance + BCardFireResistance} {WaterResistance + BCardWaterResistance} {LightResistance + BCardLightResistance} {DarkResistance + BCardDarkResistance}");
            packets.AddRange(GenerateScN());
            packets.AddRange(GenerateScP());

            LoadSpeed();

            return packets;
        }

        public string GenerateStatInfo() =>
            $"st 1 {CharacterId} {Level} {HeroLevel} {(int)(Hp / (float)HPLoad() * 100)} {(int)(Mp / (float)MPLoad() * 100)} {Hp} {Mp} {BattleEntity.HpMax} {BattleEntity.MpMax} 0{Buff.GetAllItems().Where(s => !s.StaticBuff || new short[] { 339, 340 }.Contains(s.Card.CardId)).Aggregate("", (current, buff) => current + $" {buff.Card.CardId}.{buff.Level}")}";

        public string GenerateTaF(byte victoriousteam)
        {
            ConcurrentBag<ArenaTeamMember> tm = ServerManager.Instance.ArenaTeams.ToList()
                .FirstOrDefault(s => s.Any(o => o.Session == Session));
            var score1 = 0;
            var score2 = 0;
            var life1 = 0;
            var life2 = 0;
            var call1 = 0;
            var call2 = 0;
            var atype = ArenaTeamType.ERENIA;
            if (tm == null)
            {
                return $"ta_f 0 {victoriousteam} {(byte)atype} {score1} {life1} {call1} {score2} {life2} {call2}";
            }

            var tmem = tm.FirstOrDefault(s => s.Session == Session);
            if (tmem == null)
            {
                return $"ta_f 0 {victoriousteam} {(byte)atype} {score1} {life1} {call1} {score2} {life2} {call2}";
            }

            atype = tmem.ArenaTeamType;
            IEnumerable<long> ids = tm.Replace(s => tmem.ArenaTeamType == s.ArenaTeamType)
                .Select(s => s.Session.Character.CharacterId);
            ConcurrentBag<ArenaTeamMember> oposit = tm.Replace(s => tmem.ArenaTeamType != s.ArenaTeamType);
            ConcurrentBag<ArenaTeamMember> own = tm.Replace(s => tmem.ArenaTeamType == s.ArenaTeamType);
            score1 = 3 - MapInstance.InstanceBag.DeadList.Count(s => ids.Contains(s));
            score2 = 3 - MapInstance.InstanceBag.DeadList.Count(s => !ids.Contains(s));
            life1 = 3 - own.Count(s => s.Dead);
            life2 = 3 - oposit.Count(s => s.Dead);
            call1 = 5 - own.Sum(s => s.SummonCount);
            call2 = 5 - oposit.Sum(s => s.SummonCount);
            return $"ta_f 0 {victoriousteam} {(byte)atype} {score1} {life1} {call1} {score2} {life2} {call2}";
        }

        public string GenerateTaFc(byte type) => $"ta_fc {type} {CharacterId}";

        public TalkPacket GenerateTalk(string message)
        {
            return new TalkPacket
            {
                CharacterId = CharacterId,
                Message = message
            };
        }

        public string GenerateTaM(int type)
        {
            ConcurrentBag<ArenaTeamMember> tm = ServerManager.Instance.ArenaTeams.ToList()
                .FirstOrDefault(s => s.Any(o => o.Session == Session));
            var score1 = 0;
            var score2 = 0;
            if (tm == null)
            {
                return
                    $"ta_m {type} {score1} {score2} {(type == 3 ? MapInstance.InstanceBag.Clock.SecondsRemaining / 10 : 0)} 0";
            }

            var tmem = tm.FirstOrDefault(s => s.Session == Session);
            IEnumerable<long> ids = tm.Replace(s => tmem != null && tmem.ArenaTeamType != s.ArenaTeamType)
                .Select(s => s.Session.Character.CharacterId);
            score1 = MapInstance.InstanceBag.DeadList.Count(s => ids.Contains(s));
            score2 = MapInstance.InstanceBag.DeadList.Count(s => !ids.Contains(s));
            return
                $"ta_m {type} {score1} {score2} {(type == 3 ? MapInstance.InstanceBag.Clock.SecondsRemaining / 10 : 0)} 0";
        }

        public string GenerateTaP(byte tatype, bool showOponent)
        {
            List<ArenaTeamMember> arenateam = ServerManager.Instance.ArenaTeams.ToList()
                .FirstOrDefault(s => s != null && s.Any(o => o != null && o.Session == Session))
                ?.OrderBy(s => s.ArenaTeamType).ToList();
            var type = ArenaTeamType.ERENIA;
            var groups = "";
            if (arenateam == null)
            {
                return
                    $"ta_p {tatype} {(byte)type} {5} {5} {groups.TrimEnd(' ')}";
            }

            type = arenateam.FirstOrDefault(s => s.Session == Session)?.ArenaTeamType ?? ArenaTeamType.ERENIA;

            List<ArenaTeamMember> MyTeam = arenateam.Where(s => s.ArenaTeamType == type && s.Order != null).ToList();
            List<ArenaTeamMember> EnemyTeam = arenateam.Where(s => s.ArenaTeamType != type && s.Order != null).ToList();

            for (int i = 0; i < 3; i++)
            {
                if (MyTeam.Where(s => s.Order == i).FirstOrDefault() is ArenaTeamMember arenamember)
                {
                    groups +=
                        $"{(arenamember.Dead ? 0 : 1)}.{arenamember.Session.Character.CharacterId}.{(byte)arenamember.Session.Character.Class}.{(byte)arenamember.Session.Character.Gender}.{(byte)arenamember.Session.Character.Morph} ";
                }
                else
                {
                    groups += $"-1.-1.-1.-1.-1 ";
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (EnemyTeam.Where(s => s.Order == i).FirstOrDefault() is ArenaTeamMember arenamember && showOponent)
                {
                    groups +=
                        $"{(arenamember.Dead ? 0 : 1)}.{arenamember.Session.Character.CharacterId}.{(byte)arenamember.Session.Character.Class}.{(byte)arenamember.Session.Character.Gender}.{(byte)arenamember.Session.Character.Morph} ";
                }
                else
                {
                    groups += $"-1.-1.-1.-1.-1 ";
                }
            }

            return
                $"ta_p {tatype} {(byte)type} {5 - arenateam.Where(s => s.ArenaTeamType == type).Sum(s => s.SummonCount)} {5 - arenateam.Where(s => s.ArenaTeamType != type).Sum(s => s.SummonCount)} {groups.TrimEnd(' ')}";
        }

        public string GenerateTaPs()
        {
            List<ArenaTeamMember> arenateam = ServerManager.Instance.ArenaTeams.ToList()
                .FirstOrDefault(s => s != null && s.Any(o => o?.Session == Session))?.OrderBy(s => s.ArenaTeamType)
                .ToList();
            string groups = "";
            if (arenateam == null)
            {
                return $"ta_ps {groups.TrimEnd(' ')}";
            }

            ArenaTeamType type = arenateam.FirstOrDefault(s => s.Session == Session)?.ArenaTeamType ??
                                 ArenaTeamType.ERENIA;

            List<ArenaTeamMember> MyTeam = arenateam.Where(s => s.ArenaTeamType == type && s.Order != null).ToList();
            List<ArenaTeamMember> EnemyTeam = arenateam.Where(s => s.ArenaTeamType != type && s.Order != null).ToList();

            for (int i = 0; i < 3; i++)
            {
                if (MyTeam.Where(s => s.Order == i).FirstOrDefault() is ArenaTeamMember arenamember)
                {
                    groups +=
                        $"{arenamember.Session.Character.CharacterId}.{(int)(arenamember.Session.Character.Hp / arenamember.Session.Character.HPLoad() * 100)}.{(int)(arenamember.Session.Character.Mp / arenamember.Session.Character.MPLoad() * 100)}.0 ";
                }
                else
                {
                    groups += $"-1.-1.-1.-1.-1 ";
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (EnemyTeam.Where(s => s.Order == i).FirstOrDefault() is ArenaTeamMember arenamember)
                {
                    groups +=
                        $"{arenamember.Session.Character.CharacterId}.{(int)(arenamember.Session.Character.Hp / arenamember.Session.Character.HPLoad() * 100)}.{(int)(arenamember.Session.Character.Mp / arenamember.Session.Character.MPLoad() * 100)}.0 ";
                }
                else
                {
                    groups += $"-1.-1.-1.-1.-1 ";
                }
            }

            return $"ta_ps {groups.TrimEnd(' ')}";
        }

        public string GenerateTit() =>
            $"tit {(Class == (byte)ClassType.Adventurer ? (int)GameConstString.Adventurer : Class == ClassType.Swordsman ? (int)GameConstString.Swordsman : Class == ClassType.Archer ? (int)GameConstString.Archer : Class == ClassType.Magician ? (int)GameConstString.Mage : (int)GameConstString.MartialArtist)} {Name}";

        public string GenerateTitInfo()
        {
            long tit = 0;
            long eff = 0;
            if (Title.Find(s => s.Stat.Equals(3)) != null)
            {
                tit = Title.Find(s => s.Stat.Equals(3)).TitleVnum;
            }

            if (Title.Find(s => s.Stat.Equals(7)) != null)
            {
                tit = Title.Find(s => s.Stat.Equals(7)).TitleVnum;
            }

            if (Title.Find(s => s.Stat.Equals(5)) != null)
            {
                eff = Title.Find(s => s.Stat.Equals(5)).TitleVnum;
            }

            return $"titinfo 1 {CharacterId} {tit} {(Title.Find(s => s.Stat.Equals(7)) != null ? tit : eff)}";
        }

        public string GenerateTitle()
        {
            string tit = string.Empty;
            foreach (var t in Title.ToList())
            {
                tit += $"{t.TitleVnum - 9300}.{t.Stat} ";
            }

            return $"title {tit}";
        }

        public string GenerateTp() => BattleEntity.GenerateTp();

        public void GetAct4Points(int point)
        {
            //RefreshComplimentRankingIfNeeded();
            Act4Points += point;
        }

        public int[] GetBuff(CardType type, byte subtype) => BattleEntity.GetBuff(type, subtype);

        public int GetCP()
        {
            int cpmax = (Class > 0 ? 40 : 0) + (JobLevel * 2);
            int cpused = 0;
            foreach (CharacterSkill ski in Skills.GetAllItems())
            {
                cpused += ski.Skill.CPCost;
            }

            return cpmax - cpused;
        }

        public void GetDamage(int damage, BattleEntity damager, bool dontKill = false) =>
            BattleEntity.GetDamage(damage, damager, dontKill);

        public void GetDignity(int amount)
        {
            Dignity += amount;

            if (Dignity > 100)
            {
                Dignity = 100;
            }

            Session.SendPacket(GenerateFd());
            Session.CurrentMapInstance?.Broadcast(Session, GenerateIn(InEffect: 1), ReceiverType.AllExceptMe);
            Session.CurrentMapInstance?.Broadcast(Session, GenerateGidx(), ReceiverType.AllExceptMe);
            Session.SendPacket(GenerateSay($"{Language.Instance.GetMessageFromKey("RESTORE_DIGNITY")} (+{amount})",
                11));
        }

        public int GetDignityIco()
        {
            int icoDignity = 1;

            if (Dignity <= -100)
            {
                icoDignity = 2;
            }

            if (Dignity <= -200)
            {
                icoDignity = 3;
            }

            if (Dignity <= -400)
            {
                icoDignity = 4;
            }

            if (Dignity <= -600)
            {
                icoDignity = 5;
            }

            if (Dignity <= -800)
            {
                icoDignity = 6;
            }

            return icoDignity;
        }

        public void GetDir(int pX, int pY, int nX, int nY)
        {
            BeforeDirection = Direction;
            if (pX == nX && pY < nY)
            {
                Direction = 2;
            }
            else if (pX > nX && pY == nY)
            {
                Direction = 3;
            }
            else if (pX == nX && pY > nY)
            {
                Direction = 0;
            }
            else if (pX < nX && pY == nY)
            {
                Direction = 1;
            }
            else if (pX < nX && pY < nY)
            {
                Direction = 6;
            }
            else if (pX > nX && pY < nY)
            {
                Direction = 7;
            }
            else if (pX > nX && pY > nY)
            {
                Direction = 4;
            }
            else if (pX < nX && pY > nY)
            {
                Direction = 5;
            }
        }

        public List<Portal> GetExtraPortal() => new List<Portal>(MapInstancePortalHandler
            .GenerateMinilandEntryPortals(MapInstance.Map.MapId, Miniland.MapInstanceId)
            .Concat(Family?.Act4Raid != null
                ? (MapInstancePortalHandler.GenerateAct4EntryPortals(MapInstance.Map.MapId))
                : new List<Portal>()));

        public List<string> GetFamilyHistory()
        {
            //TODO: Fix some bugs(missing history etc)
            if (Family != null)
            {
                const string packetheader = "ghis";
                List<string> packetList = new List<string>();
                string packet = "";
                int i = 0;
                int amount = 0;
                foreach (FamilyLogDTO log in Family.FamilyLogs.Where(s => s.FamilyLogType != FamilyLogType.WareHouseAdded && s.FamilyLogType != FamilyLogType.WareHouseRemoved).OrderByDescending(s => s.Timestamp).Take(100))
                {
                    packet += $" {(byte)log.FamilyLogType}|{log.FamilyLogData}|{(int)(DateTime.Now - log.Timestamp).TotalHours}";
                    i++;
                    if (i == 50)
                    {
                        i = 0;
                        packetList.Add(packetheader + (amount == 0 ? " 0 " : "") + packet);
                        amount++;
                    }
                    else if (i + (50 * amount) == Family.FamilyLogs.Count)
                    {
                        packetList.Add(packetheader + (amount == 0 ? " 0 " : "") + packet);
                    }
                }

                return packetList;
            }
            return new List<string>();
        }

        public void GetGold(long val, bool isQuest = false)
        {
            Gold += val;
            if (Gold > GameConfiguration.MaxGold)
            {
                Gold = GameConfiguration.MaxGold;
                Session?.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("MAX_GOLD"), 0));
            }

            Session?.SendPacket(isQuest
                ? GenerateSay($"Quest reward: [ {ServerManager.GetItem(1046).Name} x {val} ]", 10)
                : Session.Character.GenerateSay(
                    $"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {ServerManager.GetItem(1046).Name} x{val}",
                    10));
            Session?.SendPacket(Session.Character.GenerateGold());
        }

        public void GetHXp(long val, bool applyRate = true)
        {
            if (WorldPolicyConfiguration.DisableHeroExperience ||
                HeroLevel >= GameConfiguration.MaxHeroLevel)
            {
                return;
            }

            HeroXp += val * (applyRate ? GameConfiguration.HeroXPRate : 1) *
                (int)(1 + GetBuff(CardType.Item, (byte)AdditionalTypes.Item.EXPIncreased)[0] / 100D +
                    GetBuff(CardType.Dracula, (byte)AdditionalTypes.Dracula.ExpHeroIncrease)[0] / 100D);


            GenerateHeroXpLevelUp();
            Session.SendPacket(GenerateLev());
        }

        public void GetJobExp(long val, bool applyRate = true)
        {
            val *= applyRate ? GameConfiguration.JobLevelRate : 1;
            ItemInstance SpInstance = null;
            if (Inventory != null)
            {
                SpInstance = Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            }
            if (UseSp && SpInstance != null)
            {
                if (SpInstance.SpLevel >= GameConfiguration.MaxSPLevel)
                {
                    return;
                }
                int multiplier = SpInstance.SpLevel < 10 ? 10 : SpInstance.SpLevel < 19 ? 5 : 1;
                SpInstance.XP += (int)((val * (multiplier + GetBuff(CardType.Item, (byte)AdditionalTypes.Item.EXPIncreased)[0] / 100D + GetBuff(CardType.Item, (byte)AdditionalTypes.Item.IncreaseSPXP)[0] / 100D)));
                GenerateSpXpLevelUp(SpInstance);
                return;
            }
            if (JobLevel >= GameConfiguration.MaxJobLevel)
            {
                return;
            }
            JobLevelXp += (int)(val * (1 + GetBuff(CardType.Item, (byte)AdditionalTypes.Item.EXPIncreased)[0] / 100D));
            GenerateJobXpLevelUp();
            Session.SendPacket(GenerateLev());
        }

        public IEnumerable<string> GetMinilandEffects() =>
            MinilandObjects.Select(mp => mp.GenerateMinilandEffect(false)).ToList();

        public string GetMinilandObjectList()
        {
            string mlobjstring = "mlobjlst";
            foreach (ItemInstance item in Inventory.Where(s => s.Type == InventoryType.Miniland).OrderBy(s => s.Slot))
            {
                MinilandObject mp = MinilandObjects.Find(s => s.ItemInstanceId == item.Id);
                bool used = mp != null;
                mlobjstring +=
                    $" {item.Slot}.{(used ? 1 : 0)}.{(used ? mp.MapX : 0)}.{(used ? mp.MapY : 0)}.{(item.Item.Width != 0 ? item.Item.Width : 1)}.{(item.Item.Height != 0 ? item.Item.Height : 1)}.{(used ? mp.ItemInstance.DurabilityPoint : 0)}.100.0.1";
            }

            return mlobjstring;
        }

        public List<long> GetMTListTargetQueue_QuickFix(CharacterSkill ski, UserType entityType)
        {
            List<long> result = new List<long>();

            if (BattleEntity != null
                && MapInstance != null
                && ski?.Skill != null)
            {
                foreach (long targetId in MTListTargetQueue.Where(target => target.EntityType == entityType
                                                                            && (byte)target.TargetHitType ==
                                                                            ski.Skill.HitType).Select(s => s.TargetId))
                {
                    switch (entityType)
                    {
                        case UserType.Player:
                            {
                                Character targetCharacter = MapInstance.GetCharacterById(targetId);

                                if (targetCharacter?.BattleEntity == null /* Invalid character  */
                                    || targetCharacter.Hp < 1 /* Amen */
                                    || !BattleEntity.RangeIs(PositionX, PositionY,
                                        ski.Skill.Range) /* Character not in range */
                                    || !BattleEntity.CanAttackEntity(targetCharacter.BattleEntity) /* Try again later */
                                )
                                {
                                    continue;
                                }
                            }
                            break;

                        case UserType.Monster:
                            {
                                MapMonster targetMonster = MapInstance.GetMonsterById(targetId);

                                if (targetMonster?.BattleEntity == null /* Invalid monster */
                                    || !targetMonster.IsAlive /* Amen */
                                    || targetMonster.CurrentHp < 1 /* Schrödinger's cat */
                                    || !BattleEntity.RangeIs(PositionX, PositionY,
                                        ski.Skill.Range) /* Monster not in range */
                                    || !BattleEntity.CanAttackEntity(targetMonster.BattleEntity) /* Try again later */
                                )
                                {
                                    continue;
                                }
                            }
                            break;
                    }

                    result.Add(targetId);
                }
            }

            return result;
        }

        public void GetReputation(int amount, bool applyRate = true, bool showMessage = false)
        {
            long val2 = amount * (amount > 0 && applyRate ? GameConfiguration.ReputationRate : 1);
            int bonus = ((int)((GetBuff(CardType.Dracula, (byte)AdditionalTypes.Dracula.ReputationIncrease)[0] * 0.01) * val2));
            double Last = val2 + bonus;

            int beforeReputIco = GetReputationIco();
            Reputation += HasBuff(CardType.Dracula, (byte)AdditionalTypes.Dracula.ReputationIncrease)
                ? (val2 + (long)Last) * 1
                : val2;
            Reputation += amount;
            Session.SendPacket(GenerateFd());
            if (beforeReputIco != GetReputationIco())
            {
                Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateIn(InEffect: 1),
                    ReceiverType.AllExceptMe);
            }

            Session.CurrentMapInstance?.Broadcast(Session, Session.Character.GenerateGidx(), ReceiverType.AllExceptMe);
            if (showMessage)
            {
                if (amount > 0)
                {
                    Session.SendPacket(GenerateSay(string.Format(Language.Instance.GetMessageFromKey("REPUT_INCREASE"), Last), 12));
                    //LOGGER
                    ////LOGGER($"[REPUTATION] Increased by {Last} | Name: {Session.Character.Name}");
                }
                else if (amount < 0)
                {
                    Session.SendPacket(GenerateSay(string.Format(Language.Instance.GetMessageFromKey("REPUT_DECREASE"), amount), 11));
                    //LOGGER
                    ////LOGGER($"[REPUTATION] Decreased by {amount} | Name: {Session.Character.Name}");
                }
            }
        }

        public int GetReputationIco()
        {
            return ReputationExtension.GetReputation(Session);
        }

        public int GetShellArmor(ShellArmorEffectType effectType)
        {
            var armor = Inventory.LoadBySlotAndType((byte)EquipmentType.Armor, InventoryType.Wear);
            List<ShellEffectDTO> effects = new List<ShellEffectDTO>();
            if (armor == null)
            {
                return 0;
            }

            if (armor.ShellEffects == null)
            {
                return 0;
            }

            effects.AddRange(armor.ShellEffects);

            return effects.Where(s => s.Effect == (byte)effectType).OrderByDescending(s => s.Value).FirstOrDefault()
                ?.Value ?? 0;
        }

        public CharacterSkill GetSkill(short skillVNum) => GetSkills()?.FirstOrDefault(s => s.SkillVNum == skillVNum);

        public CharacterSkill GetSkillByCastId(short castId) =>
            GetSkills()?.FirstOrDefault(s => s.Skill?.CastId == castId);

        public List<CharacterSkill> GetSkills()
        {
            var list = new List<CharacterSkill>();
            if (UseSp)
            {
                SkillsSp.GetAllItems().Concat(Skills.Where(s => s.SkillVNum < 200)).ToList();
                Skills.GetAllItems();
                list.AddRange(SkillsSp.GetAllItems().Concat(Skills.Where(s => s.SkillVNum < 200)).ToList());
                list.AddRange(Skills.GetAllItems().Where(sd => sd.IsPartnerSkill).ToList());
                list.AddRange(Skills.GetAllItems().Where(sd => sd.IsTattoo).ToList());
            }
            else
            {
                list.AddRange(Skills.GetAllItems());
            }
            return list;
        }

        public string GetSqst()
        {
            List<QuestLogDTO> questLogs = DAOFactory.QuestLogDAO.LoadByCharacterId(CharacterId).ToList();
            List<CharacterQuest> quests = Quests.ToList();
            string sqst = "sqst  3 ";
            for (int i = 0; i < 250; i++)
            {
                string tempSqst = "}";

                //string tempSqst = "0";
                int count = 0;
                foreach (QuestLogDTO questLog in questLogs)
                {
                    if (i == ServerManager.Instance.GetQuest(questLog.QuestId).SqstPosition)
                    {
                        double test = ServerManager.Instance.GetQuest(questLog.QuestId).SqstPosition;
                        count = ServerManager.Instance.Quests.ToList().Where(s =>
                            !questLogs.Any(q => q.QuestId == s.QuestId) && s.SqstPosition == i).Count();

                        //int count2 = questLogs.Where(s => !quests.Any(q => q.QuestId == s.QuestId) && ServerManager.Instance.GetQuest(s.QuestId).SqstPosition == i).Count();

                        // O, v, }, l
                        /*switch (questLog.QuestId)
                        {
                            case 5051: // Pos 92
                                tempSqst = "O";
                                break;
                            case 5053: // Pos 93
                                tempSqst = "v";
                                break;
                            case 5055: // Pos 93
                                tempSqst = "l";
                                break;
                            case 5057: // Pos 93
                                tempSqst = "}";
                                break;
                            case 5059: // Pos 94
                                tempSqst = "v";
                                break;
                            case 5061: // Pos 94
                                tempSqst = "}";
                                break;
                            case 5065: // Pos 95
                                tempSqst = "v";
                                break;
                            case 5067: // Pos 95
                                tempSqst = "}";
                                break;
                            case 5070: // Pos 95
                                tempSqst = "}"; // Maybe } + 1
                                break;
                            case 5071: // Pos 96
                                tempSqst = "l";
                                break;
                            case 5081:
                                tempSqst = "O";
                                break;
                            case 5105:
                                tempSqst = "O";
                                break;
                            default:
                                tempSqst = "O";
                                break;
                        }*/
                    }
                }

                foreach (CharacterQuest quest in quests)
                {
                    if (i == ServerManager.Instance.GetQuest(quest.Quest.QuestId).SqstPosition)
                    {
                        double test = ServerManager.Instance.GetQuest(quest.Quest.QuestId).SqstPosition;
                        count = ServerManager.Instance.Quests
                            .Where(s => s.SqstPosition == i && !questLogs.Any(q => q.QuestId == s.QuestId)).Count();

                        //count = questLogs.Where(s => !quests.Any(q => q.QuestId == s.QuestId) && ServerManager.Instance.GetQuest(s.QuestId).SqstPosition == i).Count();

                        // 8, u, x
                        /*if (quest.Quest.QuestType == 22)
                        {
                            switch (quest.Quest.QuestId)
                            {
                                case 5051:
                                    tempSqst = "8";
                                    break;
                                case 5053:
                                    tempSqst = "u";
                                    break;
                                case 5055:
                                    tempSqst = "x";
                                    break;
                                case 5057:
                                    tempSqst = "8";
                                    break;
                                case 5059:
                                    tempSqst = "u";
                                    break;
                                case 5061:
                                    tempSqst = "x";
                                    break;
                                case 5065:
                                    tempSqst = "u";
                                    break;
                                case 5067:
                                    tempSqst = "x";
                                    break;
                                case 5071:
                                    tempSqst = "u";
                                    break;
                                case 5081:
                                    tempSqst = "8";
                                    break;
                                case 5105:
                                    tempSqst = "8";
                                    break;
                                default:
                                    tempSqst = "8";
                                    break;
                            }
                        }*/
                    }
                }

                if (i == 233)
                {
                    tempSqst = "2";
                }

                sqst += tempSqst;
            }

            return sqst;
        }

        /// <summary>
        /// Get Stuff Buffs Useful for Stats for example
        /// </summary>
        /// <param name="type"></param>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public int[] GetStuffBuff(CardType type, byte subtype)
        {
            int[] result = new int[2] { 0, 0 };

            List<BCard> bcards = new List<BCard>();

            if (Skills != null)
            {
                List<BCard> passiveSkillBCards =
                    PassiveSkillHelper.Instance.PassiveSkillToBCards(Skills.Where(s => s?.Skill?.SkillType == 0));

                if (passiveSkillBCards.Any())
                {
                    bcards.AddRange(passiveSkillBCards);
                }
            }

            List<BCard> equipmentBCards = EquipmentBCards.ToList();

            if (equipmentBCards.Any())
            {
                bcards.AddRange(equipmentBCards);
            }

            if (EffectFromTitle != null && EffectFromTitle.ToList().Any())
            {
                bcards.AddRange(EffectFromTitle.ToList());
            }

            foreach (BCard bcard in bcards.Where(s =>
                s?.Type == (byte)type && s.SubType == (byte)(subtype) && s.FirstData > 0))
            {
                result[0] += bcard.IsLevelScaled ? (bcard.FirstData * Level) : bcard.FirstData;
                result[1] += bcard.SecondData;
            }

            return result;
        }

        public void GetXp(long val, bool applyRate = true)
        {
            if (WorldPolicyConfiguration.DisableNormalExperience ||
                Level >= GameConfiguration.MaxLevel)
            {
                return;
            }

            var eventMultiplier = 1d;
            if (EventConfiguration.EXP > 0)
            {
                eventMultiplier += (EventConfiguration.EXP / 100D);
            }

            LevelXp += (long)(val * (applyRate ? GameConfiguration.XPRate : 1) * (int)(1 + GetBuff(CardType.Item, (byte)AdditionalTypes.Item.EXPIncreased)[0] / 100D) * eventMultiplier);
            GenerateLevelXpLevelUp();
            Session.SendPacket(GenerateLev());
        }

        public void GiftAdd(short itemVNum, short amount, byte rare = 0, byte upgrade = 0, short design = 0, bool forceRandom = false, byte minRare = 0)
        {
            if (Inventory != null && Session != null)
            {
                lock (Inventory)
                {
                    ItemInstance newItem = Inventory.InstantiateItemInstance(itemVNum, CharacterId, amount);
                    if (newItem.Item == null)
                    {
                        //LOGGER
                        ////LOGGER($"[GIFT] {itemVNum} does not exist");
                        return;
                    }

                    newItem.Design = design;

                    if (newItem.Item.ItemType == ItemType.Armor || newItem.Item.ItemType == ItemType.Weapon || newItem.Item.ItemType == ItemType.Shell || forceRandom)
                    {
                        if (rare != 0 && !forceRandom)
                        {
                            try
                            {
                                newItem.RarifyItem(Session, RarifyMode.Drop, RarifyProtection.None, forceRare: rare);
                                newItem.Upgrade = (byte)(newItem.Item.BasicUpgrade + upgrade);
                                if (newItem.Upgrade > 13)
                                {
                                    newItem.Upgrade = 13;
                                }
                            }
                            catch
                            {
                                throw;
                            }
                        }
                        else if (rare == 0 || forceRandom)
                        {
                            do
                            {
                                try
                                {
                                    newItem.RarifyItem(Session, RarifyMode.Drop, RarifyProtection.None);
                                    newItem.Upgrade = newItem.Item.BasicUpgrade;
                                    if (newItem.Rare >= minRare)
                                    {
                                        break;
                                    }
                                }
                                catch
                                {
                                    break;
                                }
                            } while (forceRandom);
                        }
                    }

                    if (newItem.Item.Type.Equals(InventoryType.Equipment) && rare != 0 && !forceRandom)
                    {
                        newItem.Rare = (sbyte)rare;
                        newItem.SetRarityPoint();
                    }

                    if (newItem.Item.ItemType == ItemType.Shell)
                    {
                        newItem.Upgrade = (byte)ServerManager.RandomNumber(50, 81);
                    }

                    if (newItem.Item.EquipmentSlot == EquipmentType.Gloves || newItem.Item.EquipmentSlot == EquipmentType.Boots)
                    {
                        newItem.Upgrade = upgrade;
                        newItem.DarkResistance = (short)(newItem.Item.DarkResistance * upgrade);
                        newItem.LightResistance = (short)(newItem.Item.LightResistance * upgrade);
                        newItem.WaterResistance = (short)(newItem.Item.WaterResistance * upgrade);
                        newItem.FireResistance = (short)(newItem.Item.FireResistance * upgrade);
                    }

                    if (newItem.Item.ItemType == ItemType.Jewelery && newItem.Item.ItemSubType == 3)
                    {
                        newItem.ElementRate = design;
                    }

                    List<ItemInstance> newInv = Inventory.AddToInventory(newItem);
                    if (newInv.Count > 0)
                    {
                        if (newItem.Item.IsHeroic && newItem.Item.ItemType == ItemType.Armor || newItem.Item.ItemType == ItemType.Weapon && newItem.Rare > 0)
                        {
                            newItem.GenerateHeroicShell(RarifyProtection.RandomHeroicAmulet);
                            newItem.SetRarityPoint();
                        }

                        Session.SendPacket(GenerateSay($"{Language.Instance.GetMessageFromKey("ITEM_ACQUIRED")} {newItem.Item.Name} x{amount}", 10));
                    }
                    else if (MailList.Count(s => s.Value.AttachmentVNum != null) < 40)
                    {
                        SendItem(CharacterId, itemVNum, amount, newItem.Rare, newItem.Upgrade, newItem.Design, false);
                        Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PACKET_ARRIVED"), $"{newItem.Item.Name} x {amount}"), 0));
                    }
                }
            }
        }

        public void MapBossReward(short itemVNum, short amount, byte rare = 0, byte upgrade = 0, short design = 0, bool forceRandom = false, byte minRare = 0)
        {
            if (Inventory != null)
            {
                lock (Inventory)
                {
                    ItemInstance newItem = Inventory.InstantiateItemInstance(itemVNum, CharacterId, amount);
                    if (newItem.Item == null)
                    {
                        //LOGGER
                        ////LOGGER($"[GIFT] {itemVNum} does not exist");
                    }
                    if (newItem != null)
                    {
                        newItem.Design = design;

                        if (newItem.Item.ItemType == ItemType.Armor || newItem.Item.ItemType == ItemType.Weapon || newItem.Item.ItemType == ItemType.Shell || forceRandom)
                        {
                            if (rare != 0 && !forceRandom)
                            {
                                try
                                {
                                    newItem.RarifyItem(Session, RarifyMode.Drop, RarifyProtection.None, forceRare: rare);
                                    newItem.Upgrade = (byte)(newItem.Item.BasicUpgrade + upgrade);
                                    if (newItem.Upgrade > 10)
                                    {
                                        newItem.Upgrade = 10;
                                    }
                                }
                                catch
                                {
                                    throw;
                                }
                            }
                            else if (rare == 0 || forceRandom)
                            {
                                do
                                {
                                    try
                                    {
                                        newItem.RarifyItem(Session, RarifyMode.Drop, RarifyProtection.None);
                                        newItem.Upgrade = newItem.Item.BasicUpgrade;
                                        if (newItem.Rare >= minRare)
                                        {
                                            break;
                                        }
                                    }
                                    catch
                                    {
                                        break;
                                    }
                                } while (forceRandom);
                            }
                        }

                        if (newItem.Item.Type.Equals(InventoryType.Equipment) && rare != 0 && !forceRandom)
                        {
                            newItem.Rare = (sbyte)rare;
                            newItem.SetRarityPoint();
                        }

                        if (newItem.Item.ItemType == ItemType.Shell)
                        {
                            newItem.Upgrade = (byte)ServerManager.RandomNumber(50, 81);
                        }

                        if (newItem.Item.EquipmentSlot == EquipmentType.Gloves || newItem.Item.EquipmentSlot == EquipmentType.Boots)
                        {
                            newItem.Upgrade = upgrade;
                            newItem.DarkResistance = (short)(newItem.Item.DarkResistance * upgrade);
                            newItem.LightResistance = (short)(newItem.Item.LightResistance * upgrade);
                            newItem.WaterResistance = (short)(newItem.Item.WaterResistance * upgrade);
                            newItem.FireResistance = (short)(newItem.Item.FireResistance * upgrade);
                        }

                        if (newItem.Item.ItemType == ItemType.Jewelery && newItem.Item.ItemSubType == 3)
                        {
                            newItem.ElementRate = design;
                        }

                        List<ItemInstance> newInv = Inventory.AddToInventory(newItem);
                        if (newInv.Count > 0)
                        {
                            if (newItem.Item.IsHeroic && newItem.Item.ItemType == ItemType.Armor || newItem.Item.ItemType == ItemType.Weapon && newItem.Rare > 0)
                            {
                                newItem.GenerateHeroicShell(RarifyProtection.RandomHeroicAmulet);
                                newItem.SetRarityPoint();
                            }

                            Session.SendPacket($"msg 4 You received {newItem.Item.Name} x{amount}");
                        }
                        else if (MailList.Count(s => s.Value.AttachmentVNum != null) < 40)
                        {
                            SendItem(CharacterId, itemVNum, amount, newItem.Rare, newItem.Upgrade, newItem.Design, false);
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PACKET_ARRIVED"), $"{newItem.Item.Name} x {amount}"), 0));
                        }
                    }
                }
            }
        }

        public async Task AddMysteryBoxReward(short itemVNum, short amount, byte rare = 0, byte upgrade = 0, short design = 0, bool forceRandom = false, byte minRare = 0)
        {
            if (Inventory != null)
            {
                ItemInstance newItem = Inventory.InstantiateItemInstance(itemVNum, CharacterId, amount);
                if (newItem.Item == null)
                {
                    //LOGGER //await //LOGGER($"[GIFT] {itemVNum} does not exist");
                }
                if (newItem != null)
                {
                    newItem.Design = design;

                    if (newItem.Item.ItemType == ItemType.Armor || newItem.Item.ItemType == ItemType.Weapon || newItem.Item.ItemType == ItemType.Shell || forceRandom)
                    {
                        if (rare != 0 && !forceRandom)
                        {
                            try
                            {
                                newItem.RarifyItem(Session, RarifyMode.Drop, RarifyProtection.None, forceRare: rare);
                                newItem.Upgrade = (byte)(newItem.Item.BasicUpgrade + upgrade);
                                if (newItem.Upgrade > 13)
                                {
                                    newItem.Upgrade = 13;
                                }
                            }
                            catch
                            {
                                throw;
                            }
                        }
                        else if (rare == 0 || forceRandom)
                        {
                            do
                            {
                                try
                                {
                                    newItem.RarifyItem(Session, RarifyMode.Drop, RarifyProtection.None);
                                    newItem.Upgrade = newItem.Item.BasicUpgrade;
                                    if (newItem.Rare >= minRare)
                                    {
                                        break;
                                    }
                                }
                                catch
                                {
                                    break;
                                }
                            } while (forceRandom);
                        }
                    }

                    if (newItem.Item.Type.Equals(InventoryType.Equipment) && rare != 0 && !forceRandom)
                    {
                        newItem.Rare = (sbyte)rare;
                        newItem.SetRarityPoint();
                    }

                    if (newItem.Item.ItemType == ItemType.Shell)
                    {
                        newItem.Upgrade = (byte)ServerManager.RandomNumber(50, 81);
                    }

                    if (newItem.Item.EquipmentSlot == EquipmentType.Gloves || newItem.Item.EquipmentSlot == EquipmentType.Boots)
                    {
                        newItem.Upgrade = upgrade;
                        newItem.DarkResistance = (short)(newItem.Item.DarkResistance * upgrade);
                        newItem.LightResistance = (short)(newItem.Item.LightResistance * upgrade);
                        newItem.WaterResistance = (short)(newItem.Item.WaterResistance * upgrade);
                        newItem.FireResistance = (short)(newItem.Item.FireResistance * upgrade);
                    }

                    List<ItemInstance> newInv = Inventory.AddToInventory(newItem);
                    if (newInv.Count > 0)
                        if (newInv.Count > 0)
                        {
                            Session.SendPacket(GenerateSay($"Your price was: {newItem.Item.Name} x{amount} | 500.000 has been spent", 12));
                        }
                        else if (MailList.Count(s => s.Value.AttachmentVNum != null) < 40)
                        {
                            SendItem(CharacterId, itemVNum, amount, newItem.Rare, newItem.Upgrade, newItem.Design, false);
                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(string.Format(Language.Instance.GetMessageFromKey("PACKET_ARRIVED"), $"{newItem.Item.Name} x {amount}"), 0));
                        }
                }
            }
        }

        public bool HasBuff(short cardId) => BattleEntity.HasBuff(cardId);

        public bool HasBuff(CardType type, byte subtype) => BattleEntity.HasBuff(type, subtype);

        public bool HasEreniaMedal() => StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.EreniaMedal);

        public bool HaveBackpack() => StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.BackPack);

        public bool HaveExtension() => StaticBonusList.Any(s => s.StaticBonusType == StaticBonusType.Extension);

        public double HPLoad() => BattleEntity.HPLoad();

        public Task<double> HPLoadAsync() => BattleEntity.HPLoadAsync();


        public void IncrementQuests(QuestType type, int firstData, int secondData = 0, int thirdData = 0,
            bool forGroupMember = false)
        {
            foreach (CharacterQuest quest in Quests.Where(q => q?.Quest?.QuestType == (int)type))
            {
                switch ((QuestType)quest.Quest.QuestType)
                {
                    case QuestType.Capture1:
                    case QuestType.Capture2:
                    case QuestType.WinRaid:
                        quest.Quest.QuestObjectives.Where(o => o.Data == firstData).ToList()
                            .ForEach(d => IncrementObjective(quest, d.ObjectiveIndex));
                        break;

                    case QuestType.Collect1:
                    case QuestType.Collect2:
                    case QuestType.Collect3:
                    case QuestType.Collect4:
                    case QuestType.Hunt:
                        quest.Quest.QuestObjectives.Where(o => o.Data == firstData).ToList()
                            .ForEach(d => IncrementObjective(quest, d.ObjectiveIndex));
                        if (!forGroupMember)
                        {
                            IncrementGroupQuest(type, firstData, secondData, thirdData);
                        }

                        break;

                    case QuestType.Product:
                        quest.Quest.QuestObjectives.Where(o => o.Data == firstData).ToList()
                            .ForEach(d => IncrementObjective(quest, d.ObjectiveIndex, secondData));
                        break;

                    case QuestType.Dialog1:
                    case QuestType.Dialog2:
                        quest.Quest.QuestObjectives.Where(o => o.Data == firstData).ToList().ForEach(d =>
                            IncrementObjective(quest, d.ObjectiveIndex, isOver: true));
                        break;

                    case QuestType.Wear:
                        if (quest.Quest.QuestObjectives.Any(q => q.SpecialData == firstData &&
                                                                 (Session.Character.Inventory.Any(i =>
                                                                      i.ItemVNum == q.Data &&
                                                                      i.Type == InventoryType.Wear) ||
                                                                  (quest.QuestId == 1541 || quest.QuestId == 1546) &&
                                                                  Class != ClassType.Adventurer)))
                        {
                            IncrementObjective(quest, isOver: true);
                        }

                        break;

                    case QuestType.Brings:
                    case QuestType.Required:
                        quest.Quest.QuestObjectives.Where(o => o.Data == firstData).ToList().ForEach(d =>
                        {
                            if (Inventory.CountItem(d.SpecialData ?? -1) >= d.Objective)
                            {
                                Inventory.RemoveItemAmount(d.SpecialData ?? -1, d.Objective ?? 1);
                                IncrementObjective(quest, d.ObjectiveIndex, d.Objective ?? 1);
                            }
                        });
                        break;

                    case QuestType.GoTo:
                        if (quest.Quest.TargetMap == firstData && Math.Abs(secondData - quest.Quest.TargetX ?? 0) < 3 &&
                            Math.Abs(thirdData - quest.Quest.TargetY ?? 0) < 3)
                        {
                            IncrementObjective(quest, isOver: true);
                        }

                        break;

                    case QuestType.Use:
                        quest.Quest.QuestObjectives
                            .Where(o => o.Data == firstData &&
                                        Mates.Any(m => m.NpcMonsterVNum == o.SpecialData && m.IsTeamMember)).ToList()
                            .ForEach(d => IncrementObjective(quest, d.ObjectiveIndex, d.Objective ?? 1));
                        break;

                    case QuestType.FlowerQuest:
                        if (firstData + 10 < Level)
                        {
                            continue;
                        }

                        IncrementObjective(quest, 1);
                        break;

                    case QuestType.GlacernonQuest:
                        quest.Quest.QuestObjectives.ToList().ForEach(d => IncrementObjective(quest, 1));
                        break;

                    case QuestType.TimesSpace:
                        quest.Quest.QuestObjectives.Where(o => o.SpecialData == firstData).ToList()
                            .ForEach(d => IncrementObjective(quest, d.ObjectiveIndex));
                        break;

                    //TODO : Later
                    case QuestType.TsPoint:
                    case QuestType.NumberOfKill:
                    case QuestType.Inspect:
                    case QuestType.Needed:
                    case QuestType.TargetReput:
                    case QuestType.TransmitGold:
                    case QuestType.Collect5:
                        break;
                }
            }
        }

        public void Initialize()
        {
            _random = new Random();
            ExchangeInfo = null;
            SpCooldown = 30;
            SaveX = 0;
            SaveY = 0;
            LastDefence = DateTime.Now.AddSeconds(-21);
            LastDelay = DateTime.Now.AddSeconds(-5);
            LastHealth = DateTime.Now;
            LastEffect = DateTime.Now;
            LastClockUpdate = DateTime.Now;
            LastBazaarInsert = DateTime.Now;
            LastBazaarModeration = DateTime.Now;
            LastDeposit = DateTime.Now;
            LastRepos = DateTime.Now;
            LastWithdraw = DateTime.Now;
            LastISort = DateTime.Now;
            Session = null;
            MailList = new Dictionary<int, MailDTO>();
            BattleEntity = new BattleEntity(this, null);
            Group = null;
            GmPvtBlock = false;
            Event = new EventEntity(this);
        }

        public bool IsBlockedByCharacter(long characterId) => CharacterRelations.Any(b =>
            b.RelationType == CharacterRelationType.Blocked && b.CharacterId.Equals(characterId) &&
            characterId != CharacterId);

        public bool IsBlockingCharacter(long characterId) => CharacterRelations.Any(c =>
            c.RelationType == CharacterRelationType.Blocked && c.RelatedCharacterId.Equals(characterId));

        public bool IsCoupleOfCharacter(long characterId) => CharacterRelations.Any(c =>
            characterId != CharacterId && c.RelationType == CharacterRelationType.Spouse &&
            (c.RelatedCharacterId.Equals(characterId) || c.CharacterId.Equals(characterId)));

        public bool IsFamilyTop(bool isLevel)
        {
            var family = ServerManager.Instance.GetBestFamily(isLevel);

            if (Family == null)
            {
                return false;
            }

            if (family == Family)
            {
                return true;
            }

            return false;
        }

        public bool IsFriendlistFull() => CharacterRelations.Where(s =>
            s.RelationType == CharacterRelationType.Friend ||
            s.RelationType == CharacterRelationType.Spouse).ToList().Count >= 80;

        public bool IsFriendOfCharacter(long characterId) => CharacterRelations.Any(c =>
            characterId != CharacterId &&
            (c.RelationType == CharacterRelationType.Friend || c.RelationType == CharacterRelationType.Spouse) &&
            (c.RelatedCharacterId.Equals(characterId) || c.CharacterId.Equals(characterId)));

        /// <summary>
        /// Checks if the current character is in range of the given position
        /// </summary>
        /// <param name="xCoordinate">The x coordinate of the object to check.</param>
        /// <param name="yCoordinate">The y coordinate of the object to check.</param>
        /// <param name="range">The range of the coordinates to be maximal distanced.</param>
        /// <returns>True if the object is in Range, False if not.</returns>
        public bool IsInRange(int xCoordinate, int yCoordinate, int range = 50)
        {
            return Map.GetDistance(new MapCell
            {
                X = (short)xCoordinate,
                Y = (short)yCoordinate
            }, new MapCell
            {
                X = PositionX,
                Y = PositionY
            }) <= range;
        }

        public bool IsLaurenaMorph() => Morph == 1000099 /* Hamster */ || Morph == 1000156 /* Bushtail */;

        public bool IsMuted() =>
            Session.Account.PenaltyLogs.Any(s => s.Penalty == PenaltyType.Muted && s.DateEnd > DateTime.Now);

        public int IsReputationHero()
        {
            int i = 0;

            foreach (CharacterDTO character in ServerManager.Instance.TopReputation)
            {
                i++;

                if (character.CharacterId == CharacterId)
                {
                    if (i == 1)
                    {
                        return 5;
                    }

                    if (i == 2)
                    {
                        return 4;
                    }

                    if (i == 3)
                    {
                        return 3;
                    }

                    if (i <= 13)
                    {
                        return 2;
                    }

                    if (i <= 43)
                    {
                        return 1;
                    }
                }
            }

            return 0;
        }

        public int ReputationHeroPosition()
        {
            int i = 0;

            foreach (CharacterDTO character in ServerManager.Instance.TopReputation)
            {
                i++;

                if (character.CharacterId == CharacterId)
                {
                    return i;
                }
            }

            return 0;
        }

        public void LearnAdventurerSkills(bool isCommand = false)
        {
            if (Class == 0)
            {
                bool hasLearnedNewSkill = false;

                for (short skillVNum = 200; skillVNum <= 210; skillVNum++)
                {
                    Skill skill = ServerManager.GetSkill(skillVNum);

                    if (skill?.Class == 0 && JobLevel >= skill.LevelMinimum &&
                        !Skills.Any(s => s.SkillVNum == skillVNum))
                    {
                        hasLearnedNewSkill = true;

                        Skills[skillVNum] = new CharacterSkill
                        {
                            SkillVNum = skillVNum,
                            CharacterId = CharacterId
                        };
                    }
                }

                if (!isCommand && hasLearnedNewSkill)
                {
                    Session.SendPacket(
                        UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SKILL_LEARNED"), 0));
                    Session.SendPacket(GenerateSki());
                    Session.SendPackets(GenerateQuicklist());
                }
            }
        }

        public void LearnSPSkill()
        {
            ItemInstance specialist = null;

            if (Inventory != null)
            {
                specialist = Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            }

            byte SkillSpCount = (byte)SkillsSp.Count;

            SkillsSp = new ThreadSafeSortedList<int, CharacterSkill>();

            foreach (Skill ski in ServerManager.GetAllSkill())
            {
                if (specialist != null && ski.UpgradeType == specialist.Item.Morph &&
                    ski.SkillType == (byte)SkillType.CharacterSKill && specialist.SpLevel >= ski.LevelMinimum)
                {
                    SkillsSp[ski.SkillVNum] = new CharacterSkill { SkillVNum = ski.SkillVNum, CharacterId = CharacterId };
                }
            }

            if (SkillsSp.Count != SkillSpCount)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SKILL_LEARNED"),
                    0));
            }
        }

        public void LeaveIceBreaker()
        {
            if (IceBreaker.AlreadyFrozenPlayers != null && IceBreaker.AlreadyFrozenPlayers.Contains(Session))
            {
                IceBreaker.AlreadyFrozenPlayers.Remove(Session);
            }

            if (IceBreaker.FrozenPlayers != null && IceBreaker.FrozenPlayers.Contains(Session))
            {
                IceBreaker.FrozenPlayers.Remove(Session);
                NoMove = false;
                NoAttack = false;
                Session.SendPacket(GenerateCond());
            }
        }

        public void LeaveTalentArena(bool surrender = false)
        {
            lock (ServerManager.Instance.ArenaTeams)
            {
                var memb = ServerManager.Instance.ArenaMembers.ToList().FirstOrDefault(s => s.Session == Session);
                if (memb != null)
                {
                    if (memb.GroupId != null)
                    {
                        ServerManager.Instance.ArenaMembers.ToList().Where(s => s.GroupId == memb.GroupId).ToList()
                            .ForEach(s =>
                            {
                                if (ServerManager.Instance.ArenaMembers.ToList()
                                    .Count(g => g.GroupId == memb.GroupId) == 2)
                                {
                                    s.GroupId = null;
                                }

                                s.Time = 300;
                                s.Session.SendPacket(UserInterfaceHelper.GenerateBSInfo(1, 2, s.Time, 8));
                                s.Session.SendPacket(
                                    s.Session.Character.GenerateSay(
                                        Language.Instance.GetMessageFromKey("ARENA_TEAM_LEAVE"), 11));
                            });
                    }

                    ServerManager.Instance.ArenaMembers.Remove(memb);
                    Session.SendPacket(UserInterfaceHelper.GenerateBSInfo(2, 2, 0, 0));
                }

                ConcurrentBag<ArenaTeamMember> tm = ServerManager.Instance.ArenaTeams.ToList()
                    .FirstOrDefault(s => s.Any(o => o.Session == Session));
                Session.SendPacket(Session.Character.GenerateTaM(1));
                if (tm == null)
                {
                    return;
                }

                var tmem = tm.FirstOrDefault(s => s.Session == Session);
                if (tmem != null)
                {
                    tmem.Dead = true;
                    if (surrender)
                    {
                        Session.Character.TalentSurrender++;
                    }

                    Session.SendPacket(Session.Character.GenerateTaP(1, true));
                    Session.SendPacket("ta_sv 1");
                    Session.SendPacket("taw_sv 1");
                }

                if (UseSp)
                {
                    SkillsSp.ForEach(c => c.LastUse = DateTime.Now.AddDays(-1));
                }
                else
                {
                    Skills.ForEach(c => c.LastUse = DateTime.Now.AddDays(-1));
                }

                Session.SendPacket(GenerateSki());
                Session.SendPackets(GenerateQuicklist());

                List<BuffType> bufftodisable = new List<BuffType> { BuffType.Bad };
                Session.Character.DisableBuffs(bufftodisable);
                Session.Character.RemoveBuff(491);

                Session.Character.Hp = (int)Session.Character.HPLoad();
                Session.Character.Mp = (int)Session.Character.MPLoad();
                ServerManager.Instance.ArenaTeams.Remove(tm);
                tm.RemoveWhere(s => s.Session != Session, out tm);
                if (tm.Any())
                {
                    ServerManager.Instance.ArenaTeams.Add(tm);
                }

                tm.ToList().ForEach(s =>
                {
                    if (s.ArenaTeamType == tmem.ArenaTeamType)
                    {
                        s.Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                            string.Format(Language.Instance.GetMessageFromKey("ARENA_TALENT_LEFT"),
                                Session.Character.Name), 0));
                    }

                    s.Session.SendPacket(s.Session.Character.GenerateTaP(2, true));
                });
            }
        }

        public void LoadInventory()
        {
            IEnumerable<ItemInstanceDTO> inventories = DAOFactory.ItemInstanceDAO.LoadByCharacterId(CharacterId)
                .Where(s => s.Type != InventoryType.FamilyWareHouse).ToList();
            IEnumerable<CharacterDTO> characters = DAOFactory.CharacterDAO.LoadAllByAccount(Session.Account.AccountId);
            IEnumerable<Guid> warehouseInventoryIds = new List<Guid>();

            foreach (CharacterDTO character in characters.Where(s => s.CharacterId != CharacterId))
            {
                IEnumerable<ItemInstanceDTO> characterWarehouseInventory = DAOFactory.ItemInstanceDAO
                    .LoadByCharacterId(character.CharacterId).Where(s => s.Type == InventoryType.Warehouse).ToList();
                inventories = inventories.Concat(characterWarehouseInventory);
                warehouseInventoryIds =
                    warehouseInventoryIds.Concat(characterWarehouseInventory.Select(i => i.Id).ToList());
            }

            DAOFactory.ItemInstanceDAO.DeleteGuidList(warehouseInventoryIds);

            Inventory = new Inventory(this);
            foreach (ItemInstanceDTO inventory in inventories)
            {
                inventory.CharacterId = CharacterId;
                Inventory[inventory.Id] = new ItemInstance(inventory);
            }
        }

        public void LoadQuicklists()
        {
            QuicklistEntries = new List<QuicklistEntryDTO>();
            foreach (QuicklistEntryDTO qle in DAOFactory.QuicklistEntryDAO.LoadByCharacterId(CharacterId).ToList())
            {
                QuicklistEntries.Add(qle);
            }
        }

        public void LoadSentMail()
        {
            foreach (MailDTO mail in DAOFactory.MailDAO.LoadSentByCharacter(CharacterId))
            {
                MailList.Add((MailList.Count > 0 ? MailList.OrderBy(s => s.Key).Last().Key : 0) + 1, mail);

                Session.SendPacket(GeneratePost(mail, 2));
            }
        }

        public void LoadSkills()
        {
            Skills = new ThreadSafeSortedList<int, CharacterSkill>();
            IEnumerable<CharacterSkillDTO> characterskillDTO =
                DAOFactory.CharacterSkillDAO.LoadByCharacterId(CharacterId).ToList();
            foreach (CharacterSkillDTO characterskill in characterskillDTO.OrderBy(s => s.SkillVNum))
            {
                if (!Skills.ContainsKey(characterskill.SkillVNum))
                {
                    Skills[characterskill.SkillVNum] = new CharacterSkill(characterskill);
                }
            }
        }

        public void LoadPartnerSkills(bool isSpecialist = false)
        {
            var mate = Session.Character.Mates.Find(m => m.IsAlive && m.IsTeamMember && m.MateType == MateType.Partner);

            if (mate != null)
            {
                var skill = MateHelper.Instance.PartnerSkills.FirstOrDefault(k => k.Key == mate.NpcMonsterVNum).Value;

                if (skill != 0)
                {
                    if (!isSpecialist)
                    {
                        Skills[skill] = new CharacterSkill
                        {
                            SkillVNum = skill,
                            CharacterId = CharacterId
                        };
                        skill++;
                        Skills[skill] = new CharacterSkill
                        {
                            SkillVNum = skill,
                            CharacterId = CharacterId
                        };
                    }
                    else
                    {

                        SkillsSp[skill] = new CharacterSkill
                        {
                            SkillVNum = skill,
                            CharacterId = CharacterId
                        };
                        skill++;
                        SkillsSp[skill] = new CharacterSkill
                        {
                            SkillVNum = skill,
                            CharacterId = CharacterId
                        };
                    }

                    Session.SendPacket(GenerateSki());
                    Session.SendPackets(GenerateQuicklist());
                }
            }
        }

        public void LoadSpeed()
        {
            lock (SpeedLockObject)
            {
                // only load speed if you dont use custom speed
                if (!IsVehicled && !IsCustomSpeed)
                {
                    Speed = CharacterHelper.SpeedData[(byte)Class];

                    if (UseSp)
                    {
                        ItemInstance specialist =
                            Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);

                        if (specialist?.Item != null)
                        {
                            Speed += specialist.Item.Speed;
                        }
                    }

                    byte fixSpeed = (byte)GetBuff(CardType.Move, (byte)AdditionalTypes.Move.SetMovement)[0];

                    if (fixSpeed != 0)
                    {
                        Speed = fixSpeed;
                    }
                    else
                    {
                        Speed += (byte)GetBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementSpeedIncreased)[0];
                        Speed -= (byte)GetBuff(CardType.Move, (byte)AdditionalTypes.Move.MovementSpeedDecreased)[0];
                        Speed = (byte)(Speed + ((Speed / 100D) *
                                                 (GetBuff(CardType.Move,
                                                     (byte)AdditionalTypes.Move.MoveSpeedIncreased)[0])));
                        Speed = (byte)(Speed - ((Speed / 100D) *
                                                 (GetBuff(CardType.Move,
                                                     (byte)AdditionalTypes.Move.MoveSpeedDecreased)[0])));
                    }
                }

                if (IsShopping)
                {
                    Speed = 0;
                    IsCustomSpeed = false;
                    return;
                }

                // reload vehicle speed after opening an shop for instance
                if (IsVehicled && !IsCustomSpeed)
                {
                    Speed = VehicleSpeed;

                    if (VehicleItem != null)
                    {
                        if (MapInstance?.Map?.MapTypes != null && VehicleItem.MapSpeedBoost != null &&
                            VehicleItem.ActSpeedBoost != null)
                        {
                            Speed += VehicleItem.MapSpeedBoost[MapInstance.Map.MapId];
                            if (MapInstance.Map.MapTypes.Any(s => new[]
                            {
                                (short) MapTypeEnum.Act1, (short) MapTypeEnum.CometPlain, (short) MapTypeEnum.Mine1,
                                (short) MapTypeEnum.Mine2, (short) MapTypeEnum.MeadowOfMine,
                                (short) MapTypeEnum.SunnyPlain, (short) MapTypeEnum.Fernon, (short) MapTypeEnum.FernonF,
                                (short) MapTypeEnum.Cliff
                            }.Contains(s.MapTypeId)))
                            {
                                Speed += VehicleItem.ActSpeedBoost[1];
                            }
                            else if (MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act2))
                            {
                                Speed += VehicleItem.ActSpeedBoost[2];
                            }
                            else if (MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act3))
                            {
                                Speed += VehicleItem.ActSpeedBoost[3];
                            }
                            else if (MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act4))
                            {
                                Speed += VehicleItem.ActSpeedBoost[4];
                            }
                            else if (MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act51))
                            {
                                Speed += VehicleItem.ActSpeedBoost[51];
                            }
                            else if (MapInstance.Map.MapTypes.Any(s => s.MapTypeId == (short)MapTypeEnum.Act52))
                            {
                                Speed += VehicleItem.ActSpeedBoost[52];
                            }
                        }

                        if (HasBuff(CardType.Move, (byte)AdditionalTypes.Move.TempMaximized))
                        {
                            Speed += VehicleItem.SpeedBoost;
                        }
                    }
                }
            }
        }

        public double MPLoad() => BattleEntity.MPLoad();

        public bool MuteMessage()
        {
            PenaltyLogDTO penalty = Session.Account.PenaltyLogs.OrderByDescending(s => s.DateEnd).FirstOrDefault();

            if (IsMuted() && penalty != null)
            {
                Session.CurrentMapInstance?.Broadcast(Gender == GenderType.Female
                    ? GenerateSay(Language.Instance.GetMessageFromKey("MUTED_FEMALE"), 1)
                    : GenerateSay(Language.Instance.GetMessageFromKey("MUTED_MALE"), 1));
                Session.SendPacket(GenerateSay(
                    string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"),
                        (penalty.DateEnd - DateTime.Now).ToString(@"hh\:mm\:ss")), 11));
                Session.SendPacket(GenerateSay(
                    string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"),
                        (penalty.DateEnd - DateTime.Now).ToString(@"hh\:mm\:ss")), 12));
                return true;
            }

            return false;
        }

        public string OpenFamilyWarehouse()
        {
            if (Family == null || Family.WarehouseSize == 0)
            {
                return UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NO_FAMILY_WAREHOUSE"));
            }
            IsUsingFamilyWarehouse = true;
            return this.GenerateFStashAll();
        }

        public List<string> OpenFamilyWarehouseHist()
        {
            List<string> packetList = new List<string>();
            if (Family == null || !(FamilyCharacter.Authority == FamilyAuthority.Head
                                    || FamilyCharacter.Authority == FamilyAuthority.Familydeputy
                                    || (FamilyCharacter.Authority == FamilyAuthority.Member &&
                                        Family.MemberCanGetHistory)
                                    || (FamilyCharacter.Authority == FamilyAuthority.Familykeeper &&
                                        Family.ManagerCanGetHistory)))
            {
                packetList.Add(UserInterfaceHelper.GenerateInfo(Language.Instance.GetMessageFromKey("NO_FAMILY_RIGHT")));
                return packetList;
            }

            return this.GenerateFamilyWarehouseHist();
        }

        public void RemoveBuff(short cardId, bool removePermaBuff = false) => BattleEntity.RemoveBuff(cardId, removePermaBuff);
        public void RemoveBuffList(short cardId, bool removePermaBuff = false) => BattleEntity.RemoveBuff(cardId, removePermaBuff);

        public void RemoveBuffByBCardTypeSubType(List<KeyValuePair<byte, byte>> bcardTypes)
        {
            bcardTypes.ForEach(bt =>
                Buff.Where(b => b.Card.BCards.Any(s =>
                        s.Type.Equals((byte)bt.Key) && s.SubType.Equals((byte)(bt.Value)) &&
                        (s.CastType == 0 || b.Start.AddMilliseconds(b.Card.Delay * 100 + 1500) < DateTime.Now)))
                    .ToList()
                    .ForEach(a => RemoveBuff(a.Card.CardId)));
        }

        public void RemoveQuest(long questId, bool IsGivingUp = false)
        {
            CharacterQuest questToRemove = Quests.FirstOrDefault(q => q.QuestId == questId);

            if (questToRemove == null)
            {
                return;
            }

            if (questToRemove.Quest.TargetMap != null)
            {
                Session.SendPacket(questToRemove.Quest.RemoveTargetPacket());
            }

            Quests.RemoveWhere(s => s.QuestId != questId, out ConcurrentBag<CharacterQuest> tmp);
            Quests = tmp;

            Session.SendPacket(GenerateQuestsPacket());

            if (IsGivingUp)
            {
                return;
            }

            if (questToRemove.Quest.EndDialogId != null)
            {
                Session.SendPacket(GenerateNpcDialog((int)questToRemove.Quest.EndDialogId));
            }

            if (questToRemove.Quest.NextQuestId != null)
            {
                AddQuest((long)questToRemove.Quest.NextQuestId, questToRemove.IsMainQuest);
            }

            LogHelper.Instance.InsertQuestLog(CharacterId, Session.IpAddress, questToRemove.Quest.QuestId, DateTime.Now);

            Session.SendPacket(GetSqst());
        }

        public bool RemoveSp(short vnum, bool forced)
        {
            if (Session?.HasSession == true && (!IsVehicled || forced))
            {
                if (Buff.Any(s => s.Card.BuffType == BuffType.Bad) && !forced)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(
                        Language.Instance.GetMessageFromKey("CANT_UNTRASFORM_WITH_DEBUFFS"),
                        0));
                    return false;
                }

                FishingSpotsMapId = 1;
                FishingSpotsMapX = ServerManager.RandomNumber<short>(78, 81);
                FishingSpotsMapY = ServerManager.RandomNumber<short>(114, 118);
                LastTransform = DateTime.Now;
                DisableBuffs(BuffType.All);

                EquipmentBCards.RemoveAll(s => s.ItemVNum.Equals(vnum));

                UseSp = false;
                WingsThread.RemoveBuff(Session);
                CharacterHelper.RemoveDragonBuff(Session);
                LoadSpeed();
                Session.SendPacket(GenerateCond());
                Session.SendPacket(GenerateLev());
                SpCooldown = 30;
                if (SkillsSp != null)
                {
                    foreach (CharacterSkill ski in SkillsSp.Where(s => !s.CanBeUsed()))
                    {
                        short time = ski.Skill.Cooldown;
                        double temp = (ski.LastUse - DateTime.Now).TotalMilliseconds + (time * 100);
                        temp /= 1000;
                        SpCooldown = temp > SpCooldown
                            ? (int)temp
                            : SpCooldown;
                    }
                }

                if (Authority >= AuthorityType.User || forced)
                {
                    SpCooldown = 10;
                }

                if (Authority >= AuthorityType.ADMIN || forced)
                {
                    SpCooldown = 1;
                }

                if (SpCooldown > 0)
                {
                    Session.SendPacket(GenerateSay(
                        string.Format(Language.Instance.GetMessageFromKey("STAY_TIME"), SpCooldown), 11));
                    Session.SendPacket($"sd {SpCooldown}");
                }

                Session.CurrentMapInstance?.Broadcast(GenerateCMode());
                Session.CurrentMapInstance?.Broadcast(
                    UserInterfaceHelper.GenerateGuri(6, 0, CharacterId), PositionX,
                    PositionY);

                // ms_c
                Session.SendPacket(GenerateSki());
                Session.SendPackets(GenerateQuicklist());
                Session.SendPacket(GenerateStat());
                Session.SendPackets(GenerateStatChar());
                BattleEntity.RemoveOwnedMonsters();
                LoadPartnerSkills();

                //Make sure that ftpt is being released even though it's removed by Client
                ResetState();

                //LOGGER //LOGGER($"[TRANSFORM] {Session.GenerateIdentity} | Cooldown: {SpCooldown}");
                if (SpCooldown > 0)
                {
                    Observable.Timer(TimeSpan.FromMilliseconds(SpCooldown * 1000)).Subscribe(o =>
                    {
                        Session.SendPacket(
                            GenerateSay(Language.Instance.GetMessageFromKey("TRANSFORM_DISAPPEAR"), 11));
                        Session.SendPacket("sd 0");
                    });
                }

                RemoveTemporalMates();
            }

            return true;
        }

        public void RemoveTemporalMates()
        {
            Mates.Where(s => s.IsTemporalMate).ToList().ForEach(m =>
            {
                m.GetInventory().ForEach(s => { Inventory.Remove(s.Id); });
                Mates.Remove(m);
                byte i = 0;
                Mates.Where(s => s.MateType == MateType.Partner).ToList().ForEach(s =>
                {
                    s.GetInventory().ForEach(item => item.Type = (InventoryType)(13 + i));
                    s.PetId = i;
                    i++;
                });
                Session.SendPacket(UserInterfaceHelper.GeneratePClear());
                Session.SendPackets(GenerateScP());
                Session.SendPackets(GenerateScN());
                MapInstance.Broadcast(m.GenerateOut());
            });
        }

        public void RemoveUltimatePoints(short points)
        {
            UltimatePoints -= points;

            if (UltimatePoints < 0)
            {
                UltimatePoints = 0;
            }

            if (UltimatePoints < 3000)
            {
                RemoveBuff(729);
                RemoveBuff(727);
                AddBuff(new Buff(728, 10, false), BattleEntity);
            }

            if (UltimatePoints < 2000)
            {
                RemoveBuff(728);
                RemoveBuff(729);
                AddBuff(new Buff(727, 10, false), BattleEntity);
            }

            if (UltimatePoints < 1000)
            {
                RemoveBuff(727);
                RemoveBuff(728);
                RemoveBuff(729);
            }

            Session.SendPacket(GenerateFtPtPacket());
            Session.SendPackets(GenerateQuicklist());
        }

        public void RemoveVehicle()
        {
            RemoveBuff(336);
            ItemInstance sp = null;
            if (Inventory != null)
            {
                sp = Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            }

            IsVehicled = false;
            VehicleItem = null;
            LoadSpeed();
            if (UseSp)
            {
                if (sp != null)
                {
                    Morph = sp.Item.Morph;
                    MorphUpgrade = sp.Upgrade;
                    MorphUpgrade2 = sp.Design;
                }
            }
            else
            {
                Morph = 0;
            }

            Session.CurrentMapInstance?.Broadcast(GenerateCMode());
            Session.SendPacket(GenerateCond());
            LastSpeedChange = DateTime.Now;
        }

        public void ResetSkills()
        {
            Skills.ClearAll();

            switch ((byte)Class)
            {
                case 0:
                    {
                        LearnAdventurerSkills(true);
                    }
                    break;

                case 1:
                    {
                        Session.Character.AddSkill(220);
                        Session.Character.AddSkill(221);
                        Session.Character.AddSkill(235);
                        Session.Character.AddSkill(222);
                        Session.Character.AddSkill(223);
                        Session.Character.AddSkill(224);
                        Session.Character.AddSkill(225);
                        Session.Character.AddSkill(226);
                        Session.Character.AddSkill(227);
                        Session.Character.AddSkill(228);
                        Session.Character.AddSkill(229);
                        Session.Character.AddSkill(230);
                        Session.Character.AddSkill(231);
                        Session.Character.AddSkill(232);
                        Session.Character.AddSkill(233);
                        Session.Character.AddSkill(234);
                    }
                    break;

                case 2:
                    {
                        Session.Character.AddSkill(240);
                        Session.Character.AddSkill(241);
                        Session.Character.AddSkill(236);
                        Session.Character.AddSkill(242);
                        Session.Character.AddSkill(243);
                        Session.Character.AddSkill(244);
                        Session.Character.AddSkill(245);
                        Session.Character.AddSkill(246);
                        Session.Character.AddSkill(247);
                        Session.Character.AddSkill(248);
                        Session.Character.AddSkill(249);
                        Session.Character.AddSkill(250);
                        Session.Character.AddSkill(251);
                        Session.Character.AddSkill(252);
                        Session.Character.AddSkill(253);
                        Session.Character.AddSkill(254);
                        Session.Character.AddSkill(255);
                        Session.Character.AddSkill(256);
                    }
                    break;

                case 3:
                    {
                        Session.Character.AddSkill(260);
                        Session.Character.AddSkill(261);
                        Session.Character.AddSkill(237);
                        Session.Character.AddSkill(262);
                        Session.Character.AddSkill(263);
                        Session.Character.AddSkill(264);
                        Session.Character.AddSkill(265);
                        Session.Character.AddSkill(266);
                        Session.Character.AddSkill(267);
                        Session.Character.AddSkill(268);
                        Session.Character.AddSkill(269);
                        Session.Character.AddSkill(270);
                        Session.Character.AddSkill(271);
                        Session.Character.AddSkill(272);
                        Session.Character.AddSkill(273);
                        Session.Character.AddSkill(274);
                        Session.Character.AddSkill(275);
                        Session.Character.AddSkill(276);
                        Session.Character.AddSkill(277);
                    }
                    break;

                case 4:
                    {
                        Enumerable.Range(1525, 15).ToList()
                            .ForEach(skillVNum => Session.Character.AddSkill((short)skillVNum));
                        Session.Character.AddSkill(1565);
                    }
                    break;
            }

            if (!Session.Character.UseSp)
            {
                Session.SendPacket(Session.Character.GenerateSki());
                Session.SendPackets(Session.Character.GenerateQuicklist());
            }
        }

        public void Rest()
        {
            if (LastSkillUse.AddSeconds(4) > DateTime.Now || LastDefence.AddSeconds(4) > DateTime.Now)
            {
                return;
            }

            if (!IsVehicled)
            {
                IsSitting = !IsSitting;
                Session.CurrentMapInstance?.Broadcast(GenerateRest());
                if (MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                {
                    if (Buff.ContainsKey(121) && Group != null)
                    {
                        string leecher = string.Empty;
                        string packet = string.Empty;
                        if (Group.GroupType == GroupType.Group)
                        {
                            foreach (ClientSession groupMember in Group.Sessions.Where(s =>
                                s.Character.MapInstance == MapInstance && s.Character.CharacterId != CharacterId))
                            {
                                if (!groupMember.Character.IsSitting)
                                {
                                    leecher += $"{groupMember.Character.Name}^-^";
                                }
                            }
                            if (leecher != "")
                            {

                            }
                            else
                            {

                            }
                        }
                    }
                    else
                    {

                    }
                }
                else
                {

                }
            }
            else
            {
                Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("IMPOSSIBLE_TO_USE"), 10));
            }
        }



        public void PerformItemSave(ItemInstance it)
        {
            DAOFactory.ShellEffectDAO.InsertOrUpdateFromList(it.ShellEffects, it.EquipmentSerialId);
            DAOFactory.CellonOptionDAO.InsertOrUpdateFromList(it.CellonOptions, it.EquipmentSerialId);
            DAOFactory.RuneEffectDAO.InsertOrUpdateFromList(it.RuneEffects, it.EquipmentSerialId);
            DAOFactory.FairyEnchantmentDAO.InsertOrUpdateFromList(it.FairyEnchantments, it.EquipmentSerialId);
        }

        public void SendItem(long id, short vnum, short amount, sbyte rare, byte upgrade, short design, bool isNosmall)
        {
            Item it = ServerManager.GetItem(vnum);

            if (it != null)
            {
                if (it.ItemType != ItemType.Weapon && it.ItemType != ItemType.Armor &&
                    it.ItemType != ItemType.Specialist && it.EquipmentSlot != EquipmentType.Gloves &&
                    it.EquipmentSlot != EquipmentType.Boots)
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

                // maximum size of the amount is 32767
                if (amount > InventoryConfigrationExtension.MaxItemPerSlot)
                {
                    amount = InventoryConfigrationExtension.MaxItemPerSlot;
                }

                MailDTO mail = new MailDTO
                {
                    AttachmentAmount = it.Type == InventoryType.Etc || it.Type == InventoryType.Main
                        ? amount
                        : (short)1,
                    IsOpened = false,
                    Date = DateTime.Now,
                    ReceiverId = id,
                    SenderId = CharacterId,
                    AttachmentRarity = (byte)rare,
                    AttachmentUpgrade = upgrade,
                    AttachmentDesign = design,
                    IsSenderCopy = false,
                    Title = isNosmall ? "NOSMALL" : Name,
                    AttachmentVNum = vnum,
                    SenderClass = Class,
                    SenderGender = Gender,
                    SenderHairColor = HairColor,
                    SenderHairStyle = HairStyle,
                    EqPacket = GenerateEqListForPacket(),
                    SenderMorphId = Morph == 0 ? (short)-1 : (short)(Morph > short.MaxValue ? 0 : Morph)
                };
                MailServiceClient.Instance.SendMail(mail);
            }
        }

        public void SetInvisible(bool invisible)
        {
            Invisible = invisible;

            if (!Invisible)
            {
                Buff?.GetAllItems().Where(b => b.Card?.BCards != null && b.Card.BCards.Any(bc =>
                        bc.Type == (byte)CardType.SpecialActions
                        && bc.SubType == (byte)AdditionalTypes.SpecialActions.Hide))
                    .ToList().ForEach(b => RemoveBuff(b.Card.CardId));
            }

            if (MapInstance != null)
            {
                Mates?.Where(m => m.IsTeamMember).ToList().ForEach(m => MapInstance.Broadcast(Invisible ? m.GenerateOut() : m.GenerateIn()));
                MapInstance.Broadcast(GenerateInvisible());
            }
        }

        public void SetRespawnPoint(short mapId, short mapX, short mapY)
        {
            if (Session.HasCurrentMapInstance && Session.CurrentMapInstance.Map.MapTypes.Count > 0)
            {
                long? respawnmaptype = Session.CurrentMapInstance.Map.MapTypes[0].RespawnMapTypeId;
                if (respawnmaptype != null)
                {
                    RespawnDTO resp = Respawns.Find(s => s.RespawnMapTypeId == respawnmaptype);
                    if (resp == null)
                    {
                        resp = new RespawnDTO
                        {
                            CharacterId = CharacterId,
                            MapId = mapId,
                            X = mapX,
                            Y = mapY,
                            RespawnMapTypeId = (long)respawnmaptype
                        };
                        Respawns.Add(resp);
                    }
                    else
                    {
                        resp.X = mapX;
                        resp.Y = mapY;
                        resp.MapId = mapId;
                    }
                }
            }
        }

        public void SetReturnPoint(short mapId, short mapX, short mapY)
        {
            if (Session.HasCurrentMapInstance && Session.CurrentMapInstance.Map.MapTypes.Count > 0)
            {
                long? respawnmaptype = Session.CurrentMapInstance.Map.MapTypes[0].ReturnMapTypeId;
                if (respawnmaptype != null)
                {
                    RespawnDTO resp = Respawns.Find(s => s.RespawnMapTypeId == respawnmaptype);
                    if (resp == null)
                    {
                        resp = new RespawnDTO
                        {
                            CharacterId = CharacterId,
                            MapId = mapId,
                            X = mapX,
                            Y = mapY,
                            RespawnMapTypeId = (long)respawnmaptype
                        };
                        Respawns.Add(resp);
                    }
                    else
                    {
                        resp.X = mapX;
                        resp.Y = mapY;
                        resp.MapId = mapId;
                    }
                }
            }
            else if (Session.HasCurrentMapInstance && Session.CurrentMapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
            {
                RespawnDTO resp = Respawns.Find(s => s.RespawnMapTypeId == 1);
                if (resp == null)
                {
                    resp = new RespawnDTO
                    { CharacterId = CharacterId, MapId = mapId, X = mapX, Y = mapY, RespawnMapTypeId = 1 };
                    Respawns.Add(resp);
                }
                else
                {
                    resp.X = mapX;
                    resp.Y = mapY;
                    resp.MapId = mapId;
                }
            }
        }

        public void SetSeal()
        {
            Hp = 0;
            Mp = 0;
            MapInstance.Broadcast(GenerateRevive());
            MapInstance.Broadcast(Session, $"c_mode 1 {CharacterId} 1564 0 0 0");
            IsSeal = true;
            SealDisposable?.Dispose();
            SealDisposable = Observable.Timer(TimeSpan.FromMilliseconds(5000)).Subscribe(o =>
            {
                short x = (short)(39 + ServerManager.RandomNumber(-2, 3));
                short y = (short)(42 + ServerManager.RandomNumber(-2, 3));

                IsSeal = false;

                Hp = (int)HPLoad();
                Mp = (int)MPLoad();
                if (Faction == FactionType.Angel)
                {
                    ServerManager.Instance.ChangeMap(CharacterId, 130, x, y);
                }
                else if (Faction == FactionType.Demon)
                {
                    ServerManager.Instance.ChangeMap(CharacterId, 131, x, y);
                }
                else
                {
                    MapId = 145;
                    MapX = 51;
                    MapY = 41;
                    string connection =
                        CommunicationServiceClient.Instance.RetrieveOriginWorld(Session.Account.AccountId);
                    if (string.IsNullOrWhiteSpace(connection))
                    {
                        return;
                    }

                    int port = Convert.ToInt32(connection.Split(':')[1]);
                    Session.Character.Event.EmitEvent(new PlayerChangeChannelEvent(connection.Split(':')[0], port, 3));
                    return;
                }

                MapInstance?.Broadcast(Session, GenerateTp());
                MapInstance?.Broadcast(GenerateRevive());
                Session.SendPacket(GenerateStat());
            });
        }

        public void StandUp()
        {
            if (!IsVehicled && IsSitting)
            {
                IsSitting = false;
                MapInstance?.Broadcast(GenerateRest());

            }
        }

        public void TeleportToDir(int Dir, int Distance)
        {
            WalkDisposable?.Dispose();
            short NewX = PositionX;
            short NewY = PositionY;
            bool BlockedZone = false;
            for (short i = 1;
                Map.GetDistance(new MapCell { X = PositionX, Y = PositionY }, new MapCell { X = NewX, Y = NewY }) <
                Math.Abs(Distance) && i < +Math.Abs(Distance) + 5 && !BlockedZone;
                i++)
            {
                switch (Dir)
                {
                    case 0:
                        if (!MapInstance.Map.IsBlockedZone(NewX, NewY - i))
                        {
                            NewX = PositionX;
                            NewY = (short)(PositionY - i);
                        }
                        else
                        {
                            BlockedZone = true;
                        }

                        break;

                    case 1:
                        if (!MapInstance.Map.IsBlockedZone(NewX + i, NewY))
                        {
                            NewX = (short)(PositionX + i);
                            NewY = PositionY;
                        }
                        else
                        {
                            BlockedZone = true;
                        }

                        break;

                    case 2:
                        if (!MapInstance.Map.IsBlockedZone(NewX, NewY + i))
                        {
                            NewX = PositionX;
                            NewY = (short)(PositionY + i);
                        }
                        else
                        {
                            BlockedZone = true;
                        }

                        break;

                    case 3:
                        if (!MapInstance.Map.IsBlockedZone(NewX - i, NewY))
                        {
                            NewX = (short)(PositionX - i);
                            NewY = PositionY;
                        }
                        else
                        {
                            BlockedZone = true;
                        }

                        break;

                    case 4:
                        if (!MapInstance.Map.IsBlockedZone(NewX - i, NewY - i))
                        {
                            NewX = (short)(PositionX - i);
                            NewY = (short)(PositionY - i);
                        }
                        else
                        {
                            BlockedZone = true;
                        }

                        break;

                    case 5:
                        if (!MapInstance.Map.IsBlockedZone(NewX + i, NewY - i))
                        {
                            NewX = (short)(PositionX + i);
                            NewY = (short)(PositionY - i);
                        }
                        else
                        {
                            BlockedZone = true;
                        }

                        break;

                    case 6:
                        if (!MapInstance.Map.IsBlockedZone(NewX + i, NewY + i))
                        {
                            NewX = (short)(PositionX + i);
                            NewY = (short)(PositionY + i);
                        }
                        else
                        {
                            BlockedZone = true;
                        }

                        break;

                    case 7:
                        if (!MapInstance.Map.IsBlockedZone(NewX - i, NewY + i))
                        {
                            NewX = (short)(PositionX - i);
                            NewY = (short)(PositionY + i);
                        }
                        else
                        {
                            BlockedZone = true;
                        }

                        break;
                }
            }

            PositionX = NewX;
            PositionY = NewY;
            MapInstance.Broadcast(GenerateTp());
        }
        public void TeleportOnMap(short x, short y)
        {
            if (!MapInstance.Map.IsBlockedZone(x, y))
            {
                Session.Character.PositionX = x;
                Session.Character.PositionY = y;
                Session.CurrentMapInstance?.Broadcast($"tp 1 {CharacterId} {x} {y} 0");
            }
            Session.SendPacket(GenerateCond());
        }

        public void UpdateBushFire() => BattleEntity.UpdateBushFire();

        public bool WeaponLoaded(CharacterSkill ski)
        {
            if (ski != null)
            {
                switch (Class)
                {
                    default:
                        return false;

                    case ClassType.Adventurer:
                        if (ski.Skill.Type == 1 && Inventory != null)
                        {
                            ItemInstance wearable = Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
                            if (wearable != null)
                            {
                                if (wearable.Ammo > 0)
                                {
                                    wearable.Ammo--;
                                    return true;
                                }

                                if (Inventory.CountItem(2081) < 1)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_AMMO_ADVENTURER"), 10));
                                    return false;
                                }

                                Inventory.RemoveItemAmount(2081);
                                wearable.Ammo = 100;
                                Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("AMMO_LOADED_ADVENTURER"), 10));
                                return true;
                            }

                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_WEAPON"), 10));
                            return false;
                        }

                        return true;

                    case ClassType.Swordsman:
                        if (ski.Skill.Type == 1 && Inventory != null)
                        {
                            ItemInstance inv = Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
                            if (inv != null)
                            {
                                if (inv.Ammo > 0)
                                {
                                    inv.Ammo--;
                                    return true;
                                }

                                if (Inventory.CountItem(2082) < 1)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_AMMO_SWORDSMAN"), 10));
                                    return false;
                                }

                                Inventory.RemoveItemAmount(2082);
                                inv.Ammo = 100;
                                Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("AMMO_LOADED_SWORDSMAN"), 10));
                                return true;
                            }

                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_WEAPON"), 10));
                            return false;
                        }

                        return true;

                    case ClassType.Archer:
                        if (ski.Skill.Type == 1 && Inventory != null)
                        {
                            ItemInstance inv = Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
                            if (inv != null)
                            {
                                if (inv.Ammo > 0)
                                {
                                    inv.Ammo--;
                                    return true;
                                }

                                if (Inventory.CountItem(2083) < 1)
                                {
                                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_AMMO_ARCHER"), 10));
                                    return false;
                                }

                                Inventory.RemoveItemAmount(2083);
                                inv.Ammo = 100;
                                Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("AMMO_LOADED_ARCHER"), 10));
                                return true;
                            }

                            Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_WEAPON"), 10));
                            return false;
                        }

                        return true;

                    case ClassType.Magician:
                        if (ski.Skill.Type == 1 && Inventory != null)
                        {
                            ItemInstance inv = Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
                            if (inv == null)
                            {
                                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("NO_WEAPON"), 10));
                                return false;
                            }
                        }

                        return true;

                    case ClassType.MartialArtist:
                        return true;
                }
            }

            return false;
        }

        internal void RefreshValidity()
        {
            if (StaticBonusList.RemoveAll(
                s => s.StaticBonusType == StaticBonusType.BackPack && s.DateEnd < DateTime.Now) > 0)
            {
                Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("ITEM_TIMEOUT"), 10));
                Session.SendPacket(GenerateExts());
            }

            if (StaticBonusList.RemoveAll(
                s => s.StaticBonusType == StaticBonusType.ArenaWinner && s.DateEnd < DateTime.Now) > 0)
            {
                Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("ITEM_TIMEOUT"), 10));
                Session.Character.ArenaWinner = 0;
                Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateCMode());
            }

            if (StaticBonusList.RemoveAll(s => s.DateEnd < DateTime.Now) > 0)
            {
                Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("ITEM_TIMEOUT"), 10));
            }

            if (Inventory != null)
            {
                foreach (object suit in Enum.GetValues(typeof(EquipmentType)))
                {
                    ItemInstance item = Inventory.LoadBySlotAndType((byte)suit, InventoryType.Wear);

                    bool isAmulet = false;

                    if (item?.Item.EquipmentSlot == EquipmentType.Amulet && (item?.Item.Effect == 791 || item?.Item.Effect == 792 || item?.Item.Effect == 793 || item?.Item.Effect == 794 || item?.Item.Effect == 795 || item?.Item.Effect == 796))
                    {
                        isAmulet = true;
                    }

                    if (item?.DurabilityPoint > 0 && !isAmulet)
                    {
                        item.DurabilityPoint--;
                        if (item.DurabilityPoint == 0)
                        {
                            Inventory.DeleteById(item.Id);
                            Session.SendPackets(GenerateStatChar());
                            Session.CurrentMapInstance?.Broadcast(GenerateEq());
                            Session.SendPacket(GenerateEquipment());
                            Session.SendPacket(GenerateSay(Language.Instance.GetMessageFromKey("ITEM_TIMEOUT"), 10));
                        }
                    }
                }
            }
        }

        internal void SetSession(ClientSession clientSession) => Session = clientSession;

        private void GenerateHeroXpLevelUp()
        {
            double t = HeroXPLoad();
            while (HeroXp >= t)
            {
                HeroXp -= (long)t;
                HeroLevel++;

                t = HeroXPLoad();
                if (HeroLevel >= GameConfiguration.MaxHeroLevel)
                {
                    HeroLevel = GameConfiguration.MaxHeroLevel;
                    HeroXp = 0;
                }

                Hp = (int)HPLoad();
                Mp = (int)MPLoad();
                Session.SendPacket(GenerateStat());
                Session.SendPacket(GenerateLevelUp());
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("HERO_LEVELUP"),
                    0));
                Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 8),
                    PositionX, PositionY);
                Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 198),
                    PositionX, PositionY);
                if (Family != null)
                {
                    Family.InsertFamilyLog(FamilyLogType.HeroLevelUp, Name, level: HeroLevel);
                }
                switch (HeroLevel)
                {
                    case 10:
                        switch (Session.Character.Class)
                        {
                            case ClassType.Swordsman:
                                Session.Character.SendItem(CharacterId, 4447, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4459, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4475, 1, 6, 8, 0, true);
                                break;

                            case ClassType.Archer:
                                Session.Character.SendItem(CharacterId, 4450, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4466, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4478, 1, 6, 8, 0, true);
                                break;

                            case ClassType.Magician:
                                Session.Character.SendItem(CharacterId, 4453, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4469, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4481, 1, 6, 8, 0, true);
                                break;

                            case ClassType.MartialArtist:
                                Session.Character.SendItem(CharacterId, 4456, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4484, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4472, 1, 6, 8, 0, true);
                                break;
                        }
                        break;
                } 
            }
        }


        private void GenerateJobXpLevelUp()
        {
            var t = JobXPLoad();
            while (JobLevelXp >= t)
            {
                JobLevelXp -= (long)t;
                JobLevel++;
                //RewardsHelper.Instance.GetJobRewards(Session);
                t = JobXPLoad();
                if (JobLevel >= 20 && Class == 0)
                {
                    JobLevel = 20;
                    JobLevelXp = 0;
                }
                else if (JobLevel >= GameConfiguration.MaxJobLevel)
                {
                    JobLevel = GameConfiguration.MaxJobLevel;
                    JobLevelXp = 0;
                }

                Hp = (int)HPLoad();
                Mp = (int)MPLoad();
                Session.SendPacket(GenerateStat());
                Session.SendPacket(GenerateLevelUp());
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("JOB_LEVELUP"),
                    0));
                LearnAdventurerSkills();
                Session.SendPackets(GenerateQuicklist());
                Session.CurrentMapInstance?.Broadcast(GenerateEff(8), PositionX, PositionY);
                Session.CurrentMapInstance?.Broadcast(GenerateEff(198), PositionX, PositionY);
            }
        }

        private void GenerateLevelXpLevelUp()
        {
            var t = XpLoad();
            while (LevelXp >= t)
            {
                LevelXp -= (long)t;
                Level++;

                t = XpLoad();
                if (Level >= GameConfiguration.MaxLevel)
                {
                    Level = GameConfiguration.MaxLevel;
                    LevelXp = 0;
                }


                Hp = (int)HPLoad();
                Mp = (int)MPLoad();
                Session.SendPacket(GenerateStat());
                if (Family != null)
                {
                    if (Level > 20 && (Level % 10) == 0)
                    {
                        Family.InsertFamilyLog(FamilyLogType.LevelUp, Name, level: Level);
                        GenerateFamilyXp(20 * Level);
                    }
                    else if (Level > 80)
                    {
                        Family.InsertFamilyLog(FamilyLogType.LevelUp, Name, level: Level);
                    }
                    else
                    {
                        ServerManager.Instance.FamilyRefresh(Family.FamilyId);
                        CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage()
                        {
                            DestinationCharacterId = Family.FamilyId,
                            SourceCharacterId = CharacterId,
                            SourceWorldId = ServerManager.Instance.WorldId,
                            Message = "fhis_stc",
                            Type = MessageType.Family
                        });
                    }
                }

                Session.SendPacket(GenerateLevelUp());
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("LEVELUP"), 0));
                Session.CurrentMapInstance?.Broadcast(GenerateEff(6), PositionX, PositionY);
                Session.CurrentMapInstance?.Broadcast(GenerateEff(198), PositionX, PositionY);
                ServerManager.Instance.UpdateGroup(CharacterId);

                ClientSession session = ServerManager.Instance.GetSessionByCharacterId(CharacterId);

                // Reputation
                if (Level >= 20)
                {
                    GetReputation(50);
                }
                switch (Level)
                {
                    case 30:
                        switch (Session.Character.Class)
                        {
                            case ClassType.Swordsman:
                                Session.Character.SendItem(CharacterId, 136, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 73, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 98, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 901, 1, 0, 7, 0, true);
                                break;
                            case ClassType.Archer:
                                Session.Character.SendItem(CharacterId, 143, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 81, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 111, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 903, 1, 0, 7, 0, true);
                                break;
                            case ClassType.Magician:
                                Session.Character.SendItem(CharacterId, 150, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 89, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 124, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 905, 1, 0, 7, 0, true);
                                break;
                        }
                        Session.Character.SendItem(CharacterId, 9325, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 9074, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 9041, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 1010, 99, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 9045, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 9046, 1, 0, 0, 0, true);
                        Session.Character.Reputation += 10000;
                        break;

                    case 40:
                        switch (Session.Character.Class)
                        {
                            case ClassType.Adventurer:
                                break;
                            case ClassType.Swordsman:
                                Session.Character.SendItem(CharacterId, 262, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 291, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 165, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 902, 1, 0, 7, 0, true);
                                break;
                            case ClassType.Archer:
                                Session.Character.SendItem(CharacterId, 265, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 289, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 171, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 904, 1, 0, 7, 0, true);
                                break;
                            case ClassType.Magician:
                                Session.Character.SendItem(CharacterId, 268, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 293, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 177, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 906, 1, 0, 7, 0, true);
                                break;
                            case ClassType.MartialArtist:
                                break;
                        }
                        Session.Character.SendItem(CharacterId, 4989, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 4998, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 4870, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 4997, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 4834, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 4996, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 4833, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 4995, 1, 0, 0, 0, true);
                        Session.Character.Reputation += 10000;
                        break;

                    case 50:
                        switch (Session.Character.Class)
                        {
                            case ClassType.Adventurer:
                                break;
                            case ClassType.Swordsman:
                                Session.Character.SendItem(CharacterId, 28, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 76, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 297, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 9316, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 909, 1, 0, 7, 0, true);
                                break;
                            case ClassType.Archer:
                                Session.Character.SendItem(CharacterId, 42, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 84, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 295, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 9313, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 911, 1, 0, 7, 0, true);
                                break;
                            case ClassType.Magician:
                                Session.Character.SendItem(CharacterId, 56, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 92, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 271, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 9310, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 913, 1, 0, 7, 0, true);
                                break;
                            case ClassType.MartialArtist:
                                break;
                        }
                        Session.Character.SendItem(CharacterId, 9074, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 9041, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 9017, 99, 0, 0, 0, true);
                        Session.Character.Reputation += 10000;
                        break;

                    case 60:
                        switch (Session.Character.Class)
                        {
                            case ClassType.Adventurer:
                                break;
                            case ClassType.Swordsman:
                                Session.Character.SendItem(CharacterId, 141, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 760, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 106, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 910, 1, 0, 7, 0, true);
                                break;
                            case ClassType.Archer:
                                Session.Character.SendItem(CharacterId, 148, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 762, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 119, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 912, 1, 0, 7, 0, true);
                                break;
                            case ClassType.Magician:
                                Session.Character.SendItem(CharacterId, 155, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 764, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 132, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 914, 1, 0, 7, 0, true);
                                break;
                            case ClassType.MartialArtist:
                                break;
                        }
                        Session.Character.SendItem(CharacterId, 9074, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 9041, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 2089, 50, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 2329, 50, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 2078, 99, 0, 0, 0, true);
                        Session.Character.Reputation += 20000;
                        break;

                    case 70:
                        switch (Session.Character.Class)
                        {
                            case ClassType.Adventurer:
                                break;
                            case ClassType.Swordsman:
                                Session.Character.SendItem(CharacterId, 400, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 761, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 994, 1, 5, 8, 0, true);
                                break;
                            case ClassType.Archer:
                                Session.Character.SendItem(CharacterId, 403, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 405, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 993, 1, 5, 8, 0, true);;
                                break;
                            case ClassType.Magician:
                                Session.Character.SendItem(CharacterId, 406, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 765, 1, 5, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 989, 1, 5, 8, 0, true);
                                break;
                            case ClassType.MartialArtist:
                                break;
                        }
                        Session.Character.SendItem(CharacterId, 9074, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 9041, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 8282, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 8283, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 8291, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 4039, 1, 6, 6, 0, true);
                        Session.Character.SendItem(CharacterId, 4044, 1, 6, 6, 0, true);
                        Session.Character.Reputation += 20000;
                        break;

                    case 80:
                        switch (Session.Character.Class)
                        {
                            case ClassType.Adventurer:
                                break;
                            case ClassType.Swordsman:
                                Session.Character.SendItem(CharacterId, 401, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4006, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 409, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 418, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 421, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 424, 1, 0, 0, 0, true);
                                break;
                            case ClassType.Archer:
                                Session.Character.SendItem(CharacterId, 404, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4008, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 410, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 418, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 421, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 424, 1, 0, 0, 0, true);
                                break;
                            case ClassType.Magician:
                                Session.Character.SendItem(CharacterId, 407, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4010, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 411, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 418, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 421, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 424, 1, 0, 0, 0, true);
                                break;
                            case ClassType.MartialArtist:
                                break;
                        }
                        Session.Character.SendItem(CharacterId, 9074, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 9041, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 4503, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 4504, 1, 0, 0, 0, true);
                        Session.Character.Reputation += 20000;
                        break;

                    case 85:
                        switch (Session.Character.Class)
                        {
                            case ClassType.Adventurer:
                                break;
                            case ClassType.Swordsman:
                                Session.Character.SendItem(CharacterId, 9317, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 4001, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4007, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4013, 1, 6, 8, 0, true);
                                break;
                            case ClassType.Archer:
                                Session.Character.SendItem(CharacterId, 9314, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 4003, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4009, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4016, 1, 6, 8, 0, true);
                                break;
                            case ClassType.Magician:
                                Session.Character.SendItem(CharacterId, 9312, 1, 0, 0, 0, true);
                                Session.Character.SendItem(CharacterId, 4005, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4011, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4019, 1, 6, 8, 0, true);
                                break;
                            case ClassType.MartialArtist:
                                Session.Character.SendItem(CharacterId, 9320, 1, 0, 0, 0, true);
                                break;
                        }
                        Session.Character.SendItem(CharacterId, 9074, 1, 0, 0, 0, true);
                        Session.Character.SendItem(CharacterId, 9041, 1, 0, 0, 0, true);
                        Session.Character.Reputation += 50000;
                        break;

                    case 90:
                        switch (Session.Character.Class)
                        {
                            case ClassType.Adventurer:
                                break;
                            case ClassType.Swordsman:
                                Session.Character.SendItem(CharacterId, 4901, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4910, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4919, 1, 6, 8, 0, true);
                                break;
                            case ClassType.Archer:
                                Session.Character.SendItem(CharacterId, 4904, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4913, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4922, 1, 6, 8, 0, true);
                                break;
                            case ClassType.Magician:
                                Session.Character.SendItem(CharacterId, 4907, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4916, 1, 6, 8, 0, true);
                                Session.Character.SendItem(CharacterId, 4925, 1, 6, 8, 0, true);
                                break;

                            case ClassType.MartialArtist:
                                Session.Character.SendItem(CharacterId, 9320, 1, 0, 0, 0, true);
                                break;
                        }
                        Session.Character.SendItem(CharacterId, 5487, 1, 0, 0, 0, true);
                        break;

                    case 99:
                        switch (Session.Character.Class)
                        {
                            case ClassType.Adventurer:
                                break;
                            case ClassType.Swordsman:
                                break;
                            case ClassType.Archer:
                                break;
                            case ClassType.Magician:
                                break;
                            case ClassType.MartialArtist:
                                break;
                        }
                        Session.Character.Reputation += 10000;
                        break;

                }
            }
        }

      

        private void GenerateQuickListSp2Am(ref string[] pktQs)
        {
            var morph = Morph;
            if (Class == ClassType.MartialArtist && Morph == 30 || Morph == 29)
            {
                morph = 30;
            }

            for (var i = 0; i < 30; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    QuicklistEntryDTO qi =
                        QuicklistEntries.Find(n => n.Q1 == j && n.Q2 == i && n.Morph == (UseSp ? morph : 0));
                    var pos = qi?.Pos;
                    if (pos >= 6 && pos <= 9)
                    {
                        pos += 5;
                    }

                    pktQs[j] += $" {qi?.Type ?? 7}.{qi?.Slot ?? 7}.{pos.ToString() ?? "-1"}";
                }
            }
        }

        private void GenerateQuickListSp3Am(ref string[] pktQs)
        {
            var morph = Morph;
            if (Class == ClassType.MartialArtist && Morph == 30 || Morph == 29)
            {
                morph = 30;
            }

            for (var i = 0; i < 30; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    QuicklistEntryDTO qi =
                        QuicklistEntries.Find(n => n.Q1 == j && n.Q2 == i && n.Morph == (UseSp ? morph : 0));
                    short? pos = qi?.Pos;
                    if (pos.HasValue && pos == 3 && UltimatePoints >= 2000 || pos == 4 && UltimatePoints >= 1000 ||
                        pos == 5 && UltimatePoints >= 3000)
                    {
                        pos += 8;
                    }

                    if (pos.HasValue && pos == 10 && UltimatePoints >= 3000)
                    {
                        pos += 4;
                    }

                    pktQs[j] += $" {qi?.Type ?? 7}.{qi?.Slot ?? 7}.{pos.ToString() ?? "-1"}";
                }
            }
        }

        public List<BCard> GetFairyEnchantments()
        {
            var fairyEnchantments = new List<BCard>();
            var fairy = Inventory.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
            if (fairy != null && fairy.FairyEnchantments != null && fairy.FairyEnchantments.Any())
            {
                var fairyEffects = fairy.FairyEnchantments.ConvertAll(x => x.DeepCopy()).ToList();
                var effect = fairyEffects.Select(x => new BCard
                {
                    Type = (byte)x.Type,
                    SubType = (byte)((x.SubType * 10) + 11),
                    FirstData = x.FirstData,
                    SecondData = x.SecondData,
                    ThirdData = x.ThirdData
                });
                fairyEnchantments.AddRange(effect);
            }
            fairyEnchantments = BCardList(fairyEnchantments, CardType.A7Powers1, CardType.A7Powers2);

            return fairyEnchantments;
        }


        #region Runes

        public List<BCard> GetRunesInEquipment()
        {
            var runes = new List<BCard>();
            var weapon = Inventory.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
            if (weapon != null && weapon.RuneEffects != null && weapon.RuneEffects.Any())
            {
                var runeEffects = weapon.RuneEffects.ConvertAll(x => x.DeepCopy()).ToList();

                var rune = runeEffects.Select(x => new BCard
                {
                    Type = (byte)x.Type,
                    SubType = (byte)((x.SubType * 10) + 11),
                    FirstData = x.FirstData,
                    SecondData = x.SecondData,
                    ThirdData = x.ThirdData
                });

                runes.AddRange(rune);
            }

            var secondaryWeapon = Inventory.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
            if (secondaryWeapon != null && secondaryWeapon.RuneEffects != null && secondaryWeapon.RuneEffects.Any())
            {
                var runeEffects = secondaryWeapon.RuneEffects.ToList();

                var rune = runeEffects.Select(x => new BCard
                {
                    Type = (byte)x.Type,
                    SubType = (byte)((x.SubType * 10) + 11),
                    FirstData = x.FirstData,
                    SecondData = x.SecondData,
                    ThirdData = x.ThirdData
                });

                runes.AddRange(rune);
            }

            runes = BCardList(runes, CardType.A7Powers1, CardType.A7Powers2);

            return runes;
        }

        public List<BCard> BCardList(List<BCard> list, params CardType[] cards)
        {
            var listReturn = new List<BCard>();
            if (!list.Any()) return listReturn;

            foreach (var card in cards.ToList())
            {
                var valueToAdd = list.Where(x => cards.Contains((CardType)x.Type) && x.Type == (byte)card).ToList();
                if (valueToAdd == null || !valueToAdd.Any()) continue;

                var grouped = (from x in valueToAdd
                               group x by new { x.SubType }
                        into y
                               select new BCard
                               {
                                   Type = (byte)card,
                                   SubType = y.Key.SubType,
                                   FirstData = y.Max(x => x.FirstData),
                                   SecondData = y.Max(x => x.SecondData),
                                   ThirdData = y.Max(x => x.ThirdData)
                               }
                    ).ToList();

                listReturn.AddRange(grouped);
            }

            listReturn.AddRange(
                list.Where(x =>
                    !listReturn.Any(y =>
                        y.Type == x.Type && x.SubType == y.SubType)));

            return listReturn;
        }

        #endregion

        public void GenerateSpXpLevelUp(ItemInstance specialist)
        {
            double t = SpXpLoad();
            while (UseSp && specialist.XP >= t)
            {
                specialist.XP -= (long)t;
                specialist.SpLevel++;
                t = SpXpLoad();
                Session.SendPacket(GenerateStat());
                Session.SendPacket(GenerateLevelUp());
                if (specialist.SpLevel >= GameConfiguration.MaxSPLevel)
                {
                    specialist.SpLevel = GameConfiguration.MaxSPLevel;
                    specialist.XP = 0;
                }

                LearnSPSkill();
                SkillsSp.ForEach(s => s.LastUse = DateTime.Now.AddDays(-1));
                Session.SendPacket(GenerateSki());
                Session.SendPackets(GenerateQuicklist());

                Session.SendPacket(
                    UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("SP_LEVELUP"), 0));
                Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 8),
                    PositionX, PositionY);
                Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 198),
                    PositionX, PositionY);
            }
        }

        private void GenerateXp(MapMonster monster, Dictionary<BattleEntity, long> damageList = null, bool isGroupMember = false)
        {
            if (monster?.DamageList == null)
            {
                return;
            }

            bool isKiller = false;

            if (damageList == null)
            {
                damageList = new Dictionary<BattleEntity, long>();

                lock (monster.DamageList)
                {
                    // Deep copy monster.DamageList to damageList.

                    foreach (KeyValuePair<BattleEntity, long> keyValuePair in monster.DamageList)
                    {
                        damageList.Add(keyValuePair.Key, keyValuePair.Value);
                    }
                }

                isKiller = true;
            }

            if (Session.CurrentMapInstance?.MapInstanceType == MapInstanceType.TimeSpaceInstance)
            {
                Session?.SendPacket(Session.Character.Timespace.FirstMap.InstanceBag.GenerateScore());
            }

            Group grp = null;

            if (Group?.GroupType == GroupType.Group)
            {
                grp = Group;
            }

            bool checkMonsterOwner(long entityId, Group group)
            {
                if (damageList.FirstOrDefault(s => s.Value > 0).Key is BattleEntity monsterOwner)
                {
                    return monsterOwner.MapEntityId == entityId || monsterOwner.Mate?.Owner?.CharacterId == entityId ||
                           monsterOwner.MapMonster?.Owner?.MapEntityId == entityId ||
                           group != null && group.IsMemberOfGroup(monsterOwner.MapEntityId);
                }

                return false;
            }

            bool isMonsterOwner = checkMonsterOwner(CharacterId, grp);

            lock (monster.DamageList)
            {
                if (monster.DamageList.Any())
                {
                    monster.DamageList.Where(s => s.Key.MapEntityId == CharacterId).ToList()
                        .ForEach(s => monster.DamageList.Remove(s));

                    // Call GenerateXp() for group members.

                    if (grp?.Sessions != null && !isGroupMember)
                    {
                        foreach (ClientSession groupMember in grp.Sessions.GetAllItems().Where(g =>
                            g.Character != null && g.Character.CharacterId != CharacterId &&
                            g.Character.MapInstanceId == MapInstanceId).ToList())
                        {
                            try
                            {
                                groupMember.Character?.GenerateXp(monster, damageList, true);
                            }
                            catch (Exception e)
                            {
                                //LOGGER //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
                            }
                        }
                    }
                }

                // Call GenerateXp() for others.

                if (monster.DamageList.Any() && isKiller)
                {
                    try
                    {
                        monster.DamageList.Where(s => s.Value > 0 && s.Key.MapEntityId != BattleEntity.MapEntityId)
                            .ToList().ForEach(s => s.Key.Character?.GenerateXp(monster, damageList));
                    }
                    catch (Exception e)
                    {
                        //LOGGER //LOGGERServerLog($"{e.ToString()}", LogType.ServerError);
                    }
                }
            }

            // Exp percent regarding the damge
            double totalDamage = damageList.Sum(s => s.Value);
            double damageByCharacterOrGroup = damageList.Where(s =>
                s.Key != null && s.Key.MapEntityId == CharacterId ||
                Mates.Any(m => m.MateTransportId == s.Key.MapEntityId) ||
                grp != null && grp.IsMemberOfGroup(s.Key.MapEntityId)).Sum(s => s.Value);
            double expDamageRate = damageByCharacterOrGroup / totalDamage *
                                   (isMonsterOwner && damageList.Any(s =>
                                       s.Key != null && s.Value > 0 && s.Key.MapEntityId != CharacterId &&
                                       (grp == null || !grp.IsMemberOfGroup(s.Key.MapEntityId)))
                                       ? 1.2f
                                       : 1);

            if (double.IsNaN(expDamageRate))
            {
                expDamageRate = 0;
            }

            NpcMonster monsterInfo = monster.Monster;

            if (!Session.Account.PenaltyLogs.Any(s => s.Penalty == PenaltyType.BlockExp && s.DateEnd > DateTime.Now))
            {
                if (Hp <= 0)
                {
                    return;
                }

                if ((int)(LevelXp / (XpLoad() / 10)) <
                    (int)((LevelXp + monsterInfo.XP * expDamageRate) / (XpLoad() / 10)))
                {
                    Hp = (int)HPLoad();
                    Mp = (int)MPLoad();
                    Session.SendPacket(GenerateStat());
                    Session.SendPacket(StaticPacketHelper.GenerateEff(UserType.Player, CharacterId, 5));
                }

                ItemInstance specialist = null;

                if (Inventory != null)
                {
                    specialist = Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                }

                int xp = (int)(GetXP(monster, grp) * expDamageRate * (isMonsterOwner ? 1 : 1f) * (1 + (GetBuff(CardType.Item, (byte)AdditionalTypes.Item.EXPIncreased)[0] / 100D)) * (1 + (GetBuff(CardType.MartialArts, (byte)AdditionalTypes.MartialArts.IncreaseBattleAndJobExperience)[0] / 100)));


                if (!WorldPolicyConfiguration.DisableNormalExperience &&
                    Level < GameConfiguration.MaxLevel)
                {
                    LevelXp += xp;
                }

                foreach (Mate mate in Mates.Where(x => x.IsTeamMember && x.IsAlive))
                {
                    mate.GenerateXp(xp);

                    if (mate.IsUsingSp)
                    {
                        mate.Sp.AddXp(xp);
                        mate.Owner?.Session?.SendPacket(mate.GenerateScPacket());
                    }
                }

                if ((Class == 0 && JobLevel < 20) || (Class != 0 && JobLevel < GameConfiguration.MaxJobLevel))
                {
                    if (specialist != null && UseSp && specialist.SpLevel < GameConfiguration.MaxSPLevel && specialist.SpLevel > 19)
                    {
                        JobLevelXp += (int)(GetJXP(monster, grp) * expDamageRate * (isMonsterOwner ? 1 : 0.8f) / 2D * (1 + (GetBuff(CardType.Item, (byte)AdditionalTypes.Item.EXPIncreased)[0] / 100D)) * (1 + (GetBuff(CardType.MartialArts, (byte)AdditionalTypes.MartialArts.IncreaseBattleAndJobExperience)[0] / 100)));
                    }
                    else
                    {
                        JobLevelXp += (int)(GetJXP(monster, grp) * expDamageRate * (isMonsterOwner ? 1 : 0.8f) * (1 + (GetBuff(CardType.Item, (byte)AdditionalTypes.Item.EXPIncreased)[0] / 100D)) * (1 + (GetBuff(CardType.MartialArts, (byte)AdditionalTypes.MartialArts.IncreaseBattleAndJobExperience)[0] / 100)));
                    }
                }

                if (specialist != null && UseSp && specialist.SpLevel < GameConfiguration.MaxSPLevel)
                {
                    int multiplier = specialist.SpLevel < 10 ? 10 : specialist.SpLevel < 19 ? 5 : 1;

                    var bonusXp = (int)(GetJXP(monster, grp) * expDamageRate * (multiplier + (GetBuff(CardType.Item, (byte)AdditionalTypes.Item.EXPIncreased)[0] / 100D + (GetBuff(CardType.Item, (byte)AdditionalTypes.Item.IncreaseSPXP)[0] / 100D))));
                    specialist.XP += bonusXp;
                }

                if (!WorldPolicyConfiguration.DisableHeroExperience &&
                    HeroLevel > 0 && HeroLevel < GameConfiguration.MaxHeroLevel)
                {
                    HeroXp += (int)((GetHXP(monster, grp) * (isMonsterOwner ? 1 : 0.8f) * (1 + (GetBuff(CardType.Item, (byte)AdditionalTypes.Item.EXPIncreased)[0] / 100D) + (GetBuff(CardType.Dracula, (byte)AdditionalTypes.Dracula.ExpHeroIncrease)[0] / 100D))));
                }

                ItemInstance fairy = Inventory?.LoadBySlotAndType((byte)EquipmentType.Fairy, InventoryType.Wear);
                ItemInstance mainWeapon = Inventory?.LoadBySlotAndType((byte)EquipmentType.MainWeapon, InventoryType.Wear);
                ItemInstance secondWeapon = Inventory?.LoadBySlotAndType((byte)EquipmentType.SecondaryWeapon, InventoryType.Wear);
                BattleEntity attackerBattleEntity = new BattleEntity(Session.Character, LastSkillType);

                if (fairy != null)
                {
                    double experience = CharacterHelper.LoadFairyXPData(fairy.ElementRate, fairy.Item.ElementRate) - fairy.XP;
                    byte Factor = 1;
                    if (Session.Character.HasBuff(393))
                        Factor = 2;
                    if (fairy.ElementRate < fairy.Item.MaxElementRate
                        && Level <= monsterInfo.Level + 15 && Level >= monsterInfo.Level - 15)
                    {
                        fairy.XP += GameConfiguration.FairyXPRate;
                        fairy.XP += GameConfiguration.FairyXPRate / 100 * Factor;
                    }

                    while (fairy.XP >= experience)
                    {
                        if (fairy.ElementRate != fairy.Item.MaxElementRate)
                        {
                            fairy.XP = 0;
                            fairy.ElementRate++;
                            Session.SendPacket(GeneratePairy());

                            MessageExtension.SendHeader(Session, $"{fairy.Item.Name} has levelled up");
                        }
                        else
                        {
                            MessageExtension.SendHeader(Session, $"{fairy.Item.Name} has reached the maximum Level");
                        }
                    }
                }



                GenerateLevelXpLevelUp();
                GenerateJobXpLevelUp();

                if (specialist != null)
                {
                    GenerateSpXpLevelUp(specialist);
                }

                GenerateHeroXpLevelUp();

                Session.SendPacket(GenerateLev());
            }
        }

        private int GetGold(MapMonster mapMonster)
        {
            if (MapId == 2006 || MapId == 150 || MapId == 153 || MapId == 2106)
            {
                return 0;
            }

            int lowBaseGold = ServerManager.RandomNumber(5 * mapMonster.Monster?.Level ?? 1, 15 * mapMonster.Monster?.Level ?? 1);
            var multipler = Session?.CurrentMapInstance?.Map.MapTypes?.Max(x => GameConfiguration.ActMultiplier.ContainsKey((MapTypeEnum)x.MapTypeId) ? GameConfiguration.ActMultiplier[(MapTypeEnum)x.MapTypeId] : 1);
            //var multiplier = enum15.FirstOrDefault(x => Session?.CurrentMapInstance?.Map.MapTypes?.Any(s => enum15.Any(e => e.mapType == (MapTypeEnum)s.MapTypeId)));

            return (int)(lowBaseGold * multipler);
        }

        private int GetHXP(MapMonster mapMonster, Group group)
        {
            if (WorldPolicyConfiguration.DisableHeroExperience ||
                HeroLevel >= GameConfiguration.MaxHeroLevel)
            {
                return 0;
            }

            NpcMonster npcMonster = mapMonster.Monster;

            int partySize = group?.GroupType == GroupType.Group
                ? group.Sessions.ToList().Count(s =>
                    s?.Character != null && s.Character.MapInstance == mapMonster.MapInstance &&
                    s.Character.HeroLevel > 0 &&
                    s.Character.HeroLevel < GameConfiguration.MaxHeroLevel)
                : 1;

            if (partySize < 1)
            {
                partySize = 1;
            }

            //double sharedHXp = npcMonster.HeroXp / partySize;

            double sharedHXp = npcMonster.HeroXp * (-13 + 113) / 100;

            double memberHXp = sharedHXp * CharacterHelper.ExperiencePenalty(Level, (byte)npcMonster.Level) * GameConfiguration.HeroXPRate;

            return (int)memberHXp;
        }

        private int GetJXP(MapMonster mapMonster, Group group)
        {
            NpcMonster npcMonster = mapMonster.Monster;

            int partySize = group?.GroupType != GroupType.Group
                ? 1
                : group.Sessions.ToList().Count(s =>
                {
                    if (s?.Character == null
                        || s.Character.MapInstance != mapMonster.MapInstance)
                    {
                        return false;
                    }

                    if (!s.Character.UseSp)
                    {
                        return s.Character.JobLevel <
                               (s.Character.Class == 0 ? 20 : GameConfiguration.MaxJobLevel);
                    }

                    ItemInstance sp =
                        s.Character.Inventory?.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);

                    if (sp != null)
                    {
                        return sp.SpLevel < GameConfiguration.MaxSPLevel;
                    }

                    return false;
                });

            if (partySize < 1)
            {
                partySize = 1;
            }

            double sharedJXp = (double)npcMonster.JobXP / partySize;

            double memberJxp = (sharedJXp * CharacterHelper.ExperiencePenalty(Level, (byte)npcMonster.Level) * (GameConfiguration.JobLevelRate));

            return (int)memberJxp;
        }

        private int GetShellArmorEffectValue(ShellArmorEffectType effectType)
        {
            return ShellEffectArmor.Where(s => s.Effect == (byte)effectType).FirstOrDefault()?.Value ??
                   0;
        }

        private int GetShellMainWeaponEffectValue(ShellWeaponEffectType effectType)
        {
            return ShellEffectMain.Where(s => s.Effect == (byte)effectType).FirstOrDefault()?.Value ??
                   0;
        }

        private int GetXP(MapMonster mapMonster, Group group)
        {
            if (WorldPolicyConfiguration.DisableNormalExperience)
            {
                return 0;
            }

            NpcMonster npcMonster = mapMonster.Monster;

            int partySize = group?.GroupType == GroupType.Group
                ? group.Sessions.ToList().Count(s =>
                    s?.Character != null && s.Character.MapInstance == mapMonster.MapInstance &&
                    s.Character.Level < GameConfiguration.MaxLevel)
                : 1;

            double partyBonus = 0;

            double sharedXp = npcMonster.XP;

            switch (partySize)
            {
                case 2:
                    partyBonus = 0.05 * GameConfiguration.PartyBonusEXPWith2;
                    break;

                case 3:
                    partyBonus = 0.08 * GameConfiguration.PartyBonusEXPWith3;
                    break;

                default:
                    partyBonus = 0;
                    break;
            }

            int lvlDifference = Level - npcMonster.Level;

            double memberXp = (lvlDifference < 5 ? sharedXp : (sharedXp / 3 * 2)) * CharacterHelper.ExperiencePenalty(Level, (byte)npcMonster.Level) * (GameConfiguration.XPRate + MapInstance.XpRate);

            if (Level <= 5 && lvlDifference < -4)
            {
                memberXp *= 1.5;
            }

            return (int)memberXp;
        }

        private int HealthHPLoad()
        {
            int naturalRecovery = 1;
            int regen = GetBuff(CardType.Recovery, (byte)AdditionalTypes.Recovery.HPRecoveryIncreased)[0] +
                        CellonOptions.Where(s => s.Type == CellonOptionType.HPRestore).Sum(s => s.Value);
            if (Skills != null)
            {
                naturalRecovery += Skills.Where(s => s.Skill.SkillType == 0 && s.Skill.CastId == 10)
                    .Sum(s => s.Skill.UpgradeSkill);
            }

            if (IsSitting)
            {
                return (int)((regen + (int)CharacterHelper.HpRegenSitting(Class) *
                    (1 + GetShellArmorEffectValue(ShellArmorEffectType.RecoveryHPOnRest) / 100D)));
            }

            return (DateTime.Now - LastDefence).TotalSeconds > 4
                ? (int)((regen + (int)CharacterHelper.HpRegen(Class) *
                    (1 + GetShellArmorEffectValue(ShellArmorEffectType.RecoveryHP) / 100D)) * naturalRecovery)
                : 0;
        }

        private int HealthMPLoad()
        {
            int naturalRecovery = 1;
            int regen = GetBuff(CardType.Recovery, (byte)AdditionalTypes.Recovery.MPRecoveryIncreased)[0] +
                        CellonOptions.Where(s => s.Type == CellonOptionType.MPRestore).Sum(s => s.Value);
            if (Skills != null)
            {
                naturalRecovery += Skills.Where(s => s.Skill.SkillType == 0 && s.Skill.CastId == 10)
                    .Sum(s => s.Skill.UpgradeSkill);
            }

            if (IsSitting)
            {
                return (int)((regen + (int)CharacterHelper.MpRegenSitting(Class) *
                    (1 + GetShellArmorEffectValue(ShellArmorEffectType.RecoveryMPOnRest) / 100D)));
            }

            return (DateTime.Now - LastDefence).TotalSeconds > 4
                ? (int)((regen + (int)CharacterHelper.MpRegen(Class) *
                    (1 + GetShellArmorEffectValue(ShellArmorEffectType.RecoveryMP) / 100D)) * naturalRecovery)
                : 0;
        }

        public double HeroXPLoad() => HeroLevel == 0 ? 1 : CharacterHelper.HeroXpData[HeroLevel - 1];

        private void IncrementGroupQuest(QuestType type, int firstData = 0, int secondData = 0, int thirdData = 0)
        {
            if (Group != null && Group.GroupType == GroupType.Group)
            {
                foreach (ClientSession groupMember in Group.Sessions.Where(s =>
                    s.Character.MapInstance == MapInstance && s.Character.CharacterId != CharacterId))
                {
                    groupMember.Character.IncrementQuests(type, firstData, secondData, thirdData, true);
                }
            }
        }

        private void IncrementObjective(CharacterQuest quest, byte index = 0, int amount = 1, bool isOver = false)
        {
            bool isFinish = isOver;
            Session.SendPacket(quest.GetProgressMessage(index, amount));
            quest.Incerment(index, amount);
            byte a = 1;
            if (quest.GetObjectives().All(q =>
                quest.GetObjectiveByIndex(a) == null || q >= quest.GetObjectiveByIndex(a++).Objective))
            {
                isFinish = true;
            }

            Session.SendPacket($"qsti {quest.GetInfoPacket(false)}");
            if (!isFinish)
            {
                return;
            }

            LastQuest = DateTime.Now;
            if (CustomQuestRewards((QuestType)quest.Quest.QuestType, quest.Quest.QuestId))
            {
                RemoveQuest(quest.QuestId);
                return;
            }

            Session.SendPacket(quest.Quest.GetRewardPacket(this));
            RemoveQuest(quest.QuestId);
        }

        public double JobXPLoad() => Class == (byte)ClassType.Adventurer
            ? CharacterHelper.FirstJobXPData[JobLevel - 1]
            : CharacterHelper.SecondJobXPData[JobLevel - 1];

        private double SpXpLoad()
        {
            ItemInstance specialist = null;
            if (Inventory != null)
            {
                specialist = Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
            }

            return specialist != null
                ? CharacterHelper.SPXPData[specialist.SpLevel == 0 ? 0 : specialist.SpLevel - 1]
                : 0;
        }

        public double XpLoad() => CharacterHelper.XPData[Level - 1];

        #endregion

        public void OnKill(KillEventArgs e, bool share = true)
        {
            if (share && Session.Character.Group?.GroupType == GroupType.Group)
            {
                foreach (ClientSession sess in Session.Character.Group.Sessions.GetAllItems())
                {
                    sess.Character.OnKill(e, false);
                }
            }
            else
            {
                Kill?.Invoke(this, e);
            }
        }

        public void OnDie(DieEventArgs e) => Die?.Invoke(this, e);

        public void OnMove(MoveEventArgs e) => Move?.Invoke(this, e);

        public void OnCraftRecipe(CraftRecipeEventArgs e) => CraftRecipe?.Invoke(this, e);

        public void OnPickupItem(PickupItemEventArgs e, bool share = true)
        {
            if (share && Session.Character.Group?.GroupType == GroupType.Group)
            {
                foreach (ClientSession sess in Session.Character.Group.Sessions.GetAllItems())
                {
                    sess.Character.OnPickupItem(e, false);
                }
            }
            else
            {
                PickupItem?.Invoke(this, e);
            }
        }

        public void OnTalk(TalkEventArgs e) => Talk?.Invoke(this, e);

        public void OnFinishScriptedInstance(FinishScriptedInstanceEventArgs e) => FinishScriptedInstance?.Invoke(this, e);

        private void OnCapture(CaptureEventArgs e) => Capture?.Invoke(this, e);

        public void OnReceiveHit(HitEventArgs e) => ReceiveHit?.Invoke(this, e);

        public void OnLandHit(HitEventArgs e) => LandHit?.Invoke(this, e);

    }
}
