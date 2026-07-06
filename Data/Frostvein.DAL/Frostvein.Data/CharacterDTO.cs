using Frostvein.Domain;
using System;

namespace Frostvein.Data
{
    [Serializable]
    public class CharacterDTO
    {
        #region Properties

        public long AccountId { get; set; }

        public int Act4Dead { get; set; }

        public int Act4Kill { get; set; }

        public int Act4Points { get; set; }

        public int ArenaWinner { get; set; }

        public string Biography { get; set; }

        public bool BuffBlocked { get; set; }

        public long CharacterId { get; set; }

        public ClassType Class { get; set; }

        public short Compliment { get; set; }

        public float Dignity { get; set; }

        public bool EmoticonsBlocked { get; set; }

        public bool ExchangeBlocked { get; set; }

        public FactionType Faction { get; set; }

        public bool FamilyRequestBlocked { get; set; }

        public bool FriendRequestBlocked { get; set; }

        public GenderType Gender { get; set; }

        public long Gold { get; set; }

        public long GoldBank { get; set; }

        public bool GroupRequestBlocked { get; set; }

        public HairColorType HairColor { get; set; }

        public HairStyleType HairStyle { get; set; }

        public bool HeroChatBlocked { get; set; }

        public byte HeroLevel { get; set; }

        public long HeroXp { get; set; }

        public int Hp { get; set; }

        public bool HpBlocked { get; set; }

        public bool IsPartnerAutoRelive { get; set; }

        public bool IsPetAutoRelive { get; set; }

        public bool IsSeal { get; set; }

        public byte JobLevel { get; set; }

        public long JobLevelXp { get; set; }

        public long LastFamilyLeave { get; set; }

        public byte Level { get; set; }

        public long LevelXp { get; set; }

        public short MapId { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public int MasterPoints { get; set; }

        public int MasterTicket { get; set; }

        public byte MaxMateCount { get; set; }

        public byte MaxPartnerCount { get; set; }

        public bool MinilandInviteBlocked { get; set; }

        public string MinilandMessage { get; set; }

        public short MinilandPoint { get; set; }

        public MinilandState MinilandState { get; set; }

        public bool MouseAimLock { get; set; }

        public int Mp { get; set; }

        public string Name { get; set; }

        public bool QuickGetUp { get; set; }

        public long RagePoint { get; set; }

        public long Reputation { get; set; }

        public byte Slot { get; set; }

        public int SpAdditionPoint { get; set; }

        public int SpPoint { get; set; }

        public CharacterState State { get; set; }

        public int TalentLose { get; set; }

        public int TalentSurrender { get; set; }

        public int TalentWin { get; set; }

        public bool WhisperBlocked { get; set; }

        public int ArenaDeath { get; set; }

        public int ArenaKill { get; set; }

        public bool HideHat { get; set; }

        public bool UiBlocked { get; set; }

        public int TrophyCount { get; set; }

        public int Trophy1 { get; set; }

        public int Trophy2 { get; set; }

        public int Trophy3 { get; set; }

        public int Trophy4 { get; set; }

        public int Trophy5 { get; set; }

        public int Trophy6 { get; set; }

        public int Trophy7 { get; set; }

        public int Trophy8 { get; set; }

        public int Trophy9 { get; set; }

        public int Trophy10 { get; set; }

        public int Trophy11 { get; set; }

        public int Trophy12 { get; set; }

        public int Trophy13 { get; set; }

        public int Trophy14 { get; set; }

        public int Trophy15 { get; set; }

        public int LegendaryTrophy { get; set; }

        public long MasteryXp { get; set; }

        public int MasteryLevel { get; set; }

        public int RaidCount { get; set; }

        public int MonsterCount { get; set; }

        public int MysteryBoxCount { get; set; }

        public int BattlePassPoints { get; set; }

        public bool HasPremiumBattlePass { get; set; }

        public bool UnlockedBattlePassMultiplicator { get; set; }

        public byte BuffCharge { get; set; }

        public byte LimitedBuffCharge { get; set; }

        public byte Stage { get; set; }

        public byte PrimalCharacterQuest { get; set; }

        public byte PrimalRaidQuest { get; set; }

        public byte PrimalFamilyQuest { get; set; }

        public int PrimalCharacterQuestProgress { get; set; }

        public int PrimalRaidQuestProgress { get; set; }

        public int PrimalFamilyQuestProgress { get; set; }

        public int PrimalQuestCount { get; set; }

        public byte DailyRewardChest { get; set; }

        public bool AutoLoot { get; set; }

        public bool SafeBet { get; set; }

        public int DuelWon { get; set; }

        public int DuelLost { get; set; }

        public int DuelCount { get; set; }

        public string CurrentIp { get; set; }

        public bool StarterBoxUsed { get; set; }

        public short InstanceMapId { get; set; }

        public short InstanceMapX { get; set; }

        public short InstanceMapY { get; set; }

        public int PityCount { get; set; }

        public int Icon { get; set; }

        public byte MiniPet { get; set; }

        public int PetSkill1 { get; set; }

        public int PetSkill2 { get; set; }

        public int KingWin { get; set; }

        public byte SwitchLevel()
        {
            return Level;
        }

        #endregion
    }
}