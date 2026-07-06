using Frostvein.Data;
using Frostvein.Data.Enums;
using Frostvein.Domain;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
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