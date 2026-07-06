using Frostvein.Data;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IBattlePassPrizeDAO
    {
        IEnumerable<BattlePassPrizeDTO> LoadAll();
    }
}