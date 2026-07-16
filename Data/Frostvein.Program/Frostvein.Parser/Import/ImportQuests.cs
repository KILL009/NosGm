using Frostvein.Core;
using Frostvein.DAL;
using Frostvein.Data;
using Frostvein.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Frostvein.Parser.Import
{
    public class ImportQuests : IImport
    {
        private readonly ImportConfiguration _configuration;

        public ImportQuests(ImportConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string FileQuestDat => Path.Combine(_configuration.DatFolder, "quest.dat");

        private string FileRewardsDat => Path.Combine(_configuration.DatFolder, "qstprize.dat");

        public void Import()
        {
            var qstCounter = 0;

            var dictionaryRewards = new Dictionary<long, QuestRewardDTO>();
            var _quests = DAOFactory.QuestDAO.LoadAll().ToDictionary(s => s.QuestId, s => s);
            var reward = new QuestRewardDTO();
            string line;

            using (var questRewardStream = new StreamReader(FileRewardsDat, Encoding.GetEncoding(1252)))
            {
                while ((line = questRewardStream.ReadLine()) != null)
                {
                    var currentLine = line
       .Split('\t')
       .Select(token => token
           .Trim()
           .Trim('\uFEFF', '\u200B', '\u00A0'))
       .ToArray();
                    if (currentLine.Length <= 1 && currentLine[0] != "END")
                    {
                        continue;
                    }

                    switch (currentLine[0])
                    {
                        case "VNUM":
                            reward = new QuestRewardDTO
                            {
                                QuestRewardId = long.Parse(currentLine[1]),
                                RewardType = byte.Parse(currentLine[2])
                            };
                            break;

                        case "DATA":
                            if (currentLine.Length < 3)
                            {
                                return;
                            }

                            switch ((QuestRewardType)reward.RewardType)
                            {
                                case QuestRewardType.Exp:
                                case QuestRewardType.SecondExp:
                                case QuestRewardType.JobExp:
                                case QuestRewardType.SecondJobExp:
                                case QuestRewardType.HExp:
                                    reward.Data = int.Parse(currentLine[2]) == -1 ? 0 : int.Parse(currentLine[2]);
                                    reward.Amount = int.Parse(currentLine[1]);
                                    break;

                                case QuestRewardType.WearItem:
                                    reward.Data = int.Parse(currentLine[1]);
                                    reward.Amount = 1;
                                    break;

                                case QuestRewardType.EtcMainItem:
                                    reward.Data = int.Parse(currentLine[1]);
                                    reward.Amount = int.Parse(currentLine[5]) == -1 ? 1 : int.Parse(currentLine[5]);
                                    break;

                                case QuestRewardType.Gold:
                                case QuestRewardType.SecondGold:
                                case QuestRewardType.ThirdGold:
                                case QuestRewardType.FourthGold:
                                case QuestRewardType.Reput:
                                case QuestRewardType.Unknown2:
                                    reward.Data = 0;
                                    reward.Amount = int.Parse(currentLine[1]);
                                    break;

                                default:
                                    reward.Data = int.Parse(currentLine[1]);
                                    reward.Amount = int.Parse(currentLine[2]);
                                    break;
                            }

                            break;

                        case "END":
                            dictionaryRewards[reward.QuestRewardId] = reward;
                            break;
                    }
                }
            }

            // Final List
            var quests = new List<QuestDTO>();
            var rewards = new List<QuestRewardDTO>();
            var questObjectives = new List<QuestObjectiveDTO>();

            // Current
            var quest = new QuestDTO();
            var currentRewards = new List<QuestRewardDTO>();
            var currentObjectives = new List<QuestObjectiveDTO>();

            byte objectiveIndex = 0;
            Logger.Log.Info(FileQuestDat);
            using (var questStream = new StreamReader(FileQuestDat, Encoding.GetEncoding(1252)))
            {
                while ((line = questStream.ReadLine()) != null)
                {
                    var currentLine = line
      .Split('\t')
      .Select(token => token
          .Trim()
          .Trim('\uFEFF', '\u200B', '\u00A0'))
      .ToArray();
                    if (currentLine.Length > 1 || currentLine[0] == "END")
                        switch (currentLine[0])
                        {
                            case "VNUM":
                                quest = new QuestDTO
                                {
                                    QuestId = long.Parse(currentLine[1]),
                                    QuestType = int.Parse(currentLine[2]),
                                    InfoId = int.Parse(currentLine[1])
                                };
                                switch (quest.QuestId)
                                {
                                    //TODO: Legendary Hunter quests will be marked as daily, but should be in the "secondary" slot and be daily nevertheless
                                    case 6057: // John the adventurer
                                    case 7519: // Legendary Hunter 1 time Kertos
                                    case 7520: // Legendary Hunter 1 time Valakus
                                    case 7521: // Legendary Hunter Grenigas
                                    case 7522: // Legendary Hunter Draco
                                    case 7523: // Legendary Hunter Glacerus
                                    case 7524: // Legendary Hunter Laurena
                                    case 5514: // Sherazade ice flower (n_run 65)
                                    case 5919: // Akamur's military engineer (n_run 68)
                                    case 5908: // John (n_run 67)
                                    case 5914: // Alchemist (n_run 66)
                                        quest.IsDaily = true;
                                        break;
                                }

                                objectiveIndex = 0;
                                currentRewards.Clear();
                                currentObjectives.Clear();
                                break;

                            case "LINK":
                                if (int.Parse(currentLine[1]) != -1) // Base Quest Order (ex: SpQuest)
                                {
                                    quest.NextQuestId = int.Parse(currentLine[1]);
                                    continue;
                                }

                                // Main Quest Order
                                switch (quest.QuestId)
                                {
                                    case 1997:
                                        quest.NextQuestId = 1500;
                                        break;

                                    case 1523:
                                    case 1532:
                                    case 1580:
                                    case 1610:
                                    case 1618:
                                    case 1636:
                                    case 1647:
                                    case 1664:
                                    case 3075:
                                        quest.NextQuestId = quest.QuestId + 2;
                                        break;

                                    case 1527:
                                    case 1553:
                                        quest.NextQuestId = quest.QuestId + 3;
                                        break;

                                    case 1690:
                                        quest.NextQuestId = 1694;
                                        break;

                                    case 1737:
                                        quest.NextQuestId = 1982;
                                        break;

                                    case 1751:
                                        quest.NextQuestId = 3000;
                                        break;

                                    case 1982:
                                        quest.NextQuestId = 1738;
                                        break;

                                    case 3101:
                                        quest.NextQuestId = 3200;
                                        break;

                                    case 3331:
                                        quest.NextQuestId = 3340;
                                        break;

                                    case 3374:
                                        quest.NextQuestId = 6179; // First desert quest
                                        break;

                                    default:

                                        if (quest.QuestId < 1500 || quest.QuestId >= 1751 && quest.QuestId < 3000 ||
                                            quest.QuestId >= 3374 && (quest.QuestId < 5478 || quest.QuestId >= 5513))
                                            continue;

                                        quest.NextQuestId = quest.QuestId + 1;
                                        break;
                                }

                                break;

                            case "LEVEL":
                                {
                                    if (currentLine.Length < 3)
                                    {
                                        Logger.Log.Error($"LEVEL incompleto: {line}");
                                        break;
                                    }

                                    string minimumText = currentLine[1]
                                        .Trim()
                                        .Trim('\uFEFF', '\u200B', '\u00A0');

                                    string maximumText = currentLine[2]
                                        .Trim()
                                        .Trim('\uFEFF', '\u200B', '\u00A0');

                                    if (!int.TryParse(minimumText, out int minimumLevel))
                                    {
                                        Logger.Log.Error(
                                            $"LEVEL MIN inválido: '{minimumText}' | Línea: {line}");

                                        break;
                                    }

                                    if (!int.TryParse(maximumText, out int maximumLevel))
                                    {
                                        Logger.Log.Error(
                                            $"LEVEL MAX inválido: '{maximumText}' | Línea: {line}");

                                        break;
                                    }

                                    quest.LevelMin = minimumLevel < 0
                                     ? (int)0
                                   : minimumLevel;
                                    quest.LevelMax = maximumLevel;

                                    // currentLine[3], cuando existe, pertenece al formato nuevo.
                                    // Por ahora se ignora porque QuestDTO no tiene una propiedad asociada.
                                    break;
                                }

                            case "TALK":

                                if (int.Parse(currentLine[1]) > 0) quest.StartDialogId = int.Parse(currentLine[1]);
                                if (int.Parse(currentLine[2]) > 0) quest.EndDialogId = int.Parse(currentLine[2]);
                                if (int.Parse(currentLine[3]) > 0) quest.DialogNpcVNum = int.Parse(currentLine[3]);
                                if (int.Parse(currentLine[4]) > 0) quest.DialogNpcId = int.Parse(currentLine[4]);

                                break;

                            case "TARGET":
                                if (int.Parse(currentLine[3]) > 0)
                                {
                                    quest.TargetMap = short.Parse(currentLine[3]);
                                    quest.TargetX = short.Parse(currentLine[1]);
                                    quest.TargetY = short.Parse(currentLine[2]);
                                }

                                switch (quest.QuestId)
                                {
                                    case 1708:
                                        quest.TargetMap = 76;
                                        quest.TargetX = 62;
                                        quest.TargetY = 68;
                                        break;
                                }

                                break;

                            case "DATA":
                                if (currentLine.Length < 3)
                                {
                                    return;
                                }
                                objectiveIndex++;
                                int? data = null, objective = null, specialData = null, secondSpecialData = null;
                                switch ((QuestType)quest.QuestType)
                                {
                                    case QuestType.Hunt:
                                    case QuestType.Capture1:
                                    case QuestType.Capture2:
                                    case QuestType.Collect1:
                                    case QuestType.Product:
                                        data = int.Parse(currentLine[1]);
                                        objective = int.Parse(currentLine[2]);
                                        if (objective == -1)
                                        {
                                            objective = int.Parse(currentLine[3]);
                                        }
                                        break;

                                    case QuestType.Brings: // npcVNum - ItemCount - ItemVNum //
                                    case QuestType.Collect3: // ItemVNum - Objective - TsId //
                                    case QuestType.Needed: // ItemVNum - Objective - npcVNum //
                                    case QuestType.Collect5: // ItemVNum - Objective - npcVNum //
                                        data = int.Parse(currentLine[2]);
                                        objective = int.Parse(currentLine[3]);
                                        specialData = int.Parse(currentLine[1]);
                                        break;

                                    case QuestType.Collect4: // ItemVNum - Objective - MonsterVNum - DropRate //
                                    case QuestType.Collect2: // ItemVNum - Objective - MonsterVNum - DropRate //
                                        data = int.Parse(currentLine[2]);
                                        objective = int.Parse(currentLine[3]);
                                        specialData = int.Parse(currentLine[1]);
                                        secondSpecialData = int.Parse(currentLine[4]);
                                        break;

                                    case QuestType.TimesSpace: // TS Lvl - Objective - TS Id //
                                    case QuestType.TsPoint:
                                        data = int.Parse(currentLine[4]);
                                        objective = int.Parse(currentLine[2]);
                                        specialData = int.Parse(currentLine[1]);
                                        break;

                                    case QuestType.Wear: // Item VNum - * - NpcVNum //
                                        data = int.Parse(currentLine[2]);
                                        specialData = int.Parse(currentLine[1]);
                                        break;

                                    case QuestType.TransmitGold: // NpcVNum - Gold x10K - * //
                                        data = int.Parse(currentLine[1]);
                                        objective = int.Parse(currentLine[2]) * 10000;
                                        break;

                                    case QuestType.GoTo: // Map - PosX - PosY //
                                        data = int.Parse(currentLine[1]);
                                        objective = int.Parse(currentLine[2]);
                                        specialData = int.Parse(currentLine[3]);
                                        break;

                                    case QuestType.WinRaid: // Design - Objective - ? //
                                        data = int.Parse(currentLine[1]);
                                        objective = int.Parse(currentLine[2]);
                                        specialData = int.Parse(currentLine[3]);
                                        break;

                                    case QuestType.Use: // Item to use - * - mateVnum //
                                        data = int.Parse(currentLine[1]);
                                        specialData = int.Parse(currentLine[2]);
                                        break;

                                    case QuestType.Dialog1: // npcVNum - * - * //
                                    case QuestType.Dialog2: // npcVNum - * - * //
                                        data = int.Parse(currentLine[1]);
                                        break;

                                    case QuestType.FlowerQuest:
                                        objective = 10;
                                        break;

                                    case QuestType.Inspect: // NpcVNum - Objective - ItemVNum //
                                    case QuestType.Required: // npcVNum - Objective - ItemVNum //
                                        data = int.Parse(currentLine[1]);
                                        objective = int.Parse(currentLine[3]);
                                        specialData = int.Parse(currentLine[2]);
                                        break;

                                    default:
                                        data = int.Parse(currentLine[1]);
                                        objective = int.Parse(currentLine[2]);
                                        specialData = int.Parse(currentLine[3]);
                                        break;
                                }
                                currentObjectives.Add(new QuestObjectiveDTO()
                                {
                                    Data = data,
                                    Objective = objective ?? 1,
                                    SpecialData = specialData < 0 ? null : specialData,
                                    DropRate = secondSpecialData < 0 ? null : secondSpecialData,
                                    ObjectiveIndex = objectiveIndex,
                                    QuestId = (int)quest.QuestId
                                });
                                break;

                            case "PRIZE":
                                for (var a = 1; a < 5; a++)
                                {
                                    if (!dictionaryRewards.ContainsKey(long.Parse(currentLine[a])))
                                    {
                                        continue;
                                    }

                                    var currentReward = dictionaryRewards[long.Parse(currentLine[a])];
                                    currentRewards.Add(new QuestRewardDTO
                                    {
                                        RewardType = currentReward.RewardType,
                                        Data = currentReward.Data,
                                        Amount = currentReward.Amount,
                                        QuestId = quest.QuestId
                                    });
                                }

                                break;

                            case "END":
                                if (!_quests.ContainsKey(quest.QuestId))
                                {
                                    quest.IsDaily = true;
                                    questObjectives.AddRange(currentObjectives);
                                    rewards.AddRange(currentRewards);
                                    quests.Add(quest);
                                    qstCounter++;
                                }

                                break;
                        }
                }

                DAOFactory.QuestDAO.Insert(quests);
                DAOFactory.QuestRewardDAO.Insert(rewards);
                DAOFactory.QuestObjectiveDAO.Insert(questObjectives);

                Logger.Log.Info($"{qstCounter} Quests parsed");
                Logger.Log.Info($"{rewards.Count} Quests REWARDS parsed");
                Logger.Log.Info($"{questObjectives.Count} Quests OBJECTIVE parsed");
            }
        }

        private static bool ParseQuestObjectives(IReadOnlyList<string> currentLine, QuestDTO quest,
            ICollection<QuestObjectiveDTO> currentObjectives, ref byte objectiveIndex)
        {
            if (currentLine.Count < 3) return true;

            objectiveIndex++;
            int? data = null, objective = null, specialData = null, secondSpecialData = null;
            switch ((QuestType)quest.QuestType)
            {
                case QuestType.Hunt:
                case QuestType.Capture1:
                case QuestType.Capture2:
                case QuestType.Collect1:
                case QuestType.Product:
                    data = int.Parse(currentLine[1]);
                    objective = int.Parse(currentLine[2]);
                    break;

                case QuestType.Brings: // npcVNum - ItemCount - ItemVNum //
                case QuestType.Collect3: // ItemVNum - Objective - TsId //
                case QuestType.Needed: // ItemVNum - Objective - npcVNum //
                case QuestType.Collect5: // ItemVNum - Objective - npcVNum //
                    data = int.Parse(currentLine[2]);
                    objective = int.Parse(currentLine[3]);
                    specialData = int.Parse(currentLine[1]);
                    break;

                case QuestType.Collect4: // ItemVNum - Objective - MonsterVNum - DropRate // 
                case QuestType.Collect2: // ItemVNum - Objective - MonsterVNum - DropRate // 
                    data = int.Parse(currentLine[2]);
                    objective = int.Parse(currentLine[3]);
                    specialData = int.Parse(currentLine[1]);
                    secondSpecialData = int.Parse(currentLine[4]);
                    break;

                case QuestType.TimesSpace: // TS Lvl - Objective - TS Id //
                case QuestType.TsPoint:
                    data = int.Parse(currentLine[4]);
                    objective = int.Parse(currentLine[2]);
                    specialData = int.Parse(currentLine[1]);
                    break;

                case QuestType.Wear: // Item VNum - * - NpcVNum //
                    data = int.Parse(currentLine[2]);
                    specialData = int.Parse(currentLine[1]);
                    break;

                case QuestType.TransmitGold: // NpcVNum - Gold x10K - * //
                    data = int.Parse(currentLine[1]);
                    objective = int.Parse(currentLine[2]) * 10000;
                    break;

                case QuestType.GoTo: // Map - PosX - PosY //
                    data = int.Parse(currentLine[1]);
                    objective = int.Parse(currentLine[2]);
                    specialData = int.Parse(currentLine[3]);
                    break;

                case QuestType.WinRaid: // Design - Objective - ? //
                    data = int.Parse(currentLine[1]);
                    objective = int.Parse(currentLine[2]);
                    specialData = int.Parse(currentLine[3]);
                    break;

                case QuestType.Use: // Item to use - * - mateVnum //
                    data = int.Parse(currentLine[1]);
                    specialData = int.Parse(currentLine[2]);
                    break;

                case QuestType.Dialog1: // npcVNum - * - * //
                case QuestType.Dialog2: // npcVNum - * - * //
                    data = int.Parse(currentLine[1]);
                    break;

                case QuestType.FlowerQuest:
                    objective = 10;
                    break;

                case QuestType.Inspect: // NpcVNum - Objective - ItemVNum //
                case QuestType.Required: // npcVNum - Objective - ItemVNum //
                    data = int.Parse(currentLine[1]);
                    objective = int.Parse(currentLine[3]);
                    specialData = int.Parse(currentLine[2]);
                    break;

                default:
                    data = int.Parse(currentLine[1]);
                    objective = int.Parse(currentLine[2]);
                    specialData = int.Parse(currentLine[3]);
                    break;
            }

            currentObjectives.Add(new QuestObjectiveDTO
            {
                Data = data,
                Objective = objective ?? 1,
                SpecialData = specialData < 0 ? null : specialData,
                DropRate = secondSpecialData < 0 ? null : secondSpecialData,
                ObjectiveIndex = objectiveIndex,
                QuestId = (int)quest.QuestId
            });
            return false;
        }
    }
}