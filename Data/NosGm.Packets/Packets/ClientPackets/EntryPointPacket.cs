using NosGm.Core;

namespace NosGm.Packets.Packets.ClientPackets
{
    [PacketHeader("NosGm.EntryPoint", IsCharScreen = true, Amount = 3)]
    public class NosGmEntryPointPacket : PacketDefinition
    {
        #region Properties

        [PacketIndex(0, SerializeToEnd = true)]
        public string PacketData { get; set; }

        #endregion

        //TODO: Find defined values
    }
}