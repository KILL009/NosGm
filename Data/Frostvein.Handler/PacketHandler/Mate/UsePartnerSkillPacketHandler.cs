using Frostvein.Configuration;
using Frostvein.Extension.Extension.Packet;
using Frostvein.Packets.Packets.ClientPackets;
using Frostvein.Core;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Battle;
using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace Frostvein.Handler.PacketHandler.Mate
{
    public class UsePartnerSkillPacketHandler : IPacketHandler
    {
        #region Instantiation

        public UsePartnerSkillPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void UseSkill(UsePartnerSkillPacket usePartnerSkillPacket)
        {
            if (!GameConfiguration.PartnerSkillsEnabled && Session.CurrentMapInstance.MapInstanceType is MapInstanceType.TimeSpaceInstance or MapInstanceType.RaidInstance or MapInstanceType.CustomInstance)
            {
                MessageExtension.SendRed(Session, "This can't be done here");
                return;
            }

            #region Invalid packet

            if (usePartnerSkillPacket == null)
            {
                return;
            }
            #endregion

            #region Mate not found (or invalid)

            var mate = Session?.Character?.Mates?.ToList().FirstOrDefault(s => s.IsTeamMember && s.MateType == MateType.Partner && s.MateTransportId == usePartnerSkillPacket.TransportId);

            if (mate?.Monster == null)
            {
                return;
            }

            #endregion

            #region Not using PSP

            if (mate.Sp == null || !mate.IsUsingSp)
            {
                return;
            }

            #endregion

            #region Skill not found

            PartnerSkill partnerSkill = mate.Sp.GetSkill(usePartnerSkillPacket.CastId);

            if (partnerSkill == null)
            {
                return;
            }

            #endregion

            #region Convert PartnerSkill to Skill

            Skill skill = PartnerSkillHelper.ConvertToNormalSkill(partnerSkill);

            #endregion

            #region Battle entities

            BattleEntity battleEntityAttacker = new BattleEntity(mate.BattleEntity.Mate);
            BattleEntity battleEntityDefender = null;

            switch (usePartnerSkillPacket.TargetType)
            {
                case UserType.Player:
                    {
                        Character target = Session.Character.MapInstance?.GetCharacterById(usePartnerSkillPacket.TargetId);
                        battleEntityDefender = target?.BattleEntity;
                    }
                    break;

                case UserType.Npc:
                    {
                        var target = Session.Character.MapInstance?.GetMate(usePartnerSkillPacket.TargetId);
                        battleEntityDefender = target?.BattleEntity;
                    }
                    break;

                case UserType.Monster:
                    {
                        MapMonster target = Session.Character.MapInstance?.GetMonsterById(usePartnerSkillPacket.TargetId);
                        battleEntityDefender = target?.BattleEntity;
                    }
                    break;

                default:
                    Session.SendPacket(StaticPacketHelper.Cancel(2, usePartnerSkillPacket.TargetId));
                    break;
            }

            #endregion

            #region Attack

            MateExt.PartnerSkillTargetHit(battleEntityAttacker, battleEntityDefender, skill);

            #endregion
        }



        #endregion
    }
}