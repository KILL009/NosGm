using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.Configuration
{
    public static class GameConfiguration
    {
        public static int XPRate = 5;
        public static int HeroXPRate = 5;
        public static int DropRate = 1;
        public static int GoldDropRate = 10;
        public static int GoldRate = 2;
        public static int ReputationRate = 1;
        public static int JobLevelRate = 20;
        public static int FairyXPRate = 10;
        public static int QuestDropRate = 10;
        public static long PartnerSpXP = 10;

        //Official Factor: 0 | 3
        public static byte LowerFairyFactor = 0;
        public static byte HigherFairyFactor = 3;

        public static byte MaxLevel = 99;
        public static byte MaxHeroLevel = 60;
        public static byte MaxJobLevel = 80;
        public static byte MaxSPLevel = 99;
        public static long MaxGold = 2000000000;

        public static bool SendWorldInformation = true;
        public static bool SceneOnCreate = false;
        public static bool EnableTimeSpaceQuest = false;
        public static bool CModEnabled = false;
        public static bool BazaarEnabled = true;
        public static bool TradeEnabled = true;
        public static bool ShopEnabled = true;
        public static bool DropEnabled = true;
        public static bool ChatEnabled = true;

        public static bool BattlePassEnabled = false;
        public static int MaxBattlePassPoints = 1800;

        public static byte MonsterToFollowMate = 5;
        public static byte MonsterToFollowPlayer = 5;
        public static byte MonsterToFollowDefault = 5;

        public static bool TimeSpaceQuestEnabled = false;
        public static bool PartnerSkillsEnabled = true;

        public static string BN0 = $"{ServerConfiguration.ServerName}^Staff^will^NEVER^ask^for^your^password";
        public static string BN1 = "Website:^https://sumeria.xyz";
        public static string BN2 = $"Welcome^to^{ServerConfiguration.ServerName}";
        public static string BN3 = "Make^sure^to^check^out^our^Discord^Server";
        public static string BN4 = "Please^use^our^suggestion^Channel^on^Discord^to^help^us^with^your^ideas";
        public static string BN5 = "Players^that^purchase^gold^will^be^banned";
        public static string BN6 = $"Do^you^want^to^join^the^{ServerConfiguration.ServerName}^Staff?^Check^out^our^Discord^Server^to^apply";
        public static string BN7 = "Game^Rules^can^be^found^on^our^Discord";
        public static string BN8 = "Report^a^player^if^their^actions^are^against^the^rules";
        public static string BN9 = "Visit^our^Ticket^Server^if^you^need^support";

        public static string CrashReportMessage = "Hello Sumeria Player!\n\nThe Server just crashed.\n\nWe were able to log the issue. However, the Server will shut down in 30 seconds.\nPlease use $Save or just log out.\nThe Channel should be back in roughly 40 seconds.";
        public static string RebootMessage = "Hello Sumeria Player!\n\nThe Server will now perform the planned Auto-Reboot. The Server will shut down in 30 Seconds and be back in another 30 Seconds.";
        public static string RebootShutdownMessage = "The Server will now restart";

        public static byte Season = (byte)SeasonType.Despair;

        public static bool EquipmentReputationRequirement = false;

        public static int ReputationDevidedBy = 3;
        public static int ReputationDevidedByInGroup = 3;

        public static int PartyBonusEXPWith2 = 1;
        public static int PartyBonusEXPWith3 = 1;

        public static List<short> BuffsToAdd = new List<short> {4121, 4122, 4123, 4124, 4125, 4126, 4127, 4128, 4129, 4130, 4131, 4132 }; 

        public static readonly Dictionary<MapTypeEnum, float> ActMultiplier = new Dictionary<MapTypeEnum, float>()
        {
            { MapTypeEnum.Act52, 2.5f },
            { MapTypeEnum.Act61, 2.5f },
            { MapTypeEnum.Act62, 3f },
            { MapTypeEnum.Act7, 3.5f},
            { MapTypeEnum.Act8, 4f }
        };

        public static int Act6ZenasRaidMultiplier = 10;
        public static int Act6EreniaRaidMultiplier = 10;

        public static bool DebugBCards = false;

        #region Titan Shield
        public static string ConfigurationList = "";

        public static async Task Update(ConfigurationType type, object value)
        {
            try
            {
                switch (type)
                {
                    case ConfigurationType.XPRate:
                        XPRate = (int)value;
                        break;
                    case ConfigurationType.HeroXPRate:
                        HeroXPRate = (int)value;
                        break;
                    case ConfigurationType.DropRate:
                        DropRate = (int)value;
                        break;
                    case ConfigurationType.GoldDropRate:
                        GoldDropRate = (int)value;
                        break;
                    case ConfigurationType.GoldRate:
                        GoldRate = (int)value;
                        break;
                    case ConfigurationType.ReputationRate:
                        ReputationRate = (int)value;
                        break;
                    case ConfigurationType.JobLevelRate:
                        JobLevelRate = (int)value;
                        break;
                    case ConfigurationType.FairyXPRate:
                        FairyXPRate = (int)value;
                        break;
                    case ConfigurationType.QuestDropRate:
                        QuestDropRate = (int)value;
                        break;
                    case ConfigurationType.PartnerSpXP:
                        PartnerSpXP = (long)value;
                        break;
                    case ConfigurationType.MaxLevel:
                        MaxLevel = (byte)value;
                        break;
                    case ConfigurationType.MaxHeroLevel:
                        MaxHeroLevel = (byte)value;
                        break;
                    case ConfigurationType.MaxJobLevel:
                        MaxJobLevel = (byte)value;
                        break;
                    case ConfigurationType.MaxSPLevel:
                        MaxSPLevel = (byte)value;
                        break;
                    case ConfigurationType.MaxGold:
                        MaxGold = (long)value;
                        break;
                    case ConfigurationType.SendWorldInformation:
                        SendWorldInformation = (bool)value;
                        break;
                    case ConfigurationType.SceneOnCreate:
                        SceneOnCreate = (bool)value;
                        break;
                    case ConfigurationType.EnableTimeSpaceQuest:
                        EnableTimeSpaceQuest = (bool)value;
                        break;
                    case ConfigurationType.CModEnabled:
                        CModEnabled = (bool)value;
                        break;
                    case ConfigurationType.BazaarEnabled:
                        BazaarEnabled = (bool)value;
                        break;
                    case ConfigurationType.TradeEnabled:
                        TradeEnabled = (bool)value;
                        break;
                    case ConfigurationType.ShopEnabled:
                        ShopEnabled = (bool)value;
                        break;
                    case ConfigurationType.DropEnabled:
                        DropEnabled = (bool)value;
                        break;
                    case ConfigurationType.ChatEnabled:
                        ChatEnabled = (bool)value;
                        break;
                    case ConfigurationType.BattlePassEnabled:
                        BattlePassEnabled = (bool)value;
                        break;
                    case ConfigurationType.MaxBattlePassPoints:
                        MaxBattlePassPoints = (int)value;
                        break;
                    case ConfigurationType.MonsterToFollowMate:
                        MonsterToFollowMate = (byte)value;
                        break;
                    case ConfigurationType.MonsterToFollowPlayer:
                        MonsterToFollowPlayer = (byte)value;
                        break;
                    case ConfigurationType.MonsterToFollowDefault:
                        MonsterToFollowDefault = (byte)value;
                        break;
                    case ConfigurationType.TimeSpaceQuestEnabled:
                        TimeSpaceQuestEnabled = (bool)value;
                        break;
                    case ConfigurationType.PartnerSkillsEnabled:
                        PartnerSkillsEnabled = (bool)value;
                        break;
                    case ConfigurationType.Season:
                        Season = (byte)value;
                        break;
                    case ConfigurationType.EquipmentReputationRequirement:
                        EquipmentReputationRequirement = (bool)value;
                        break;
                    case ConfigurationType.ReputationDevidedBy:
                        ReputationDevidedBy = (byte)value;
                        break;
                }
            }
            catch (ArgumentOutOfRangeException arg)
            {
                await HandleError(arg);
            }
        }

        public static async Task HandleError(ArgumentOutOfRangeException arg)
        {
            Console.WriteLine(arg);
            await Task.FromResult(0);
        }

        public static object GetConfigurationValue(ConfigurationType type)
        {
            switch (type)
            {
                case ConfigurationType.XPRate:
                    return XPRate;
                case ConfigurationType.HeroXPRate:
                    return HeroXPRate;
                case ConfigurationType.DropRate:
                    return DropRate;
                case ConfigurationType.GoldDropRate:
                    return GoldDropRate;
                case ConfigurationType.GoldRate:
                    return GoldRate;
                case ConfigurationType.ReputationRate:
                    return ReputationRate;
                case ConfigurationType.JobLevelRate:
                    return JobLevelRate;
                case ConfigurationType.FairyXPRate:
                    return FairyXPRate;
                case ConfigurationType.QuestDropRate:
                    return QuestDropRate;
                case ConfigurationType.PartnerSpXP:
                    return PartnerSpXP;
                case ConfigurationType.MaxLevel:
                    return MaxLevel;
                case ConfigurationType.MaxHeroLevel:
                    return MaxHeroLevel;
                case ConfigurationType.MaxJobLevel:
                    return MaxJobLevel;
                case ConfigurationType.MaxSPLevel:
                    return MaxSPLevel;
                case ConfigurationType.MaxGold:
                    return MaxGold;
                case ConfigurationType.SendWorldInformation:
                    return SendWorldInformation;
                case ConfigurationType.SceneOnCreate:
                    return SceneOnCreate;
                case ConfigurationType.EnableTimeSpaceQuest:
                    return EnableTimeSpaceQuest;
                case ConfigurationType.CModEnabled:
                    return CModEnabled;
                case ConfigurationType.BazaarEnabled:
                    return BazaarEnabled;
                case ConfigurationType.TradeEnabled:
                    return TradeEnabled;
                case ConfigurationType.ShopEnabled:
                    return ShopEnabled;
                case ConfigurationType.DropEnabled:
                    return DropEnabled;
                case ConfigurationType.ChatEnabled:
                    return ChatEnabled;
                case ConfigurationType.BattlePassEnabled:
                    return BattlePassEnabled;
                case ConfigurationType.MaxBattlePassPoints:
                    return MaxBattlePassPoints;
                case ConfigurationType.MonsterToFollowMate:
                    return MonsterToFollowMate;
                case ConfigurationType.MonsterToFollowPlayer:
                    return MonsterToFollowPlayer;
                case ConfigurationType.MonsterToFollowDefault:
                    return MonsterToFollowDefault;
                case ConfigurationType.TimeSpaceQuestEnabled:
                    return TimeSpaceQuestEnabled;
                case ConfigurationType.PartnerSkillsEnabled:
                    return PartnerSkillsEnabled;
                case ConfigurationType.Season:
                    return Season;
                case ConfigurationType.EquipmentReputationRequirement:
                    return EquipmentReputationRequirement;
                case ConfigurationType.ReputationDevidedBy:
                    return ReputationDevidedBy;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static void UpdateConfigurationList()
        {
            foreach (ConfigurationType type in Enum.GetValues(typeof(ConfigurationType)))
            {
                ConfigurationList = $"{(int)type}. {type}: {GetConfigurationValue(type)}\n";
            }
        }
        #endregion
    }
}
