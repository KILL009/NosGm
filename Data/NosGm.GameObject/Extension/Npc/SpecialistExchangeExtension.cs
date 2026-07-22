using System.Collections.Generic;
using System.Threading.Tasks;
using NosGm.Packets.Packets.ClientPackets;

namespace NosGm.GameObject.Extension.Npc;

public static class SpecialistExchangeExtension
{
	private const short SpecialistShardVnum = 13005;

	private static readonly Dictionary<short, (short[] vnums, int amount)> DictPacketValues = new()
	{
		{ 0, (new short[] { 901, 903, 905 }, 5) },
		{ 1, (new short[] { 902, 904, 906 }, 10) },
		{ 2, (new short[] { 909, 911, 913 }, 35) },
		{ 3, (new short[] { 910, 912, 914 }, 50) },
		{ 4, (new short[] { 4500, 4501, 4502 }, 65) },
		{ 5, (new short[] { 4497, 4498, 4499 }, 75) },
		{ 6, (new short[] { 4493, 4492, 4491 }, 85) },
		{ 7, (new short[] { 4489, 4488, 4487 }, 95) },
		{ 8, (new short[] { 4581, 4582, 4583 }, 120) },
		{ 9, (new short[] { 8521, 8522, 8523 }, 150) }
	};

	public static void Exchange(ClientSession session, NRunPacket packet)
	{
		if ( packet.Type == 10 )
		{
			session.SendPacket("modal 1 The Specialist Shard is a Monster Drop.\nWhen killing a Monster, there is a certain Chance to obtain a Shard. Works in any Act/Map.");
			return;
		}

		if ( !DictPacketValues.TryGetValue(packet.Type, out var exchangeValues) )
			return; //Shouldn't ever happen but security against Item not in Dictionary found

		var (vnums, amountRequired) = exchangeValues;
		var specialistToAdd = vnums[(int)session.Character.Class - 1];

		if ( !session.Character.Inventory.CanAddItem(specialistToAdd) )
		{
			session.SendPacket("info You don't have enough Space in your Inventory!");
			return;
		}

		if ( session.Character.Inventory.CountItem(SpecialistShardVnum) < amountRequired )
		{
			session.SendPacket("msg 4 You dont have enough Specialist Shards");
			return;
		}

		session.Character.GiftAdd(specialistToAdd, 1);
		session.Character.Inventory.RemoveItemAmount(SpecialistShardVnum, amountRequired);
	}
}
