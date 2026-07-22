using System.Collections.Generic;

namespace NosGm.GameObject
{
    public class ZoneEvent
    {
        #region Instantiation

        public ZoneEvent()
        {
            Events = new List<EventContainer>();
            Range = 1;
        }

        #endregion

        #region Methods

        public bool InZone(short positionX, short positionY) => positionX <= X + Range && positionX >= X - Range && positionY <= Y + Range && positionY >= Y - Range;

        #endregion

        #region Properties

        public List<EventContainer> Events { get; set; }

        public short Range { get; set; }

        public short X { get; set; }

        public short Y { get; set; }

        #endregion
    }
}