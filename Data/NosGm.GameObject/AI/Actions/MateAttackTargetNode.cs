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

            if (mate?.BattleEntity == null ||
                target == null ||
                target.Hp <= 0 ||
                mate.BattleEntity.Hp <= 0 ||
                target.MapInstance != mate.BattleEntity.MapInstance)
            {
                _castStartTime = null;
                blackboard.Remove("Target");
                return BehaviorStatus.Failure;
            }

            if (_castStartTime == null)
            {
                IEnumerable<NpcMonsterSkill> mateSkills = mate.PSkills;
                if (mateSkills == null || !mateSkills.Any())
                {
                    return BehaviorStatus.Failure;
                }

                List<NpcMonsterSkill> possibleSkills = mateSkills
                    .Where(skill => skill?.Skill != null &&
                                    ((DateTime.Now - skill.LastSkillUse).TotalMilliseconds >=
                                     1000 * skill.Skill.Cooldown || skill.Rate == 0))
                    .ToList();

                _skill = null;
                foreach (NpcMonsterSkill skill in possibleSkills.OrderBy(_ => ServerManager.RandomNumber()))
                {
                    if (skill.Rate == 0)
                    {
                        _skill = skill;
                    }
                    else if (ServerManager.RandomNumber() < skill.Rate)
                    {
                        _skill = skill;
                        break;
                    }
                }

                if (_skill == null)
                {
                    // Fallback to the basic skill (easiest way is to pick the first one with rate 0, or just the first skill)
                    _skill = mateSkills.FirstOrDefault(s => s != null && s.Rate == 0) ?? mateSkills.FirstOrDefault(s => s != null);
                    if (_skill == null)
                    {
                        return BehaviorStatus.Running;
                    }
                }

                if (!mate.BattleEntity.CanAttackEntity(target))
                {
                    blackboard.Remove("Target");
                    return BehaviorStatus.Failure;
                }

                // A mate attack must create threat for the mate itself. Previously all
                // pet damage was effectively associated with the owner, so monsters
                // ignored the pet and it could never tank.
                if (target.MapMonster != null)
                {
                    target.MapMonster.AddToAggroList(mate.BattleEntity);
                    target.MapMonster.Target = mate.BattleEntity;
                }

                mate.TargetHit(target, _skill);

                _castTimeMs = _skill.Skill.CastTime > 0
                    ? _skill.Skill.CastTime * 100
                    : Math.Max(1000, _skill.Skill.Cooldown * 100);
                _castStartTime = DateTime.Now;
                return BehaviorStatus.Running;
            }

            if ((DateTime.Now - _castStartTime.Value).TotalMilliseconds < _castTimeMs)
            {
                return BehaviorStatus.Running;
            }

            _castStartTime = null;
            return BehaviorStatus.Success;
        }
    }
}
