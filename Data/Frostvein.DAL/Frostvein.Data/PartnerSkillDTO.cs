using System;

namespace Frostvein.Data
{
    [Serializable]
    public class PartnerSkillDTO
    {
        #region Properties

        public Guid EquipmentSerialId { get; set; }

        public byte Level { get; set; }

        public long PartnerSkillId { get; set; }

        public short SkillVNum { get; set; }

        #endregion
    }
}