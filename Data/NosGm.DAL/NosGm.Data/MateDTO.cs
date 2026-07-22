using NosGm.Domain;
using System;

namespace NosGm.Data
{
    [Serializable]
    public class MateDTO
    {
        #region Properties

        public byte Attack { get; set; }

        public bool CanPickUp { get; set; }

        public long CharacterId { get; set; }

        public byte Defence { get; set; }

        public byte Direction { get; set; }

        public long Experience { get; set; }

        public double Hp { get; set; }

        public bool IsSummonable { get; set; }

        public bool IsTeamMember { get; set; }

        public byte Level { get; set; }

        public byte StarRating { get; set; }

        public byte TrainerLevel { get; set; }

        public int Skill1 { get; set; }

        public int Skill2 { get; set; }

        public int CurrentEXP { get; set; }

        public int EXPNeeded { get; set; }

        public short Loyalty { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public long MateId { get; set; }

        public MateType MateType { get; set; }

        public double Mp { get; set; }

        public string Name { get; set; }

        public short NpcMonsterVNum { get; set; }

        public short Skin { get; set; }

        #endregion
    }
}