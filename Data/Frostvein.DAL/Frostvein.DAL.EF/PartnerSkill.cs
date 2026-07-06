using System;
using System.ComponentModel.DataAnnotations;

namespace Frostvein.DAL.EF
{
    public class PartnerSkill
    {
        #region Properties

        public Guid EquipmentSerialId { get; set; }

        public byte Level { get; set; }

        [Key] public long PartnerSkillId { get; set; }

        public short SkillVNum { get; set; }

        #endregion
    }
}