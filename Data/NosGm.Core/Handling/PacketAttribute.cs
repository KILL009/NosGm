using System;

namespace NosGm.Core.Handling
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class PacketAttribute : Attribute
    {
        #region Instantiation

        //[Obsolete]
        public PacketAttribute(int amount = 1, params string[] header)
        {
            Header = header;
            Amount = amount;
        }

        public PacketAttribute(params string[] header)
        {
            Header = header;
            Amount = 1;
        }

        public PacketAttribute(bool isCharScreen, params string[] header)
        {
            Header = header;
            Amount = 1;
            IsCharScreen = isCharScreen;
        }

        #endregion

        #region Properties

        public int Amount { get; }

        public string[] Header { get; }

        public bool IsCharScreen { get; }

        #endregion
    }
}