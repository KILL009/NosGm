using NosGm.GameObject.Extension.Message;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NosGm.GameObject.TitanShield.Thread;
using NosGm.GameObject.ThreadEnum;
using NosGm.GameObject.DiscordHelperThread;
using Newtonsoft.Json;

namespace NosGm.GameObject.TitanShield
{
    public static class TitanShield
    {
        private const string TitanWebhookEnvironmentVariable = "NOSGM_TITANSHIELD_WEBHOOK_URL";
        private const string SharedWebhookEnvironmentVariable = "NOSGM_DISCORD_WEBHOOK_URL";

        public static ClientSession Session { get; set; }

        private static readonly HttpClient _client = new HttpClient();

        public static void Log(string Input)
        {
            ////await //LOGGER("[Titan Shield] " + Input);
        }

        public static async Task<HttpResponseMessage> SendToDiscord(string Text)
        {
            string webhook = ResolveWebhook();
            if (webhook == null)
            {
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }

            using (var msg = new StringContent(
                JsonConvert.SerializeObject(new WebhookObject { Content = Text }),
                Encoding.UTF8,
                "application/json"))
            {
                return await _client.PostAsync(webhook, msg).ConfigureAwait(false);
            }
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

        private static string ResolveWebhook()
        {
            string webhook = Environment.GetEnvironmentVariable(TitanWebhookEnvironmentVariable);
            if (!IsValidDiscordWebhook(webhook))
            {
                webhook = Environment.GetEnvironmentVariable(SharedWebhookEnvironmentVariable);
            }

            return IsValidDiscordWebhook(webhook) ? webhook : null;
        }

        private static bool IsValidDiscordWebhook(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool trustedHost = string.Equals(uri.Host, "discord.com", StringComparison.OrdinalIgnoreCase) ||
                               uri.Host.EndsWith(".discord.com", StringComparison.OrdinalIgnoreCase) ||
                               string.Equals(uri.Host, "discordapp.com", StringComparison.OrdinalIgnoreCase) ||
                               uri.Host.EndsWith(".discordapp.com", StringComparison.OrdinalIgnoreCase);

            return trustedHost &&
                   uri.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
