using Frostvein.DAL.EF;
using Frostvein.DAL.EF.Helpers;
using Frostvein.DAL.Interface;
using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.DAO
{
    public class BattlePassPrizeDAO : IBattlePassPrizeDAO
    {
        public IEnumerable<BattlePassPrizeDTO> LoadAll()
        {
            using (FrostveinContext context = DataAccessHelper.CreateContext())
            {
                List<BattlePassPrizeDTO> result = new List<BattlePassPrizeDTO>();
                foreach (BattlePassPrize prize in context.BattlePassPrize)
                {
                    BattlePassPrizeDTO dto = new BattlePassPrizeDTO();
                    Mapper.Mappers.BattlePassPrizeMapper.ToBpQuestDTOPrize(prize, dto);
                    result.Add(dto);
                }
                return result;
            }
        }
    }
}