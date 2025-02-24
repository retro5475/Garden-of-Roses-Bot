using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class WordFilterPlugin
{
    private readonly DiscordSocketClient _client;
    private readonly HashSet<string> _bannedWords = new() { "badword1", "badword2" }; 

    public WordFilterPlugin(DiscordSocketClient client)
    {
        _client = client;
        _client.MessageReceived += OnMessageReceivedAsync;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("WordFilterPlugin is running.");
        await Task.CompletedTask;
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        if (_bannedWords.Any(word => message.Content.ToLower().Contains(word)))
        {
            await message.DeleteAsync();
            await message.Channel.SendMessageAsync($"{message.Author.Mention}, please refrain from using inappropriate language.");
        }
    }
}
