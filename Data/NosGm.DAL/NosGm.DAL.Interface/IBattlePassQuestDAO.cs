using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IBattlePassQuestDAO
    {
        IEnumerable<BattlePassQuestDTO> LoadAll();

        SaveResult InsertOrUpdate(ref BattlePassQuestDTO account);

        void Insert(List<BattlePassQuestDTO> battlePassQuests);
    }
}