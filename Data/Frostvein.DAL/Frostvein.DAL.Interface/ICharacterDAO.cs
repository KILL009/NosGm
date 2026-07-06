using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.DAL.Interface
{
    public interface ICharacterDAO
    {
        #region Methods

        DeleteResult DeleteByPrimaryKey(long accountId, byte characterSlot);

        List<CharacterDTO> GetTopCompliment();

        List<CharacterDTO> GetTopPoints();

        List<CharacterDTO> GetTopReputation();

        List<CharacterDTO> GetTopDuel();

        List<CharacterDTO> GetTopMonster();

        Task<SaveResult> InsertOrUpdate(CharacterDTO character);

        Task<SaveResult> InsertOrUpdateAsync(CharacterDTO character);

        IEnumerable<CharacterDTO> LoadAll();

        IEnumerable<CharacterDTO> LoadAllByAccount(long accountId);

        IEnumerable<CharacterDTO> LoadByAccount(long accountId);        

        CharacterDTO LoadById(long characterId);

        CharacterDTO LoadByName(string name);

        CharacterDTO LoadBySlot(long accountId, byte slot);

        public bool CheckNameAlreadyExists(string name);

        #endregion
    }
}