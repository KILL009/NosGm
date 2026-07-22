using System.Threading;

namespace NosGm.SCS.Communication.Scs.Server
{
    internal static class ScsServerManager
    {
        private static long _lastClientId;

        public static long GetClientId() => Interlocked.Increment(ref ScsServerManager._lastClientId);
    }
}
