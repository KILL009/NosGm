using Frostvein.GameObject.Extension.Message;
using Frostvein.GameObject.Networking;
using System.Threading.Tasks;

namespace Frostvein.GameObject.Extension
{
    public static class InstanceExtension
    {
       public static void AddBattlePassPoint(ClientSession Session)
       {
            int chance = ServerManager.RandomNumber(0, 100);
            if (Session.Character.UnlockedBattlePassMultiplicator)
            {
                if (chance < 5)
                {
                    Session.Character.BattlePassPoints += 8;
                    MessageExtension.SendGreen(Session, "You received 8 Battle Pass Points");
                }
                else if (chance < 10)
                {
                    Session.Character.BattlePassPoints += 6;
                    MessageExtension.SendGreen(Session, "You received 6 Battle Pass Points");
                }
                else if (chance < 30)
                {
                    Session.Character.BattlePassPoints += 4;
                    MessageExtension.SendGreen(Session, "You received 4 Battle Pass Points");
                }
                else
                {
                    Session.Character.BattlePassPoints += 2;
                    MessageExtension.SendGreen(Session, "You received 2 Battle Pass Points");
                }
            }
            else
            {
                if (chance < 5)
                {
                    Session.Character.BattlePassPoints += 4;
                    MessageExtension.SendGreen(Session, "You received 4 Battle Pass Points");
                }
                else if (chance < 10)
                {
                    Session.Character.BattlePassPoints += 3;
                    MessageExtension.SendGreen(Session, "You received 3 Battle Pass Points");
                }
                else if (chance < 30)
                {
                    Session.Character.BattlePassPoints += 2;
                    MessageExtension.SendGreen(Session, "You received 2 Battle Pass Points");
                }
                else
                {
                    Session.Character.BattlePassPoints += 1;
                    MessageExtension.SendGreen(Session, "You received 1 Battle Pass Point");
                }
            }
       }
    }
}