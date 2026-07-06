using Frostvein.Configuration;
using Frostvein.Packets.Packets.CommandPackets;
using Frostvein.Core;
using Frostvein.GameObject;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Frostvein.Thread.System;
using Frostvein.GameObject.TitanShield;
using System;
using System.Threading.Tasks;
using Frostvein.GameObject.ThreadEnum;
using Frostvein.GameObject.TitanShield.Thread;

namespace Frostvein.Handler.PacketHandler.Command
{
    public class TitanShieldHandler : IPacketHandler
    {
        public TitanShieldHandler(ClientSession session) => Session = session;

        public ClientSession Session { get; }

        public void TitanShieldCommand(TitanShieldPacket titanShieldPacket)
        {
           if (titanShieldPacket == null) return;
           if (titanShieldPacket.Action == null) return;

           switch (titanShieldPacket.Action)
           {
                case "GarbageCollection":
                    if (titanShieldPacket.Type == 1)
                    {
                        GarbageCollectionThread.Start();
                    }
                    else
                    {
                        GarbageCollectionThread.Run();
                        TitanShield.Callback(Session, "Successfully cleared the Garbage Collection");
                    }
                    break;

                case "MemoryCache":
                    MemoryCacheThread.Run();
                    TitanShield.Callback(Session, "Successfully cleared the Memory Cache");
                    break;

                case "WriteToDiscord":
                    TitanShield.SendToDiscord(titanShieldPacket.Message);
                    break;

                case "Player":
                    MessageExtension.SendHero(Session, $"Player Online: {PlayerCountThread.PlayerCount}");
                    break;

                case "Update":
                    GameConfiguration.Update((ConfigurationType)titanShieldPacket.Type, titanShieldPacket.Value);
                    TitanShield.Callback(Session, $"[ID {titanShieldPacket.Type}] Changed value to {titanShieldPacket.Value}");
                    break;

                case "UpdateInfo":
                    string info = "";
                    foreach (ConfigurationType type in Enum.GetValues(typeof(ConfigurationType)))
                    {
                        info += $"[{(int)type}] {type}: {GameConfiguration.GetConfigurationValue(type)}\n";
                    }
                    MessageExtension.SendModal(Session, info);
                    break;
           }
        }
    }
}