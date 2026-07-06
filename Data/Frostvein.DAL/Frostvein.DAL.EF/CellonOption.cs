using Frostvein.Domain;
using System;

namespace Frostvein.DAL.EF
{
    public class CellonOption
    {
        #region Properties

        public long CellonOptionId { get; set; }

        public Guid EquipmentSerialId { get; set; }

        public byte Level { get; set; }

        public CellonOptionType Type { get; set; }

        public int Value { get; set; }

        #endregion
    }
}