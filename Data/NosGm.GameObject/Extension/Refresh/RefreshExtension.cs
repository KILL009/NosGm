using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
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
            ExecuteDailyAction(session, PrimalQuestRefreshKey, () =>
            {
                session.SendPacket(session.Character.GenerateSay("Your Primal Quests have been refreshed", 12));
                session.Character.PrimalQuestCount = 0;
            });
        }

        public static void DuelCountRefresh(ClientSession session)
        {
            ExecuteDailyAction(session, DuelCountRefreshKey, () =>
            {
                session.SendPacket(session.Character.GenerateSay("Your Duel Count has been refreshed.", 12));
                session.Character.DuelCount = 0;
            });
        }

        public static void IceFlowerRefresh(ClientSession session)
        {
            ExecuteDailyAction(session, IceFlowerRefreshKey, () =>
            {
                session.Character.HasDoneIceFlowerQuest = false;
            });
        }

        private static void ExecuteDailyAction(ClientSession session, string actionKey, Action action)
        {
            DateTime actionDate = DateTime.Now.Date;
            DailyActionClaimResult claim = DAOFactory.AccountDailyActionDAO.TryClaim(
                session.Account.AccountId,
                session.Character.CharacterId,
                actionKey,
                actionDate);

            if (claim == DailyActionClaimResult.AlreadyClaimed)
            {
                return;
            }

            if (claim != DailyActionClaimResult.Claimed)
            {
                Logger.Error(
                    $"Daily action {actionKey} failed for account {session.Account.AccountId}. Result: {claim}.");
                session.SendPacket(session.Character.GenerateSay(
                    "Daily action service is temporarily unavailable.", 11));
                return;
            }

            try
            {
                action();
                WriteRefreshLog(session, actionKey);
            }
            catch (Exception exception)
            {
                DAOFactory.AccountDailyActionDAO.ReleaseClaim(
                    session.Account.AccountId,
                    actionKey,
                    actionDate);
                Logger.Error($"Unable to complete daily action {actionKey}.", exception);
            }
        }

        private static void WriteRefreshLog(ClientSession session, string actionKey)
        {
            DAOFactory.GeneralLogDAO.Enqueue(new GeneralLogDTO
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
