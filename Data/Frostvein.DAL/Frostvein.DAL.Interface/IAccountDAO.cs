using Frostvein.Data;
using Frostvein.Data.Enums;
using Frostvein.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Frostvein.DAL.Interface
{
    public interface IAccountDAO
    {
        #region Methods

        DeleteResult Delete(long accountId);

        SaveResult InsertOrUpdate(ref AccountDTO account);

        bool ContainsAccounts();

        void Insert(List<AccountDTO> account);

        AccountDTO LoadById(long accountId);

        AccountDTO LoadByName(string name);

        Task WriteGeneralLog(long accountId, string ipAddress, long? characterId, GeneralLogType logType,
            string logData);

        #endregion
    }
}