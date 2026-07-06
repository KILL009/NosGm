using System.Threading.Tasks;
using Frostvein.GameObject.Networking;

namespace Frostvein.GameObject.Service
{
    public static class LotteryService
    {
        public static async Task GenerateLotteryAsync(ClientSession Session)
        {
            if (Session.Character.Gold < 1000000)
            {
                Session.SendPacket("info You don't have enough Gold");
                return;
            }
            var rnd = ServerManager.RandomNumber(0, 100);
            Session.Character.Gold -= 1000000;
            Session.SendPacket(Session.Character.GenerateGold());
            if (rnd < 97)
            {
                Session.SendPacket("msg 4 Sadly, you did not won anything");
            }
            else
            {
                Session.SendPacket("msg 4 Congratulations! You won in the Instant Lottery!");
                Session.Character.Gold += 30000000;
                Session.SendPacket(Session.Character.GenerateGold());
            }
        }

        public static async Task GenerateLotteryInfoAsync(ClientSession Session)
        {
            Session.SendPacket("modal 1 " + 
                "[ Lottery System ]\n\n" + 
                "After spending 1.000.000 Gold when buying a Ticket, there is\n" +
                "a random Chance of winning the Grand Price\n" +
                "NOTE: The money you spent can not be given back\n" +
                "Play at your own risk.");
        }

        public static async Task GenerateLotteryChancesAsync(ClientSession Session)
        {
            Session.SendPacket("modal 1 " +
                "[ Lottery System ]\n\n" +
                "Grand Price: 30.000.000 Gold\n" +
                "Chance: 3%");
        }
    }
}