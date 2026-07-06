using Frostvein.GameObject._Event;

namespace Frostvein.GameObject.Characters.Events
{
    public class PlayerChangeChannelEvent : PlayerEvent
    {
        public PlayerChangeChannelEvent(string ip, int port, byte mode) => (Ip, Port, Mode) = (ip, port, mode);

        public string Ip { get; }

        public int Port { get; }

        public byte Mode { get; }
    }
}
