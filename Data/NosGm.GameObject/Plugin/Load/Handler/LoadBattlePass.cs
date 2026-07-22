using NosGm.Configuration;
using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NosGm.GameObject.Plugin.Load
{
    public static class PluginLoadBattlePass
    {
        public static void Load()
        {
            ServerManager.Instance.BattlePassQuests = DAOFactory.BattlePassQuestDAO.LoadAll().ToList();
            ServerManager.Instance.BattlePassPrizes = DAOFactory.BattlePassPrizeDAO.LoadAll().ToList();


            GeneralLogDTO dailyBp = DAOFactory.GeneralLogDAO.LoadByLogType("DAILY_BP", null).LastOrDefault();
            GeneralLogDTO weeklyBp = DAOFactory.GeneralLogDAO.LoadByLogType("WEEKLY_BP", null).LastOrDefault();
            GeneralLogDTO seasonBp = DAOFactory.GeneralLogDAO.LoadByLogType("SEASON_BP", null).LastOrDefault();

            if (dailyBp == null)
            {
                dailyBp = new GeneralLogDTO
                {
                    LogType = "DAILY_BP",
                    LogData = "daily bp start",
                    Timestamp = DateTime.UtcNow
                };

                DAOFactory.GeneralLogDAO.Insert(dailyBp);
            }
            else
            {
                if (dailyBp.Timestamp.Date.AddDays(1) < DateTime.UtcNow)
                {
                    dailyBp = new GeneralLogDTO
                    {
                        LogType = "DAILY_BP",
                        LogData = "daily bp start",
                        Timestamp = DateTime.UtcNow
                    };

                    DAOFactory.GeneralLogDAO.Insert(dailyBp);
                }
            }

            if (weeklyBp == null)
            {
                weeklyBp = new GeneralLogDTO
                {
                    LogType = "WEEKLY_BP",
                    LogData = "weekly bp start",
                    Timestamp = DateTime.UtcNow
                };

                DAOFactory.GeneralLogDAO.Insert(weeklyBp);
            }
            else
            {
                if (weeklyBp.Timestamp.Date.AddDays(7) < DateTime.UtcNow)
                {
                    weeklyBp = new GeneralLogDTO
                    {
                        LogType = "WEEKLY_BP",
                        LogData = "weekly bp start",
                        Timestamp = DateTime.UtcNow
                    };

                    DAOFactory.GeneralLogDAO.Insert(weeklyBp);
                }
            }

            if (seasonBp == null)
            {
                seasonBp = new GeneralLogDTO
                {
                    LogType = "SEASON_BP",
                    LogData = "season bp start",
                    Timestamp = DateTime.UtcNow
                };

                DAOFactory.GeneralLogDAO.Insert(seasonBp);
            }
            else
            {
                if (seasonBp.Timestamp.Date.AddDays(45) < DateTime.UtcNow)
                {
                    seasonBp = new GeneralLogDTO
                    {
                        LogType = "SEASON_BP",
                        LogData = "season bp start",
                        Timestamp = DateTime.UtcNow
                    };

                    DAOFactory.GeneralLogDAO.Insert(seasonBp);
                }
            }

            ServerManager.DailyBpDate = dailyBp.Timestamp.Date;
            ServerManager.Instance.WeeklyBpDate = weeklyBp.Timestamp.Date;
            ServerManager.Instance.SeasonBpDate = seasonBp.Timestamp.Date;

            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.BattlePassPrizes.Count} Battle Pass Prizes - Status: Successful", Domain.LogType.LOAD);
            LoggerService.LogServer.Logger.UpdateLoadOutput($"{ServerManager.Instance.BattlePassQuests.Count} Battle Pass Quests - Status: Successful", Domain.LogType.LOAD);
        }
    }
}
