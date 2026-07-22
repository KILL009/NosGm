using NosGm.DAL.EF;
using NosGm.DAL.EF.Helpers;
using NosGm.DAL.Interface;
using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.DAO
{
    public class BattlePassPrizeDAO : IBattlePassPrizeDAO
    {
        public IEnumerable<BattlePassPrizeDTO> LoadAll()
        {
            using (NosGmContext context = DataAccessHelper.CreateContext())
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