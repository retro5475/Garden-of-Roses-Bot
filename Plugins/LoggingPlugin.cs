using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

public class LoggingPlugin
{
    private readonly DiscordSocketClient _client;
    private readonly ulong _logChannelId; //ID

    public LoggingPlugin(DiscordSocketClient client, ulong logChannelId)
    {
        _client = client;
        _logChannelId = logChannelId;

        _client.UserJoined += OnUserJoinedAsync;
        _client.UserLeft += OnUserLeftAsync;
        _client.MessageDeleted += OnMessageDeletedAsync;
        _client.MessageUpdated += OnMessageEditedAsync;
        _client.GuildMemberUpdated += OnUserUpdatedAsync;
        _client.ChannelUpdated += OnChannelUpdatedAsync;
        _client.UserBanned += OnUserBannedAsync;
        _client.UserUnbanned += OnUserUnbannedAsync;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("LoggingPlugin is running.");
        await Task.CompletedTask;
    }

    private async Task SendLogAsync(Embed embed)
    {
        var channel = _client.GetChannel(_logChannelId) as ISocketMessageChannel;
        if (channel != null)
        {
            await channel.SendMessageAsync(embed: embed);
        }
    }

    private async Task OnUserJoinedAsync(SocketGuildUser user)
    {
        var embed = new EmbedBuilder()
            .WithTitle("User Joined")
            .WithDescription($"{user.Mention} ({user.Username}#{user.Discriminator}) joined the server.")
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithColor(Color.Green)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnUserLeftAsync(SocketGuild guild, SocketUser user)
    {
        var embed = new EmbedBuilder()
            .WithTitle("User Left")
            .WithDescription($"{user.Username}#{user.Discriminator} left or was kicked.")
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithColor(Color.Red)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnMessageDeletedAsync(Cacheable<IMessage, ulong> cache, Cacheable<IMessageChannel, ulong> channel)
    {
        var message = await cache.GetOrDownloadAsync();
        if (message == null || message.Author.IsBot) return;

        var embed = new EmbedBuilder()
            .WithTitle("Message Deleted")
            .WithDescription($"Message from {message.Author.Mention} in {channel.Value} was deleted.")
            .AddField("Content", message.Content ?? "*No content*")
            .WithColor(Color.DarkRed)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnMessageEditedAsync(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
    {
        var oldMessage = await before.GetOrDownloadAsync();
        if (oldMessage == null || oldMessage.Author.IsBot || oldMessage.Content == after.Content) return;

        var embed = new EmbedBuilder()
            .WithTitle("Message Edited")
            .WithDescription($"Message from {oldMessage.Author.Mention} in {channel} was edited.")
            .AddField("Before", oldMessage.Content ?? "*No content*")
            .AddField("After", after.Content ?? "*No content*")
            .WithColor(Color.Orange)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnUserUpdatedAsync(SocketUser before, SocketUser after)
    {
        if (before.Username != after.Username || before.Discriminator != after.Discriminator)
        {
            var embed = new EmbedBuilder()
                .WithTitle("Username Changed")
                .WithDescription($"{before.Username}#{before.Discriminator} → {after.Username}#{after.Discriminator}")
                .WithColor(Color.Blue)
                .WithTimestamp(DateTimeOffset.Now)
                .Build();

            await SendLogAsync(embed);
        }
    }

    private async Task OnChannelUpdatedAsync(SocketChannel before, SocketChannel after)
    {
        if (before is not SocketGuildChannel oldChannel || after is not SocketGuildChannel newChannel) return;
        
        var embed = new EmbedBuilder()
            .WithTitle("Channel Updated")
            .WithDescription($"**{oldChannel.Name}** → **{newChannel.Name}**")
            .WithColor(Color.Purple)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnUserBannedAsync(SocketUser user, SocketGuild guild)
    {
        var embed = new EmbedBuilder()
            .WithTitle("User Banned")
            .WithDescription($"{user.Username}#{user.Discriminator} was **banned** from {guild.Name}.")
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithColor(Color.DarkRed)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await SendLogAsync(embed);
    }

    private async Task OnUserUnbannedAsync(SocketUser user, SocketGuild guild)
    {
        var embed = new EmbedBuilder()
            .WithTitle("User Unbanned")
            .WithDescription($"{user.Username}#{user.Discriminator} was **unbanned** in {guild.Name}.")
            .WithThumbnailUrl(user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl())
            .WithColor(Color.Green)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();

        await SendLogAsync(embed);
    }
}
