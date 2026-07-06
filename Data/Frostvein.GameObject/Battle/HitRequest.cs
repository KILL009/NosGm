using Frostvein.Data;
using Frostvein.Domain;
using System;
using System.Collections.Generic;

namespace Frostvein.GameObject.Battle
{
    public class HitRequest
    {
        #region Instantiation

        public HitRequest(TargetHitType targetHitType, ClientSession session, Mate mate, NpcMonsterSkill skill)
        {
            HitTimestamp = DateTime.Now;
            Mate = mate;
            Skill = skill?.Skill;
            TargetHitType = targetHitType;
            Session = session;
            SkillBCards = skill?.Skill.BCards ?? new List<BCard>();
            SkillEffect = skill?.Skill.Effect ?? 0;
        }

        public HitRequest(TargetHitType targetHitType, ClientSession session, Mate mate, Skill skill)
        {
            HitTimestamp = DateTime.Now;
            Mate = mate;
            Skill = skill;
            TargetHitType = targetHitType;
            Session = session;
            SkillBCards = skill?.BCards ?? new List<BCard>();
            SkillEffect = skill?.Effect ?? 0;
        }

        public HitRequest(TargetHitType targetHitType, MapMonster monster, NpcMonsterSkill skill,
            bool showTargetAnimation = false)
        {
            HitTimestamp = DateTime.Now;
            Monster = monster;
            Skill = skill?.Skill;
            TargetHitType = targetHitType;
            SkillBCards = skill?.Skill.BCards ?? new List<BCard>();
            SkillEffect = skill.Skill.Effect;
            ShowTargetHitAnimation = showTargetAnimation;
        }

        public HitRequest(TargetHitType targetHitType, ClientSession session, Skill skill, short? skillEffect = null,
            short? mapX = null, short? mapY = null, ComboDTO skillCombo = null, bool showTargetAnimation = false,
            List<BCard> skillBCards = null, int directDamage = 0)
        {
            HitTimestamp = DateTime.Now;
            Session = session;
            Skill = skill;
            TargetHitType = targetHitType;
            SkillEffect = skillEffect ?? skill.Effect;
            ShowTargetHitAnimation = showTargetAnimation;
            DirectDamage = directDamage;

            if (mapX.HasValue) MapX = mapX.Value;

            if (mapY.HasValue) MapY = mapY.Value;

            if (skillCombo != null) SkillCombo = skillCombo;

            if (skillBCards != null)
                SkillBCards = skillBCards;
            else
                SkillBCards = skill?.BCards ?? new List<BCard>();
        }

        #endregion

        #region Properties

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
    }
}