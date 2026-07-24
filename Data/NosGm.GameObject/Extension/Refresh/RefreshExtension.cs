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
            ExecuteDailyAction(
                session,
                PrimalQuestRefreshKey,
                () => session.Character.PrimalQuestCount = 0,
                "Your Primal Quests have been refreshed");
        }

        public static void DuelCountRefresh(ClientSession session)
        {
            ExecuteDailyAction(
                session,
                DuelCountRefreshKey,
                () => session.Character.DuelCount = 0,
                "Your Duel Count has been refreshed.");
        }

        public static void IceFlowerRefresh(ClientSession session)
        {
            ExecuteDailyAction(
                session,
                IceFlowerRefreshKey,
                () => session.Character.HasDoneIceFlowerQuest = false,
                null);
        }

        private static void ExecuteDailyAction(
            ClientSession session,
            string actionKey,
            Action stateMutation,
            string confirmationMessage)
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
                stateMutation();
            }
            catch (Exception exception)
            {
                DAOFactory.AccountDailyActionDAO.ReleaseClaim(
                    session.Account.AccountId,
                    actionKey,
                    actionDate);
                Logger.Error($"Unable to complete daily action {actionKey}.", exception);
                return;
            }

            if (!string.IsNullOrWhiteSpace(confirmationMessage))
            {
                try
                {
                    session.SendPacket(session.Character.GenerateSay(confirmationMessage, 12));
                }
                catch (Exception exception)
                {
                    Logger.Error($"Daily action {actionKey} completed but its confirmation failed.", exception);
                }
            }

            if (!WriteRefreshLog(session, actionKey))
            {
                Logger.Error(
                    $"Daily action audit log {actionKey} failed for account {session.Account.AccountId}.");
            }
        }

        private static bool WriteRefreshLog(ClientSession session, string actionKey)
        {
            return DAOFactory.GeneralLogDAO.Enqueue(new GeneralLogDTO
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
