using System;

namespace NosGm.GameObject.EventArguments
{
    public class TalkEventArgs : EventArgs
    {
        public TalkEventArgs(MapNpc mapNpc) => MapNpc = mapNpc;

        public MapNpc MapNpc { get; }
    }
}
