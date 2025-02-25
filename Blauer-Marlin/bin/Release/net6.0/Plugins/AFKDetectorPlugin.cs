using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;
using System.Text.Json;
using Newtonsoft.Json;

public class AFKDetectorPlugin
{
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, Timer> _userTimers = new();

    public AFKDetectorPlugin(DiscordSocketClient client)
    {
        _client = client;
        _client.MessageReceived += OnMessageReceivedAsync;
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        if (_userTimers.TryGetValue(message.Author.Id, out var timer))
        {
            timer.Stop();
            timer.Start();
        }
        else
        {
            var newTimer = new Timer(600000); // 10 minutes
            newTimer.Elapsed += async (s, e) => await MarkUserAFK(message.Author);
            newTimer.AutoReset = false;
            newTimer.Start();
            _userTimers[message.Author.Id] = newTimer;
        }
    }

    private async Task MarkUserAFK(SocketUser user)
    {
        if (_userTimers.TryRemove(user.Id, out var timer))
        {
            timer.Stop();
            timer.Dispose();
        }

        try
        {
            var dmChannel = await user.GetOrCreateDMChannelAsync();
            if (dmChannel != null)
            {
                await dmChannel.SendMessageAsync("You have been marked as AFK due to inactivity.");
            }
        }
        catch (Exception ex)
        {

        }
    }
}
