using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Discord.Commands;
using Maina.Core;

namespace Maina.General.Commands
{
    [Name("General")]
    public class PingCommand : MainaBase
    {
        [Command("ping")]
        public async Task BaseCommand()
        {
            var netLatency = (await new Ping().SendPingAsync("8.8.8.8")).RoundtripTime;
            var gateLatency = Context.Client.Latency;

            EmbedColor color;
            //var latency = netLatency + gateLatency / 2f;
            var latency = Math.Max(netLatency, gateLatency);
            if (latency < 100)
                color = EmbedColor.Green;
            else if (latency < 250)
                color = EmbedColor.Yellow;
            else
                color = EmbedColor.Red;

            await ReplyAsync(string.Empty,
                CreateEmbed(color).WithAuthor("Maina Latency Information")
                    .AddField("Discord Gateway", $"{gateLatency} ms", true)
                    .AddField("Internet", $"{netLatency} ms", true).Build());
        }
    }
}