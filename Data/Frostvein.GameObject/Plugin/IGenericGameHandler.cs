using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using System;

namespace Frostvein.GameObject._plugins
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