using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;

namespace Frostvein.DAL.Interface
{
    public interface ICharacterTimespaceLogDAO
    {
        IEnumerable<CharacterTimespaceLogDTO> LoadByCharactedId(long characterId);

        SaveResult InsertOrUpdateFromList(IEnumerable<CharacterTimespaceLogDTO> logs);

        CharacterTimespaceLogDTO GetHighestScoreByScriptedInstanceId(long scriptedInstanceId);

        SaveResult InsertOrUpdate(CharacterTimespaceLogDTO card);

        CharacterTimespaceLogDTO Insert(CharacterTimespaceLogDTO timespaceLog);

        List<CharacterTimespaceLogDTO> LoadAll();

        bool IdAlreadySet(long id);
    }
}
