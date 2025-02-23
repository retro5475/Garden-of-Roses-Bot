using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

public class PingResponderPlugin
{
    private readonly DiscordSocketClient _client;

    // Konstruktor: DiscordSocketClient wird übergeben
    public PingResponderPlugin(DiscordSocketClient client)
    {
        _client = client;

        // Event-Handler registrieren
        _client.MessageReceived += OnMessageReceivedAsync;
    }

    // Diese Methode wird vom Plugin-Loader aufgerufen
    public async Task RunAsync()
    {
        Console.WriteLine("PingResponderPlugin is running.");
        await Task.CompletedTask; // Keine zusätzliche Initialisierung nötig
    }

    // Event-Handler: Wird ausgelöst, wenn eine Nachricht empfangen wird
    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        // Stelle sicher, dass die Nachricht kein Bot ist
        if (message.Author.IsBot) return;

        // Überprüfe, ob der Bot in der Nachricht erwähnt wurde
        if (message.MentionedUsers.Any(user => user.Id == _client.CurrentUser.Id))
        {
            // Antwort
            await message.Channel.SendMessageAsync($"Hallo {message.Author.Mention}, bitte benutze /help, wenn du Hilfe benötigst.");
        }
    }
}
