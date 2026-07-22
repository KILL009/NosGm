
using NosGm.GameObject.Extension.Message;
using NosGm.GameObject.Networking;
using System;
using System.Threading.Tasks;

namespace NosGm.GameObject.TitanShield.Thread
{
    public static class ResponseWithIdThread
    {
        public static void Run(ClientSession Session, string Source, string FirstContext, string SecondContext, string Description)
        {
            try
            {
                int RandomNumber = ServerManager.RandomNumber(100000, 999999);
                string Spacer = "\n\n";

                MessageExtension.SendModal(Session, $"TITAN SHIELD" +
                    $"{Spacer}Oh! Something went wrong with {Source}" +
                    $"Please contact us immediately to report this issue using your Titan Shield ID!" +
                    $"{Spacer}" +
                    $"Titan Shield ID: {RandomNumber}");

                Task.Run(() => Discord.Discord.SendEmbed("TITAN SHIELD", FirstContext, SecondContext, $"Titan Shield ID {RandomNumber}"));
            }
            catch (Exception ex)
            {
                MessageExtension.SendInfo(Session, "info Something went wrong while sending a Response");
                //await //LOGGER(ex.ToString());
            }
            
        }
    }
}
