using Frostvein.Data;
using Frostvein.Domain;
using Frostvein.GameObject.Networking;
using System.Collections.Generic;
using System.Linq;

namespace Frostvein.GameObject
{
    public class CharacterQuest : CharacterQuestDTO
    {
        #region Members

        private Quest _quest;

        #endregion

        #region Instantiation

        public CharacterQuest(CharacterQuestDTO characterQuest)
        {
            CharacterId = characterQuest.CharacterId;
            QuestId = characterQuest.QuestId;
            FirstObjective = characterQuest.FirstObjective;
            SecondObjective = characterQuest.SecondObjective;
            ThirdObjective = characterQuest.ThirdObjective;
            FourthObjective = characterQuest.FourthObjective;
            FifthObjective = characterQuest.FifthObjective;
            IsMainQuest = characterQuest.IsMainQuest;
        }

        public CharacterQuest(long questId, long characterId)
        {
            QuestId = questId;
            CharacterId = characterId;
        }

        #endregion

        #region Properties

        public Quest Quest => _quest ?? (_quest = ServerManager.Instance.GetQuest(QuestId));

        public short QuestNumber { get; set; }

        public List<QuestRewardDTO> QuestRewards { get; set; }

        public bool RewardInWaiting { get; set; }

        #endregion

        #region Methods

        public string GetInfoPacket(bool sendMsg)
        {
            return
                $"{QuestNumber}.{Quest.InfoId}.{((IsMainQuest || Quest.IsDaily || Quest.CanBeDoneOnlyOnce) && Quest.QuestId < 5000 ? Quest.InfoId : 0)}.{Quest.QuestType}.{FirstObjective}.{GetObjectiveByIndex(1)?.Objective ?? 0}.{(RewardInWaiting ? 1 : 0)}.{SecondObjective}.{GetObjectiveByIndex(2)?.Objective ?? 0}.{ThirdObjective}.{GetObjectiveByIndex(3)?.Objective ?? 0}.{FourthObjective}.{GetObjectiveByIndex(4)?.Objective ?? 0}.{FifthObjective}.{GetObjectiveByIndex(5)?.Objective ?? 0}.{(sendMsg ? 1 : 0)}";
        }

        public QuestObjectiveDTO GetObjectiveByIndex(byte index)
        {
            return Quest.QuestObjectives.FirstOrDefault(q => q.ObjectiveIndex.Equals(index));
        }

        public int[] GetObjectives()
        {
            return new[] { FirstObjective, SecondObjective, ThirdObjective, FourthObjective, FifthObjective };
        }

        public string GetProgressMessage(byte index, int amount)
        {
            string type = null;
            string objectiveName = null;
            switch ((QuestType)Quest.QuestType)
            {
                case QuestType.Capture1:
                case QuestType.Capture2:
                    type = "capturing";
                    objectiveName = ServerManager.GetNpcMonster((short)GetObjectiveByIndex(index).Data).Name;
                    break;

                case QuestType.Collect1:
                case QuestType.Collect2:
                case QuestType.Collect3:
                case QuestType.Collect4:
                    objectiveName = ServerManager.GetItem((short)GetObjectiveByIndex(index).Data).Name;
                    type = "collecting";
                    break;

                case QuestType.Hunt:
                    type = "hunting";
                    objectiveName = ServerManager.GetNpcMonster((short)GetObjectiveByIndex(index).Data).Name;
                    break;
            }

            if (!string.IsNullOrEmpty(type) && !string.IsNullOrEmpty(objectiveName) &&
                GetObjectives()[index - 1] < GetObjectiveByIndex(index).Objective)
            {
                switch ((QuestType)Quest.QuestType)
                {
                    case QuestType.Hunt:
                        return $"say 2 {CharacterId} 11 [{objectiveName}] {type}: {GetObjectives()[index - 1] + amount}/?";
                        break;

                    case QuestType.Collect2:
                        return $"say 2 {CharacterId} 11 [{objectiveName}] {type}: {GetObjectives()[index - 1] + amount}/?";
                        break;

                    default:
                        return $"say 2 {CharacterId} 11 [{objectiveName}] {type}: {GetObjectives()[index - 1] + amount}/{GetObjectiveByIndex(index).Objective}";
                        break;
                }
            }
            return "";
        }

        public void Incerment(byte index, int amount)
        {
            switch (index)
            {
                case 1:
                    FirstObjective += FirstObjective >= GetObjectiveByIndex(index)?.Objective ? 0 : amount;
                    break;

                case 2:
                    SecondObjective += SecondObjective >= GetObjectiveByIndex(index)?.Objective ? 0 : amount;
                    break;

                case 3:
                    ThirdObjective += ThirdObjective >= GetObjectiveByIndex(index)?.Objective ? 0 : amount;
                    break;

                case 4:
                    FourthObjective += FourthObjective >= GetObjectiveByIndex(index)?.Objective ? 0 : amount;
                    break;

                case 5:
                    FifthObjective += FifthObjective >= GetObjectiveByIndex(index)?.Objective ? 0 : amount;
                    break;
            }
        }

        #endregion
    }
}