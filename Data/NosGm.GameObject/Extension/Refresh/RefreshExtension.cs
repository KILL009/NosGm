using NosGm.DAL;
using NosGm.Data;
using System;
using System.Linq;

namespace NosGm.GameObject.Service
{
    public static class RefreshExtension
    {
        public static void RefreshPrimalQuest(ClientSession session)
        {
            var count = DAOFactory.GeneralLogDAO.LoadByAccount(session.Account.AccountId).Count(s => s.LogData == ("PRIMALQUEST_REFRESH") && s.Timestamp.Day >= DateTime.Now.Day);

            if (count != 0)
            {
                return;
            }

            session.SendPacket(session.Character.GenerateSay("Your Primal Quests have been refreshed", 12));
            session.Character.PrimalQuestCount = 0;

            DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
            {
                AccountId = session.Account.AccountId,
                CharacterId = session.Character.CharacterId,
                Timestamp = DateTime.Now,
                IpAddress = session.IpAddress,
                LogData = "PRIMALQUEST_REFRESH",
                LogType = "World"
            });
        }

        public static void DuelCountRefresh(ClientSession session)
        {
            var count = DAOFactory.GeneralLogDAO.LoadByAccount(session.Account.AccountId).Count(s => s.LogData == ("DUELCOUNT_REFRESH") && s.Timestamp.Day >= DateTime.Now.Day);

            if (count != 0)
            {
                return;
            }

            session.SendPacket(session.Character.GenerateSay("Your Duel Count has been refreshed.", 12));
            session.Character.DuelCount = 0;
            DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
            {
                AccountId = session.Account.AccountId,
                CharacterId = session.Character.CharacterId,
                Timestamp = DateTime.Now,
                IpAddress = session.IpAddress,
                LogData = "DUELCOUNT_REFRESH",
                LogType = "World"
            });
        }

        public static void IceFlowerRefresh(ClientSession session)
        {
            var count = DAOFactory.GeneralLogDAO.LoadByAccount(session.Account.AccountId).Count(s => s.LogData == ("ICEFLOWER_REFRESH") && s.Timestamp.Day >= DateTime.Now.Day);

            if (count != 0)
            {
                return;
            }

            session.Character.HasDoneIceFlowerQuest = false;
            DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
            {
                AccountId = session.Account.AccountId,
                CharacterId = session.Character.CharacterId,
                Timestamp = DateTime.Now,
                IpAddress = session.IpAddress,
                LogData = "ICEFLOWER_REFRESH",
                LogType = "World"
            });
        }
    }
}