using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;

public class ReactionRolePlugin
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _messageId; // ID
    private readonly ulong _roleId;

    public ReactionRolePlugin(DiscordSocketClient client, ulong messageId, ulong roleId)
    {
        _client = client;
        _messageId = messageId;
        _roleId = roleId;
        _client.ReactionAdded += OnReactionAddedAsync;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("ReactionRolePlugin is running.");
        await Task.CompletedTask;
    }

    private async Task OnReactionAddedAsync(Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        if (message.Id != _messageId) return;

        var user = reaction.User.Value as SocketGuildUser;
        var role = user?.Guild.GetRole(_roleId);
        if (role != null)
        {
            await user.AddRoleAsync(role);
            await user.SendMessageAsync($"You've been given the {role.Name} role!");
        }
    }
}
