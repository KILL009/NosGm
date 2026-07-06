using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface IBattlePassQuestDAO
    {
        IEnumerable<BattlePassQuestDTO> LoadAll();

        SaveResult InsertOrUpdate(ref BattlePassQuestDTO account);

        void Insert(List<BattlePassQuestDTO> battlePassQuests);
    }
}