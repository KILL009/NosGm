using NosGm.Data;
using NosGm.Data.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NosGm.DAL.Interface
{
    public interface IGeneralLogDAO
    {
        #region Methods

        bool IdAlreadySet(long id);

        GeneralLogDTO Insert(GeneralLogDTO generalLog);

        Task<GeneralLogDTO> InsertAsync(GeneralLogDTO generalLog);

        SaveResult InsertOrUpdate(ref GeneralLogDTO generalLog);

        IEnumerable<GeneralLogDTO> LoadAll();

        IEnumerable<GeneralLogDTO> LoadByAccount(long? accountId);

        IEnumerable<GeneralLogDTO> LoadByIp(string ip);

        IEnumerable<GeneralLogDTO> LoadByLogType(string logType, long? characterId, bool onlyToday = false);

        IEnumerable<GeneralLogDTO> LoadByLogTypeAndAccountId(string logType, long? accountId);

        void SetCharIdNull(long? characterId);

        void WriteGeneralLog(long accountId, string ipAddress, long? characterId, string logType, string logData);

        #endregion
    }
}