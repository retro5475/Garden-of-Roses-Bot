using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Timers;
using System.Collections.Generic;
using Timer = System.Timers.Timer;
using Serilog;
using System.Reactive;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Discord.Commands;
using System.Reflection.Metadata;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using log4net.Plugin;
using System;
using System.Reflection;
using Serilog;
using System.Diagnostics;

class Program
{
    private static List<Assembly> _loadedPlugins = new List<Assembly>();
    private static DiscordSocketClient _client;
    private static SocketTextChannel _channel;
    private static CommandService _commands;
    private static IServiceProvider _services;
    private static Timer _timer;
    private static IUserMessage _currentMessage;
    private static Dictionary<string, bool> _regionPingStatus;

    private static readonly string ConfigFilePath = "files/regionPingStatus.json";
    private static readonly string ConfigFilePathConfig = "files/channelconf.json";

    private static readonly List<RegionInfo> _regions = new List<RegionInfo>  //! ToDo use Constants!
    {
        new RegionInfo
        {
            Name = "USA",
            Servers = new List<ServerInfo>
            {
                new ServerInfo { Name = "Aether: LOGIN", IP = "204.2.29.80" }
            }
        },
        new RegionInfo
        {
            Name = "Europe",
            Servers = new List<ServerInfo>
            {
                new ServerInfo { Name = "🌼Chaos: LOGIN", IP = "80.239.145.6" }, //neolobby06.ffxiv.com > 80.239.145.6
                new ServerInfo { Name = "🌸Light: LOGIN", IP = "80.239.145.7" }, // neolobby07.ffxiv.com > 80.239.145.7
                new ServerInfo { Name = "🌸Light: Alpha", IP = "80.239.145.91" },
                new ServerInfo { Name = "🌸Light: Lich", IP = "80.239.145.92" },
                new ServerInfo { Name = "🌸Light: Odin", IP = "80.239.145.93" },
                new ServerInfo { Name = "🌸Light: Phönx", IP = "80.239.145.94" },
                new ServerInfo { Name = "🌸Light: Raiden", IP = "80.239.145.95" },
                new ServerInfo { Name = "🌸Light: Shiva", IP = "80.239.145.96" },
                new ServerInfo { Name = "🌸Light: Twin", IP = "80.239.145.97" }
                //new ServerInfo { Name = "🌸Light:   Zodi", IP = "80.239.145.90" } //! Ding is, ich find die ip net :´D
                


            }
        },
        new RegionInfo
        {
            Name = "Japan",
            Servers = new List<ServerInfo>
            {
                //new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.61" },
                //new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.62" },
                //new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.63" },
                //new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.64" },
                //new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.65" },
                //new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.66" },
                //new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.67" },
                //new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.68" },
                //new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.69" },
                //new ServerInfo { Name = "ELEM: LOGIN", IP = "119.252.37.70" }
            }
        }
    };

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


    

    private static async Task StartBotAsync()
    {
        try
        {
            Log.Information("Starting the bot...");

            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged
            });

            _client.Log += logMessage =>
            {
                Log.Information($"Discord Log: {logMessage}");
                return Task.CompletedTask;
            };

            _client.Ready += ReadyAsync;
            _client.SlashCommandExecuted += HandleSlashCommandAsync;

            await _client.SetStatusAsync(UserStatus.DoNotDisturb);
            await _client.SetGameAsync("N/A");

            var token = "MTMzMzQ3MDI1NTk4ODg3MTE5Mg.GT3deu.3OUCkammp0iYcY6LuiH3X3OHB8aD_zWVIXCI0s"; 
            await _client.LoginAsync(TokenType.Bot, token);
            await PluginLoader.LoadAndExecutePluginsAsync(_client);

            await _client.StartAsync();

            _timer = new Timer(60000);
            _timer.Elapsed += async (sender, e) => await PingServers();
            _timer.Start();

            Log.Information("Bot started. Waiting for commands...");
            await Task.Delay(-1);
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
        await _client.SetGameAsync("FFXIV Server Status");
        Log.Information("Registering commands...");

        await commands.RegisterSlashCommands(_client);

        // Load Channel-ID from config
        ulong channelId = await LoadChannelConfigAsync();
        if (channelId == 0)
        {
            Log.Error("Channel-ID nicht gefunden. Bitte setze den Channel mit /setchannel.");
            return;
        }

        // Get the channel from the loaded ID
        var channel = _client.GetChannel(channelId) as SocketTextChannel;
        if (channel == null)
        {
            Log.Error($"Der gespeicherte Channel (ID: {channelId}) konnte nicht gefunden werden.");
            return;
        }

        _channel = channel;

        // Send initialization message
        _currentMessage = await _channel.SendMessageAsync(embed: CreateEmbed("Initializing server status..."));
        if (_currentMessage == null)
        {
            Log.Error("Fehler beim Senden der Initialisierungsnachricht.");
            return;
        }

        Log.Information("Ready event handled.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Fehler beim Verarbeiten von ReadyAsync.");
    }
}

private static async Task<ulong> LoadChannelConfigAsync()
{
    if (!File.Exists(ConfigFilePathConfig))
    {
        Log.Error($"Config-Datei '{ConfigFilePathConfig}' nicht gefunden.");
        return 0;
    }

    try
    {
        var json = await File.ReadAllTextAsync(ConfigFilePathConfig);
        var configData = JsonSerializer.Deserialize<Dictionary<string, ulong>>(json);

        if (configData != null && configData.TryGetValue("channelId", out var savedChannelId))
        {
            return savedChannelId;
        }
        else
        {
            Log.Error("Channel-ID fehlt oder ist ungültig in der Konfigurationsdatei.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Fehler beim Lesen der Channel-Konfigurationsdatei.");
    }

    return 0;
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

    private static async Task<SocketTextChannel?> GetSavedChannelAsync()
{
    if (!File.Exists(ConfigFilePathConfig))
    {
        Log.Error($"Config-Datei '{ConfigFilePathConfig}' nicht gefunden.");
        return null;
    }

    try
    {
        var json = await File.ReadAllTextAsync(ConfigFilePathConfig);
        var configData = JsonSerializer.Deserialize<Dictionary<string, ulong>>(json);

        if (configData != null && configData.TryGetValue("channelId", out var channelId))
        {
            var channel = _client.GetChannel(channelId) as SocketTextChannel;
            if (channel == null)
            {
                Log.Error($"Der gespeicherte Channel (ID: {channelId}) konnte nicht gefunden werden.");
                return null;
            }
            return channel;
        }
        else
        {
            Log.Error("Channel-ID fehlt oder ist ungültig in der Konfigurationsdatei.");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Fehler beim Laden der Channel-Konfigurationsdatei.");
    }

    return null;
}


private static async Task PingServers(bool forceNewEmbed = false)
{
    try
    {
        Log.Information("Pinging servers...");

        // Ensure we load the correct channel before sending
        var channel = await GetSavedChannelAsync();
        if (channel == null)
        {
            Log.Error("Kein gültiger Channel gefunden. Bitte setze den Channel mit /setchannel.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("FFXIV Server Status")
            .WithColor(Color.Blue)
            .WithImageUrl("https://lds-img.finalfantasyxiv.com/h/e/2a9GxMb6zta1aHsi8u-Pw9zByc.jpg")
            .WithTimestamp(DateTimeOffset.Now);

        Dictionary<string, bool> regionStatuses = new();
        if (File.Exists(ConfigFilePath))
        {
            try
            {
                var json = File.ReadAllText(ConfigFilePath);
                regionStatuses = JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error reading the region status configuration file.");
            }
        }
        else
        {
            Log.Warning($"Config file {ConfigFilePath} not found. Defaulting to active for all regions.");
        }

        foreach (var region in _regions)
        {
            if (!regionStatuses.TryGetValue(region.Name.ToLower(), out var isActive) || !isActive)
            {
                Log.Information($"Skipping region {region.Name} (inactive).");
                continue;
            }

            string table = "```\nServer         | Ping (ms) | Loss | Status\n" +
                           "--------------|-----------|------|-------\n";

            foreach (var server in region.Servers)
            {
                string status, responseTime;
                string statusEmoji;
                string packetLoss = "0%";

                int successfulPings = 0;
                int totalPings = 5;

                try
                {
                    for (int i = 0; i < totalPings; i++)
                    {
                        using var ping = new Ping();
                        var reply = await ping.SendPingAsync(server.IP);

                        if (reply.Status == IPStatus.Success)
                        {
                            successfulPings++;
                        }
                    }

                    int loss = totalPings - successfulPings;
                    packetLoss = loss > 0 ? $"{(loss * 100 / totalPings)}%" : "0%";

                    using var lastPing = new Ping();
                    var finalReply = await lastPing.SendPingAsync(server.IP);

                    if (finalReply.Status == IPStatus.Success)
                    {
                        status = "Online";
                        responseTime = finalReply.RoundtripTime.ToString();
                        statusEmoji = "🟢";
                    }
                    else
                    {
                        status = "Offline";
                        responseTime = "N/A";
                        statusEmoji = "🔴";
                    }
                }
                catch (Exception ex)
                {
                    status = "Error";
                    responseTime = "N/A";
                    statusEmoji = "⚪";
                    Log.Error(ex, $"Error pinging server {server.Name} ({server.IP}).");
                }

                table += $"{server.Name.PadRight(15)}| {responseTime.PadLeft(5)} ms  | {packetLoss.PadLeft(5)}| {status} {statusEmoji}\n";
            }

            table += "```";
            embed.AddField(region.Name, table, false);
        }

        // Handling the message update or creation
        if (forceNewEmbed)
        {
            // Delete the old message if it exists
            if (_currentMessage != null)
            {
                try
                {
                    Log.Information("Deleting the old message.");
                    await _currentMessage.DeleteAsync();
                    _currentMessage = null; // Reset the message reference to null after deletion
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to delete the old message.");
                }
            }

            Log.Information("Sending a new embed message.");
            try
            {
                _currentMessage = await channel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error sending the new embed message.");
            }
        }
        else
        {
            // Update the existing message if it exists
            if (_currentMessage != null)
            {
                try
                {
                    Log.Information("Modifying the existing embed message.");
                    await _currentMessage.ModifyAsync(msg => msg.Embed = embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error modifying the existing message. Trying to send a new one.");
                    
                    // If modifying fails, reset _currentMessage and send a new one instead
                    _currentMessage = null; // Reset the reference to null
                    try
                    {
                        Log.Information("Sending a new embed message due to modification failure.");
                        _currentMessage = await channel.SendMessageAsync(embed: embed.Build());
                    }
                    catch (Exception retryEx)
                    {
                        Log.Error(retryEx, "Error sending the new embed message after modification failure.");
                    }
                }
            }
            else
            {
                Log.Information("No existing message found, sending a new embed.");
                try
                {
                    _currentMessage = await channel.SendMessageAsync(embed: embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error sending the new embed message.");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error pinging servers.");
    }
}








    static void CheckAndCreateDirectories()
    {
        if (!Directory.Exists("files"))
            Directory.CreateDirectory("files");
        if (!Directory.Exists("plugins"))
            Directory.CreateDirectory("plugins");

    }


    public class PluginLoader
    {
        private static readonly List<Assembly> _loadedPlugins = new List<Assembly>();

        public static async Task LoadAndExecutePluginsAsync(DiscordSocketClient _client)
        {
            _loadedPlugins.Clear();

            var pluginFiles = Directory.GetFiles("plugins", "*.cs"); // Alle .cs-Dateien im Ordner "plugins"
            foreach (var file in pluginFiles)
            {
                try
                {
                    Console.WriteLine($"Loading plugin: {Path.GetFileName(file)}");

                    // Plugin-Code einlesen
                    var code = File.ReadAllText(file);

                    // ScriptOptions konfigurieren
                    var scriptOptions = ScriptOptions.Default
                        .WithReferences(AppDomain.CurrentDomain.GetAssemblies())
                        .WithImports(
                            "System",
                            "System.IO",
                            "System.Linq",
                            "System.Collections.Generic",
                            "Discord",
                            "Discord.WebSocket",
                            "Discord.Commands",
                            "System.Threading.Tasks"
                        );

                    // Script erstellen und kompilieren
                    var script = CSharpScript.Create(code, scriptOptions);
                    var compilation = script.GetCompilation();

                    using var ms = new MemoryStream();
                    var result = compilation.Emit(ms); // Code kompilieren und in MemoryStream speichern

                    if (result.Success)
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        var assembly = Assembly.Load(ms.ToArray()); // Assembly aus Stream laden

                        _loadedPlugins.Add(assembly); // Geladene Assembly zur Liste hinzufügen
                        Console.WriteLine($"Loaded plugin: {Path.GetFileName(file)}");
                    }
                    else
                    {
                        Console.WriteLine($"Error compiling plugin {file}: {string.Join(", ", result.Diagnostics)}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading plugin {file}: {ex.Message}");
                }
            }

            // Starte die Plugins in einem separaten Task
            _ = Task.Run(async () =>
            {
                foreach (var plugin in _loadedPlugins)
                {
                    await ExecutePluginAsync(plugin, _client);
                }
            });
        }

        private static async Task ExecutePluginAsync(Assembly assembly, DiscordSocketClient _client)
        {
            try
            {
                // Alle Typen in der Assembly durchsuchena
                foreach (var type in assembly.GetTypes())
                {
                    if (type.GetMethod("RunAsync") != null)
                    {
                        // Hier wird die Instanz mit dem richtigen Konstruktor erstellt und der Client übergeben
                        var constructor = type.GetConstructor(new[] { typeof(DiscordSocketClient) });

                        if (constructor != null)
                        {
                            // Instanz mit dem Konstruktor erstellen
                            var instance = constructor.Invoke(new object[] { _client });

                            var method = type.GetMethod("RunAsync");

                            if (method != null)
                            {
                                // Plugin in einem separaten Task ausführen, um Blockierungen zu verhindern
                                _ = Task.Run(async () =>
                                {
                                    try
                                    {
                                        Console.WriteLine($"Executing plugin: {type.Name}");
                                        var task = method.Invoke(instance, null) as Task;

                                        if (task != null)
                                        {
                                            await task; // Ausführung der Methode abwarten
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error executing plugin {type.Name}: {ex.Message}");
                                    }
                                });
                            }
                        }
                        else
                        {
                            Console.WriteLine($"No valid constructor found for {type.Name}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing plugin: {ex.Message}");
            }
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
                        "`/warn` - Warns a user.\n" +
                        "`/clearwarns` - Clears a user's warnings.\n" +
                        "`/userinfo` - Displays user information.\n" +
                        "`/setnickname` - Sets a user's nickname.\n" +
                        "`/lockdown` - Locks the channel temporarily.\n" +
                        "`/unlock` - Unlocks the channel.\n" +
                        "`/addrole` - Adds a role to a user.\n" +
                        "`/removerole` - Removes a role from a user.\n" +
                        "`/mutechannel` - Mutes a channel.\n" +
                        "`/unmutechannel` - Unmutes a channel.\n" +
                        "`/slowmode` - Sets a slow mode on a channel.\n" +
                        "`/announce` - Sends an announcement.\n" +
                        "`/setprefix` - Sets a custom prefix.\n" +
                        "`/clear` - Clears messages in a channel.\n" +
                        "`/purge` - Purges messages older than a certain time.\n" +
                        "`/filter` - Set up a word filter.\n" +
                        "`/showwarns` - Shows warnings for a user.\n" +
                        "`/clearallwarns` - Clears all warnings from the server.\n" +
                        "`/banlist` - Displays the ban list.\n" +
                        "`/tempmute` - Temporarily mutes a user.\n" +
                        "`/tempban` - Temporarily bans a user.\n" +
                        "`/tempkick` - Temporarily kicks a user.\n" +
                        "`/setwelcome` - Sets a welcome message.\n" +
                        "`/setgoodbye` - Sets a goodbye message.\n" +
                        "`/toggleprefix` - Toggles the prefix feature.\n" +
                        "`/addreaction` - Adds a reaction to a message.\n" +
                        "`/clearreactions` - Clears reactions from a message.\n" +
                        "`/suspend` - Suspends a user from chatting.\n" +
                        "`/rejoin` - Allows a suspended user to rejoin the chat.\n" +
                        "`/serverinfo` - Displays server information.\n" +
                        "`/poll` - Creates a poll.\n" +
                        "`/setautomod` - Sets up an automated moderation system.\n" +
                        "`/cleanchannels` - Deletes unused channels.\n" +
                        "`/setlogchannel` - Sets a channel for logs.\n" +
                        "`/purgeuser` - Purges messages from a specific user.\n", Color.Green));
                    break;
            


                     case "status":
                    Log.Information("Status command executed.");
                    string statusMessage = "Current Region Status:\n\n";
                    foreach (var region in _regions)
                    {
                        var isActive = _regionPingStatus.GetValueOrDefault(region.Name.ToLower(), false);
                        statusMessage += $"{region.Name}: {(isActive ? "Active" : "Inactive")}\n";
                    }
                    await command.RespondAsync(embed: CreateEmbed(statusMessage, Color.Blue));
                    break;

            case "reload": //! How this works now is, setting an flag of the reload *forcenewembed* true, so we get an new embed!
                 Log.Information("Reloading server status...");
                 await PingServers(forceNewEmbed: true);
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
                        await command.RespondAsync(embed: CreateEmbed($"Ping for {regionName} {status}.", _regionPingStatus[regionName] ? Color.Green : Color.Red), ephemeral: true);
                    }
                    break;

             case "shutdown":
    Log.Information("Shutting down the bot...");
    await command.RespondAsync("Shutting down...", ephemeral: true);
    Environment.Exit(0);
    break;

case "ban":
    var banUser = (SocketUser)command.Data.Options.First().Value;
    var banReason = command.Data.Options.Count > 1 ? command.Data.Options.ElementAt(1).Value.ToString() : "No reason provided";
    await BanUserAsync(banUser, banReason);
    await command.RespondAsync($"User {banUser.Username} has been banned for: {banReason}", ephemeral: true);
    break;

case "kick":
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

var warnUser = (SocketUser)command.Data.Options.First().Value;
var warnMessage = command.Data.Options.Count > 1 ? command.Data.Options.ElementAt(1).Value.ToString() : "No reason provided";

await WarnUserAsync(warnUser, warnMessage);

try
{
    var dmChannel = await warnUser.CreateDMChannelAsync();
    await dmChannel.SendMessageAsync($"You have been warned: {warnMessage}");
}
catch (Exception ex)
{
    Log.Information($"Could not send DM to {warnUser.Username}: {ex.Message}");
}

// Send the warning message to a specific channel
if (command.Channel is IGuildChannel guildChannel)
{
    var guild = guildChannel.Guild;
    var warnChannel = await guild.GetTextChannelAsync(1318285896029573153);
    
    if (warnChannel != null)
    {
        await warnChannel.SendMessageAsync($"{warnUser.Mention} has been warned for: {warnMessage}");
    }
    else
    {
        Log.Warning("Warning channel not found.");
    }
}

await command.RespondAsync($"User {warnUser.Username} has been warned for: {warnMessage}", ephemeral: true);
break;



case "clearwarns":
    var clearWarnUser = (SocketUser)command.Data.Options.First().Value;
    await ClearWarningsAsync(clearWarnUser);
    await command.RespondAsync($"Warnings for {clearWarnUser.Username} have been cleared.", ephemeral: true);
    break;

case "setnickname":
    try
    {
        if (command.Data.Options == null || !command.Data.Options.Any())
        {
            await command.RespondAsync("No user or nickname provided. Please specify a user and a nickname.", ephemeral: true);
            return;
        }

        var nicknameUserOption = command.Data.Options.FirstOrDefault(o => o.Name == "user");
        var nicknameOption = command.Data.Options.FirstOrDefault(o => o.Name == "nickname");

        if (nicknameUserOption == null || nicknameOption == null)
        {
            await command.RespondAsync("Invalid arguments. Please provide both a user and a nickname.", ephemeral: true);
            return;
        }

        var nicknameUser = nicknameUserOption.Value as SocketGuildUser;
        var nickname = nicknameOption.Value?.ToString();

        if (nicknameUser == null)
        {
            await command.RespondAsync("User not found in this guild. Please ensure the user exists.", ephemeral: true);
            return;
        }

        if (string.IsNullOrWhiteSpace(nickname))
        {
            await command.RespondAsync("Please provide a valid nickname.", ephemeral: true);
            return;
        }

        await nicknameUser.ModifyAsync(u => u.Nickname = nickname);
        await command.RespondAsync($"User {nicknameUser.Username}'s nickname has been set to {nickname}.", ephemeral: true);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error occurred while setting nickname.");
        await command.RespondAsync("Failed to set the nickname. Ensure the bot has the necessary permissions.", ephemeral: true);
    }
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
    if (command.Data.Options.FirstOrDefault(x => x.Name == "channel")?.Value is SocketTextChannel selectedChannel)
    {
        try
        {
            // Speichere die Kanal-ID in der JSON-Datei
            await SaveChannelConfigAsync(selectedChannel.Id);

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


    //TODO Own Class


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

private static async Task WarnUserAsync(SocketUser user, string message)
{
    // TODO: logic send in channel & send to user
    Log.Information($"User {user.Username} warned for: {message}");
}

private static async Task ClearWarningsAsync(SocketUser user)
{
    // TODO: logic send in channel
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
                _regionPingStatus = _regions.ToDictionary(r => r.Name.ToLower(), _ => true);
                SaveRegionPingStatus();
                Log.Information("No configuration file found, initializing default ping status.");
            }
            else
            {
                var json = File.ReadAllText(ConfigFilePath);
                _regionPingStatus = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                Log.Information("Loaded region ping status from file.");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading region ping status.");
            _regionPingStatus = _regions.ToDictionary(r => r.Name.ToLower(), _ => true);
        }
    }

    private static void SaveRegionPingStatus()
    {
        try
        {
            Log.Information("Saving region ping status...");
            var json = JsonSerializer.Serialize(_regionPingStatus, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFilePath, json);
            Log.Information("Region ping status saved.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving region ping status.");
        }
    }

   private static async Task SaveChannelConfigAsync(ulong channelId)
{
    try
    {
        var configData = new Dictionary<string, ulong> { { "channelId", channelId } };
        var json = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(ConfigFilePathConfig, json);
        Log.Information($"Channel-ID {channelId} gespeichert.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Fehler beim Speichern der Channel-ID.");
    }
}
}


public class ServerInfo
{
    public string? Name { get; set; }
    public string? IP { get; set; }
}

public class RegionInfo
{
    public string? Name { get; set; }
    public List<ServerInfo> Servers { get; set; } = new List<ServerInfo>();
}
