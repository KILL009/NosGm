using Frostvein.Domain;
using System.Collections.Generic;

namespace Frostvein.GameObject.Helpers
{
    public class PassiveSkillHelper
    {
        #region Members

        private static PassiveSkillHelper _instance;

        #endregion

        #region Properties

        public static PassiveSkillHelper Instance => _instance ?? (_instance = new PassiveSkillHelper());

        #endregion

        #region Methods

        public List<BCard> PassiveSkillToBCards(IEnumerable<CharacterSkill> skills)
        {
            var bcards = new List<BCard>();

            if (skills != null)
                foreach (var skill in skills)
                    switch (skill.Skill.CastId)
                    {
                        case 0:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.MeleeIncreased
                            });
                            break;

                        case 1:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Target,
                                SubType = (byte)AdditionalTypes.Target.AllHitRateIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.DodgeAndDefencePercent,
                                SubType = (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.RangedIncreased
                            });
                            break;

                        case 2:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.MagicalIncreased
                            });
                            break;

                        case 4:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.MaxHPMP,
                                SubType = (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased
                            });
                            break;

                        case 5:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.MaxHPMP,
                                SubType = (byte)AdditionalTypes.MaxHPMP.MaximumMPIncreased
                            });
                            break;

                        case 6:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.AllAttacksIncreased
                            });
                            break;

                        case 7:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.AllIncreased
                            });
                            break;

                        case 8:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Recovery,
                                SubType = (byte)AdditionalTypes.Recovery.HPRecoveryIncreased
                            });
                            break;

                        case 9:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeSkill,
                                Type = (byte)BCardType.CardType.Recovery,
                                SubType = (byte)AdditionalTypes.Recovery.MPRecoveryIncreased
                            });
                            break;

                        case 19:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.SpecialisationBuffResistance,
                                SubType = (byte)AdditionalTypes.SpecialisationBuffResistance.IncreaseDamageInPVP
                            });
                            break;

                        case 20:
                            bcards.Add(new BCard
                            {
                                FirstData = -skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.SpecialisationBuffResistance,
                                SubType = (byte)AdditionalTypes.SpecialisationBuffResistance.DecreaseDamageInPVP
                            });
                            break;

                        case 21:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.MeleeAttacksIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.MeleeIncreased
                            });
                            break;

                        case 22:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Target,
                                SubType = (byte)AdditionalTypes.Target.AllHitRateIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.DodgeAndDefencePercent,
                                SubType = (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.RangedIncreased
                            });
                            break;

                        case 23:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.MagicalIncreased
                            });
                            break;

                        case 24:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.MaxHPMP,
                                SubType = (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased
                            });
                            break;

                        case 25:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.MaxHPMP,
                                SubType = (byte)AdditionalTypes.MaxHPMP.MaximumMPIncreased
                            });
                            break;

                        case 26:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.AllIncreased
                            });
                            break;

                        case 27:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.AllAttacksIncreased
                            });
                            break;

                        case 28:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.ElementResistance,
                                SubType = (byte)AdditionalTypes.ElementResistance.AllIncreased
                            });
                            break;

                        case 29:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Item,
                                SubType = (byte)AdditionalTypes.Item.EXPIncreased
                            });
                            break;

                        case 30:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Item,
                                SubType = (byte)AdditionalTypes.Item.IncreaseEarnedGold
                            });
                            break;

                        case 31:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.MaxHPMP,
                                SubType = (byte)AdditionalTypes.MaxHPMP.MaximumHPIncreased
                            });
                            break;

                        case 32:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Target,
                                SubType = (byte)AdditionalTypes.Target.AllHitRateIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.DodgeAndDefencePercent,
                                SubType = (byte)AdditionalTypes.DodgeAndDefencePercent.DodgeIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.RangedIncreased
                            });
                            break;

                        case 33:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.MagicalAttacksIncreased
                            });
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.Defence,
                                SubType = (byte)AdditionalTypes.Defence.MagicalIncreased
                            });
                            break;

                        case 34:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.AttackPower,
                                SubType = (byte)AdditionalTypes.AttackPower.AllAttacksIncreased
                            });
                            break;

                        case 35:
                            bcards.Add(new BCard
                            {
                                FirstData = skill.Skill.UpgradeType,
                                Type = (byte)BCardType.CardType.MaxHPMP,
                                SubType = (byte)AdditionalTypes.MaxHPMP.MaximumMPIncreased
                            });
                            break;
                    }

            return bcards;
        }

        #endregion
    }
}