using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Serilog;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Discord.Rest;

//TODO Move Command Shit to own CLasses

class Program
{

    private static DiscordSocketClient _client;
    private static SocketTextChannel _channel;
    private static CommandService _commands;
    private static IServiceProvider _services;
    private static System.Timers.Timer _timer;
    private static IUserMessage _currentMessage;
    private static Dictionary<string, bool> _regionPingStatus;

    private static readonly string ConfigFilePath = "files/regionPingStatus.json";
    

  static async Task Main(string[] args)
{
    _client = new DiscordSocketClient();
    _commands = new CommandService();
    CheckAndCreateDirectories();
   
    var buildDate = GetBuildDate(Assembly.GetExecutingAssembly());
    var author = "Ambiente + Retro";

    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
            theme: Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme.Code)  // Apply the cool ANSI theme
        .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
        .Enrich.WithProperty("Application", "Blauer-Marlin-Bot")
        .CreateLogger();

    try
    {
        Log.Information("Bot starting...");
        Log.Information($"Build Date: {buildDate}");
        Log.Information($"Author: {author}");

        await StartBotAsync();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "An unhandled exception occurred during startup.");
    }
    finally
    {
        Log.CloseAndFlush();
    }
}

private static DateTime GetBuildDate(Assembly assembly)
{
    var filePath = assembly.Location;
    var fileInfo = new FileInfo(filePath);
    return fileInfo.LastWriteTime;  
}

private static async Task<ulong> GetLogChannelForGuild(ulong guildId)
{
    // 🔹 Replace with actual logic to fetch log channels dynamically
    return 1318285896029573153; // Replace with an actual log channel ID
}


private static async Task StartBotAsync()
{
    try
    {
        Log.Information("Starting the bot...");

        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged
        });

         _commands = new CommandService();

        _client.Log += logMessage =>
        {
            Log.Information($"Discord Log: {logMessage}");
            return Task.CompletedTask;
        };

        _client.Ready += ReadyAsync;
        _client.SlashCommandExecuted += HandleSlashCommandAsync;

        await _client.SetStatusAsync(UserStatus.Online);
        await _client.SetGameAsync("N/A");
       
        var token = "MTMzMzQ3MDI1NTk4ODg3MTE5Mg.GRxw_Y.ENzQs6oOadCFI1yqOaBVzg2gyLXoQ2fdyThVKQ"; // Bot token here
        await _client.LoginAsync(TokenType.Bot, token);
        await PluginLoader.LoadAndExecutePluginsForAllGuildsAsync(_client, async (guildId) =>
        {
            return await GetLogChannelForGuild(guildId);
        });

        await _client.StartAsync();

        _timer = new System.Timers.Timer(60000);

        _timer.Elapsed += async (sender, e) =>
        {
            try
            {
                foreach (var guild in _client.Guilds)
                {
                    ulong guildId = guild.Id;
                    var channelId = await ChannelConfigManager.LoadChannelIdForGuildAsync(guildId);

                    if (channelId != null)
                    {
                        await ServerPingManager.PingServersAsync(_client, guildId, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in server ping timer.");
            }
        };

        _timer.Start();

        Log.Information("Bot started. Waiting for commands...");
       await Task.Delay(Timeout.Infinite); 
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error occurred during bot startup.");
    }
}

private static async Task ReadyAsync()
{
    try
    {
        Log.Information("Bot is ready!");
        LoadRegionPingStatus();

        StatusManager.StartRotatingStatus(_client);

        Log.Information("Registering commands...");
       await commands.RegisterSlashCommands(_client);


        ulong guildId = _client.Guilds.FirstOrDefault()?.Id ?? 0;
        if (guildId == 0)
        {
            Log.Error("No guild found. The bot must be in a server.");
            return;
        }

        var channel = await ChannelConfigManager.GetSavedChannelAsync(_client, guildId);
        if (channel == null)
        {
            Log.Error($"No saved channel found for Guild {guildId}. Please set the channel using /setchannel.");
            return;
        }

        _channel = channel;
        _currentMessage = await _channel.SendMessageAsync(embed: CreateEmbed("Initializing server status..."));

        Log.Information("Ready event handled.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error processing ReadyAsync.");
    }
}



    private static Embed CreateEmbed(string description, Color? color = null)
    {
        Log.Information("Creating embed with description: {Description}", description);
        return new EmbedBuilder()
            .WithTitle("FFXIV Server Status")
            .WithDescription(description)
            .WithColor(color ?? Color.Blue)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();
    }

    

private static void CheckAndCreateDirectories()
{
    string directoryPath = Path.GetDirectoryName(ConfigFilePath);
    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
    {
        Directory.CreateDirectory(directoryPath);
        Log.Information($"Created missing directory: {directoryPath}");
    }
}

    private static async Task HandleSlashCommandAsync(SocketSlashCommand command)
    {
        try
        {
            Log.Information("Handling slash command: {CommandName}", command.CommandName);

            switch (command.CommandName)
            {
case "help":
    Log.Information("User requested help.");
    await command.RespondAsync(embed: CreateEmbed("Available commands:\n\n" +
        "`/europe` - Toggles Europe server ping.\n" +
        "`/usa` - Toggles North America server ping.\n" +
        "`/japan` - Toggles Japan server ping.\n" +
        "`/status` - Displays the current ping status.\n" +
        "`/reload` - Reloads server status.\n" +
        "`/ban` - Bans a user.\n" +
        "`/unban` - Unbans a user.\n" +
        "`/kick` - Kicks a user.\n" +
        "`/mute` - Mutes a user.\n" +
        "`/unmute` - Unmutes a user.\n" +
        "`/warn` - Warns a user and sends a DM with details.\n" +
        "`/clearwarns` - Clears warnings for a user.\n" +
        "`/userinfo` - Displays user information.\n" +
        "`/setnickname` - Sets a user's nickname.\n" +
        "`/lockdown` - Locks the channel temporarily.\n" +
        "`/unlock` - Unlocks the channel.\n" +
        "`/addrole` - Adds a role to a user.\n" +
        "`/removerole` - Removes a role from a user.\n" +
        "`/slowmode` - Sets a slow mode on a channel.\n" +
        "`/announce` - Sends an announcement to a chosen channel.\n" +
        "`/setprefix` - Sets a custom prefix.\n" +
        "`/clear` - Clears messages in a channel.\n", Color.Green));
    break;


case "status":
    Log.Information("Status command executed.");
    string statusMessage = "Current Region Status:\n\n";
    
    foreach (var region in RegionData.Regions)
    {
        var isActive = _regionPingStatus.GetValueOrDefault(region.Name.ToLower(), false);
        statusMessage += $"{region.Name}: {(isActive ? "Active" : "Inactive")}\n";
    }

    await command.RespondAsync(embed: CreateEmbed(statusMessage, Color.Blue));
    break;
case "reload":
    Log.Information("Reloading server status...");
    await ServerPingManager.PingServersAsync(_client, command.GuildId.Value, true);
    await command.RespondAsync("Server status reloaded. The old embed has been deleted, and a new one has been sent.", ephemeral: true);
    break;

case "europe":
case "usa":
case "japan":
    Log.Information($"{command.CommandName} command executed.");
    var regionName = command.CommandName;
    if (_regionPingStatus.ContainsKey(regionName))
    {
        _regionPingStatus[regionName] = !_regionPingStatus[regionName];
        SaveRegionPingStatus();
        var status = _regionPingStatus[regionName] ? "enabled" : "disabled";
        await command.RespondAsync(embed: CreateEmbed($"Ping for {regionName} {status}.", 
            _regionPingStatus[regionName] ? Color.Green : Color.Red), ephemeral: true);
    }
    break;


             case "shutdown":
    Log.Information("Shutting down the bot...");
    await command.RespondAsync("Shutting down...", ephemeral: true);
    Environment.Exit(0);
    break;

case "ban":
    if (!(command.User as SocketGuildUser)?.GuildPermissions.BanMembers ?? false)
    {
        await command.RespondAsync("You do not have permission to use this command.", ephemeral: true);
        break;
    }

    var banUser = (SocketUser)command.Data.Options.First().Value;
    var banReason = command.Data.Options.Count > 1 ? command.Data.Options.ElementAt(1).Value.ToString() : "No reason provided";
    
    await command.RespondAsync($"User {banUser.Username} has been banned for: {banReason}", ephemeral: true);
    break;


case "kick":
    if (!(command.User as SocketGuildUser)?.GuildPermissions.BanMembers ?? false)
    {
        await command.RespondAsync("You do not have permission to use this command.", ephemeral: true);
        break;
    }
    var kickUser = (SocketUser)command.Data.Options.First().Value;
    await KickUserAsync(kickUser);
    await command.RespondAsync($"User {kickUser.Username} has been kicked.", ephemeral: true);
    break;

case "mute":
    var muteUser = (SocketUser)command.Data.Options.First().Value;
    await MuteUserAsync(muteUser);
    await command.RespondAsync($"User {muteUser.Username} has been muted.", ephemeral: true);
    break;

case "unmute":
    var unmuteUser = (SocketUser)command.Data.Options.First().Value;
    await UnmuteUserAsync(unmuteUser);
    await command.RespondAsync($"User {unmuteUser.Username} has been unmuted.", ephemeral: true);
    break;

 case "warn":
    var warnUser = (SocketUser)command.Data.Options.First(o => o.Name == "user").Value;
    var warnReason = command.Data.Options.Count > 1 ? command.Data.Options.ElementAt(1).Value.ToString() : "No reason provided";

    // Fetch the user who issued the warning
    var issuer = command.User;

    // Send DM to the warned user
    try
    {
        var warnDmChannel = await warnUser.CreateDMChannelAsync();
        await warnDmChannel.SendMessageAsync($"⚠ **You have been warned!** ⚠\n\n" +
                                             $"**Reason:** {warnReason}\n" +
                                             $"If you believe this is a mistake, contact a moderator.");
    }
    catch (Exception ex)
    {
        Log.Warning($"Could not send DM to {warnUser.Username}: {ex.Message}");
    }

    // Send a copy of the warning to the issuer
    try
    {
        var issuerDmChannel = await issuer.CreateDMChannelAsync();
        var warneduserInfo = $"**Username:** {warnUser.Username}#{warnUser.Discriminator}\n" +
                       $"**User ID:** {warnUser.Id}\n" +
                       $"**Account Created:** {warnUser.CreatedAt.UtcDateTime} UTC\n";

        await issuerDmChannel.SendMessageAsync($"✅ **Warning Issued Successfully**\n\n" +
                                               $"You have warned {warnUser.Mention}.\n\n" +
                                               $"{warneduserInfo}\n" +
                                               $"**Reason:** {warnReason}");
    }
    catch (Exception ex)
    {
        Log.Warning($"Could not send DM to {issuer.Username}: {ex.Message}");
    }

    // Acknowledge the command execution
    await command.RespondAsync($"User {warnUser.Username} has been warned for: {warnReason}", ephemeral: true);
    break;


case "userinfo":
    var userInfo = command.Data.Options.FirstOrDefault()?.Value as SocketUser ?? command.User;
    var guildUser = userInfo as SocketGuildUser;
   

    // Fetch User Roles
    string roles = guildUser != null && guildUser.Roles.Count > 1
        ? string.Join(", ", guildUser.Roles.Where(r => r.Id != guildUser.Guild.Id).Select(r => r.Mention))
        : "None";

    // Fetch Status and Device Info
    string userStatus = guildUser?.Status.ToString() ?? "Unknown";
    string devices = guildUser?.ActiveClients.Count > 0
        ? string.Join(", ", guildUser.ActiveClients.Select(c => c.ToString()))
        : "None";

    // Fetch Badges
    var badges = userInfo.PublicFlags.HasValue ? userInfo.PublicFlags.Value.ToString() : "None";

    // Fetch Boosting Info
    string boostingSince = guildUser?.PremiumSince.HasValue == true
        ? guildUser.PremiumSince.Value.ToString("f")
        : "Not Boosting";

    // Fetch Nickname
    string nickname = guildUser?.Nickname ?? "None";

    // Fetch Join Date
    string joinedAt = guildUser?.JoinedAt.HasValue == true
        ? guildUser.JoinedAt.Value.ToString("f")
        : "Unknown";

    // Fetch Highest Role
    string highestRole = guildUser?.Roles.OrderByDescending(r => r.Position).FirstOrDefault()?.Name ?? "None";

    // Fetch Avatar and Banner
    string avatarUrl = userInfo.GetAvatarUrl() ?? userInfo.GetDefaultAvatarUrl();
  

    // Build Embed
    var userEmbed = new EmbedBuilder()
        .WithTitle($"👤 {userInfo.Username}'s Info")
        .WithThumbnailUrl(avatarUrl)
        .WithColor(Color.Blue)
        .AddField("🆔 User ID", userInfo.Id, true)
        .AddField("📝 Username", userInfo.Username, true)
        .AddField("🏷️ Discriminator", $"#{userInfo.Discriminator}", true)
        .AddField("🔖 Badges", badges, true)
        .AddField("🎭 Nickname", nickname, true)
        .AddField("📅 Account Created", userInfo.CreatedAt.ToString("f"), true)
        .AddField("📥 Joined Server", joinedAt, true)
        .AddField("💎 Boosting Since", boostingSince, true)
        .AddField("💼 Highest Role", highestRole, true)
        .AddField("📌 Roles", roles, false)
        .AddField("📶 Status", userStatus, true)
        .AddField("📱 Active Devices", devices, true)
        .WithFooter($"Requested by {command.User.Username}", command.User.GetAvatarUrl())
        .WithTimestamp(DateTimeOffset.Now)
        .Build();

    await command.RespondAsync(embed: userEmbed);
    break;


case "announce":
//!
//Problem: If the target channel doesn’t exist, the bot crashes.
//Fix: Wrap it in try-catch and check for null. ->
//!
    try
    {
        var announceChannel = (SocketTextChannel)command.Data.Options.First(o => o.Name == "channel").Value;
        var announceMessage = (string)command.Data.Options.First(o => o.Name == "message").Value;
        
        if (announceChannel == null)
        {
            await command.RespondAsync("Error: Specified channel not found.", ephemeral: true);
            break;
        }

        await announceChannel.SendMessageAsync(announceMessage);
        await command.RespondAsync($"Announcement sent to {announceChannel.Mention} successfully.", ephemeral: true);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error in announce command.");
        await command.RespondAsync("An error occurred while sending the announcement.", ephemeral: true);
    }
    break;



case "clearwarns":
    var clearWarnUser = (SocketUser)command.Data.Options.First().Value;
    await ClearWarningsAsync(clearWarnUser);
    await command.RespondAsync($"Warnings for {clearWarnUser.Username} have been cleared.", ephemeral: true);
    break;

case "setnickname":
//! Problem: If no user/nickname is provided, the command fails without explanation.
//! Fix: Check if required options exist before execution. ->
    if (command.Data.Options == null || !command.Data.Options.Any())
    {
        await command.RespondAsync("No user or nickname provided. Please specify a user and a nickname.", ephemeral: true);
        break;
    }

    var nicknameUserOption = command.Data.Options.FirstOrDefault(o => o.Name == "user");
    var newNicknameOption = command.Data.Options.FirstOrDefault(o => o.Name == "nickname");

    if (nicknameUserOption == null || newNicknameOption == null)
    {
        await command.RespondAsync("Invalid input. Please specify both a user and a nickname.", ephemeral: true);
        break;
    }

    var nicknameUser = (SocketGuildUser)nicknameUserOption.Value;
    var newNickname = newNicknameOption.Value.ToString();

    await nicknameUser.ModifyAsync(x => x.Nickname = newNickname);
    await command.RespondAsync($"Nickname for {nicknameUser.Username} changed to `{newNickname}`.", ephemeral: true);
    break;



case "addrole":
    var addRoleUser = (SocketUser)command.Data.Options.First().Value;
    var addRole = (SocketRole)command.Data.Options.ElementAt(1).Value;
    await AddRoleToUserAsync(addRoleUser, addRole);
    await command.RespondAsync($"Role {addRole.Name} has been added to {addRoleUser.Username}.", ephemeral: true);
    break;

case "removerole":
    var removeRoleUser = (SocketUser)command.Data.Options.First().Value;
    var removeRole = (SocketRole)command.Data.Options.ElementAt(1).Value;
    await RemoveRoleFromUserAsync(removeRoleUser, removeRole);
    await command.RespondAsync($"Role {removeRole.Name} has been removed from {removeRoleUser.Username}.", ephemeral: true);
    break;


case "slowmode":
    var slowModeDuration = (int)command.Data.Options.First().Value;
    await SetSlowModeAsync(command, slowModeDuration);
    await command.RespondAsync($"Slow mode has been set for {slowModeDuration} seconds.", ephemeral: true);
    break;

case "clear":
    var clearCount = (int)command.Data.Options.First().Value;
    await ClearMessagesAsync(command, clearCount);
    await command.RespondAsync($"Cleared {clearCount} messages.", ephemeral: true);
    break;
case "setchannel":
    if (command.GuildId == null)
    {
        await command.RespondAsync("Dieser Befehl kann nur in einem Server verwendet werden.", ephemeral: true);
        break;
    }

    if (command.Data.Options.FirstOrDefault(x => x.Name == "channel")?.Value is SocketTextChannel selectedChannel)
    {
        try
        {
            ulong guildId = command.GuildId.Value;

            // Use the manager to save the channel!!!!
            await ChannelConfigManager.SaveChannelConfigAsync(guildId, selectedChannel.Id);

            await command.RespondAsync($"Der Kanal wurde erfolgreich auf {selectedChannel.Mention} gesetzt.", ephemeral: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Fehler beim Speichern des Kanals.");
            await command.RespondAsync($"Fehler beim Speichern des Kanals: {ex.Message}", ephemeral: true);
        }
    }
    else
    {
        await command.RespondAsync("Fehler: Bitte wähle einen gültigen Textkanal.", ephemeral: true);
    }
    break;

      default:
    await command.RespondAsync("Unknown command.", ephemeral: true);
    Log.Warning($"Unknown command {command.CommandName} invoked.");
    break;

            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred while handling command.");
            await command.RespondAsync("An error occurred while processing your request.", ephemeral: true);
        }
    }


private static async Task SetSlowModeAsync(SocketSlashCommand command, int duration)
{
    var channel = command.Channel as ITextChannel;
    if (channel != null)
    {
        await channel.ModifyAsync(properties => properties.SlowModeInterval = duration);
        Log.Information($"Slow mode set to {duration} seconds.");
    }
}

private static async Task ClearMessagesAsync(SocketSlashCommand command, int count)
{
    if (count <= 0 || count > 100)
    {
        Log.Warning("Invalid message count for clearing.");
        await command.RespondAsync("Please provide a valid number of messages to clear (1-100).", ephemeral: true);
        return;
    }

    var channel = command.Channel as ITextChannel;
    if (channel != null)
    {
        var messages = await channel.GetMessagesAsync(count).FlattenAsync();
        await channel.DeleteMessagesAsync(messages);
        Log.Information($"Cleared {count} messages in {channel.Name}.");
    }
}

private static Embed CreateEmbed(string description, Color color)
{
    return new EmbedBuilder()
        .WithDescription(description)
        .WithColor(color)
        .Build();
}

private static async Task BanUserAsync(SocketUser user, string reason)
{
    if (user is SocketGuildUser guildUser)
    {
        await guildUser.BanAsync(reason: reason);
        Log.Information($"User {user.Username} banned for: {reason}");
    }
}

private static async Task KickUserAsync(SocketUser user)
{
    if (user is SocketGuildUser guildUser)
    {
        await guildUser.KickAsync();
        Log.Information($"User {user.Username} kicked.");
    }
}

private static async Task MuteUserAsync(SocketUser user)
{
    if (user is SocketGuildUser guildUser)
    {
        var mutedRole = guildUser.Guild.Roles.FirstOrDefault(r => r.Name == "Muted");
        if (mutedRole != null)
        {
            await guildUser.AddRoleAsync(mutedRole);
            Log.Information($"User {user.Username} muted.");
        }
        else
        {
            Log.Warning("Muted role not found.");
        }
    }
}
//test
private static async Task UnmuteUserAsync(SocketUser user)
{
    if (user is SocketGuildUser guildUser)
    {
        var mutedRole = guildUser.Guild.Roles.FirstOrDefault(r => r.Name == "Muted");
        if (mutedRole != null)
        {
            await guildUser.RemoveRoleAsync(mutedRole);
            Log.Information($"User {user.Username} unmuted.");
        }
        else
        {
            Log.Warning("Muted role not found.");
        }
    }
}


private static async Task ClearWarningsAsync(SocketUser user)
{
   //TODO LOGIC
    Log.Information($"Warnings for user {user.Username} cleared.");
}

private static async Task SetNicknameAsync(SocketUser user, string nickname)
{
    if (user is SocketGuildUser guildUser)
    {
        await guildUser.ModifyAsync(properties => properties.Nickname = nickname);
        Log.Information($"Nickname for {user.Username} set to {nickname}.");
    }
}

private static async Task AddRoleToUserAsync(SocketUser user, SocketRole role)
{
    if (user is SocketGuildUser guildUser)
    {
        await guildUser.AddRoleAsync(role);
        Log.Information($"Role {role.Name} added to {user.Username}.");
    }
}

private static async Task RemoveRoleFromUserAsync(SocketUser user, SocketRole role)
{
    if (user is SocketGuildUser guildUser)
    {
        await guildUser.RemoveRoleAsync(role);
        Log.Information($"Role {role.Name} removed from {user.Username}.");
    }
} 



 private static void LoadRegionPingStatus()
    {
        try
        {
            Log.Information("Loading region ping status...");

            if (!File.Exists(ConfigFilePath))
            {
                Log.Warning("No configuration file found, initializing default ping status.");
                _regionPingStatus = RegionData.Regions.ToDictionary(r => r.Name.ToLower(), _ => true);
                SaveRegionPingStatus();
            }
            else
            {
                var json = File.ReadAllText(ConfigFilePath);
                _regionPingStatus = JsonSerializer.Deserialize<Dictionary<string, bool>>(json)
                                    ?? RegionData.Regions.ToDictionary(r => r.Name.ToLower(), _ => true);
                Log.Information("Loaded region ping status from file.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading region ping status. Reverting to default settings.");
            _regionPingStatus = RegionData.Regions.ToDictionary(r => r.Name.ToLower(), _ => true);
        }
    }

    private static void SaveRegionPingStatus()
    {
        try
        {
            Log.Information("Saving region ping status...");
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigFilePath) ?? "");

            var json = JsonSerializer.Serialize(_regionPingStatus, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);

            Log.Information("Region ping status saved.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving region ping status.");
        }
    }


}
