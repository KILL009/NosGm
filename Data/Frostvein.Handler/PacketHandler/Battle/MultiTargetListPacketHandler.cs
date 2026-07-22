using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Battle;
using Frostvein.GameObject.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Battle
{
    public class MultiTargetListPacketHandler : IPacketHandler
    {
        private const int MaximumTargetsPerPacket = 50;

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

            if (multiTargetListPacket.TargetsAmount == 0)
            {
                Session.Character.MTListTargetQueue.Clear();
                return;
            }

            if (multiTargetListPacket.TargetsAmount != multiTargetListPacket.Targets.Count ||
                multiTargetListPacket.TargetsAmount > MaximumTargetsPerPacket)
            {
                RejectTargetList();
                return;
            }

            var targetKeys = new HashSet<string>();
            var validatedTargets = new List<MTListHitTarget>(multiTargetListPacket.Targets.Count);

            foreach (MultiTargetListSubPacket subpacket in multiTargetListPacket.Targets)
            {
                if (subpacket == null || subpacket.TargetId <= 0)
                {
                    RejectTargetList();
                    return;
                }

                string targetKey = $"{(byte)subpacket.TargetType}:{subpacket.TargetId}";
                if (!targetKeys.Add(targetKey))
                {
                    RejectTargetList();
                    return;
                }

                BattleEntity target = Session.Character.MapInstance.BattleEntities.FirstOrDefault(entity =>
                    entity != null && entity.UserType == subpacket.TargetType &&
                    entity.MapEntityId == subpacket.TargetId);

                if (target == null || target.Hp <= 0)
                {
                    RejectTargetList();
                    return;
                }

                validatedTargets.Add(new MTListHitTarget(subpacket.TargetType, subpacket.TargetId,
                    TargetHitType.AOETargetHit));
            }

            Session.Character.MTListTargetQueue.Clear();
            foreach (MTListHitTarget target in validatedTargets)
            {
                Session.Character.MTListTargetQueue.Push(target);
            }
        }

        private void RejectTargetList()
        {
            Session.Character.MTListTargetQueue.Clear();
            Session.SendPacket(StaticPacketHelper.Cancel());
            Session.SendPacket("say 1 0 10 Action has been logged");
        }

        #endregion
    }
}
