using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Helpers;
using System;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Mate
{
    public class PtCtlPacketHandler : IPacketHandler
    {
        #region Instantiation

        public PtCtlPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void PetMove(PtCtlPacket ptCtlPacket)
        {
            if (ptCtlPacket.PacketEnd == null || ptCtlPacket.Amount < 1)
            {
                Session.SendPacket($"say 1 0 10 Fixxed, thanks for try.");
                return;
            }

            var packetsplit = ptCtlPacket.PacketEnd.Split(' ');

            foreach (var mate in Session.Character.Mates)
            {
                if (mate == null || !mate.IsTeamMember || !mate.IsAlive) continue;

                // Find the Mate's ID in the packet string
                int idx = Array.IndexOf(packetsplit, mate.MateTransportId.ToString());
                if (idx != -1 && idx + 2 < packetsplit.Length)
                {
                    if (short.TryParse(packetsplit[idx + 1], out var positionX) && 
                        short.TryParse(packetsplit[idx + 2], out var positionY))
                    {
                        if (!mate.HasBuff(BCardType.CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible) &&
                            mate.Owner.Session.HasCurrentMapInstance && !mate.Owner.IsChangingMapInstance &&
                            Session.CurrentMapInstance?.Map?.IsBlockedZone(positionX, positionY) == false)
                        {
                            if (mate.Loyalty > 0)
                            {
                                mate.PositionX = positionX;
                                mate.PositionY = positionY;
                                if (Session.Character.MapInstance.MapInstanceType == MapInstanceType.BaseMapInstance)
                                {
                                    mate.MapX = positionX;
                                    mate.MapY = positionY;
                                }

                                Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.Move(UserType.Npc, mate.MateTransportId, positionX, positionY, mate.Monster.Speed));
                                if (mate.LastMonsterAggro.AddSeconds(5) > DateTime.Now) mate.UpdateBushFire();

                                Session.CurrentMapInstance?.OnMoveOnMapEvents?.ForEach(e => EventHelper.Instance.RunEvent(e));
                                Session.CurrentMapInstance?.OnMoveOnMapEvents?.RemoveAll(s => s != null);
                            }

                            Session.SendPacket(mate.GenerateCond());
                        }
                    }
                }
            }
        }

        #endregion
    }
}