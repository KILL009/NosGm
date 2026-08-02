using NosGm.AI.Core;
using NosGm.GameObject;
using NosGm.GameObject.Networking;
using NosGm.GameObject.Battle;
using System.Linq;
using System;
using NosGm.Core;
using System.Collections.Generic;

namespace NosGm.GameObject.AI.Actions
{
    public class MateAttackTargetNode : IBehaviorNode
    {
        private DateTime? _castStartTime;
        private int _castTimeMs;
        private NpcMonsterSkill _skill;

        public BehaviorStatus Tick(Blackboard blackboard)
        {
            var mate = blackboard.Get<Mate>("Self");
            var target = blackboard.Get<BattleEntity>("Target");

            if (mate == null || target == null || target.Hp <= 0 || mate.BattleEntity.Hp <= 0) return BehaviorStatus.Failure;

            if (_castStartTime == null)
            {
                // Select a skill like SuctlPacketHandler does
                IEnumerable<NpcMonsterSkill> mateSkills = mate.PSkills;
                if (mateSkills == null || !mateSkills.Any()) return BehaviorStatus.Failure;

                List<NpcMonsterSkill> PossibleSkills = mateSkills.Where(s => s.Skill != null && ((DateTime.Now - s.LastSkillUse).TotalMilliseconds >= 1000 * s.Skill.Cooldown || s.Rate == 0)).ToList();

                _skill = null;
                foreach (NpcMonsterSkill ski in PossibleSkills.OrderBy(rnd => ServerManager.RandomNumber()))
                {
                    if (ski.Rate == 0)
                    {
                        _skill = ski;
                    }
                    else if (ServerManager.RandomNumber() < ski.Rate)
                    {
                        _skill = ski;
                        break;
                    }
                }

                if (_skill == null) return BehaviorStatus.Running; // Wait for cooldown

                // Mates just attack instantly, there isn't a complex cast time like monsters usually have, but let's check distance
                if (!mate.BattleEntity.CanAttackEntity(target)) return BehaviorStatus.Failure;

                // Move if needed? Mate usually moves client-side, but if we enforce server side attack:
                // Actually, SuctlPacketHandler just calls TargetHit directly.
                mate.TargetHit(target, _skill);
                
                // Add a small cooldown/delay based on skill cast time to avoid spamming
                _castTimeMs = _skill.Skill.CastTime > 0 ? _skill.Skill.CastTime * 100 : 1000;
                _castStartTime = DateTime.Now;
                return BehaviorStatus.Running;
            }
            else
            {
                if ((DateTime.Now - _castStartTime.Value).TotalMilliseconds >= _castTimeMs)
                {
                    _castStartTime = null; // reset
                    return BehaviorStatus.Success;
                }
                return BehaviorStatus.Running;
            }
        }
    }
}
