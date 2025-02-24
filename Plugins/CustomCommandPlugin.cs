using Discord.WebSocket;
using System.Threading.Tasks;

public class CustomCommandPlugin
{
    private readonly DiscordSocketClient _client;

    public CustomCommandPlugin(DiscordSocketClient client)
    {
        _client = client;
        _client.MessageReceived += OnMessageReceivedAsync;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("CustomCommandPlugin is running.");
        await Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        if (message.Content.ToLower() == "!hello")
        {
            await message.Channel.SendMessageAsync($"Hello, {message.Author.Mention}!");
        }
    }
}
