using NosGm.Domain;
using System;

namespace NosGm.GameObject.EventArguments
{
    public class DieEventArgs : EventArgs
    {
        public DieEventArgs(UserType type, object killingEntity)
        {
            UserType = type;
            KillingEntity = killingEntity;
        }

        public UserType UserType { get; }

        public object KillingEntity { get; }
    }
}
