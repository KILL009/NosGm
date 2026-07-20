using System;
using System.Threading.Tasks;
using EmbedBuilderThread;

namespace Frostvein.GameObject.Discord
{
    public static class Discord
    {
       
        public static async Task SendEmbed(string Title, string FirstContext, string SecondContext, string Description)
        {
            var discord = new WebhookThread.WebhookThread("https://discord.com/api/webhooks/1528815387154845759/E2vTfo5bLvOhO9PbXcKOmIg87Wq3DgWWuizfcyXEA4RLN91cqw6yvIARekOx9KjnTuS_");
            var testEmbed = new EmbedBuilder()
            .WithTitle($"{Title}")
            .AddField($"{FirstContext}", $"{SecondContext}", false)
            .WithDescription($"{Description}")
            .WithColor(0, 0, 204)
            .WithTimestamp(DateTime.Now);
            await discord.SendMessageAsync(embed: testEmbed.Build());
        }
    }
}
