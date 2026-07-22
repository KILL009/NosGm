using NosGm.DAL;
using NosGm.Data;
using System;

namespace NosGm.GameObject.Helpers
{
    public class LogHelper
    {
        #region Members

        private static LogHelper _instance;

        #endregion

        #region Properties

        public static LogHelper Instance
        {
            get { return _instance ?? (_instance = new LogHelper()); }
        }

        #endregion

        #region Methods

        public void InsertQuestLog(long characterId, string ipAddress, long questId, DateTime lastDaily)
        {
            var log = new QuestLogDTO
            {
                CharacterId = characterId,
                IpAddress = ipAddress,
                QuestId = questId,
                LastDaily = lastDaily
            };
            DAOFactory.QuestLogDAO.InsertOrUpdate(ref log);
        }

       
        #endregion
    }
}
