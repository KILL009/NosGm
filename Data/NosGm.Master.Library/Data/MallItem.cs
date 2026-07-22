using System;

namespace NosGm.Master.Library.Data
{
    [Serializable]
    public class MallItem
    {
        #region Properties

        public int Amount { get; set; }

        public short Design { get; set; }

        public short ItemVNum { get; set; }

        public byte Level { get; set; }

        /// <summary>
        /// Stable purchase transaction identifier. API callers should reuse the same value
        /// when retrying a delivery so the server cannot create a duplicate parcel.
        /// </summary>
        public Guid OperationId { get; set; }

        public byte Rare { get; set; }

        public byte Upgrade { get; set; }

        #endregion
    }
}
