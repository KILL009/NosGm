using Game.Configuration;
using Game.Configuration.BCards;
using NosGm.Data;
using NosGm.GameObject.Battle;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace NosGm.GameObject
{
    public class BCard : BCardDTO
    {
        public BCard()
        {
        }

        public BCard(BCardDTO input) : this()
        {
            BCardId = input.BCardId;
            CardId = input.CardId;
            CastType = input.CastType;
            FirstData = input.FirstData;
            IsLevelDivided = input.IsLevelDivided;
            IsLevelScaled = input.IsLevelScaled;
            ItemVNum = input.ItemVNum;
            NpcMonsterVNum = input.NpcMonsterVNum;
            SecondData = input.SecondData;
            SkillVNum = input.SkillVNum;
            SubType = input.SubType;
            ThirdData = input.ThirdData;
            Type = input.Type;
        }

        #region Properties

        public bool IsPartnerSkillBCard { get; set; }

        public int ForceDelay { get; set; }

        #endregion Properties

        #region Methods

        public void ApplyBCards(BattleEntity target, BattleEntity caster, short x = 0, short y = 0,
            short partnerBuffLevel = 0, short levelUpgraded = 0,
            BCardExecutionPhase executionPhase = BCardExecutionPhase.Unspecified,
            SkillCastContext castContext = null, HitContext hitContext = null)
        {
            int firstData = FirstData;
            int casterLevel = caster.MapMonster?.Owner?.Level ?? caster.Level;

            Card card = null;
            Skill skill = null;
            int delayTime = 0;
            int duration = 0;

            if (CardId is short cardId2 && ServerManager.Instance.GetCardByCardId(cardId2) is Card BuffCard)
            {
                card = BuffCard;

                if (CastType == 1)
                {
                    delayTime = card.Delay * 100;
                }

                duration = card.Duration * 100 - delayTime;
            }

            if (SkillVNum is short skillVNum && ServerManager.GetSkill(skillVNum) is Skill Skill)
            {
                skill = Skill;
                if (caster.Character != null)
                {
                    List<CharacterSkill> skills = caster.Character.GetSkills();

                    if (skills != null)
                    {
                        firstData = skills.Find(s => s.SkillVNum == skill.SkillVNum)?.GetSkillBCards()
                            .OrderByDescending(s => s.SkillVNum)
                            .FirstOrDefault(b => b.Type == Type && b.SubType == SubType)?.FirstData ?? FirstData;
                        if (firstData == 0)
                        {
                            firstData = FirstData;
                        }
                    }
                }
            }

            if (ForceDelay > 0)
            {
                delayTime = ForceDelay * 100;
            }

            int disposableKey = skill?.SkillVNum == 1098 ? skill.SkillVNum * 1000 : BCardId;
            if (BCardId > 0)
            {
                target.BCardDisposables[disposableKey]?.Dispose();
            }

            target.BCardDisposables[disposableKey] =
                Observable.Timer(TimeSpan.FromMilliseconds(delayTime)).Subscribe(o =>
                {
                    PluginFacility.HandleBCard(new BCardEvent
                    {
                        Target = target,
                        Caster = caster,
                        Card = card,
                        BCard = this,
                        LevelUpgraded = levelUpgraded,
                        X = x,
                        Y = y,
                        Skill = skill,
                        FirstData = firstData,
                        CasterLevel = casterLevel,
                        DelayTime = delayTime,
                        Duration = duration,
                        ExecutionPhase = executionPhase,
                        CastContext = castContext,
                        HitContext = hitContext
                    });
                });
        }

        #endregion Methods
    }
}
