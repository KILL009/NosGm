using Frostvein.Domain;
using System;

namespace Frostvein.GameObject.EventArguments
{
    public class KillEventArgs : EventArgs
    {
        public KillEventArgs(UserType type, object killedEntity)
        {
            UserType = type;
            KilledEntity = killedEntity;
        }

        public UserType UserType { get; }

        public object KilledEntity { get; }
    }
}
