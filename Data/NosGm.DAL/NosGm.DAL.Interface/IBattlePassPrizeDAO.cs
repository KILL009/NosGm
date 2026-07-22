using NosGm.Data;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IBattlePassPrizeDAO
    {
        IEnumerable<BattlePassPrizeDTO> LoadAll();
    }
}