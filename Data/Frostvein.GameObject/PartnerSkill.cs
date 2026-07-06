using Frostvein.Data;
using Frostvein.GameObject.Networking;
using System;

namespace Frostvein.GameObject
{
    public class PartnerSkill
    {
        #region Members

        private Skill _skill;

        #endregion

        #region Instantiation

        public PartnerSkill(PartnerSkillDTO input)
        {
            PartnerSkillId = input.PartnerSkillId;
            EquipmentSerialId = input.EquipmentSerialId;
            SkillVNum = input.SkillVNum;
            Level = input.Level;
        }

        #endregion

        #region Methods

        public bool CanBeUsed() => Skill != null && LastUse.AddMilliseconds(Skill.Cooldown * 100) < DateTime.Now;

        #endregion

        #region Properties

        public long PartnerSkillId { get; set; }

        public Guid EquipmentSerialId { get; set; }

        public short SkillVNum { get; set; }

        public byte Level { get; set; }

        public Skill Skill => _skill ?? (_skill = ServerManager.GetSkill(SkillVNum));

        public DateTime LastUse { get; set; }

        #endregion
    }
}