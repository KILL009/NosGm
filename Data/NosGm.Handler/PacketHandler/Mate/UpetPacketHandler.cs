using NosGm.Packets.Packets.ClientPackets;
using NosGm.Extension.Extension.Packet;
using NosGm.Core;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Helpers;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace NosGm.Handler.PacketHandler.Mate
{
    public class UpetPacketHandler : IPacketHandler
    {
        #region Instantiation

        public UpetPacketHandler(ClientSession session)
        {
            Session = session;
        }

        #endregion

        #region Properties

        public ClientSession Session { get; }

        #endregion

        #region Methods

        public async Task SpecialSkillAsync(UpetPacket upetPacket)
        {
            if (upetPacket == null)
            {
                return;
            }

            PenaltyLogDTO penalty = Session.Account.PenaltyLogs.OrderByDescending(s => s.DateEnd).FirstOrDefault();
            if (Session.Character.IsMuted() && penalty != null)
            {
                if (Session.Character.Gender == GenderType.Female)
                {
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_FEMALE"), 1));
                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"), (penalty.DateEnd - DateTime.Now).ToString("hh\\:mm\\:ss")), 11));
                }
                else
                {
                    Session.CurrentMapInstance?.Broadcast(Session.Character.GenerateSay(Language.Instance.GetMessageFromKey("MUTED_MALE"), 1));
                    Session.SendPacket(Session.Character.GenerateSay(string.Format(Language.Instance.GetMessageFromKey("MUTE_TIME"), (penalty.DateEnd - DateTime.Now).ToString("hh\\:mm\\:ss")), 11));
                }

                return;
            }

            var attacker = Session.Character.Mates.Find(x => x.IsTeamMember && x.MateType == MateType.Pet);
            if (attacker == null)
            {
                return;
            }

            NpcMonsterSkill mateSkill = null;
            if (attacker.Monster.Skills.Any())
            {
                mateSkill = attacker.Monster.Skills.FirstOrDefault(sk => MateHelper.Instance.PetSkills.Contains(sk.SkillVNum));
            }

            if (mateSkill == null)
            {
                mateSkill = new NpcMonsterSkill
                {
                    SkillVNum = 200
                };
            }

            if (mateSkill.LastSkillUse.AddMilliseconds((mateSkill.Skill?.Cooldown * 100) ?? 500) > DateTime.Now || (mateSkill != null && Session.Character.npcMonstersSkillsInCd.Contains(mateSkill.SkillVNum)))
            {
                return;
            }

            NpcMonsterSkill petskill = mateSkill;

            if (attacker.IsSitting)
            {
                return;
            }

            Skill skill = PartnerSkillHelper.ConvertToNormalPSkill(petskill);

            BattleEntity battleEntityAttacker = attacker.BattleEntity;
            BattleEntity battleEntityDefender = null;

            Observable.Timer(TimeSpan.FromMilliseconds(mateSkill.Skill.Cooldown * 100)).Subscribe(x => Session.Character.npcMonstersSkillsInCd.Remove(mateSkill.SkillVNum));
            Session.Character.npcMonstersSkillsInCd.Add(mateSkill.SkillVNum);

            switch (upetPacket.TargetType)
            {
                case UserType.Player:
                    {
                        var target = Session.Character.MapInstance?.GetCharacterById(upetPacket.TargetId);
                        battleEntityDefender = target?.BattleEntity;
                        Session.SendPacketAfter("petsr 0", mateSkill.Skill.Cooldown * 100);
                    }
                    break;

                case UserType.Npc:
                    {
                        var target = Session.Character.MapInstance?.GetMate(upetPacket.TargetId);
                        battleEntityDefender = target?.BattleEntity;
                        Session.SendPacketAfter("petsr 0", mateSkill.Skill.Cooldown * 100);
                    }
                    break;

                case UserType.Monster:
                    {
                        var target = Session.Character.MapInstance?.GetMonsterById(upetPacket.TargetId);
                        battleEntityDefender = target?.BattleEntity;
                        Session.SendPacketAfter("petsr 0", mateSkill.Skill.Cooldown * 100);
                    }
                    break;
            }

            MateExt.PetSkillTargetHit(battleEntityAttacker, battleEntityDefender, skill);
        }

        #endregion
    }
}