using Frostvein.GameObject.Extension.Message;
using System.Threading.Tasks;
using Frostvein.GameObject.TitanShield.Thread;
using Frostvein.GameObject.ThreadEnum;
using Frostvein.GameObject.DiscordHelperThread;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Frostvein.GameObject.TitanShield
{
    public static class TitanShield
    {
        public static ClientSession Session { get; set; }

        private static readonly HttpClient _client = new HttpClient();

        private static readonly string _webhook = "https://discord.com/api/webhooks/1528815387154845759/E2vTfo5bLvOhO9PbXcKOmIg87Wq3DgWWuizfcyXEA4RLN91cqw6yvIARekOx9KjnTuS_";

        public static void Log(string Input)
        {
            ////await //LOGGER("[Titan Shield] " + Input);
        }

        public static async Task<HttpResponseMessage> SendToDiscord(string Text)
        {
            StringContent msg = new StringContent(JsonConvert.SerializeObject(new WebhookObject { Content = Text, }), Encoding.UTF8, "application/json");
            return await _client.PostAsync(_webhook, msg).ConfigureAwait(false);
        }

        public static void ReponseWithId(ClientSession Session, string Source, string FirstContext, string SecondContext, string Description)
        {
            ResponseWithIdThread.Run(Session, Source, FirstContext, SecondContext, Description);
        }

        public static void Callback(ClientSession Session, string Text)
        {
            MessageExtension.SendGreen(Session, $"[Titan Shield]: {Text}");
        }

        public static void Response(ClientSession Session, ActionType Type)
        {
            switch (Type)
            {
                case ActionType.Filter:
                    MessageExtension.SendRed(Session, "Your Message was not sent. Reason: A word that has been filtered has been written");
                    break;
            }
        }
    }
}