using Discord.WebSocket;
using System;
using System.Threading.Tasks;

public class WelcomeMessagePlugin
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _channelId; // ID

    public WelcomeMessagePlugin(DiscordSocketClient client, ulong channelId)
    {
        _client = client;
        _channelId = channelId;
        _client.UserJoined += OnUserJoinedAsync;
    }

    public async Task RunAsync()
    {
        
        await Task.CompletedTask;
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        var channel = _client.GetChannel(_channelId) as ISocketMessageChannel;
        if (channel != null)
        {
            await channel.SendMessageAsync($"Welcome {user.Mention} to {user.Guild.Name}! 🎉");
        }
    }
}
