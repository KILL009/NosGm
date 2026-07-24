using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Networking;
using System;

namespace NosGm.GameObject.Extension
{
    public static class RewardExtension
    {
        private const string DailyRewardKey = "DAILY_REWARD";

        public static void DailyReward(ClientSession session)
        {
            DateTime actionDate = DateTime.Now.Date;
            DailyActionClaimResult claim = DAOFactory.AccountDailyActionDAO.TryClaim(
                session.Account.AccountId,
                session.Character.CharacterId,
                DailyRewardKey,
                actionDate);

            if (claim == DailyActionClaimResult.AlreadyClaimed)
            {
                return;
            }

            if (claim != DailyActionClaimResult.Claimed)
            {
                Logger.Error(
                    $"Daily reward claim failed for account {session.Account.AccountId}. Result: {claim}.");
                session.SendPacket(session.Character.GenerateSay(
                    "Daily reward service is temporarily unavailable.", 11));
                return;
            }

            try
            {
                session.Character.GiftAdd(11008, 1);
            }
            catch (Exception exception)
            {
                DAOFactory.AccountDailyActionDAO.ReleaseClaim(
                    session.Account.AccountId,
                    DailyRewardKey,
                    actionDate);
                Logger.Error("Unable to grant the daily reward.", exception);
                return;
            }

            try
            {
                session.SendPacket(session.Character.GenerateSay("You claimed your Daily Reward!", 12));
            }
            catch (Exception exception)
            {
                Logger.Error("Daily reward was granted but its confirmation message failed.", exception);
            }

            if (!DAOFactory.GeneralLogDAO.Enqueue(new GeneralLogDTO
                {
                    AccountId = session.Account.AccountId,
                    CharacterId = session.Character.CharacterId,
                    Timestamp = DateTime.Now,
                    IpAddress = session.IpAddress,
                    LogData = DailyRewardKey,
                    LogType = "World"
                }))
            {
                Logger.Error($"Daily reward audit log failed for account {session.Account.AccountId}.");
            }
        }

        public static void HandleMultipleItemDrops(ClientSession session, MapMonster monsterToAttack, long? owner,
            DropDTO drop)
        {
            session.CurrentMapInstance.DropItemByMonster(owner, drop,
                (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)),
                (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            session.CurrentMapInstance.DropItemByMonster(owner, drop,
                (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)),
                (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            session.CurrentMapInstance.DropItemByMonster(owner, drop,
                (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)),
                (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            session.CurrentMapInstance.DropItemByMonster(owner, drop,
                (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)),
                (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            session.CurrentMapInstance.DropItemByMonster(owner, drop,
                (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)),
                (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            session.CurrentMapInstance.DropItemByMonster(owner, drop,
                (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)),
                (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            session.CurrentMapInstance.DropItemByMonster(owner, drop,
                (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)),
                (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            session.CurrentMapInstance.DropItemByMonster(owner, drop,
                (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)),
                (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            session.CurrentMapInstance.DropItemByMonster(owner, drop,
                (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)),
                (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
        }
    }
}
