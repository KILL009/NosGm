using Game.Configuration.BCards;
using Frostvein.Data;
using Frostvein.Domain;
using System;
using System.Collections.Generic;

namespace Frostvein.GameObject.Battle
{
    public class HitRequest
    {
        #region Instantiation

        public HitRequest(TargetHitType targetHitType, ClientSession session, Mate mate, NpcMonsterSkill skill,
            SkillCastContext castContext = null)
        {
            HitTimestamp = DateTime.Now;
            Mate = mate;
            Skill = skill?.Skill;
            TargetHitType = targetHitType;
            Session = session;
            SkillBCards = skill?.Skill.BCards ?? new List<BCard>();
            SkillEffect = skill?.Skill.Effect ?? 0;
            CastContext = castContext ?? CreateCastContext(mate?.BattleEntity, Skill);
        }

        public HitRequest(TargetHitType targetHitType, ClientSession session, Mate mate, Skill skill,
            SkillCastContext castContext = null)
        {
            HitTimestamp = DateTime.Now;
            Mate = mate;
            Skill = skill;
            TargetHitType = targetHitType;
            Session = session;
            SkillBCards = skill?.BCards ?? new List<BCard>();
            SkillEffect = skill?.Effect ?? 0;
            CastContext = castContext ?? CreateCastContext(mate?.BattleEntity, Skill);
        }

        public HitRequest(TargetHitType targetHitType, MapMonster monster, NpcMonsterSkill skill,
            bool showTargetAnimation = false, SkillCastContext castContext = null)
        {
            HitTimestamp = DateTime.Now;
            Monster = monster;
            Skill = skill?.Skill;
            TargetHitType = targetHitType;
            SkillBCards = skill?.Skill.BCards ?? new List<BCard>();
            SkillEffect = skill?.Skill.Effect ?? 0;
            ShowTargetHitAnimation = showTargetAnimation;
            CastContext = castContext ?? CreateCastContext(monster?.BattleEntity, Skill);
        }

        public HitRequest(TargetHitType targetHitType, ClientSession session, Skill skill, short? skillEffect = null,
            short? mapX = null, short? mapY = null, ComboDTO skillCombo = null, bool showTargetAnimation = false,
            List<BCard> skillBCards = null, int directDamage = 0, SkillCastContext castContext = null)
        {
            HitTimestamp = DateTime.Now;
            Session = session;
            Skill = skill;
            TargetHitType = targetHitType;
            SkillEffect = skillEffect ?? skill?.Effect ?? 0;
            ShowTargetHitAnimation = showTargetAnimation;
            DirectDamage = directDamage;

            if (mapX.HasValue) MapX = mapX.Value;

            if (mapY.HasValue) MapY = mapY.Value;

            if (skillCombo != null) SkillCombo = skillCombo;

            if (skillBCards != null)
                SkillBCards = skillBCards;
            else
                SkillBCards = skill?.BCards ?? new List<BCard>();

            CastContext = castContext ?? CreateCastContext(session?.Character?.BattleEntity, Skill);
        }

        #endregion

        #region Properties

        public SkillCastContext CastContext { get; set; }

        public int DirectDamage { get; }

        public DateTime HitTimestamp { get; set; }

        public short MapX { get; set; }

        public short MapY { get; set; }

        public Mate Mate { get; set; }

        public MapMonster Monster { get; set; }

        public NpcMonsterSkill NpcMonsterSkill { get; set; }

        public ClientSession Session { get; set; }

        /// <summary>
        ///     Some AOE Skills need to show additional SU packet for Animation
        /// </summary>
        public bool ShowTargetHitAnimation { get; set; }

        public Skill Skill { get; set; }

        public List<BCard> SkillBCards { get; set; }

        public ComboDTO SkillCombo { get; set; }

        public short SkillEffect { get; set; }

        public TargetHitType TargetHitType { get; set; }

        #endregion

        public HitContext CreateHitContext(BattleEntity target, int hitIndex = 0)
        {
            return new HitContext
            {
                CastContext = CastContext,
                Target = target,
                HitIndex = hitIndex
            };
        }

        private static SkillCastContext CreateCastContext(BattleEntity caster, Skill skill)
        {
            short originX = caster?.PositionX ?? 0;
            short originY = caster?.PositionY ?? 0;
            return SkillCastContext.Create(caster, skill, originX, originY,
                caster?.Character != null && skill != null && caster.Character.MapInstance != null &&
                caster.Character.MapInstance.MapInstanceType == MapInstanceType.PVPInstance);
        }
    }
}
