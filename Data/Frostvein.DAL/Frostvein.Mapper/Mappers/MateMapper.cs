using Frostvein.DAL.EF.Entities;
using Frostvein.Data;

namespace Frostvein.Mapper.Mappers
{
    public static class MateMapper
    {
        #region Methods

        public static bool ToMate(MateDTO input, Mate output)
        {
            if (input == null) return false;

            output.Attack = input.Attack;
            output.CanPickUp = input.CanPickUp;
            output.CharacterId = input.CharacterId;
            output.Defence = input.Defence;
            output.Direction = input.Direction;
            output.Experience = input.Experience;
            output.Hp = input.Hp;
            output.IsSummonable = input.IsSummonable;
            output.IsTeamMember = input.IsTeamMember;
            output.Level = input.Level;
            output.StarRating = input.StarRating;
            output.TrainerLevel = input.TrainerLevel;
            output.Skill1 = input.Skill1;
            output.Skill2 = input.Skill2;
            output.CurrentEXP = input.CurrentEXP;
            output.EXPNeeded = input.EXPNeeded;
            output.Loyalty = input.Loyalty;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.MateId = input.MateId;
            output.MateType = input.MateType;
            output.Mp = input.Mp;
            output.Name = input.Name;
            output.NpcMonsterVNum = input.NpcMonsterVNum;
            output.Skin = input.Skin;

            return true;
        }

        public static bool ToMateDTO(Mate input, MateDTO output)
        {
            if (input == null) return false;

            output.Attack = input.Attack;
            output.CanPickUp = input.CanPickUp;
            output.CharacterId = input.CharacterId;
            output.Defence = input.Defence;
            output.Direction = input.Direction;
            output.Experience = input.Experience;
            output.Hp = input.Hp;
            output.IsSummonable = input.IsSummonable;
            output.IsTeamMember = input.IsTeamMember;
            output.Level = input.Level;
            output.StarRating = input.StarRating;
            output.TrainerLevel = input.TrainerLevel;
            output.Skill1 = input.Skill1;
            output.Skill2 = input.Skill2;
            output.CurrentEXP = input.CurrentEXP;
            output.EXPNeeded = input.EXPNeeded;
            output.Loyalty = input.Loyalty;
            output.MapX = input.MapX;
            output.MapY = input.MapY;
            output.MateId = input.MateId;
            output.MateType = input.MateType;
            output.Mp = input.Mp;
            output.Name = input.Name;
            output.NpcMonsterVNum = input.NpcMonsterVNum;
            output.Skin = input.Skin;

            return true;
        }

        #endregion
    }
}