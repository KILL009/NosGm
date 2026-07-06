using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
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