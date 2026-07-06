using Game.Configuration.BCards;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject.Buff;
using OpenNos.GameObject.Helpers;
using OpenNos.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Configuration
{
    public class AddBuffHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.Buff;

        public void Execute(BCardEvent evnt)
        {
            var caster = evnt.Caster;
            var target = evnt.Target;
            var SecondData = evnt.BCard.SecondData;
            var firstData = evnt.FirstData;
            var x = evnt.X;
            var y = evnt.Y;
            var skill = evnt.Skill;
            var ThirdData = evnt.BCard.ThirdData;
            var CardId = evnt.BCard.CardId;
            var SkillVNum = evnt.BCard.SkillVNum;
            var BCardId = evnt.BCard.BCardId;
            var SubType = evnt.BCard.SubType;
            var FirstData = evnt.FirstData;
            var casterLevel = evnt.CasterLevel;
            var delaytime = evnt.DelayTime;
            var duration = evnt.Duration;

            short cardId = (short)(SecondData + evnt.LevelUpgraded);

            // Memorial should only be applied on 1st Mass Teleport activation

            if (cardId == 620 && caster?.Character?.SavedLocation != null)
            {
                return;
            }

            Buff buff = new(cardId, casterLevel)
            {
                SkillVNum = SkillVNum
            };

            if (buff.Card.BuffType == BuffType.Bad && target.HasBuff(BCardType.CardType.NoDefeatAndNoDamage, (byte)AdditionalTypes.NoDefeatAndNoDamage.TransferAttackPower))
            {
                return;
            }

            int Chance = firstData == 0 ? ThirdData : firstData;
            List<short> CardsToProtect = new List<short>();
            if (buff.Card.BuffType == BuffType.Bad && target.GetBuff(BCardType.CardType.DebuffResistance, (byte)AdditionalTypes.DebuffResistance.NeverBadEffectChance) is int[] NeverBadEffectChance)
            {
                if (ServerManager.RandomNumber() < NeverBadEffectChance[1]
                    && buff.Card.Level <= NeverBadEffectChance[0])
                {
                    return;
                }
            }
            if (target.GetBuff(BCardType.CardType.DebuffResistance, (byte)AdditionalTypes.DebuffResistance.NeverBadGeneralEffectChance) is int[] NeverBadGeneralEffectChance)
            {
                if (ServerManager.RandomNumber() < NeverBadGeneralEffectChance[1]
                    && buff.Card.Level <= NeverBadGeneralEffectChance[0]
                    && buff.Card.BuffType == BuffType.Bad)
                {
                    return;
                }
            }
            if (target.GetBuff(BCardType.CardType.Buff, (byte)AdditionalTypes.Buff.PreventingBadEffect) is int[] PreventingBadEffect && (PreventingBadEffect[1] > 0 || PreventingBadEffect[2] > 0))
            {
                int Prob = 100 - PreventingBadEffect[1] * 10;
                int ProtectType = PreventingBadEffect[0];

                if (PreventingBadEffect[2] > 0)
                {
                    Prob = PreventingBadEffect[2];
                    ProtectType = PreventingBadEffect[1];
                }

                if (ServerManager.RandomNumber() < Prob && buff.Card.BuffType == BuffType.Bad)
                {
                    switch (ProtectType)
                    {
                        case 0:
                            //Bleedings
                            CardsToProtect.Add(1);
                            CardsToProtect.Add(21);
                            CardsToProtect.Add(42);
                            CardsToProtect.Add(82);
                            CardsToProtect.Add(189);
                            CardsToProtect.Add(190);
                            CardsToProtect.Add(191);
                            CardsToProtect.Add(192);
                            break;

                        case 4:
                            //Blackouts
                            CardsToProtect.Add(7);
                            CardsToProtect.Add(66);
                            CardsToProtect.Add(100);
                            CardsToProtect.Add(195);
                            CardsToProtect.Add(196);
                            CardsToProtect.Add(197);
                            CardsToProtect.Add(198);
                            break;

                        case 32:
                            //Side-effects of resurrecting
                            CardsToProtect.Add(44);
                            break;

                        case 85:
                            //Foggy Colossus' poison
                            break;
                    }
                }
            }

            if (buff.Card.BuffType == BuffType.Bad && target.GetBuff(BCardType.CardType.SpecialisationBuffResistance, (byte)AdditionalTypes.SpecialisationBuffResistance.ResistanceToEffect, buff.Card.CardId) is int[] ResistanceToEffect)
            {
                if (ServerManager.RandomNumber() < ResistanceToEffect[0])
                {
                    CardsToProtect.Add((short)ResistanceToEffect[1]);
                }
            }

            if (CardsToProtect.Contains(buff.Card.CardId))
            {
                return;
            }

            int antiDebuffBonus = 0;
            if (target.Character != null)
            {
                foreach (ShellEffectDTO eqopt in target.Character.ShellEffectArmor)
                {
                    switch ((ShellArmorEffectType)eqopt.Effect)
                    {
                        case ShellArmorEffectType.ReducedAllNegativeEffect:
                            antiDebuffBonus += eqopt.Value;
                            break;

                        case ShellArmorEffectType.ReducedAllStun:
                            if (BuffHelper.Instance.SyncopeGlobal.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;

                        case ShellArmorEffectType.ReducedAllBleedingType:
                            if (BuffHelper.Instance.BleedingGlobal.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;

                        case ShellArmorEffectType.ReducedParalysis:
                            if (BuffHelper.Instance.ReducedParalysis.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;

                        case ShellArmorEffectType.ReducedPoisonParalysis:
                            if (BuffHelper.Instance.PoisonParalysis.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;

                        case ShellArmorEffectType.ReducedStun:
                            if (BuffHelper.Instance.Syncope.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;

                        case ShellArmorEffectType.ReducedSlow:
                            if (BuffHelper.Instance.Slowness.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;

                        case ShellArmorEffectType.ReducedShock:
                            if (BuffHelper.Instance.Shock.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;

                        case ShellArmorEffectType.ReducedMinorBleeding:
                            if (BuffHelper.Instance.BleedingMinor.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;

                        case ShellArmorEffectType.ReducedFreeze:
                            if (BuffHelper.Instance.Freeze.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;

                        case ShellArmorEffectType.ReducedBlind:
                            if (BuffHelper.Instance.Blinding.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;

                        case ShellArmorEffectType.ReducedBleedingAndMinorBleeding:
                            if (BuffHelper.Instance.BleedingMinorBleeding.Any(bf => bf == buff.Card.CardId))
                            {
                                antiDebuffBonus += eqopt.Value;
                            }

                            break;
                    }
                }
            }

            if (ServerManager.RandomNumber() < (antiDebuffBonus >= 100 ? 97 : antiDebuffBonus) && buff?.Card?.BuffType == BuffType.Bad)
            {
                return;
            }

            switch (SubType)
            {
                case (byte)AdditionalTypes.Buff.ChanceCausing:
                    if (ServerManager.RandomNumber() < Chance)
                    {
                        if (SkillVNum != null && (buff.Card.CardId == 570 || buff.Card.CardId == 56))
                        {
                            caster.AddBuff(buff, caster, x: x, y: y, forced: true);
                        }
                        else if (buff.Card?.BuffType == BuffType.Bad
                            && target.HasBuff(BCardType.CardType.TauntSkill, (byte)AdditionalTypes.TauntSkill.ReflectBadEffect)
                            && ServerManager.RandomNumber() < FirstData)
                        {
                            caster.AddBuff(buff, caster, x: x, y: y);
                        }
                        else
                        {
                            target.AddBuff(buff, caster, x: x, y: y);
                        }
                    }
                    break;

                case (byte)AdditionalTypes.Buff.ChanceRemoving:
                    if (ServerManager.RandomNumber() < Chance)
                    {
                        target.RemoveBuff(cardId);
                    }
                    break;
            }

        }
    }
}