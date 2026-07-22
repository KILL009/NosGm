using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NosGm.Parser.Import
{
    public class ImportNpcMonsters : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportNpcMonsters(ImportConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string FileMonsterDat => Path.Combine(_configuration.DatFolder, "monster.dat");

        private string FileMonsterLang =>
            Path.Combine(_configuration.LangFolder, $"_code_{_configuration.Lang}_monster.txt");

        public void Import()
        {
            var MAX_LEVEL = 101;
            var basicHp = new int[MAX_LEVEL];
            var basicPrimaryMp = new int[MAX_LEVEL];
            var basicSecondaryMp = new int[MAX_LEVEL];
            var basicXp = new int[MAX_LEVEL];
            var basicJXp = new int[MAX_LEVEL];

            // basicHpLoad
            var baseHp = 138;
            var HPbasup = 18;
            for (var i = 0; i < basicHp.GetLength(0); i++)
            {
                basicHp[i] = baseHp;
                HPbasup++;
                baseHp += HPbasup;

                if (i == 37)
                {
                    baseHp = 1765;
                    HPbasup = 65;
                }

                if (i < 41) continue;

                if ((99 - i) % 8 == 0) HPbasup++;
            }

            //Race == 0
            basicPrimaryMp[0] = 10;
            basicPrimaryMp[1] = 10;
            basicPrimaryMp[2] = 15;

            var primaryBasup = 5;
            byte count = 0;
            var isStable = true;
            var isDouble = false;

            for (var i = 3; i < basicPrimaryMp.GetLength(0); i++)
            {
                if (i % 10 == 1)
                {
                    basicPrimaryMp[i] += basicPrimaryMp[i - 1] + primaryBasup * 2;
                    continue;
                }

                if (!isStable)
                {
                    primaryBasup++;
                    count++;

                    if (count == 2)
                    {
                        if (isDouble)
                        {
                            isDouble = false;
                        }
                        else
                        {
                            isStable = true;
                            isDouble = true;
                            count = 0;
                        }
                    }

                    if (count == 4)
                    {
                        isStable = true;
                        count = 0;
                    }
                }
                else
                {
                    count++;
                    if (count == 2)
                    {
                        isStable = false;
                        count = 0;
                    }
                }

                basicPrimaryMp[i] = basicPrimaryMp[i - (i % 10 == 2 ? 2 : 1)] + primaryBasup;
            }

            // Race == 2
            basicSecondaryMp[0] = 60;
            basicSecondaryMp[1] = 60;
            basicSecondaryMp[2] = 78;

            var secondaryBasup = 18;
            var boostup = false;

            for (var i = 3; i < basicSecondaryMp.GetLength(0); i++)
            {
                if (i % 10 == 1)
                {
                    basicSecondaryMp[i] += basicSecondaryMp[i - 1] + i + 10;
                    continue;
                }

                if (boostup)
                {
                    secondaryBasup += 3;
                    boostup = false;
                }
                else
                {
                    secondaryBasup++;
                    boostup = true;
                }

                basicSecondaryMp[i] = basicSecondaryMp[i - (i % 10 == 2 ? 2 : 1)] + secondaryBasup;
            }

            // basicXPLoad
            for (var i = 0; i < basicXp.GetLength(0); i++) basicXp[i] = i * 180;

            // basicJXpLoad
            for (var i = 0; i < basicJXp.GetLength(0); i++) basicJXp[i] = 360;

            var existingSkills = DAOFactory.SkillDAO.LoadAll().Select(x => x.SkillVNum).ToHashSet();
            var existingNpcMonsters = DAOFactory.NpcMonsterDAO.LoadAll().Select(x => x.NpcMonsterVNum).ToHashSet();

            var existingNpcMonsterSkills = DAOFactory.NpcMonsterSkillDAO.LoadAll();
            var existingDrops = DAOFactory.DropDAO.LoadAll().ToList();

            var dictionaryIdLang = new Dictionary<string, string>();
            var npcs = new List<NpcMonsterDTO>();
            var drops = new List<DropDTO>();
            var monsterCards = new List<BCardDTO>();
            var skills = new List<NpcMonsterSkillDTO>();

            // Store like this: (vnum, (name, level))
            var npc = new NpcMonsterDTO();

            var itemAreaBegin = false;
            var counter = 0;
            long unknownData = 0;

            using (var npcIdLangStream = new StreamReader(FileMonsterLang, Encoding.GetEncoding(1252)))
            {
                string line;
                while ((line = npcIdLangStream.ReadLine()) != null)
                {
                    var linesave = line.Split('\t');
                    if (linesave.Length > 1 && !dictionaryIdLang.ContainsKey(linesave[0]))
                        dictionaryIdLang.Add(linesave[0], linesave[1]);
                }
            }

            using (var npcIdStream = new StreamReader(FileMonsterDat, Encoding.GetEncoding(1252)))
            {
                string line;
                while ((line = npcIdStream.ReadLine()) != null)
                {
                    var currentLine = line.Split('\t');

                    if (currentLine.Length > 2 && currentLine[1] == "VNUM")
                    {
                        npc = new NpcMonsterDTO
                        {
                            NpcMonsterVNum = Convert.ToInt16(currentLine[2])
                        };

                        if (!existingNpcMonsters.Contains(npc.NpcMonsterVNum))
                        {
                            itemAreaBegin = true;
                            npcs.Add(npc);
                            counter++;
                        }

                        unknownData = 0;
                        DAOFactory.BCardDAO.DeleteByMonsterVNum(npc.NpcMonsterVNum);
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "NAME")
                    {
                        if (!itemAreaBegin) continue;

                        npc.Name = dictionaryIdLang.ContainsKey(currentLine[2]) ? dictionaryIdLang[currentLine[2]] : "";
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "LEVEL")
                    {
                        if (!itemAreaBegin) continue;

                        npc.Level = Convert.ToInt16(currentLine[2]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "RACE")
                    {
                        npc.Race = Convert.ToInt16(currentLine[2]);
                        npc.RaceType = Convert.ToInt16(currentLine[3]);
                        npc.HeroLevel = Convert.ToInt16(currentLine[4]);
                    }
                    else if (currentLine.Length > 7 && currentLine[1] == "ATTRIB")
                    {
                        npc.Element = Convert.ToInt16(currentLine[2]);
                        npc.ElementRate = Convert.ToInt16(currentLine[3]);
                        npc.FireResistance = Convert.ToInt16(currentLine[4]);
                        npc.WaterResistance = Convert.ToInt16(currentLine[5]);
                        npc.LightResistance = Convert.ToInt16(currentLine[6]);
                        npc.DarkResistance = Convert.ToInt16(currentLine[7]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "HP/MP")
                    {
                        npc.MaxHP = Convert.ToInt32(currentLine[2]) + basicHp[npc.Level];
                        npc.MaxMP = Convert.ToInt32(currentLine[3]) + npc.Race == 0
                            ? basicPrimaryMp[npc.Level]
                            : basicSecondaryMp[npc.Level];
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "EXP")
                    {
                        FillXpInformations(npc, currentLine, basicXp, basicJXp);
                    }
                    else if (currentLine.Length > 6 && currentLine[1] == "PREATT")
                    {
                        npc.IsHostile = currentLine[2] != "0";
                        npc.GroupAttack = (GroupAttackType)byte.Parse(currentLine[3]);
                        npc.NoticeRange = Convert.ToInt16(currentLine[4]);
                        npc.Speed = Convert.ToByte(currentLine[5]);
                        npc.RespawnTime = Convert.ToInt32(currentLine[6]);
                    }
                    else if (currentLine.Length > 6 && currentLine[1] == "WEAPON")
                    {
                        switch (currentLine[3])
                        {
                            case "1":
                                npc.DamageMinimum = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 4 + 32 +
                                                                    Convert.ToInt16(currentLine[4]) +
                                                                    Math.Round(Convert.ToDecimal((npc.Level - 1) / 5)));
                                npc.DamageMaximum = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 6 + 40 +
                                                                    Convert.ToInt16(currentLine[5]) -
                                                                    Math.Round(Convert.ToDecimal((npc.Level - 1) / 5)));
                                npc.Concentrate =
                                    Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 5 + 27 +
                                                    Convert.ToInt16(currentLine[6]));
                                npc.CriticalChance = Convert.ToByte(4 + Convert.ToInt16(currentLine[7]));
                                npc.CriticalRate = Convert.ToInt16(70 + Convert.ToInt16(currentLine[8]));
                                break;
                            case "2":
                                npc.DamageMinimum =
                                    Convert.ToInt16(Convert.ToInt16(currentLine[2]) * 6.5f + 23 +
                                                    Convert.ToInt16(currentLine[4]));
                                npc.DamageMaximum =
                                    Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 8 + 38 +
                                                    Convert.ToInt16(currentLine[5]));
                                npc.Concentrate = Convert.ToInt16(70 + Convert.ToInt16(currentLine[6]));
                                break;
                        }
                    }
                    else if (currentLine.Length > 6 && currentLine[1] == "ARMOR")
                    {
                        npc.CloseDefence = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 2 + 18);
                        npc.DistanceDefence = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 3 + 17);
                        npc.MagicDefence = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 2 + 13);
                        npc.DefenceDodge = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 5 + 31);
                        npc.DistanceDefenceDodge = Convert.ToInt16((Convert.ToInt16(currentLine[2]) - 1) * 5 + 31);
                    }
                    else if (currentLine.Length > 7 && currentLine[1] == "ETC")
                    {
                        unknownData = Convert.ToInt64(currentLine[2]);
                        npc.Catch = currentLine[2] == "8";
                        switch (unknownData)
                        {
                            case -2147481593:
                                npc.MonsterType = MonsterType.Special;
                                break;
                            case -2147483616:
                            case -2147483647:
                            case -2147483646:
                                if (npc.Race == 8 && npc.RaceType == 0)
                                    npc.NoAggresiveIcon = true;
                                else
                                    npc.NoAggresiveIcon = false;

                                break;
                        }

                        if (npc.NpcMonsterVNum >= 588 && npc.NpcMonsterVNum <= 607) npc.MonsterType = MonsterType.Elite;
                    }
                    else if (currentLine.Length > 6 && currentLine[1] == "SETTING")
                    {
                        if (currentLine[4] == "0") continue;

                        npc.VNumRequired = Convert.ToInt16(currentLine[4]);
                        npc.AmountRequired = 1;
                    }
                    else if (currentLine.Length > 4 && currentLine[1] == "PETINFO")
                    {
                        if (npc.VNumRequired != 0 || unknownData != -2147481593 && unknownData != -2147481599 &&
                            unknownData != -1610610681) continue;

                        npc.VNumRequired = Convert.ToInt16(currentLine[2]);
                        npc.AmountRequired = Convert.ToByte(currentLine[3]);
                        npc.Tameable = Convert.ToByte(currentLine[6]);
                    }
                    else if (currentLine.Length > 2 && currentLine[1] == "EFF")
                    {
                        npc.BasicSkill = Convert.ToInt16(currentLine[2]);
                    }
                    else if (currentLine.Length > 8 && currentLine[1] == "ZSKILL")
                    {
                        npc.AttackClass = Convert.ToByte(currentLine[2]);
                        switch (npc.NpcMonsterVNum)
                        {
                            case 45:
                            case 46:
                            case 47:
                            case 48:
                            case 49:
                            case 50:
                            case 51:
                            case 52:
                            case 53: // Pii Pods ^
                            case 195: // Training Stake
                            case 208:
                            case 209: // Beehives ^
                                npc.BasicRange = 0;
                                break;

                            default:
                                npc.BasicRange = Convert.ToByte(currentLine[3]);
                                break;
                        }

                        npc.BasicArea = Convert.ToByte(currentLine[5]);
                        npc.BasicCooldown = Convert.ToInt16(currentLine[6]);
                    }
                    else if (currentLine.Length > 4 && currentLine[1] == "WINFO")
                    {
                        npc.AttackUpgrade = Convert.ToByte(unknownData == 1 ? currentLine[2] : currentLine[4]);
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "AINFO")
                    {
                        npc.DefenceUpgrade = Convert.ToByte(unknownData == 1 ? currentLine[2] : currentLine[3]);
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "SKILL")
                    {
                        for (var i = 2; i < currentLine.Length - 3; i += 3)
                        {
                            var vnum = short.Parse(currentLine[i]);
                            if (vnum == -1 || vnum == 0) break;

                            if (!existingSkills.Contains(vnum)) continue;

                            if (existingNpcMonsterSkills.Any(x =>
                                x.NpcMonsterVNum == npc.NpcMonsterVNum && x.SkillVNum == vnum)) continue;

                            skills.Add(new NpcMonsterSkillDTO
                            {
                                SkillVNum = vnum,
                                Rate = Convert.ToInt16(currentLine[i + 1]),
                                NpcMonsterVNum = npc.NpcMonsterVNum
                            });
                        }
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "CARD")
                    {
                        for (var i = 0; i < 4; i++)
                        {
                            var type = (byte)int.Parse(currentLine[5 * i + 2]);
                            if (type == 0 || type == 255) continue;

                            var first = int.Parse(currentLine[5 * i + 3]);
                            var itemCard = new BCardDTO
                            {
                                NpcMonsterVNum = npc.NpcMonsterVNum,
                                Type = type,
                                SubType = (byte)(int.Parse(currentLine[5 * i + 5]) + 1 * 10 + 1 + (first > 0 ? 0 : 1)),
                                IsLevelScaled = Convert.ToBoolean(first % 4),
                                IsLevelDivided = first % 4 == 2,
                                FirstData = (short)((first > 0 ? first : -first) / 4),
                                SecondData = (short)(int.Parse(currentLine[5 * i + 4]) / 4),
                                ThirdData = (short)(int.Parse(currentLine[5 * i + 6]) / 4),
                                CastType = byte.Parse(currentLine[6 + 5 * i])
                            };
                            monsterCards.Add(itemCard);
                        }
                    }
                    else if (currentLine.Length > 1 && currentLine[1] == "BASIC")
                    {
                        for (var i = 0; i < 10; i++)
                        {
                            var type = (byte)int.Parse(currentLine[5 * i + 2]);
                            if (type == 0 || type == 255) continue;

                            var first = int.Parse(currentLine[3 + 5 * i]);
                            var itemCard = new BCardDTO
                            {
                                NpcMonsterVNum = npc.NpcMonsterVNum,
                                Type = type,
                                SubType = (byte)((Convert.ToByte(currentLine[5 + 5 * i]) + 1) * 10 + 1 +
                                                  (first < 0 ? 1 : 0)),
                                IsLevelScaled = Convert.ToBoolean(first % 4),
                                IsLevelDivided = first % 4 == 2,
                                FirstData = (short)((first > 0 ? first : -first) / 4),
                                SecondData = (short)(int.Parse(currentLine[4 + 5 * i]) / 4),
                                ThirdData = (short)(int.Parse(currentLine[6 + 5 * i]) / 4),
                                CastType = byte.Parse(currentLine[6 + 5 * i]),
                            };
                            monsterCards.Add(itemCard);
                        }
                    }
                    else if (currentLine.Length > 3 && currentLine[1] == "ITEM")
                    {
                        for (var i = 2; i < currentLine.Length - 3; i += 3)
                        {
                            var vnum = Convert.ToInt16(currentLine[i]);
                            if (vnum == -1) break;

                            if (existingDrops.Any(x => x.MonsterVNum.HasValue && x.MonsterVNum == npc.NpcMonsterVNum))
                                continue;

                            drops.Add(new DropDTO
                            {
                                ItemVNum = vnum,
                                Amount = Convert.ToInt32(currentLine[i + 2]),
                                MonsterVNum = npc.NpcMonsterVNum,
                                DropChance = Convert.ToInt32(currentLine[i + 1])
                            });
                        }

                        itemAreaBegin = false;
                    }
                }

                DAOFactory.NpcMonsterDAO.Insert(npcs);
                DAOFactory.NpcMonsterSkillDAO.Insert(skills);
                DAOFactory.BCardDAO.Insert(monsterCards);
                DAOFactory.DropDAO.Insert(drops);

                Logger.Log.Info($"{counter} NpcMonsters parsed");
                Logger.Log.Info($"{skills.Count} NpcMonsters SKILL parsed");
                Logger.Log.Info($"{drops.Count} NpcMonsters DROPS parsed");
                Logger.Log.Info($"{monsterCards.Count} NpcMonsters BCARD parsed");
            }
        }

        private static void FillXpInformations(NpcMonsterDTO npc, IReadOnlyList<string> currentLine,
            IReadOnlyList<int> basicXp, IReadOnlyList<int> basicJXp)
        {
            npc.XP = Math.Abs(Convert.ToInt32(currentLine[2]));
            npc.JobXP = Convert.ToInt32(currentLine[3]);
            npc.HeroXp = Convert.ToInt32(currentLine[3]);
        }
    }
}