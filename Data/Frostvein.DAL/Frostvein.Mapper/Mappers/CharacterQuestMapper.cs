using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class CharacterQuestMapper
    {
        #region Methods

        public static bool ToCharacterQuest(CharacterQuestDTO input, CharacterQuest output)
        {
            if (input == null) return false;

            output.Id = input.Id;
            output.CharacterId = input.CharacterId;
            output.QuestId = input.QuestId;
            output.FirstObjective = input.FirstObjective;
            output.SecondObjective = input.SecondObjective;
            output.ThirdObjective = input.ThirdObjective;
            output.FourthObjective = input.FourthObjective;
            output.FifthObjective = input.FifthObjective;
            output.IsMainQuest = input.IsMainQuest;

            return true;
        }

        public static bool ToCharacterQuestDTO(CharacterQuest input, CharacterQuestDTO output)
        {
            if (input == null) return false;

            output.Id = input.Id;
            output.CharacterId = input.CharacterId;
            output.QuestId = input.QuestId;
            output.FirstObjective = input.FirstObjective;
            output.SecondObjective = input.SecondObjective;
            output.ThirdObjective = input.ThirdObjective;
            output.FourthObjective = input.FourthObjective;
            output.FifthObjective = input.FifthObjective;
            output.IsMainQuest = input.IsMainQuest;

            return true;
        }

        #endregion
    }
}