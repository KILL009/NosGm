using NosGm.Extension.Extension.Packet;
using NosGm.Packets.Packets.ClientPackets;
using NosGm.Core;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Extension;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace NosGm.Handler.PacketHandler.Battle
{
    public class UseSkillPacketHandler : IPacketHandler
    {
        #region Instantiation

        public UseSkillPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public void UseSkill(UseSkillPacket useSkillPacket)
        {
            if ((Session.Character.MapInstance == null) && !Session.Character.IsFishing)
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }
            if ((DateTime.Now - Session.Character.LastTransform).TotalSeconds < 3)
            {
                Session.SendPacket(UserInterfaceHelper.GenerateMsg(Language.Instance.GetMessageFromKey("CANT_ATTACK"),
                    0));
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }

            Session.Character.WalkDisposable?.Dispose();
            Session.Character.Direction = Session.Character.BeforeDirection;

            switch (useSkillPacket.UserType)
            {
                case UserType.Npc:
                    {
                        var target = Session.Character.MapInstance.GetNpc(useSkillPacket.MapMonsterId);

                        if (target != null)
                            if (Session.Character.Morph == 1000099 /* Hamster */ && target.NpcVNum == 2329
                                || Session.Character.Morph == 1000156 /* Bushtail */ && target.NpcVNum == 2330)
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                                Session.SendPacket($"delay 1000 13 #guri^513^2^{target.MapNpcId}");
                                Session.Character.MapInstance.Broadcast(UserInterfaceHelper.GenerateGuri(2, 1, Session.Character.CharacterId),ReceiverType.AllExceptMe);
                                return;
                            }
                    }
                    break;
            }

            if (!Session.Character.CanAttack())
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }

            switch (useSkillPacket.UserType)
            {
                case UserType.Player:
                    {
                        var target = ServerManager.Instance.GetSessionByCharacterId(useSkillPacket.MapMonsterId)?.Character;

                        if (target != null && target.CharacterId != Session.Character.CharacterId)
                            if (target.HasBuff(BCardType.CardType.FrozenDebuff,
                                (byte)AdditionalTypes.FrozenDebuff.EternalIce))
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                                Session.SendPacket($"delay 2000 5 #guri^502^1^{target.CharacterId}");
                                Session.Character.MapInstance.Broadcast(
                                    UserInterfaceHelper.GenerateGuri(2, 1, Session.Character.CharacterId),
                                    ReceiverType.AllExceptMe);
                                return;
                            }
                    }
                    break;
            }

            if (Session.Character.IsLaurenaMorph())
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }

            if (Session.Character.HasBuff(BCardType.CardType.HealingBurningAndCasting,
                    (byte)AdditionalTypes.HealingBurningAndCasting.DecreaseHPWhenCasting) &&
                useSkillPacket.CastId != 0)
            {
                var debuff = Session.Character.GetBuff(BCardType.CardType.HealingBurningAndCasting,
                    (byte)AdditionalTypes.HealingBurningAndCasting.DecreaseHPWhenCasting);

                Session.Character.Hp -= debuff[0];
                Session.CurrentMapInstance.Broadcast(Session.Character.GenerateDm(debuff[0]));
            }

            if (Session.Character.HasBuff(BCardType.CardType.Casting, (byte)AdditionalTypes.Casting.CastingSkillFailed) && useSkillPacket.CastId != 0)
            {
                var probs = Session.Character.GetBuff(BCardType.CardType.Casting,
                    (byte)AdditionalTypes.Casting.CastingSkillFailed);

                var probsFailing = 100 - probs[0];

                if (ServerManager.RandomProbabilityCheck(probsFailing))
                {
                    Session.SendPacket(StaticPacketHelper.Cancel());
                    return;
                }
            }

            if ((Session.Character.Morph == 29 && !useSkillPacket.CastId.CheckIfInRange(0, 7)) || (Session.Character.Morph == 30 && !useSkillPacket.CastId.CheckIfInRange(8, 16)))
            {
                Session.SendPacket(StaticPacketHelper.Cancel());
                return;
            }

            if (Session.Character.CanFight && useSkillPacket != null && !Session.Character.IsSeal)
            {
                if (useSkillPacket.UserType == UserType.Monster)
                {
                    var monsterToAttack = Session.Character.MapInstance.GetMonsterById(useSkillPacket.MapMonsterId);
                    if (monsterToAttack != null)
                    {
                        if (Session.Character.Quests.Any(q => q.Quest.QuestType == (int)QuestType.Required &&
                            q.Quest.QuestObjectives.Any(s => s.Data == monsterToAttack.MonsterVNum)))
                        {
                            Session.Character.IncrementQuests(QuestType.Required, monsterToAttack.MonsterVNum);
                            Session.SendPacket(StaticPacketHelper.Cancel());
                            return;
                        }
                    }
                }

                var skills = Session.Character.GetSkills();

                if (skills != null)
                {
                    var ski = skills.FirstOrDefault(s =>
                        s?.Skill?.CastId == useSkillPacket.CastId &&
                        (s.Skill?.UpgradeSkill == 0 || s.Skill?.SkillType == (byte)SkillType.CharacterSKill));

                    if (ski != null)
                    {
                        if (ski.GetSkillBCards().ToList().Any(s => s.Type.Equals((byte)BCardType.CardType.MeditationSkill)
                            && s.SubType.Equals((byte)AdditionalTypes.MeditationSkill.Sacrifice)))
                        {
                            if (Session.Character.MapInstance.BattleEntities.ToList().FirstOrDefault(s => s.UserType == useSkillPacket.UserType &&
                                    s.MapEntityId == useSkillPacket.MapMonsterId) is BattleEntity targetEntity)
                            {
                                if (Session.Character.BattleEntity.CanAttackEntity(targetEntity))
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel());
                                    return;
                                }
                            }
                        }
                        else if (!(ski.Skill.TargetType == 1 && ski.Skill.HitType != 1)
                                 && !(ski.Skill.TargetType == 2 && ski.Skill.HitType == 0)
                                 && !(ski.Skill.TargetType == 1 && ski.Skill.HitType == 1))
                        {
                            if (Session.Character.MapInstance.BattleEntities.ToList().FirstOrDefault(s => s.UserType == useSkillPacket.UserType &&
                                    s.MapEntityId == useSkillPacket.MapMonsterId) is BattleEntity targetEntity)
                            {
                                if (!Session.Character.BattleEntity.CanAttackEntity(targetEntity))
                                {
                                    Session.SendPacket(StaticPacketHelper.Cancel());
                                    return;
                                }
                            }
                        }
                    }
                }

                Session.Character.RemoveBuff(614);
                Session.Character.RemoveBuff(615);
                Session.Character.RemoveBuff(616);

                var isMuted = Session.Character.MuteMessage();

                if (isMuted || Session.Character.IsVehicled || Session.Character.InvisibleGm || skills?.FirstOrDefault(s => s?.Skill?.CastId == useSkillPacket.CastId)?.CanBeUsed() == false)
                {
                    Session.SendPacket(StaticPacketHelper.Cancel());
                    return;
                }

                // Client-provided coordinates are targeting metadata. They must never relocate the
                // authoritative server-side character position.

                if (Session.Character.IsSitting)
                {
                    Session.Character.Rest();
                }

                // The UserType is the enemy's type
                switch (useSkillPacket.UserType)
                {
                    case UserType.Npc:
                    case UserType.Monster:
                        if (Session.Character.Hp > 0)
                        {
                            Session.TargetHit(useSkillPacket.CastId, useSkillPacket.UserType,useSkillPacket.MapMonsterId);
                        }

                        break;

                    case UserType.Player:
                        if (Session.Character.Hp > 0)
                        {
                            if (useSkillPacket.MapMonsterId != Session.Character.CharacterId)
                                Session.TargetHit(useSkillPacket.CastId, useSkillPacket.UserType,
                                    useSkillPacket.MapMonsterId, true);
                            else
                                Session.TargetHit(useSkillPacket.CastId, useSkillPacket.UserType,
                                    useSkillPacket.MapMonsterId);
                        }
                        else
                        {
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                        }

                        break;

                    default:
                        Session.SendPacket(StaticPacketHelper.Cancel(2));
                        return;
                }

                if (skills != null)
                {
                    var ski = skills.FirstOrDefault(s => s?.Skill?.CastId == useSkillPacket.CastId);

                    if (ski == null || !(ski.Skill.TargetType == 1 && ski.Skill.HitType == 1))
                    {
                        if (Session.Character.MapInstance.BattleEntities.FirstOrDefault(s => s.MapEntityId == useSkillPacket.MapMonsterId) is BattleEntity target)
                        {
                            if (target.Hp <= 0)
                            {
                                Session.SendPacket(StaticPacketHelper.Cancel(2));
                            }
                        }
                        else
                        {
                            Session.SendPacket(StaticPacketHelper.Cancel(2));
                        }
                    }
                }
            }
            else
            {
                Session.SendPacket(StaticPacketHelper.Cancel(2));
            }
        }

        #endregion
    }
}
