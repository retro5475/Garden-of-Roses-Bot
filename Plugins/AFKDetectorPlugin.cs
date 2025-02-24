using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;

public class AFKDetectorPlugin
{
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, Timer> _userTimers = new();

    public AFKDetectorPlugin(DiscordSocketClient client)
    {
        _client = client;
        _client.MessageReceived += OnMessageReceivedAsync;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("AFKDetectorPlugin is running.");
        await Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        if (_userTimers.TryGetValue(message.Author.Id, out var timer))
        {
            timer.Stop();
        }
        else
        {
            _userTimers[message.Author.Id] = new Timer(600000); // 10 minutes
            _userTimers[message.Author.Id].Elapsed += async (s, e) => await MarkUserAFK(message.Author);
            _userTimers[message.Author.Id].Start();
        }
    }

    private async Task MarkUserAFK(SocketUser user)
    {
        await user.SendMessageAsync("You have been marked as AFK due to inactivity.");
        _userTimers.TryRemove(user.Id, out _);
    }
}
