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
            IsCharScreen = ContainsModernLoginHeader(header);
        }

        public PacketAttribute(params string[] header)
        {
            Header = header;
            Amount = 1;
            IsCharScreen = ContainsModernLoginHeader(header);
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

        private static bool ContainsModernLoginHeader(string[] header)
        {
            return header != null && Array.Exists(
                header,
                value => string.Equals(value, "NoS0576", StringComparison.Ordinal) ||
                         string.Equals(value, "NoS0577", StringComparison.Ordinal));
        }
    }
}