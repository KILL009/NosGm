using NosGm.GameObject.Networking;
using NosGm.GameObject.ThreadEnum;
using System;
using System.Threading;

namespace NosGm.GameObject.NosGm.Thread.System
{
    public static class PlayerCountThread
    {
        private static int _playerCount;

        public static int PlayerCount
        {
            get => Volatile.Read(ref _playerCount);
            set => Interlocked.Exchange(ref _playerCount, Math.Max(0, value));
        }

        static void UpdateTitle()
        {
            Console.Title = $"NosGm - World Server [Channel {ServerManager.Instance.ChannelId} | {PlayerCount} Player Online]";
        }

        public static void UpdatePlayerCount(PlayerCountType playerCountType)
        {
            switch (playerCountType)
            {
                case PlayerCountType.Increased:
                    Interlocked.Increment(ref _playerCount);
                    break;

                case PlayerCountType.Decreased:
                    DecrementWithoutGoingNegative();
                    break;
            }

            // Read the current atomic value when updating the title. Under a
            // concurrent login/logout wave an earlier caller may reach this point
            // after a later caller, so using the live value prevents a stale title.
            UpdateTitle();
        }

        private static void DecrementWithoutGoingNegative()
        {
            while (true)
            {
                int current = Volatile.Read(ref _playerCount);
                if (current <= 0)
                {
                    return;
                }

                if (Interlocked.CompareExchange(
                        ref _playerCount,
                        current - 1,
                        current) == current)
                {
                    return;
                }
            }
        }
    }
}
