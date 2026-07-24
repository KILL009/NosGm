using NosGm.DAL;
using NosGm.Data;
using NosGm.GameObject.Networking;
using System;

namespace NosGm.GameObject.Extension
{
    public static class RewardExtension
    {
        private const string DailyRewardKey = "DAILY_REWARD";

        public static void DailyReward(ClientSession session)
        {
            DateTime today = DateTime.Now.Date;
            if (DAOFactory.GeneralLogDAO.ExistsForAccount(
                    session.Account.AccountId,
                    DailyRewardKey,
                    today,
                    today.AddDays(1)))
            {
                return;
            }

            session.Character.GiftAdd(11008, 1);
            session.SendPacket(session.Character.GenerateSay("You claimed your Daily Reward!", 12));

            DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
            {
                AccountId = session.Account.AccountId,
                CharacterId = session.Character.CharacterId,
                Timestamp = DateTime.Now,
                IpAddress = session.IpAddress,
                LogData = DailyRewardKey,
                LogType = "World"
            });
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
