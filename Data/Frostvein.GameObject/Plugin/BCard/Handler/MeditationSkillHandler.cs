using Game.Configuration.BCards;
using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject;
using Frostvein.GameObject.Helpers;
using Frostvein.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Game.Configuration
{
    public class MeditationSkillHandler : IBCardHandler
    {
        public BCardType.CardType ActionType => BCardType.CardType.MeditationSkill;

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

            if (caster.Character is Character character)
            {
                if (SubType.Equals((byte)AdditionalTypes.MeditationSkill.Sacrifice))
                {
                    target.AddBuff(new Buff((short)SecondData, casterLevel), caster);
                }
                else if (character.LastSkillComboUse < DateTime.Now)
                {
                    ItemInstance sp = null;

                    if (character.Inventory != null)
                    {
                        sp = character.Inventory.LoadBySlotAndType((byte)EquipmentType.Sp, InventoryType.Wear);
                    }

                    if (sp != null)
                    {
                        short newSkillVNum = (short)SecondData;

                        if (SkillVNum.HasValue
                            && SubType.Equals((byte)AdditionalTypes.MeditationSkill.CausingChance)
                            && ServerManager.RandomNumber() < firstData)
                        {
                            if (character.SkillComboCount < 7)
                            {
                                if (SkillHelper.IsCausingChance(SkillVNum.Value)
                                    && target != caster)
                                {
                                    return;
                                }

                                CharacterSkill oldSkill = character.GetSkill(SkillVNum.Value);
                                CharacterSkill newSkill = character.GetSkill(newSkillVNum);

                                if (oldSkill?.Skill != null && newSkill?.Skill != null)
                                {
                                    newSkill.FirstCastId = oldSkill.FirstCastId;
                                    {
                                        character.SkillComboCount++;
                                        character.LastSkillComboUse = DateTime.Now;

                                        Observable.Timer(TimeSpan.FromMilliseconds(100)).Subscribe(observer =>
                                        {
                                            character.Session.SendPacket($"mslot {newSkill.Skill.SkillVNum} -1");

                                            IEnumerable<QuicklistEntryDTO> quicklistEntries = character.QuicklistEntries.Where(s => s.Morph == sp.Item.Morph && s.Pos.Equals(oldSkill.FirstCastId.Value));

                                            foreach (QuicklistEntryDTO quicklistEntry in quicklistEntries)
                                            {
                                                character.Session.SendPacket($"qset {quicklistEntry.Q1} {quicklistEntry.Q2} {quicklistEntry.Type}.{quicklistEntry.Slot}.{newSkill.Skill.CastId}.0");
                                            }
                                        });

                                        if (oldSkill.Skill.CastId > 10)
                                        {
                                            Observable.Timer(TimeSpan.FromMilliseconds((oldSkill.Skill.Cooldown * 100) + 500)).Subscribe(observer =>
                                            {
                                                if (character.SkillsSp != null && character.SkillsSp.Any(s => s.Skill.SkillVNum == oldSkill.SkillVNum && s.CanBeUsed()))
                                                {
                                                    character.Session.SendPacket(StaticPacketHelper.SkillReset(oldSkill.Skill.CastId));
                                                }
                                            });
                                        }
                                    }
                                }
                            }
                        }

                        if (character.MeditationDictionary != null)
                        {
                            lock (character.MeditationDictionary)
                            {
                                switch (SubType)
                                {
                                    case 21:
                                        character.MeditationDictionary[newSkillVNum] = DateTime.Now.AddSeconds(4);
                                        break;

                                    case 31:
                                        character.MeditationDictionary[newSkillVNum] = DateTime.Now.AddSeconds(8);
                                        break;

                                    case 41:
                                        character.MeditationDictionary[newSkillVNum] = DateTime.Now.AddSeconds(12);
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
