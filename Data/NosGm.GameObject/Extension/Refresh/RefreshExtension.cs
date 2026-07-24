using NosGm.DAL;
using NosGm.Data;
using System;

namespace NosGm.GameObject.Service
{
    public static class RefreshExtension
    {
        private const string PrimalQuestRefreshKey = "PRIMALQUEST_REFRESH";
        private const string DuelCountRefreshKey = "DUELCOUNT_REFRESH";
        private const string IceFlowerRefreshKey = "ICEFLOWER_REFRESH";

        public static void RefreshPrimalQuest(ClientSession session)
        {
            if (WasRunToday(session, PrimalQuestRefreshKey))
            {
                return;
            }

            session.SendPacket(session.Character.GenerateSay("Your Primal Quests have been refreshed", 12));
            session.Character.PrimalQuestCount = 0;
            WriteRefreshLog(session, PrimalQuestRefreshKey);
        }

        public static void DuelCountRefresh(ClientSession session)
        {
            if (WasRunToday(session, DuelCountRefreshKey))
            {
                return;
            }

            session.SendPacket(session.Character.GenerateSay("Your Duel Count has been refreshed.", 12));
            session.Character.DuelCount = 0;
            WriteRefreshLog(session, DuelCountRefreshKey);
        }

        public static void IceFlowerRefresh(ClientSession session)
        {
            if (WasRunToday(session, IceFlowerRefreshKey))
            {
                return;
            }

            session.Character.HasDoneIceFlowerQuest = false;
            WriteRefreshLog(session, IceFlowerRefreshKey);
        }

        private static bool WasRunToday(ClientSession session, string actionKey)
        {
            DateTime today = DateTime.Now.Date;
            return DAOFactory.GeneralLogDAO.ExistsForAccount(
                session.Account.AccountId,
                actionKey,
                today,
                today.AddDays(1));
        }

        private static void WriteRefreshLog(ClientSession session, string actionKey)
        {
            DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
            {
                AccountId = session.Account.AccountId,
                CharacterId = session.Character.CharacterId,
                Timestamp = DateTime.Now,
                IpAddress = session.IpAddress,
                LogData = actionKey,
                LogType = "World"
            });
        }
    }
}
