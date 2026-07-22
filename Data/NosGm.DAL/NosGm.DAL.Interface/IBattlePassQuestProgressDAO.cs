using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Domain;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IBattlePassQuestProgressDAO
    {
        IEnumerable<BattlePassQuestProgressDTO> LoadAll();

        IEnumerable<BattlePassQuestProgressDTO> LoadByAccountId(long id);

        IEnumerable<BattlePassQuestProgressDTO> LoadByType(BpTimeType type);

        SaveResult InsertOrUpdateFromList(IEnumerable<BattlePassQuestProgressDTO> battlePassQuests);

        DeleteResult Delete(long id);
    }
}