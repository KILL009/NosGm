using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;

namespace NosGm.DAL.Interface
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
