using Frostvein.DAL.EF;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class FamilyMapper
    {
        #region Methods

        public static bool ToFamily(FamilyDTO input, Family output)
        {
            if (input == null) return false;

            output.FamilyFaction = input.FamilyFaction;
            output.FamilyExperience = input.FamilyExperience;
            output.FamilyHeadGender = input.FamilyHeadGender;
            output.FamilyId = input.FamilyId;
            output.FamilyLevel = input.FamilyLevel;
            output.FamilyMessage = input.FamilyMessage;
            output.LastFactionChange = input.LastFactionChange;
            output.ManagerAuthorityType = input.ManagerAuthorityType;
            output.ManagerCanGetHistory = input.ManagerCanGetHistory;
            output.ManagerCanInvite = input.ManagerCanInvite;
            output.ManagerCanNotice = input.ManagerCanNotice;
            output.ManagerCanShout = input.ManagerCanShout;
            output.MaxSize = input.MaxSize;
            output.MemberAuthorityType = input.MemberAuthorityType;
            output.MemberCanGetHistory = input.MemberCanGetHistory;
            output.Name = input.Name;
            output.WarehouseSize = input.WarehouseSize;
            output.IconTopOne = input.IconTopOne;
            output.IconTopRaid = input.IconTopRaid;

            return true;
        }

        public static bool ToFamilyDTO(Family input, FamilyDTO output)
        {
            if (input == null) return false;

            output.FamilyFaction = input.FamilyFaction;
            output.FamilyExperience = input.FamilyExperience;
            output.FamilyHeadGender = input.FamilyHeadGender;
            output.FamilyId = input.FamilyId;
            output.FamilyLevel = input.FamilyLevel;
            output.FamilyMessage = input.FamilyMessage;
            output.LastFactionChange = input.LastFactionChange;
            output.ManagerAuthorityType = input.ManagerAuthorityType;
            output.ManagerCanGetHistory = input.ManagerCanGetHistory;
            output.ManagerCanInvite = input.ManagerCanInvite;
            output.ManagerCanNotice = input.ManagerCanNotice;
            output.ManagerCanShout = input.ManagerCanShout;
            output.MaxSize = input.MaxSize;
            output.MemberAuthorityType = input.MemberAuthorityType;
            output.MemberCanGetHistory = input.MemberCanGetHistory;
            output.Name = input.Name;
            output.WarehouseSize = input.WarehouseSize;
            output.IconTopOne = input.IconTopOne;
            output.IconTopRaid = input.IconTopRaid;

            return true;
        }

        #endregion
    }
}