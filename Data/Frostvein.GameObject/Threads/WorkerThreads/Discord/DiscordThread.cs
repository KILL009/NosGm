using System;
using System.Threading.Tasks;
using EmbedBuilderThread;

namespace Frostvein.GameObject.Discord
{
    public static class Discord
    {
       
        public static async Task SendEmbed(string Title, string FirstContext, string SecondContext, string Description)
        {
            var discord = new WebhookThread.WebhookThread("https://discord.com/api/webhooks/1210307866867531857/XXdTmZGdcIJDwY-EHWmqs7eVAus2XYjAArNHYt8Udb-aPmt9WST0ep3u6615FpmtK1OS");
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
