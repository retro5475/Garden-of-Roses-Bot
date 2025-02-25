using Discord;
using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;

public class LoggingPlugin
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _logChannelId = 1318285896029573153; // ✅ Hardcoded log channel ID

    public LoggingPlugin(DiscordSocketClient client)
    {
        _client = client;

        _client.UserJoined += OnUserJoinedAsync;
        _client.UserLeft += OnUserLeftAsync;
        _client.MessageDeleted += OnMessageDeletedAsync;
        _client.MessageUpdated += OnMessageEditedAsync;
        _client.GuildMemberUpdated += OnUserUpdatedAsync;
        _client.ChannelUpdated += OnChannelUpdatedAsync;
        _client.UserBanned += OnUserBannedAsync;
        _client.UserUnbanned += OnUserUnbannedAsync;
    }

    private async Task SendLogAsync(Embed embed)
    {
        if (_client.GetChannel(_logChannelId) is IMessageChannel channel)
        {
            await channel.SendMessageAsync(embed: embed);
        }
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        var embed = new EmbedBuilder()
            .WithTitle("User Joined")
            .WithDescription($"{user.Mention} ({user.Username}) joined the server.")
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithColor(Color.Green)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        var embed = new EmbedBuilder()
            .WithTitle("User Left")
            .WithDescription($"{user.Username} left or was kicked.")
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithColor(Color.Red)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnMessageDeletedAsync(Cacheable<IMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel)
    {
        var message = await cache.GetOrDownloadAsync();
        if (message == null || message.Author.IsBot) return;

        var embed = new EmbedBuilder()
            .WithTitle("Message Deleted")
            .WithDescription($"Message from {message.Author.Mention} deleted in {channel.Value}.")
            .WithColor(Color.DarkRed)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnMessageEditedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        var oldMessage = await before.GetOrDownloadAsync();
        if (oldMessage == null || oldMessage.Author.IsBot || oldMessage.Content == after.Content) return;

        var embed = new EmbedBuilder()
            .WithTitle("Message Edited")
            .WithDescription($"Message edited in {channel}.")
            .AddField("Before", oldMessage.Content ?? "*No content*")
            .AddField("After", after.Content ?? "*No content*")
            .WithColor(Color.Orange)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnUserUpdatedAsync(Cacheable<SocketGuildUser, ulong> beforeCache, SocketGuildUser after)
    {
        var before = await beforeCache.GetOrDownloadAsync();
        if (before == null || after == null) return;

        if (before.Nickname != after.Nickname || !before.Roles.SequenceEqual(after.Roles))
        {
            var embed = new EmbedBuilder()
                .WithTitle("User Updated")
                .WithDescription($"{after.Username} updated their profile.")
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.UtcNow)
                .Build();

            await SendLogAsync(embed);
        }
    }

    private async Task OnChannelUpdatedAsync(SocketChannel before, SocketChannel after)
    {
        if (before is not SocketGuildChannel oldChannel || after is not SocketGuildChannel newChannel) return;

        var embed = new EmbedBuilder()
            .WithTitle("Channel Updated")
            .WithDescription($"{oldChannel.Name} → {newChannel.Name}")
            .WithColor(Color.Purple)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnUserBannedAsync(SocketUser user, SocketGuild guild)
    {
        var embed = new EmbedBuilder()
            .WithTitle("User Banned")
            .WithDescription($"{user.Username} was banned from {guild.Name}.")
            .WithColor(Color.DarkRed)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnUserUnbannedAsync(SocketUser user, SocketGuild guild)
    {
        var embed = new EmbedBuilder()
            .WithTitle("User Unbanned")
            .WithDescription($"{user.Username} was unbanned in {guild.Name}.")
            .WithColor(Color.Green)
            .WithTimestamp(DateTimeOffset.UtcNow)
            .Build();

        await SendLogAsync(embed);
    }
}
