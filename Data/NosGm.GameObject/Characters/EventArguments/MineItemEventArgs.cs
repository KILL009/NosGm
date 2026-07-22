using System;

namespace NosGm.GameObject.EventArguments
{
    public class MineItemEventArgs : EventArgs
    {
        public MineItemEventArgs(Item item) => Item = item;

        public Item Item { get; }
    }
}
