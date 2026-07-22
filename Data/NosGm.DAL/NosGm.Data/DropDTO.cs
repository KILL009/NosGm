using System;

namespace NosGm.Data
{
    [Serializable]
    public class DropDTO
    {
        #region Properties

        public int Amount { get; set; }

        public int DropChance { get; set; }

        public short DropId { get; set; }

        public short ItemVNum { get; set; }

        public short? MapTypeId { get; set; }

        public short? MonsterVNum { get; set; }

        #endregion
    }
}