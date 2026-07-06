using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Battle;
using Frostvein.GameObject.Helpers;
using System;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Battle
{
    public class MultiTargetListPacketHandler : IPacketHandler
    {
        #region Instantiation

        public MultiTargetListPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void MultiTargetListHit(MultiTargetListPacket multiTargetListPacket)
        {
            if (multiTargetListPacket?.Targets == null || Session?.Character?.MapInstance == null)
            {
                return;
            }

            if (Session.Character.IsVehicled || Session.Character.MuteMessage())
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }

            if ((DateTime.Now - Session.Character.LastTransform).TotalSeconds < 3)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"), 0));
                return;
            }

            if (multiTargetListPacket.TargetsAmount > 0 && multiTargetListPacket.Targets == null)
            {
                Session.SendPacket($"say 1 0 10 Action has been logged");
                return;
            }

            if (multiTargetListPacket.TargetsAmount > 0 && multiTargetListPacket.TargetsAmount == multiTargetListPacket.Targets.Count && multiTargetListPacket.Targets != null)
            {
                Session.Character.MTListTargetQueue.Clear();
                foreach (MultiTargetListSubPacket subpacket in multiTargetListPacket.Targets)
                {
                    Session.Character.MTListTargetQueue.Push(new MTListHitTarget(subpacket.TargetType, subpacket.TargetId,TargetHitType.AOETargetHit));
                }
            }
        }

        #endregion
    }
}