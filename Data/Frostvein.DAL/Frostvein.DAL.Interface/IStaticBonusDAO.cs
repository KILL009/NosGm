using Frostvein.Data;
using Frostvein.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.DAL.Interface
{
    public interface IStaticBonusDAO
    {
        #region Methods

        void Delete(short bonusToDelete, long characterId);

        /// <summary>
        ///     Inserts new object to database context
        /// </summary>
        /// <param name="staticBonus"></param>
        /// <returns></returns>
        SaveResult InsertOrUpdate(ref StaticBonusDTO staticBonus);

        /// <summary>
        ///     Inserts new object to database context
        /// </summary>
        /// <param name="staticBonus"></param>
        /// <returns></returns>
        Task<SaveResult> InsertOrUpdateAsync(StaticBonusDTO staticBonus);

        /// <summary>
        ///     Loads staticBonus by characterid
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        IEnumerable<StaticBonusDTO> LoadByCharacterId(long characterId);

        IEnumerable<short> LoadTypeByCharacterId(long characterId);

        #endregion
    }
}