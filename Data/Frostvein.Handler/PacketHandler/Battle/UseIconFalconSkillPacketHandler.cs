using Frostvein.Extension.Extension.Packet;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Battle;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Battle
{
    public class UseIconFalconSkillPacketHandler : IPacketHandler
    {
        #region Instantiation

        public UseIconFalconSkillPacketHandler(ClientSession session) => Session = session;

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void UseIconFalconSkill(UseIconFalconSkillPacket useIconFalconSkillPacket)
        {
            bool IsActivated = false;
            if (IsActivated)
            {
                if ((DateTime.Now - Session.Character.LastTransform).TotalSeconds < 3)
                {
                    Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"), 0));
                    return;
                }
                if (Session.Character.LastSkillUseNew > DateTime.Now)
                {
                    Session.SendPacket(StaticPacketHelper.Cancel());
                    return;
                }
                if (Session.Character.LastBugSkill > DateTime.Now)
                {
                    Session.SendPacket(StaticPacketHelper.Cancel());
                    return;
                }
                if (Session.Character.BattleEntity.FalconFocusedEntityId > 0)
                {
                    var iconSkillHitRequest = new HitRequest(TargetHitType.SingleTargetHit, Session, ServerManager.GetSkill(1248), 4283);
                    if (Session.CurrentMapInstance.BattleEntities.FirstOrDefault(s => s.MapEntityId == Session.Character.BattleEntity.FalconFocusedEntityId) is BattleEntity FalconFocusedEntity)
                    {
                        Session.SendPacket("ob_ar");
                        switch (FalconFocusedEntity.EntityType)
                        {
                            case EntityType.Player:
                                Session.PvpHit(iconSkillHitRequest, FalconFocusedEntity.Character.Session);
                                break;

                            case EntityType.Monster:
                                FalconFocusedEntity.MapMonster.HitQueue.Enqueue(iconSkillHitRequest);
                                break;

                            case EntityType.Mate:
                                FalconFocusedEntity.Mate.HitRequest(iconSkillHitRequest);
                                break;
                        }

                        Session.CurrentMapInstance.Broadcast(Session,
                            $"eff_ob {(byte)FalconFocusedEntity.UserType} {FalconFocusedEntity.MapEntityId} 0 4269",
                            ReceiverType.AllExceptMe);
                    }
                }
                Session.Character.LastSkillUseNew = DateTime.Now.AddMilliseconds(700);
                Session.Character.LastBugSkill = DateTime.Now.AddSeconds(25);
            }
        }
            

        #endregion
    }
}