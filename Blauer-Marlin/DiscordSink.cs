using Serilog.Core;
using Serilog.Events;
using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

public class DiscordSink : ILogEventSink
{
    private readonly IFormatProvider _formatProvider;
    private readonly DiscordSocketClient _client;
    private readonly ulong _logChannelId;

    public DiscordSink(IFormatProvider formatProvider, DiscordSocketClient client, ulong logChannelId)
    {
        _formatProvider = formatProvider;
        _client = client;
        _logChannelId = logChannelId;
    }

    public void Emit(LogEvent logEvent)
    {
        if (_client == null || _logChannelId == 0) return;

        var channel = _client.GetChannel(_logChannelId) as IMessageChannel;
        if (channel == null) return;

        var message = logEvent.RenderMessage(_formatProvider);

        // Map log level to Discord embed color
        var color = logEvent.Level switch
        {
            LogEventLevel.Debug => Color.LightGrey,
            LogEventLevel.Information => Color.Blue,
            LogEventLevel.Warning => Color.Orange,
            LogEventLevel.Error => Color.Red,
            LogEventLevel.Fatal => Color.DarkRed,
            _ => Color.Default
        };

        // Build embed with timestamp and level info
        var embed = new EmbedBuilder()
            .WithTitle($"📝 **{logEvent.Level}**")
            .WithDescription(message)
            .WithColor(color)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        // Send to Discord channel (in a fire-and-forget manner)
        Task.Run(async () => await channel.SendMessageAsync(embed: embed));
    }
}
