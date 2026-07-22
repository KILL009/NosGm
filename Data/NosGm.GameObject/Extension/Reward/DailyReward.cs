using NosGm.Core;
using NosGm.DAL;
using NosGm.Data;
using NosGm.Domain;
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Helpers;
using NosGm.GameObject.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using static System.Collections.Specialized.BitVector32;

namespace NosGm.GameObject.Extension
{
    public static class DailyRewardExtension
    {
        public static async Task GenerateReward(ClientSession Session)
        {
            var rnd = ServerManager.RandomNumber(0, 1000);
            if (rnd < 5)
            {
                short[] vnums =
                {
                    8183
                };
                byte[] counts = { 1 };
                var item = ServerManager.RandomNumber(0, 1);
                Session.Character.GiftAdd(vnums[item], counts[item]);
                Session.Character.DailyRewardChest += 1;
                MessageExtension.SendGreen(Session, $"You opened {Session.Character.DailyRewardChest}/100 Daily Reward Chests");
            }
            else if (rnd < 30)
            {
                short[] vnums = { 361, 362, 363, 366, 367, 368, 371, 372, 373 };
                Session.Character.GiftAdd(vnums[ServerManager.RandomNumber(0, 9)], 1);
                Session.Character.DailyRewardChest += 1;
                MessageExtension.SendGreen(Session, $"You opened {Session.Character.DailyRewardChest}/100 Daily Reward Chests");
            }
            else
            {
                short[] vnums =
                {
                    1161, 2282, 1030, 1244, 1218, 5369, 1012, 1363, 1364, 2160, 2173,
                    5959, 5983, 2514,
                    2515, 2516, 2517, 2518, 2519, 2520, 2521, 1685, 1686, 5087, 5203,
                    2418, 2310, 2303,
                    2169, 2280, 5892, 5893, 5894, 5895, 5896, 5897, 5898, 5899, 5332,
                    5105, 2161, 2162
                };
                byte[] counts =
                {
                   10, 10, 20, 5, 1, 1, 99, 1, 1, 5, 5, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2,
                   1, 1, 1, 1, 5, 20,
                   20, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1
                };
                var item = ServerManager.RandomNumber(0, 42);
                Session.Character.GiftAdd(vnums[item], counts[item]);
                Session.Character.DailyRewardChest += 1;
                MessageExtension.SendGreen(Session, $"You opened {Session.Character.DailyRewardChest}/100 Daily Reward Chests");
            }
            await GenerateRareReward(Session);
        }

        public static async Task GenerateRareReward(ClientSession Session)
        {
            int rnd = ServerManager.RandomNumber(1, 3);

            switch (Session.Character.DailyRewardChest)
            {
                case 10:
                    switch (rnd)
                    {
                        case 1:
                            Session.Character.GiftAdd(5498, 1);
                            break;

                        case 2:
                            Session.Character.GiftAdd(5499, 1);
                            break;

                        case 3:
                            Session.Character.GiftAdd(5431, 1);
                            break;
                    }
                    break;

                case 25:
                    switch (rnd)
                    {
                        case 1:
                            Session.Character.GiftAdd(5432, 1);
                            break;

                        case 2:
                            Session.Character.GiftAdd(5238, 1);
                            break;

                        case 3:
                            Session.Character.GiftAdd(5240, 1);
                            break;
                    }
                    break;

                case 50:
                    switch (rnd)
                    {
                        case 1:
                            Session.Character.GiftAdd(9577, 1);
                            break;

                        case 2:
                            Session.Character.GiftAdd(5553, 1);
                            break;

                        case 3:
                            Session.Character.GiftAdd(5560, 1);
                            break;
                    }
                    break;

                case 100:
                    switch (rnd)
                    {
                        case 1:
                            Session.Character.GiftAdd(9760, 1);
                            break;

                        case 2:
                            Session.Character.GiftAdd(9776, 1);
                            break;

                        case 3:
                            Session.Character.GiftAdd(9824, 1);
                            break;
                    }
                    Session.Character.DailyRewardChest = 0;
                    break;
            }
        }
    }
}