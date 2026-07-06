using Frostvein.Core;

namespace Frostvein.Packets.Packets.ClientPackets
{
	[PacketHeader("Br")]
	public class BrPacket : PacketDefinition
	{
		#region Properties

		[PacketIndex(0)]
		public int ItemId { get; set; }

		[PacketIndex(1)]
		public short PosX { get; set; }

		[PacketIndex(2)]
		public short PosY { get; set; }


		#endregion
	}

}