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

class Program
{
    private static DiscordSocketClient _client;
    private static SocketTextChannel _channel;
    private static Timer _timer;
    private static IUserMessage _currentMessage;
    private static Dictionary<string, bool> _regionPingStatus;

    private static readonly string ConfigFilePath = "regionPingStatus.json";

    private static readonly List<RegionInfo> _regions = new List<RegionInfo>
    {
        new RegionInfo
        {
            Name = "North America",
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
                //new ServerInfo { Name = "🌸Light:   Zodi", IP = "80.239.145.90" }


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
                new ServerInfo { Name = "ELEM: LOGIN", IP = "119.252.37.70" }
            }
        }
    };

    static async Task Main(string[] args)
    {
        // Initialize Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Bot starting...");
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

    private static async Task RegisterSlashCommands()
    {
        try
        {
            Log.Information("Registering slash commands...");

            if (_client == null)
            {
                Log.Error("Error: _client is null. Ensure the bot is started first.");
                return;
            }

            var commands = new List<SlashCommandBuilder>
            {
                new SlashCommandBuilder().WithName("help").WithDescription("Shows a list of available commands."),
                new SlashCommandBuilder().WithName("status").WithDescription("Displays the current status of all regions."),
                new SlashCommandBuilder().WithName("bob").WithDescription("Responds with a funny message."),
                new SlashCommandBuilder().WithName("shutdown").WithDescription("Shuts down the bot."),
            };

            if (commands == null || commands.Count == 0)
            {
                Log.Error("Error: No commands to register.");
                return;
            }

            ulong guildId = 1318269917769764884;
            foreach (var command in commands)
            {
                var commandProperties = command.Build();
                if (commandProperties != null)
                {
                    await _client.Rest.CreateGuildCommand(commandProperties, guildId);
                    Log.Information($"Command {command.Name} registered.");
                }
                else
                {
                    Log.Error($"Error: Failed to build command {command.Name}.");
                }
            }

            Log.Information("Slash commands registered successfully for the guild!");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error registering slash commands.");
        }
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
            await _client.SetGameAsync("Ambi");

            var token = "MTMzMTcxMjU2MDY4NDIwODIzOQ.G4ydyX.jirNnSH_G6cxyubz6uXFLa6gncuKdYGp6HXDBk"; 
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _timer = new Timer(5000);
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

            await RegisterSlashCommands();
            var channelId = ulong.Parse("1331803694294896796");
            _channel = (SocketTextChannel)_client.GetChannel(channelId);
            _currentMessage = await _channel.SendMessageAsync(embed: CreateEmbed("Initializing server status..."));
            Log.Information("Ready event handled.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error handling ReadyAsync.");
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

    private static async Task PingServers()
    {
        try
        {
            Log.Information("Pinging servers...");

            var embed = new EmbedBuilder()
                .WithTitle("FFXIV Server Status")
                .WithColor(Color.Blue)
                .WithImageUrl("https://lds-img.finalfantasyxiv.com/h/e/2a9GxMb6zta1aHsi8u-Pw9zByc.jpg")
                .WithTimestamp(DateTimeOffset.Now);

            foreach (var region in _regions)
            {
                //idk breaks things
                //if (!_regionPingStatus.TryGetValue(region.Name.ToLower(), out var isActive) || !isActive)
                //    continue;

                string table = "```\nServer    | Ping (ms) | Loss |   Status\n" +
                               "\n ---------|-----------|------|---------- \n";

                foreach (var server in region.Servers)
                {
                    string status, responseTime;
                    string statusEmoji;
                    string packetLoss = "0%";  // Default packet loss

                    int successfulPings = 0;
                    int totalPings = 5;  // Number of pings to test for packet loss

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

                        // Calculate packet loss as percentage
                        int loss = totalPings - successfulPings;
                        packetLoss = (loss > 0) ? $"{(loss * 100 / totalPings)}%" : "0%";

                        // Ping last time for roundtrip time
                        using var lastPing = new Ping();
                        var finalReply = await lastPing.SendPingAsync(server.IP);

                        if (finalReply.Status == IPStatus.Success)
                        {
                            status = "Online";
                            responseTime = finalReply.RoundtripTime.ToString();
                            statusEmoji = "🟢"; // Green circle for online
                        }
                        else
                        {
                            status = "Offline";
                            responseTime = "N/A";
                            statusEmoji = "🔴"; // Red circle for offline
                        }
                    }
                    catch (Exception ex)
                    {
                        status = "Error";
                        responseTime = "N/A";
                        statusEmoji = "⚪"; // White circle for error
                        Log.Error(ex, $"Error pinging server {server.Name} ({server.IP}).");
                    }

                    table += $"\n{server.Name} |  {responseTime}ms | {packetLoss}loss| {status} {statusEmoji}\n"; 
                }

                table += "\n```";
                embed.AddField(region.Name, table, false);
            }

            if (_currentMessage != null)
            {
                Log.Information("Modifying current message with new embed.");
                await _currentMessage.ModifyAsync(msg => msg.Embed = embed.Build());
            }
            else
            {
                Log.Information("Sending new message with embed.");
                _currentMessage = await _channel.SendMessageAsync(embed: embed.Build());
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error pinging servers.");
        }
    }




    private static async Task HandleSlashCommandAsync(SocketSlashCommand command) //maybe seperate class later
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
                        "`/reload` - Reloads server status.", Color.Green));
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

                case "reload":
                    Log.Information("Reloading server status...");
                    await PingServers();
                    await command.RespondAsync("Server status reloaded.", ephemeral: true);
                    break;

                case "bob":
                    Log.Information("User invoked 'bob' command.");
                    await command.RespondAsync("Kannst das Ding eh net gewinne, denn der Bob Tschigerillo macht mit", ephemeral: true);
                    break;

                case "shutdown":
                    Log.Information("Shutting down the bot...");
                    await command.RespondAsync("Shutting down...", ephemeral: true);
                    Environment.Exit(0);
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
}

public class ServerInfo
{
    public string Name { get; set; }
    public string IP { get; set; }
}

public class RegionInfo
{
    public string Name { get; set; }
    public List<ServerInfo> Servers { get; set; } = new List<ServerInfo>();
}
