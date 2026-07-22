using NosGm.Data;
using NosGm.Data.Enums;
using NosGm.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
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