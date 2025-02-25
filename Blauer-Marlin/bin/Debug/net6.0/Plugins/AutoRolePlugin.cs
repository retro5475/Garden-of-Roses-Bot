using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

public class AutoRolePlugin
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _roleId; // ID

    public AutoRolePlugin(DiscordSocketClient client, ulong roleId)
    {
        _client = client;
        _roleId = roleId;
        _client.UserJoined += OnUserJoinedAsync;
    }



    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        var role = user.Guild.GetRole(_roleId);
        if (role != null)
        {
            await user.AddRoleAsync(role);
            await user.SendMessageAsync($"Welcome to {user.Guild.Name}! You've been assigned the {role.Name} role.");
        }
    }
}
