using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using NosGm.Master.Library.Client;
using NosGm.Master.Library.Data;
using System;
using System.Collections.Generic;

namespace NosGm.GameObject
{
    public class Family : FamilyDTO
    {
        #region Instantiation

        public Family() => FamilyCharacters = new List<FamilyCharacter>();

        public Family(FamilyDTO input)
        {
            FamilySkillMissions = new List<FamilySkillMission>();
            FamilyCharacters = new List<FamilyCharacter>();
            FamilyExperience = input.FamilyExperience;
            FamilyFaction = input.FamilyFaction;
            FamilyHeadGender = input.FamilyHeadGender;
            FamilyId = input.FamilyId;
            FamilyLevel = input.FamilyLevel;
            FamilyMessage = input.FamilyMessage;
            LastFactionChange = input.LastFactionChange;
            ManagerAuthorityType = input.ManagerAuthorityType;
            ManagerCanGetHistory = input.ManagerCanGetHistory;
            ManagerCanInvite = input.ManagerCanInvite;
            ManagerCanNotice = input.ManagerCanNotice;
            ManagerCanShout = input.ManagerCanShout;
            MaxSize = input.MaxSize;
            MemberAuthorityType = input.MemberAuthorityType;
            MemberCanGetHistory = input.MemberCanGetHistory;
            Name = input.Name;
            WarehouseSize = input.WarehouseSize;
            FamilyRoomLevel = input.FamilyRoomLevel;
            TowerLevel = input.TowerLevel;
            MaxLevel = input.MaxLevel;
            FamilySkill1 = input.FamilySkill1;
            FamilySkill2 = input.FamilySkill2;
            FamilySkill3 = input.FamilySkill3;
            FamilySkill4 = input.FamilySkill4;
            FamilySkill5 = input.FamilySkill5;
            FamilySkill6 = input.FamilySkill6;
            FamilySkill7 = input.FamilySkill7;
        }

        #endregion

        #region Properties

        public MapInstance Act4Raid { get; set; }

        public MapInstance Act4RaidBossMap { get; set; }

        public List<FamilyCharacter> FamilyCharacters { get; set; }

        public List<FamilySkillMission> FamilySkillMissions { get; set; }

        public List<FamilyLogDTO> FamilyLogs { get; set; }

        public MapInstance LandOfDeath { get; set; }

        public MapInstance NewEvent { get; set; }

        public Inventory Warehouse { get; set; }

        public int TowerGameRound { get; set; }

        public MapInstance FamilyTower { get; set; }

        public MapInstance FamilyRoom { get; set; }


        #endregion

        #region Methods

        public void ChangeFaction(byte faction, ClientSession session)
        {
            session.Character.Family.FamilyFaction = faction;
            session.Character.Family.LastFactionChange = DateTime.Now.Ticks;
            FamilyDTO fam = session.Character.Family;
            DAOFactory.FamilyDAO.InsertOrUpdate(ref fam);

            ServerManager.Instance.FamilyRefresh(FamilyId, true);
        }

        public void InsertFamilyLog(FamilyLogType logtype, string characterName = "", string characterName2 = "",
            string rainBowFamily = "", string message = "", byte level = 0, int experience = 0, int itemVNum = 0,
            byte upgrade = 0, int raidType = 0, FamilyAuthority authority = FamilyAuthority.Head, int righttype = 0,
            int rightvalue = 0)
        {
            var value = "";
            switch (logtype)
            {
                case FamilyLogType.DailyMessage:
                    value = $"{characterName}|{message}";
                    break;

                case FamilyLogType.FamilyXP:
                    value = $"{characterName}|{experience}";
                    break;

                case FamilyLogType.FamilyMission:
                    value = $"{itemVNum}";
                    break;

                case FamilyLogType.FamilyExtension:
                    value = $"{itemVNum}";
                    break;

                case FamilyLogType.SkillUse:
                    value = $"{characterName}|{level}";
                    break;

                case FamilyLogType.LevelUp:
                case FamilyLogType.HeroLevelUp:
                    value = $"{characterName}|{level}";
                    break;

                case FamilyLogType.RaidWon:
                    value = raidType.ToString();
                    break;

                case FamilyLogType.ItemUpgraded:
                    value = $"{characterName}|{itemVNum}|{upgrade}";
                    break;

                case FamilyLogType.UserManaged:
                    value = $"{characterName}|{characterName2}";
                    break;

                case FamilyLogType.FamilyLevelUp:
                    value = level.ToString();
                    break;

                case FamilyLogType.AuthorityChanged:
                    value = $"{characterName}|{(byte)authority}|{characterName2}";
                    break;

                case FamilyLogType.FamilyManaged:
                    value = characterName;
                    break;

                case FamilyLogType.RainbowBattle:
                    value = rainBowFamily;
                    break;

                case FamilyLogType.RightChanged:
                    value = $"{characterName}|{(byte)authority}|{righttype}|{rightvalue}";
                    break;

                case FamilyLogType.WareHouseAdded:
                case FamilyLogType.WareHouseRemoved:
                    value = $"{characterName}|{message}";
                    break;
            }

            var log = new FamilyLogDTO
            {
                FamilyId = FamilyId,
                FamilyLogData = value,
                FamilyLogType = logtype,
                Timestamp = DateTime.Now
            };
            DAOFactory.FamilyLogDAO.InsertOrUpdate(ref log);
            ServerManager.Instance.FamilyRefresh(FamilyId);
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = FamilyId,
                SourceCharacterId = 0,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = "fhis_stc",
                Type = MessageType.Family
            });
        }

        public void SendPacket(string packet)
        {
            CommunicationServiceClient.Instance.SendMessageToCharacter(new SCSCharacterMessage
            {
                DestinationCharacterId = FamilyId,
                SourceCharacterId = 0,
                SourceWorldId = ServerManager.Instance.WorldId,
                Message = packet,
                Type = MessageType.Family
            });
        }

        internal Family DeepCopy() => (Family)MemberwiseClone();

        #endregion
    }
}