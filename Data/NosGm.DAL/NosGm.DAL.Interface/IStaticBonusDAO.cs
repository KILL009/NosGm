using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
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