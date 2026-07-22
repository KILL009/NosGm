using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
{
    public interface IBattlePassAccountLogDAO
    {
        bool IdAlreadySet(long id);

        IEnumerable<BattlePassAccountLogDTO> LoadAll();

        IEnumerable<BattlePassAccountLogDTO> LoadAllById(long id);

        SaveResult InsertOrUpdateFromList(IEnumerable<BattlePassAccountLogDTO> battlePassQuests);

        DeleteResult Delete(long id);
    }
}