using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using System;

namespace NosGm.GameObject._plugins
{
    public interface INrunHandler : IGenericGameHandler<NRunPacket, NRunType>
    {

    }

    public interface IGuriHandler : IGenericGameHandler<GuriPacket, GuriType>
    {

    }

    public interface IGenericGameHandler<in T, V> where T : PacketDefinition where V : Enum
    {
        public V[] ActionType { get; }

        void Execute(ClientSession player, T packet);
    }
}