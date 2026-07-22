using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using static NosGm.Domain.BCardType;

namespace NosGm.GameObject
{
    public class CharacterSkill : CharacterSkillDTO
    {
        #region Members

        private short? _firstCastId;

        private Skill _skill;

        #endregion

        #region Instantiation

        public CharacterSkill()
        {
            LastUse = DateTime.Now.AddHours(-1);
            Hit = 0;
        }

        public CharacterSkill(CharacterSkillDTO input) : this()
        {
            CharacterId = input.CharacterId;
            Id = input.Id;
            SkillVNum = input.SkillVNum;
            IsTattoo = input.IsTattoo;
            TattooLevel = input.TattooLevel;
        }

        #endregion

        #region Properties

        public short? FirstCastId
        {
            get => _firstCastId ?? (_firstCastId = Skill.CastId);
            set => _firstCastId = value;
        }

        public short Hit { get; set; }

        public DateTime LastUse { get; set; }

        public Skill Skill => _skill ?? (_skill = ServerManager.GetSkill(SkillVNum));

        #endregion

        #region Methods

        public bool CanBeUsed()
        {
            return Skill != null && LastUse.AddMilliseconds(Skill.Cooldown * 100) < DateTime.Now;
        }

        public void ReinstantiateSkill()
        {
            var newSkill = ServerManager.GetSkill(SkillVNum);
            _skill = new Skill(newSkill);

            _skill.BCards = newSkill.BCards;
            _skill.Combos = newSkill.Combos;
            _skill.PartnerSkill = newSkill.PartnerSkill;
        }

        public List<BCard> GetSkillBCards()
        {
            var SkillBCards = new List<BCard>();
            SkillBCards.AddRange(Skill.BCards);
            if (ServerManager.Instance.GetSessionByCharacterId(CharacterId) is ClientSession Session)
            {
                var skills = Session.Character.GetSkills();

                //Upgrade Skills
                var upgradeSkills = skills.FindAll(s => s.Skill?.UpgradeSkill == SkillVNum);
                if (upgradeSkills?.Count > 0)
                {
                    foreach (var upgradeSkill in upgradeSkills) SkillBCards.AddRange(upgradeSkill.Skill.BCards);
                    if (upgradeSkills.OrderByDescending(s => s.SkillVNum).FirstOrDefault() is CharacterSkill
                        LastUpgradeSkill)
                        if (LastUpgradeSkill.Skill.BCards.Any(s => s.Type == 25 && s.SubType == 11))
                            SkillBCards.Where(s =>
                                    s.Type == 25 && s.SubType == 11 && s.SkillVNum != LastUpgradeSkill.SkillVNum)
                                .ToList()
                                .ForEach(s => SkillBCards.Remove(s)); // Only buffs of last upgrade skill
                }

                //Passive Skills
                SkillBCards.AddRange(
                    PassiveSkillHelper.Instance.PassiveSkillToBCards(
                        Session.Character.Skills?.Where(s => s.Skill.SkillType == 0)));

                NormalizeModernGaugeContributors(SkillBCards);

                if (Skill.SkillVNum == 1123)
                    foreach (var ambushBCard in Session.Character.Buff.GetAllItems().SelectMany(s =>
                        s.Card.BCards.Where(b =>
                            b.Type == (byte)CardType.FearSkill &&
                            b.SubType == (byte)AdditionalTypes.FearSkill.ProduceWhenAmbushe)))
                        SkillBCards.Add(ambushBCard);
                else if (Skill.SkillVNum == 1124)
                    foreach (var sniperAttackBCard in Session.Character.Buff.GetAllItems().SelectMany(s =>
                        s.Card.BCards.Where(b =>
                            b.Type == (byte)CardType.SniperAttack &&
                            b.SubType == (byte)AdditionalTypes.SniperAttack.ChanceCausing)))
                        SkillBCards.Add(sniperAttackBCard);
                foreach (var ambushAttackBCard in Session.Character.Buff.GetAllItems().SelectMany(s =>
                    s.Card.BCards.Where(b =>
                        b.Type == (byte)CardType.SniperAttack &&
                        b.SubType == (byte)AdditionalTypes.SniperAttack.ProduceChance)))
                    SkillBCards.Add(ambushAttackBCard);
            }

            return SkillBCards.ToList();
        }

        private void NormalizeModernGaugeContributors(List<BCard> skillBCards)
        {
            if (skillBCards == null || skillBCards.Count < 2)
            {
                return;
            }

            var groupedContributors = skillBCards
                .Where(bCard => bCard != null && bCard.SkillVNum.HasValue &&
                                (bCard.Type == (byte)CardType.TokenGauge ||
                                 bCard.Type == (byte)CardType.DimensionalSynchronization))
                .GroupBy(bCard => new { bCard.Type, bCard.SubType })
                .Where(group => group.Select(bCard => bCard.SkillVNum.Value).Distinct().Count() > 1)
                .ToList();

            foreach (var group in groupedContributors)
            {
                short preferredSkillVNum = group.Any(bCard => bCard.SkillVNum == SkillVNum)
                    ? SkillVNum
                    : group.Max(bCard => bCard.SkillVNum.Value);

                group.Where(bCard => bCard.SkillVNum != preferredSkillVNum)
                    .ToList()
                    .ForEach(bCard => skillBCards.Remove(bCard));
            }
        }

        public int GetSkillRange()
        {
            int skillRange = Skill.Range;
            if (ServerManager.Instance.GetSessionByCharacterId(CharacterId) is ClientSession Session)
                skillRange += Session.Character.GetBuff(CardType.FearSkill,
                    (byte)AdditionalTypes.FearSkill.AttackRangedIncreased)[0];
            return skillRange;
        }

        public short MpCost()
        {
            var mpCost = Skill.MpCost;
            if (ServerManager.Instance.GetSessionByCharacterId(CharacterId) is ClientSession Session)
            {
                var skills = Session.Character.GetSkills();

                //Upgrade Skills
                var upgradeSkills = skills.FindAll(s => s.Skill?.UpgradeSkill == SkillVNum);
                if (upgradeSkills?.Count > 0)
                    foreach (var upgradeSkill in upgradeSkills)
                        mpCost += upgradeSkill.Skill.MpCost;
            }

            return mpCost;
        }

        public byte TargetRange()
        {
            var targetRange = Skill.TargetRange;

            if (Skill.HitType != 0)
                if (ServerManager.Instance.GetSessionByCharacterId(CharacterId) is ClientSession Session)
                    Session.Character.Buff.GetAllItems().SelectMany(s => s.Card.BCards).Where(s =>
                            s.Type == (byte)CardType.FireCannoneerRangeBuff
                            && s.SubType == (byte)AdditionalTypes.FireCannoneerRangeBuff.AOEIncreased).ToList()
                        .ForEach(s => targetRange += (byte)s.FirstData);

            return targetRange;
        }

        #endregion
    }
}
