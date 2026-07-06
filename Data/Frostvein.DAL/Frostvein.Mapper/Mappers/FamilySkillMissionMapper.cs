using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public class FamilySkillMissionMapper
    {
        public static bool ToFamilySkillMission(FamilySkillMissionDTO input, FamilySkillMission output)
        {
            if (input == null)
            {
                return false;
            }
            //DONT MODIFY IDENTITY :) ( ID )
            output.FamilyId = input.FamilyId;
            output.ItemVNum = input.ItemVNum;
            output.CurrentValue = input.CurrentValue;
            output.Date = input.Date;
            output.TotalValue = input.TotalValue;

            return true;
        }

        public static bool ToFamilySkillMissionDTO(FamilySkillMission input, FamilySkillMissionDTO output)
        {
            if (input == null)
            {
                return false;
            }
            output.FamilySkillMissionId = input.FamilySkillMissionId;
            output.FamilyId = input.FamilyId;
            output.ItemVNum = input.ItemVNum;
            output.CurrentValue = input.CurrentValue;
            output.Date = input.Date;
            output.TotalValue = input.TotalValue;


            return true;
        }
    }
}
