namespace Frostvein.Core
{
    public static class StringExtension
    {
        #region Methods

        public static string GetIp(this string ip) => ip.Substring(6, ip.LastIndexOf(':') - 6);

        #endregion
    }
}