using System;
using System.Threading.Tasks;
using EmbedBuilderThread;

namespace NosGm.GameObject.Discord
{
    public static class Discord
    {
        private const string WebhookEnvironmentVariable = "NOSGM_DISCORD_WEBHOOK_URL";

        public static async Task SendEmbed(string Title, string FirstContext, string SecondContext, string Description)
        {
            string webhook = Environment.GetEnvironmentVariable(WebhookEnvironmentVariable);
            if (!IsValidDiscordWebhook(webhook))
            {
                return;
            }

            var discord = new WebhookThread.WebhookThread(webhook);
            var testEmbed = new EmbedBuilder()
                .WithTitle($"{Title}")
                .AddField($"{FirstContext}", $"{SecondContext}", false)
                .WithDescription($"{Description}")
                .WithColor(0, 0, 204)
                .WithTimestamp(DateTime.Now);

            await discord.SendMessageAsync(embed: testEmbed.Build()).ConfigureAwait(false);
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
