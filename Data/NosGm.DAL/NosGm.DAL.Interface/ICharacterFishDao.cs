using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public interface ICharacterFishDao
    {
        IEnumerable<CharacterFishDto> LoadByCharacterId(long characterId);

        SaveResult InsertOrUpdate(IEnumerable<CharacterFishDto> fishes);

        Task<SaveResult> InsertOrUpdateAsync(IEnumerable<CharacterFishDto> fishes);

        SaveResult InsertOrUpdateFromList(IEnumerable<CharacterFishDto> logs);
    }
}
