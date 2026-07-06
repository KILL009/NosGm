using Frostvein.GameObject.Networking;
using Frostvein.GameObject.ThreadEnum;
using System;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Frostvein.Thread.System
{
    public static class PlayerCountThread
    {
        public static int PlayerCount { get; set; }

        static void UpdateTitle()
        {
            Console.Title = $"Frostvein - World Server [Channel {ServerManager.Instance.ChannelId} | {PlayerCount} Player Online]";
        }

        public static void UpdatePlayerCount(PlayerCountType playerCountType)
        {
            switch (playerCountType)
            {
                case PlayerCountType.Increased:
                    PlayerCount++;
                    break;

                case PlayerCountType.Decreased:
                    PlayerCount--;
                    break;
            }

            UpdateTitle();
        }
    }
}
