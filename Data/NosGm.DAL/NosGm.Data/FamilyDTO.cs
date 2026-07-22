using NosGm.Domain;
using System;

namespace NosGm.Data
{
    [Serializable]
    public class FamilyDTO
    {
        #region Properties

        public int FamilyExperience { get; set; }

        public byte FamilyFaction { get; set; }

        public GenderType FamilyHeadGender { get; set; }

        public long FamilyId { get; set; }

        public byte FamilyLevel { get; set; }

        public string FamilyMessage { get; set; }

        public long LastFactionChange { get; set; }

        public FamilyAuthorityType ManagerAuthorityType { get; set; }

        public bool ManagerCanGetHistory { get; set; }

        public bool ManagerCanInvite { get; set; }

        public bool ManagerCanNotice { get; set; }

        public bool ManagerCanShout { get; set; }

        public short MaxSize { get; set; }

        public FamilyAuthorityType MemberAuthorityType { get; set; }

        public bool MemberCanGetHistory { get; set; }

        public string Name { get; set; }

        public byte WarehouseSize { get; set; }

        public byte? IconTopOne { get; set; }

        public byte? IconTopRaid { get; set; }

        public byte? IconBestFam { get; set; }

        public byte FamilyRoomLevel { get; set; }

        public int TowerLevel { get; set; }

        public int MaxLevel { get; set; }

        public int FamilySkill1 { get; set; }

        public int FamilySkill2 { get; set; }

        public int FamilySkill3 { get; set; }

        public int FamilySkill4 { get; set; }

        public int FamilySkill5 { get; set; }

        public int FamilySkill6 { get; set; }

        public int FamilySkill7 { get; set; }

        #endregion
    }
}