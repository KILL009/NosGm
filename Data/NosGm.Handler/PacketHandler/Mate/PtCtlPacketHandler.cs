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
            for (var i = 0; i < ptCtlPacket.Amount * 3; i += 3)
                if (packetsplit.Length >= ptCtlPacket.Amount * 3 && int.TryParse(packetsplit[i], out var petId)
                                                                 && short.TryParse(packetsplit[i + 1],
                                                                     out var positionX)
                                                                 && short.TryParse(packetsplit[i + 2],
                                                                     out var positionY))
                {
                    var mate = Session.Character.Mates.Find(s => s.MateTransportId == petId);
                    if (mate != null && mate.IsAlive &&
                        !mate.HasBuff(BCardType.CardType.Move, (byte)AdditionalTypes.Move.MovementImpossible) &&
                        mate.Owner.Session.HasCurrentMapInstance && !mate.Owner.IsChangingMapInstance
                        && Session.CurrentMapInstance?.Map?.IsBlockedZone(positionX, positionY) == false)
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

                            Session.CurrentMapInstance?.Broadcast(StaticPacketHelper.Move(UserType.Npc, petId, positionX, positionY, mate.Monster.Speed));
                            if (mate.LastMonsterAggro.AddSeconds(5) > DateTime.Now) mate.UpdateBushFire();

                            Session.CurrentMapInstance?.OnMoveOnMapEvents?.ForEach(e => EventHelper.Instance.RunEvent(e));
                            Session.CurrentMapInstance?.OnMoveOnMapEvents?.RemoveAll(s => s != null);
                        }

                        Session.SendPacket(mate.GenerateCond());
                    }
                }
        }

        #endregion
    }
}