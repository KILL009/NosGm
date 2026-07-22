using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public interface IStaticBuffDAO
    {
        #region Methods

        void Delete(short bonusToDelete, long characterId);

        /// <summary>
        ///     Inserts new object to database context
        /// </summary>
        /// <param name="staticBuff"></param>
        /// <returns></returns>
        SaveResult InsertOrUpdate(ref StaticBuffDTO staticBuff);

        /// <summary>
        ///     Inserts new object to database context
        /// </summary>
        /// <param name="staticBuff"></param>
        /// <returns></returns>
        Task<SaveResult> InsertOrUpdateAsync(StaticBuffDTO staticBuff);

        /// <summary>
        ///     Loads staticBonus by characterid
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        IEnumerable<StaticBuffDTO> LoadByCharacterId(long characterId);

        /// <summary>
        ///     Loads by CharacterId
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns>IEnumerable list of CardIds</returns>
        IEnumerable<short> LoadByTypeCharacterId(long characterId);

        #endregion
    }
}