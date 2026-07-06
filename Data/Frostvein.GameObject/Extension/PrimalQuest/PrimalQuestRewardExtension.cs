namespace Frostvein.GameObject.Service
{
    public static class PrimalQuestRewardExtension
    {
        public static void AddCharacterReward(ClientSession session, short amount)
        {
            short rewardVnum = 13010;
            if (session.Character.Inventory.CanAddItem(rewardVnum))
            {
                session.Character.PrimalCharacterQuestProgress = 0;
                session.Character.PrimalCharacterQuest = 0;
                session.Character.GiftAdd(rewardVnum, amount);
                session.SendPacket($"qr 7 {rewardVnum} {amount}");
                session.SendPacket("msg 3 [Primal Quest] Congratulations! You finished the Quest.");
            }
            else
            {
                session.SendPacket("modal 1 Primal Quest\n\nAttention!\nYou dont have any space in your Inventory.\nTherefore, the Primal Coin couldn't be added.\nYour Primal Quest Progress decreased by 1");
                session.Character.PrimalCharacterQuestProgress -= 1;
            }
        }

        public static void AddRaidReward(ClientSession session, short amount)
        {
            short rewardVnum = 13010;
            if (session.Character.Inventory.CanAddItem(rewardVnum))
            {
                session.Character.PrimalRaidQuestProgress = 0;
                session.Character.PrimalRaidQuest = 0;
                session.Character.GiftAdd(rewardVnum, amount);
                session.SendPacket($"qr 7 {rewardVnum} {amount}");
                session.SendPacket("msg 3 [Primal Quest] Congratulations! You finished the Quest.");
            }
            else
            {
                session.SendPacket("modal 1 Primal Quest\n\nAttention!\nYou dont have any space in your Inventory.\nTherefore, the Primal Coin couldn't be added.\nYour Primal Quest Progress decreased by 1");
                session.Character.PrimalRaidQuestProgress -= 1;
            }
        }

        public static void GenerateCharacterReward(ClientSession session, MapMonster monsterToAttack)
        {
            if (session.Character.PrimalCharacterQuest == 0)
            {
                return;
            }
            #region Character Quest
            switch (session.Character.PrimalCharacterQuest)
            {
                //4 x50
                case 1:
                    if (monsterToAttack.MonsterVNum == 4)
                    {
                        if (session.Character.PrimalCharacterQuestProgress < 50)
                        {
                            session.Character.PrimalCharacterQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Hunted Dusi-Fox: {session.Character.PrimalCharacterQuestProgress}/50");
                        }
                        if (session.Character.PrimalCharacterQuestProgress >= 50)
                        {
                            AddCharacterReward(session, 1);
                        }
                    }
                    break;

                //152 x50
                case 2:
                    if (monsterToAttack.MonsterVNum == 152)
                    {
                        if (session.Character.PrimalCharacterQuestProgress < 50)
                        {
                            session.Character.PrimalCharacterQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Hunted Kenko Raider: {session.Character.PrimalCharacterQuestProgress}/50");
                        }
                        if (session.Character.PrimalCharacterQuestProgress >= 50)
                        {
                            AddCharacterReward(session, 2);
                        }
                    }
                    break;

                //439 x100
                case 3:
                    if (monsterToAttack.MonsterVNum == 439)
                    {
                        if (session.Character.PrimalCharacterQuestProgress < 100)
                        {
                            session.Character.PrimalCharacterQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Hunted Revenant Skeleton: {session.Character.PrimalCharacterQuestProgress}/100");
                        }
                        if (session.Character.PrimalCharacterQuestProgress >= 100)
                        {
                            AddCharacterReward(session, 4);
                        }
                    }
                    break;

                //1042 x250
                case 4:
                    if (monsterToAttack.MonsterVNum == 1042)
                    {
                        if (session.Character.PrimalCharacterQuestProgress < 250)
                        {
                            session.Character.PrimalCharacterQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Hunted Magmaros: {session.Character.PrimalCharacterQuestProgress}/250");
                        }
                        if (session.Character.PrimalCharacterQuestProgress >= 250)
                        {
                            AddCharacterReward(session, 6);
                        }
                    }
                    break;

                //2510 x250
                case 5:
                    if (monsterToAttack.MonsterVNum == 2510)
                    {
                        if (session.Character.PrimalCharacterQuestProgress < 250)
                        {
                            session.Character.PrimalCharacterQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Hunted Tallion: {session.Character.PrimalCharacterQuestProgress}/250");
                        }
                        if (session.Character.PrimalCharacterQuestProgress >= 250)
                        {
                            AddCharacterReward(session, 8);
                        }
                    }
                    break;

                //2521 x100
                case 6:
                    if (monsterToAttack.MonsterVNum == 2521)
                    {
                        if (session.Character.PrimalCharacterQuestProgress < 100)
                        {
                            session.Character.PrimalCharacterQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Hunted Sentinel: {session.Character.PrimalCharacterQuestProgress}/100");
                        }
                        if (session.Character.PrimalCharacterQuestProgress >= 100)
                        {
                            AddCharacterReward(session, 8);
                        }
                    }
                    break;
                
                //2561 x200
                case 7:
                    if (monsterToAttack.MonsterVNum == 2561)
                    {
                        if (session.Character.PrimalCharacterQuestProgress < 200)
                        {
                            session.Character.PrimalCharacterQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Hunted Unknown Spirit Mage: {session.Character.PrimalCharacterQuestProgress}/200");
                        }
                        if (session.Character.PrimalCharacterQuestProgress >= 200)
                        {
                            AddCharacterReward(session, 9);
                        }
                    }
                    break;

                //3009 x300
                case 8:
                    if (monsterToAttack.MonsterVNum == 3009)
                    {
                        if (session.Character.PrimalCharacterQuestProgress < 300)
                        {
                            session.Character.PrimalCharacterQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Hunted Twisted Goblin: {session.Character.PrimalCharacterQuestProgress}/300");
                        }
                        if (session.Character.PrimalCharacterQuestProgress >= 300)
                        {
                            AddCharacterReward(session, 10);
                        }
                    }
                    break;

                //3165 x200
                case 9:
                    if (monsterToAttack.MonsterVNum == 3165)
                    {
                        if (session.Character.PrimalCharacterQuestProgress < 200)
                        {
                            session.Character.PrimalCharacterQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Hunted Gryphon: {session.Character.PrimalCharacterQuestProgress}/200");
                        }
                        if (session.Character.PrimalCharacterQuestProgress >= 200)
                        {
                            AddCharacterReward(session, 12);
                        }
                    }
                    break;
            }
            #endregion
        }

        public static void GenerateRaidReward(ClientSession session, byte Type)
        {
            if (session.Character.PrimalRaidQuest == 0)
            {
                return;
            }
            switch (session.Character.PrimalRaidQuest)
            {
                case 1:
                    if (Type == 1)
                    {
                        if (session.Character.PrimalRaidQuestProgress < 5)
                        {
                            session.Character.PrimalRaidQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Finished Raids: {session.Character.PrimalRaidQuestProgress}/5");
                        }
                        if (session.Character.PrimalRaidQuestProgress >= 5)
                        {
                            AddRaidReward(session, 2);
                        }
                    }
                    break;

                case 2:
                    if (Type == 2)
                    {
                        if (session.Character.PrimalRaidQuestProgress < 10)
                        {
                            session.Character.PrimalRaidQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Finished Raids: {session.Character.PrimalRaidQuestProgress}/10");
                        }
                        if (session.Character.PrimalRaidQuestProgress >= 10)
                        {
                            AddRaidReward(session, 5);
                        }
                    }
                    break;

                case 3:
                    if (Type == 3)
                    {
                        if (session.Character.PrimalRaidQuestProgress < 50)
                        {
                            session.Character.PrimalRaidQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Finished Raids: {session.Character.PrimalRaidQuestProgress}/50");
                        }
                        if (session.Character.PrimalRaidQuestProgress >= 50)
                        {
                            AddRaidReward(session, 30);
                        }
                    }
                    break;

                case 4:
                    if (Type == 4)
                    {
                        if (session.Character.PrimalRaidQuestProgress < 100)
                        {
                            session.Character.PrimalRaidQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Finished Raids: {session.Character.PrimalRaidQuestProgress}/100");
                        }
                        if (session.Character.PrimalRaidQuestProgress >= 100)
                        {
                            AddRaidReward(session, 80);
                        }
                    }
                    break;

                case 5:
                    if (Type == 5)
                    {
                        if (session.Character.PrimalRaidQuestProgress < 5)
                        {
                            session.Character.PrimalRaidQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Finished Cuby Raids: {session.Character.PrimalRaidQuestProgress}/5");
                        }
                        if (session.Character.PrimalRaidQuestProgress >= 5)
                        {
                            AddRaidReward(session, 2);
                        }
                    }
                    break;

                case 6:
                    if (Type == 6)
                    {
                        if (session.Character.PrimalRaidQuestProgress < 5)
                        {
                            session.Character.PrimalRaidQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Finished Ibrahim Raids: {session.Character.PrimalRaidQuestProgress}/5");
                        }
                        if (session.Character.PrimalRaidQuestProgress >= 5)
                        {
                            AddRaidReward(session, 4);
                        }
                    }
                    break;

                case 7:
                    if (Type == 7)
                    {
                        if (session.Character.PrimalRaidQuestProgress < 5)
                        {
                            session.Character.PrimalRaidQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Finished Draco Raids: {session.Character.PrimalRaidQuestProgress}/5");
                        }
                        if (session.Character.PrimalRaidQuestProgress >= 5)
                        {
                            AddRaidReward(session, 4);
                        }
                    }
                    break;

                case 8:
                    if (Type == 8)
                    {
                        if (session.Character.PrimalRaidQuestProgress < 5)
                        {
                            session.Character.PrimalRaidQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Finished Glacerus Raids: {session.Character.PrimalRaidQuestProgress}/5");
                        }
                        if (session.Character.PrimalRaidQuestProgress >= 5)
                        {
                            AddRaidReward(session, 4);
                        }
                    }
                    break;

                case 9:
                    if (Type == 9)
                    {
                        if (session.Character.PrimalRaidQuestProgress < 5)
                        {
                            session.Character.PrimalRaidQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Finished Erenia Raids: {session.Character.PrimalRaidQuestProgress}/5");
                        }
                        if (session.Character.PrimalRaidQuestProgress >= 5)
                        {
                            AddRaidReward(session, 4);
                        }
                    }
                    break;

                case 10:
                    if (Type == 10)
                    {
                        if (session.Character.PrimalRaidQuestProgress < 5)
                        {
                            session.Character.PrimalRaidQuestProgress += 1;
                            session.SendPacket($"msg 3 [Primal Quest] Finished Zenas Raids: {session.Character.PrimalRaidQuestProgress}/5");
                        }
                        if (session.Character.PrimalRaidQuestProgress >= 5)
                        {
                            AddRaidReward(session, 4);
                        }
                    }
                    break;
            }
        }
    }
}