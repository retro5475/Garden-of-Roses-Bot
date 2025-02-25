using Discord.WebSocket;
using System;
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
       
        await Task.Yield(); // Ensures proper async execution
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot || message.Content == null) return;

        string loweredContent = message.Content.ToLower();
        if (_bannedWords.Any(word => loweredContent.Contains(word)))
        {
            await message.DeleteAsync();
            await message.Channel.SendMessageAsync($"{message.Author.Mention}, please refrain from using inappropriate language.");
        }
    }
}
