using NosGm.Configuration;
using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NosGm.GameObject
{
    public class PartnerSp
    {
        #region Instantiation

        public PartnerSp(ItemInstance instance)
        {
            Instance = instance;

            Skills = new List<PartnerSkill>();

            Initialize();
        }

        #endregion

        #region Properties

        public ItemInstance Instance { get; }

        private List<PartnerSkill> Skills { get; set; }

        private long XpMax => GameConfiguration.PartnerSpXP;

        #endregion

        #region Methods

        public int GetLevelForAllSkill()
        {
            var value = 0;
            foreach (var skill in Skills.Where(s => s != null)) value += skill.Level;
            return value;
        }

        public bool AddSkill(Mate mate, byte castId)
        {
            var skillSp = new Skill();

            if (mate.Sp == null) return false;

            if (mate.Sp.Instance.Item.MorphSp == 0)
            {
                Logger.Warn($"Partner skill not found (Morph: {Instance.Item.Morph}, CastId: {castId})");
                return false;
            }

            foreach (var ski in ServerManager.GetAllSkill())
                if (ski.UpgradeType == mate.Sp.Instance.Item.MorphSp &&
                    ski.SkillType == (byte)SkillType.PartnerSpSkill && ski.CastId == castId)
                    skillSp = ski;

            if (skillSp != null)
            {
                var partnerSkillDTO = new PartnerSkillDTO
                {
                    EquipmentSerialId = Instance.EquipmentSerialId,
                    SkillVNum = skillSp.SkillVNum,
                    Level = ServerManager.RandomNumber<byte>(1, 8)
                };

                if (DAOFactory.PartnerSkillDAO.Insert(partnerSkillDTO) is PartnerSkillDTO result)
                {
                    Skills.Add(new PartnerSkill(result));

                    return true;
                }
            }
            else
            {
                Logger.Warn($"Partner skill not found (Morph: {Instance.Item.Morph}, CastId: {castId})");
            }

            return false;
        }

        public void AddXp(long amount)
        {
            if (Instance.XP < XpMax) Instance.XP = Math.Min(Instance.XP + amount, XpMax);
        }

        public bool CanLearnSkill()
        {
            return Instance.XP >= XpMax;
        }

        public void ClearSkills()
        {
            for (byte castId = 0; castId < 3; castId++) RemoveSkill(castId);

            ReloadSkills();
        }

        public void FullXp()
        {
            Instance.XP = XpMax;
        }

        public string GeneratePski()
        {
            var pski = "pski";

            foreach (var partnerSkill in Skills.OrderBy(s => s.Skill.CastId))
                pski += $" {partnerSkill.Skill.SkillVNum}";

            return pski;
        }

        public string GenerateSkills(bool withLevel = true, bool justLevels = false)
        {
            string skills = "";

            for (byte castId = 0; castId < 3; castId++)
            {
                PartnerSkill partnerSkill = GetSkill(castId);
                if (justLevels)
                {
                    if (partnerSkill != null)
                        skills += $" {(partnerSkill.Level == 7 ? 4237 + castId : 0)}";
                    else
                        skills += $" 0";

                    continue;
                }
                if (partnerSkill != null)
                {
                    skills += withLevel ? $" {partnerSkill.SkillVNum}.{partnerSkill.Level}" : $" {partnerSkill.SkillVNum}";
                }
                else
                {
                    skills += withLevel ? $" 0.0" : $" 0";
                }
            }

            return skills;
        }

        public int GetCooldown()
        {
            double maxCooldown = 30000;

            foreach (var partnerSkill in Skills.ToList().Where(s => !s.CanBeUsed()))
            {
                var remaining = (partnerSkill.LastUse - DateTime.Now).TotalMilliseconds +
                                partnerSkill.Skill.Cooldown * 100;

                if (remaining > maxCooldown) maxCooldown = remaining;
            }

            return (int)(maxCooldown / 1000D);
        }

        public string GetName()
        {
            switch (Instance.ItemVNum)
            {
                case 4324: return "Guardian^Lucifer";
                case 4325: return "Sheriff^Chloe";
                case 4326: return "Bone^Warrior^Ragnar";
                case 4343: return "Mad^Professor^Macavity";
                case 4349: return "Archdaemon^Amon";
                case 4405: return "Magic^Student^Yuna";
                case 4413: return "Amora";
                case 4417: return "Mad^March^Hare";
                case 4446: return "One^Winged^Perti";
                case 4800: return "Aegir^the^Angry";
                case 4802: return "Barni^the^Clever";
                case 4803: return "Freya^the^Fateful";
                case 4804: return "Shinobi^the^Silent";
                case 4805: return "Lotus^the^Graceful";
                case 4806: return "Orkani^the^Turbulent";
                case 4807: return "Foxy";
                case 4808: return "Maru";
                case 4809: return "Maru^in^Mother's^Fur";
                case 4810: return "Hongbi";
                case 4811: return "Cheongbi";
                case 4812: return "Lucifer";
                case 4813: return "Witch^Laurena";
                case 4814: return "Amon";
                case 4815: return "Lucy^Lopea﻿rs";
                case 4817: return "Cowgirl^Chloe";
                case 4818: return "Fiona";
                case 4819: return "Jinn";
                case 4820: return "Ice^Princess^Eliza";
                case 4821: return "Daniel^Ducats";
                case 4822: return "Palina^Puppet^Master";
                case 4823: return "Harlequin";
                case 4824: return "Nelia^Nymph";
                case 4825: return "Little^Pri﻿ncess^Venus";
                case 4547: return "Akhenaton^the^Cursed^Pharaoh";
            }

            return Instance.Item.Name.Replace(' ', '^');
        }

        public PartnerSkill GetSkill(byte castId)
        {
            return Skills.Find(s => s.Skill.CastId == castId);
        }

        public int GetSkillsCount()
        {
            return Skills.Count;
        }

        public int GetXpPercent()
        {
            return (int)Math.Floor(100D / XpMax * Instance.XP);
        }

        public void ReloadSkills()
        {
            LoadSkills();
        }

        public bool RemoveSkill(byte castId)
        {
            var partnerSkill = GetSkill(castId);

            if (partnerSkill == null) return false;
            return partnerSkill != null &&
                   DAOFactory.PartnerSkillDAO.Remove(partnerSkill.PartnerSkillId) != DeleteResult.Error;
        }

        public void ResetXp()
        {
            Instance.XP = 0;
        }

        private void Initialize()
        {
            if (Instance.EquipmentSerialId == Guid.Empty) Instance.EquipmentSerialId = Guid.NewGuid();

            LoadSkills();
        }

        private void LoadSkills()
        {
            Skills = new List<PartnerSkill>();
            foreach (var skill in DAOFactory.PartnerSkillDAO.LoadByEquipmentSerialId(Instance.EquipmentSerialId))
                Skills.Add(new PartnerSkill(skill));
        }

        #endregion
    }
}