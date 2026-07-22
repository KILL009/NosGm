using System;

namespace NosGm.GameObject.EventArguments
{
    public class MoveEventArgs : EventArgs
    {
        public MoveEventArgs(short mapId, short posX, short posY)
        {
            MapId = mapId;
            PositionX = posX;
            PositionY = posY;
        }

        public short MapId { get; }

        public short PositionX { get; }

        public short PositionY { get; }
    }
}
