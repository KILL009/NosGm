using System;

namespace Frostvein.Master.Library.Data
{
    [Serializable]
    public class MallItem
    {
        #region Properties

        public int Amount { get; set; }

        public short Design { get; set; }

        public short ItemVNum { get; set; }

        public byte Level { get; set; }

        public byte Rare { get; set; }

        public byte Upgrade { get; set; }

        #endregion
    }
}