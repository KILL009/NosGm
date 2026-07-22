using NosGm.Domain;

namespace NosGm.GameObject.Extension
{
    public static class LevelRewardExtension
    {
        public static void LevelRewards(ClientSession Session, int Level)
        {
            switch (Level)
            {
                case 30:
                    switch (Session.Character.Class)
                    {
                        case ClassType.Swordsman:
                            Session.Character.GiftAdd(136, 1, 5, 7);
                            Session.Character.GiftAdd(73, 1, 5, 7);
                            Session.Character.GiftAdd(98, 1, 5, 7);
                            break;
                        case ClassType.Archer:
                            Session.Character.GiftAdd(143, 1, 5, 7);
                            Session.Character.GiftAdd(81, 1, 5, 7);
                            Session.Character.GiftAdd(111, 1, 5, 7);
                            break;
                        case ClassType.Magician:
                            Session.Character.GiftAdd(150, 1, 5, 7);
                            Session.Character.GiftAdd(89, 1, 5, 7);
                            Session.Character.GiftAdd(124, 1, 5, 7);
                            break;
                        case ClassType.MartialArtist:
                            break;

                    }
                    Session.Character.GiftAdd(9325, 1);
                    Session.Character.GiftAdd(9074, 1);
                    Session.Character.GiftAdd(9041, 1);
                    Session.Character.GiftAdd(1010, 99);
                    Session.Character.Reputation += 1000;
                    break;

                case 40:
                    switch (Session.Character.Class)
                    {
                        case ClassType.Adventurer:
                            break;
                        case ClassType.Swordsman:
                            Session.Character.GiftAdd(262, 1, 5, 7);
                            Session.Character.GiftAdd(291, 1, 5, 7);
                            Session.Character.GiftAdd(165, 1, 5, 7);
                            break;
                        case ClassType.Archer:
                            Session.Character.GiftAdd(265, 1, 5, 7);
                            Session.Character.GiftAdd(289, 1, 5, 7);
                            Session.Character.GiftAdd(171, 1, 5, 7);
                            break;
                        case ClassType.Magician:
                            Session.Character.GiftAdd(268, 1, 5, 7);
                            Session.Character.GiftAdd(293, 1, 5, 7);
                            Session.Character.GiftAdd(177, 1, 5, 7);
                            break;
                        case ClassType.MartialArtist:
                            break;
                    }
                    Session.Character.GiftAdd(9074, 1);
                    Session.Character.GiftAdd(9041, 1);
                    Session.Character.GiftAdd(4989, 1, 5, 5);
                    Session.Character.GiftAdd(4998, 1, 5, 5);
                    Session.Character.GiftAdd(4870, 1, 5, 5);
                    Session.Character.GiftAdd(4997, 1, 5, 5);
                    Session.Character.GiftAdd(4834, 1, 5, 5);
                    Session.Character.GiftAdd(4996, 1, 5, 5);
                    Session.Character.GiftAdd(4833, 1, 5, 5);
                    Session.Character.GiftAdd(4995, 1, 5, 5);
                    Session.Character.GiftAdd(1010, 99);
                    Session.Character.Reputation += 1000;
                    break;

                case 50:
                    switch (Session.Character.Class)
                    {
                        case ClassType.Adventurer:
                            break;
                        case ClassType.Swordsman:
                            Session.Character.GiftAdd(140, 1, 5, 7);
                            Session.Character.GiftAdd(76, 1, 5, 7);
                            Session.Character.GiftAdd(297, 1, 5, 7);
                            Session.Character.GiftAdd(9316, 1);
                            break;
                        case ClassType.Archer:
                            Session.Character.GiftAdd(147, 1, 5, 7);
                            Session.Character.GiftAdd(84, 1, 5, 7);
                            Session.Character.GiftAdd(295, 1, 5, 7);
                            Session.Character.GiftAdd(9313, 1);
                            break;
                        case ClassType.Magician:
                            Session.Character.GiftAdd(154, 1, 5, 7);
                            Session.Character.GiftAdd(92, 1, 5, 7);
                            Session.Character.GiftAdd(271, 1, 5, 7);
                            Session.Character.GiftAdd(9310, 1);
                            break;
                        case ClassType.MartialArtist:
                            break;
                    }
                    Session.Character.GiftAdd(9074, 1);
                    Session.Character.GiftAdd(9041, 1);
                    Session.Character.GiftAdd(1010, 99);
                    Session.Character.Reputation += 1000;
                    break;

                case 60:
                    switch (Session.Character.Class)
                    {
                        case ClassType.Adventurer:
                            break;
                        case ClassType.Swordsman:
                            Session.Character.GiftAdd(141, 1, 5, 7);
                            Session.Character.GiftAdd(77, 1, 5, 7);
                            Session.Character.GiftAdd(106, 1, 5, 7);
                            break;
                        case ClassType.Archer:
                            Session.Character.GiftAdd(148, 1, 5, 7);
                            Session.Character.GiftAdd(762, 1, 5, 7);
                            Session.Character.GiftAdd(119, 1, 5, 7);
                            break;
                        case ClassType.Magician:
                            Session.Character.GiftAdd(155, 1, 5, 7);
                            Session.Character.GiftAdd(764, 1, 5, 7);
                            Session.Character.GiftAdd(132, 1, 5, 7);
                            break;
                        case ClassType.MartialArtist:
                            break;
                    }
                    Session.Character.GiftAdd(9074, 1);
                    Session.Character.GiftAdd(9041, 1);
                    Session.Character.Reputation += 20000;
                    break;

                case 70:
                    switch (Session.Character.Class)
                    {
                        case ClassType.Adventurer:
                            break;
                        case ClassType.Swordsman:
                            Session.Character.GiftAdd(400, 1, 6, 7);
                            Session.Character.GiftAdd(761, 1, 6, 7);
                            Session.Character.GiftAdd(994, 1, 6, 7);
                            break;
                        case ClassType.Archer:
                            Session.Character.GiftAdd(403, 1, 6, 7);
                            Session.Character.GiftAdd(405, 1, 6, 7);
                            Session.Character.GiftAdd(993, 1, 6, 7);
                            break;
                        case ClassType.Magician:
                            Session.Character.GiftAdd(406, 1, 6, 7);
                            Session.Character.GiftAdd(765, 1, 6, 7);
                            Session.Character.GiftAdd(989, 1, 6, 7);
                            break;
                        case ClassType.MartialArtist:
                            break;
                    }
                    Session.Character.GiftAdd(9074, 1);
                    Session.Character.GiftAdd(9041, 1);
                    Session.Character.GiftAdd(8282, 1);
                    Session.Character.GiftAdd(8283, 1);
                    Session.Character.GiftAdd(8291, 1);
                    Session.Character.GiftAdd(4039, 1, 6, 6);
                    Session.Character.GiftAdd(4044, 1, 6, 6);

                    Session.Character.Reputation += 5000;
                    break;

                case 80:
                    switch (Session.Character.Class)
                    {
                        case ClassType.Adventurer:
                            break;
                        case ClassType.Swordsman:
                            Session.Character.GiftAdd(401, 1, 6, 7);
                            Session.Character.GiftAdd(4006, 1, 6, 7);
                            Session.Character.GiftAdd(409, 1, 6, 7);
                            Session.Character.GiftAdd(418, 1);
                            Session.Character.GiftAdd(421, 1);
                            Session.Character.GiftAdd(424, 1);
                            break;
                        case ClassType.Archer:
                            Session.Character.GiftAdd(404, 1, 6, 7);
                            Session.Character.GiftAdd(4008, 1, 6, 7);
                            Session.Character.GiftAdd(410, 1, 6, 7);
                            Session.Character.GiftAdd(418, 1);
                            Session.Character.GiftAdd(421, 1);
                            Session.Character.GiftAdd(424, 1);
                            break;
                        case ClassType.Magician:
                            Session.Character.GiftAdd(407, 1, 6, 7);
                            Session.Character.GiftAdd(4010, 1, 6, 7);
                            Session.Character.GiftAdd(411, 1, 6, 7);
                            Session.Character.GiftAdd(418, 1);
                            Session.Character.GiftAdd(421, 1);
                            Session.Character.GiftAdd(424, 1);
                            break;
                        case ClassType.MartialArtist:
                            break;
                    }
                    Session.Character.GiftAdd(9074, 1);
                    Session.Character.GiftAdd(9041, 1);
                    Session.Character.GiftAdd(4503, 1);
                    Session.Character.GiftAdd(4504, 1);
                    Session.Character.Reputation += 5000;
                    break;

                case 85:
                    switch (Session.Character.Class)
                    {
                        case ClassType.Adventurer:
                            break;
                        case ClassType.Swordsman:
                            Session.Character.GiftAdd(9317, 1);
                            Session.Character.GiftAdd(4001, 1, 6, 8);
                            Session.Character.GiftAdd(4007, 1, 6, 8);
                            Session.Character.GiftAdd(4013, 1, 6, 8);
                            break;
                        case ClassType.Archer:
                            Session.Character.GiftAdd(9314, 1);
                            Session.Character.GiftAdd(4003, 1, 6, 8);
                            Session.Character.GiftAdd(4009, 1, 6, 8);
                            Session.Character.GiftAdd(4016, 1, 6, 8);
                            break;
                        case ClassType.Magician:
                            Session.Character.GiftAdd(9312, 1);
                            Session.Character.GiftAdd(4005, 1, 6, 8);
                            Session.Character.GiftAdd(4011, 1, 6, 8);
                            Session.Character.GiftAdd(4019, 1, 6, 8);
                            break;
                        case ClassType.MartialArtist:
                            Session.Character.GiftAdd(9320, 1);
                            break;
                    }
                    Session.Character.GiftAdd(9074, 1);
                    Session.Character.GiftAdd(9041, 1);
                    Session.Character.GiftAdd(8009, 1);
                    Session.Character.GiftAdd(8010, 1);
                    Session.Character.GiftAdd(8011, 1);
                    Session.Character.GiftAdd(8012, 1);
                    Session.Character.Reputation += 50000;
                    break;

                case 90:
                    switch (Session.Character.Class)
                    {
                        case ClassType.Adventurer:
                            break;
                        case ClassType.Swordsman:
                            Session.Character.GiftAdd(4901, 1, 6, 8);
                            Session.Character.GiftAdd(4910, 1, 6, 8);
                            Session.Character.GiftAdd(4919, 1, 6, 8);
                            break;
                        case ClassType.Archer:
                            Session.Character.GiftAdd(4904, 1, 6, 8);
                            Session.Character.GiftAdd(4913, 1, 6, 8);
                            Session.Character.GiftAdd(4922, 1, 6, 8);
                            break;
                        case ClassType.Magician:
                            Session.Character.GiftAdd(4907, 1, 6, 8);
                            Session.Character.GiftAdd(4916, 1, 6, 8);
                            Session.Character.GiftAdd(4925, 1, 6, 8);
                            break;

                        case ClassType.MartialArtist:
                            Session.Character.GiftAdd(9320, 1);
                            break;
                    }
                    break;
            }
        }
    }
}