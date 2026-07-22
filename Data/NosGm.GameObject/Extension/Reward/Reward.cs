using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

using static System.Collections.Specialized.BitVector32;

namespace NosGm.GameObject.Extension
{
    public static class RewardExtension
    {
        public static void DailyReward(ClientSession session)
        {
            var isMartial = session.Character.Class.Equals(ClassType.MartialArtist);
            var count = DAOFactory.GeneralLogDAO.LoadByAccount(session.Account.AccountId)
                .Count(s => s.LogData == (isMartial ? "DAILY_REWARD" : "DAILY_REWARD") && s.Timestamp.Day >= DateTime.Now.Day);
            if (count != 0)
            {
                return;
            }
            session.Character.GiftAdd((short)(isMartial ? 11008 : 11008), (short)(isMartial ? 1 : 1));
            session.SendPacket(session.Character.GenerateSay("You claimed your Daily Reward!", 12));

            DAOFactory.GeneralLogDAO.Insert(new GeneralLogDTO
            {
                AccountId = session.Account.AccountId,
                CharacterId = session.Character.CharacterId,
                Timestamp = DateTime.Now,
                IpAddress = session.IpAddress,
                LogData = isMartial ? "DAILY_REWARD" : "DAILY_REWARD",
                LogType = "World"
            });
        }

        public static void HandleMultipleItemDrops(ClientSession Session, MapMonster monsterToAttack, long? owner, DropDTO drop)
        {
            Session.CurrentMapInstance.DropItemByMonster(owner, drop, (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)), (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            Session.CurrentMapInstance.DropItemByMonster(owner, drop, (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)), (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            Session.CurrentMapInstance.DropItemByMonster(owner, drop, (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)), (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            Session.CurrentMapInstance.DropItemByMonster(owner, drop, (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)), (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            Session.CurrentMapInstance.DropItemByMonster(owner, drop, (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)), (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            Session.CurrentMapInstance.DropItemByMonster(owner, drop, (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)), (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            Session.CurrentMapInstance.DropItemByMonster(owner, drop, (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)), (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            Session.CurrentMapInstance.DropItemByMonster(owner, drop, (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)), (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
            Session.CurrentMapInstance.DropItemByMonster(owner, drop, (short)(monsterToAttack.MapX + ServerManager.RandomNumber(-10, 10)), (short)(monsterToAttack.MapY + ServerManager.RandomNumber(-10, 10)));
        }
    }
}