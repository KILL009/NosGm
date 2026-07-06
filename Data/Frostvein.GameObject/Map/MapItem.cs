using Frostvein.Domain;
using Frostvein.GameObject.Helpers;
using System;

namespace Frostvein.GameObject
{
    public abstract class MapItem
    {
        #region Instantiation

        protected MapItem(short x, short y)
        {
            PositionX = x;
            PositionY = y;
            CreatedDate = DateTime.Now;
            TransportId = 0;
        }

        #endregion

        #region Members

        protected ItemInstance _itemInstance;

        private readonly object _lockObject = new object();
        private long _transportId;

        #endregion

        #region Properties

        public abstract short Amount { get; set; }

        public DateTime CreatedDate { get; set; }

        public abstract short ItemVNum { get; set; }

        public short PositionX { get; set; }

        public short PositionY { get; set; }

        public long TransportId
        {
            get
            {
                lock (_lockObject)
                {
                    if (_transportId == 0) _transportId = TransportFactory.Instance.GenerateTransportId();
                    return _transportId;
                }
            }

            private set
            {
                if (value != _transportId) _transportId = value;
            }
        }

        #endregion

        #region Methods

        public string GenerateIn()
        {
            return StaticPacketHelper.In(UserType.Object, ItemVNum, TransportId, PositionX, PositionY,
                this is MonsterMapItem monsterMapItem && monsterMapItem.GoldAmount > 1
                    ? monsterMapItem.GoldAmount
                    : Amount, 0, 0, 0, 0, false, "-", false, 0, 0, 0, 0, 0, 0, 0, 0);
        }

        public abstract ItemInstance GetItemInstance();

        public bool CanSpawn { get; set; }

        #endregion
    }
}