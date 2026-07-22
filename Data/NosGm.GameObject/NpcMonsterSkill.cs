using NosGm.Data;
using NosGm.GameObject.Networking;
using System;

namespace NosGm.GameObject
{
    public class NpcMonsterSkill : NpcMonsterSkillDTO
    {
        #region Members

        private Skill _skill;

        #endregion

        #region Instantiation

        public NpcMonsterSkill()
        {
        }

        public NpcMonsterSkill(NpcMonsterSkillDTO input)
        {
            NpcMonsterSkillId = input.NpcMonsterSkillId;
            NpcMonsterVNum = input.NpcMonsterVNum;
            Rate = input.Rate;
            SkillVNum = input.SkillVNum;
        }

        #endregion

        #region Properties

        public short Hit { get; set; }

        public DateTime LastSkillUse
        {
            get; set;
        }

        public Skill Skill => _skill ?? (_skill = ServerManager.GetSkill(SkillVNum));

        #region Methods

        public bool CanBeUsed() => Skill != null && LastSkillUse.AddMilliseconds(Skill.Cooldown * 100) < DateTime.Now;

        #endregion


        #endregion
    }
}