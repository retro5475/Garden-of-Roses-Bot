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
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.70" },
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.71" },
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.72" },
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.73" },
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.74" },
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.75" },
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.76" },
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.77" },
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.78" },
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.79" },
                new ServerInfo { Name = "Aether Lobby", IP = "204.2.29.80" }
            }
        },
        new RegionInfo
        {
            Name = "Europe",
            Servers = new List<ServerInfo>
            {
                new ServerInfo { Name = "Chaos Lobby", IP = "80.239.145.79" },
                new ServerInfo { Name = "Chaos Lobby", IP = "80.239.145.80" },
                new ServerInfo { Name = "Chaos Lobby", IP = "80.239.145.81" },
                new ServerInfo { Name = "Chaos Lobby", IP = "80.239.145.82" },
                new ServerInfo { Name = "Chaos Lobby", IP = "80.239.145.83" },
                new ServerInfo { Name = "Chaos Lobby", IP = "80.239.145.84" },
                new ServerInfo { Name = "Chaos Lobby", IP = "80.239.145.87" },
                new ServerInfo { Name = "Chaos Lobby", IP = "80.239.145.88" },
                new ServerInfo { Name = "Chaos Lobby", IP = "80.239.145.89" },
                new ServerInfo { Name = "Light Lobby", IP = "80.239.145.91" }
            }
        },
        new RegionInfo
        {
            Name = "Japan",
            Servers = new List<ServerInfo>
            {
                new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.61" },
                new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.62" },
                new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.63" },
                new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.64" },
                new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.65" },
                new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.66" },
                new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.67" },
                new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.68" },
                new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.69" },
                new ServerInfo { Name = "Elemental Lobby", IP = "119.252.37.70" }
            }
        }
    };

    public static Func<LogMessage, Task> LogAsync => logMessage =>
    {
        Console.WriteLine(logMessage.ToString());
        return Task.CompletedTask;
    };

    static async Task Main(string[] args)
    {
        LoadRegionPingStatus();

        Console.WriteLine("Starte die Befehlsregistrierung...");
        await RegisterSlashCommands(); // Befehlsregistrierung
        Console.WriteLine("Befehle erfolgreich registriert!");

        await StartBotAsync();
    }

    private static async Task RegisterSlashCommands()
    {
        try
        {
            var commands = new List<ApplicationCommandProperties>
            {
                new SlashCommandBuilder()
                    .WithName("help")
                    .WithDescription("Zeigt eine Liste der verfügbaren Befehle an.")
                    .Build(),
                new SlashCommandBuilder()
                    .WithName("europe")
                    .WithDescription("Aktiviert oder deaktiviert den Ping für die Europa-Server.")
                    .Build(),
                new SlashCommandBuilder()
                    .WithName("usa")
                    .WithDescription("Aktiviert oder deaktiviert den Ping für die Nordamerika-Server.")
                    .Build(),
                new SlashCommandBuilder()
                    .WithName("japan")
                    .WithDescription("Aktiviert oder deaktiviert den Ping für die Japan-Server.")
                    .Build(),
                new SlashCommandBuilder()
                    .WithName("status")
                    .WithDescription("Zeigt den aktuellen Status aller Regionen an.")
                    .Build(),
                new SlashCommandBuilder()
                    .WithName("reload")
                    .WithDescription("Lädt den Serverstatus neu.")
                    .Build(),
                new SlashCommandBuilder()
                    .WithName("bob")
                    .WithDescription("Antwortet mit einer lustigen Nachricht.")
                    .Build(),
                new SlashCommandBuilder()
                    .WithName("shutdown")
                    .WithDescription("Fährt den Bot herunter.")
                    .Build(),
                new SlashCommandBuilder()
                    .WithName("restart")
                    .WithDescription("Startet den Bot neu.")
                    .Build()
            };

            // Registriere neger Commands
            await _client.BulkOverwriteGlobalApplicationCommandsAsync(commands.ToArray());
            Console.WriteLine("Slash-Commands erfolgreich registriert!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler bei der Registrierung der Slash-Commands: {ex.Message}");
        }
    }

    private static async Task StartBotAsync()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged
        });

        _client.Log += LogAsync;
        _client.Ready += ReadyAsync;
        _client.MessageReceived += HandleMessageAsync;

        await _client.SetStatusAsync(UserStatus.DoNotDisturb);
        await _client.SetGameAsync("Loading..");

        var token = "MTMzMTcxMjU2MDY4NDIwODIzOQ.G4ydyX.jirNnSH_G6cxyubz6uXFLa6gncuKdYGp6HXDBk"; // Bot-Token 
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        _timer = new Timer(5000);
        _timer.Elapsed += async (sender, e) => await PingServers();
        _timer.Start();

        await Task.Delay(-1);
    }

    private static async Task ReadyAsync()
    {
        Console.WriteLine("Bot ist bereit!");
        await _client.SetGameAsync("Marlin-Status");

        var channelId = ulong.Parse("1331663013719048243"); // Channel-ID
        _channel = (SocketTextChannel)_client.GetChannel(channelId);
        _currentMessage = await _channel.SendMessageAsync(embed: CreateEmbed("Initialisiere Server-Status..."));
    }

    private static Embed CreateEmbed(string description, Color? color = null)
    {
        return new EmbedBuilder()
            .WithTitle("FFXIV Server Status")
            .WithDescription(description)
            .WithColor(color ?? Color.Blue)
            .WithTimestamp(DateTimeOffset.Now)
            .Build();
    }




    private static async Task PingServers()
    {
        var embed = new EmbedBuilder()
            .WithTitle("FFXIV Server Status")
            .WithColor(Color.Blue)
            .WithImageUrl("https://lds-img.finalfantasyxiv.com/h/e/2a9GxMb6zta1aHsi8u-Pw9zByc.jpg") 
            .WithTimestamp(DateTimeOffset.Now);

        foreach (var region in _regions)
        {
            if (!_regionPingStatus.TryGetValue(region.Name.ToLower(), out var isActive) || !isActive)
                continue;

            string table = "```\nServer              | Ping (ms) | Status\n" +
                           "--------------------|-----------|--------\n";

            foreach (var server in region.Servers)
            {
                string status, responseTime;
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(server.IP);

                    if (reply.Status == IPStatus.Success)
                    {
                        status = "Online";
                        responseTime = reply.RoundtripTime.ToString();
                    }
                    else
                    {
                        status = "Offline";
                        responseTime = "N/A";
                    }
                }
                catch
                {
                    status = "Fehler";
                    responseTime = "N/A";
                }

                table += $"{server.Name.PadRight(20)}| {responseTime.PadLeft(9)} | {status}\n";
            }

            table += "```";
            embed.AddField(region.Name, table, false);
        }

        if (_currentMessage != null)
        {
            await _currentMessage.ModifyAsync(msg => msg.Embed = embed.Build());
        }
        else
        {
            _currentMessage = await _channel.SendMessageAsync(embed: embed.Build());
        }
    }


    private static async Task HandleMessageAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;

        var content = message.Content.ToLower();
        if (content == "/help")
        {
            var embed = CreateEmbed("Verfügbare Befehle:\n\n" +
                                    "`/europe` - Pingt Europa-Server nicht mehr.\n" +
                                    "`/usa` - Pingt Nordamerika-Server nicht mehr.\n" +
                                    "`/japan` - Pingt Japan-Server nicht mehr.\n" +
                                    "`/status` - Zeigt aktuelle Ping-Status an.\n" +
                                    "`/reload` - Lädt Server-Status neu.", Color.Green);
            await message.Channel.SendMessageAsync(embed: embed);
        }
        else if (content == "/status")
        {
            var statuses = string.Join("\n", _regionPingStatus.Select(kv => $"{kv.Key}: {(kv.Value ? "Aktiv" : "Inaktiv")}"));
            await message.Channel.SendMessageAsync(embed: CreateEmbed($"Region-Status:\n{statuses}", Color.Gold));
        }
        else if (content == "/europe" || content == "/usa" || content == "/japan")
        {
            var regionName = content.Substring(1); // Entfernt "/"
            if (_regionPingStatus.ContainsKey(regionName))
            {
                _regionPingStatus[regionName] = !_regionPingStatus[regionName];
                SaveRegionPingStatus();
                var status = _regionPingStatus[regionName] ? "aktiviert" : "deaktiviert";

                await UpdateEmbedForRegion(regionName, _regionPingStatus[regionName]);
                await message.Channel.SendMessageAsync(embed: CreateEmbed($"Ping für {regionName} {status}.", _regionPingStatus[regionName] ? Color.Green : Color.Red));
            }
        }
        else if (content == "/bob")
        {
            await message.Channel.SendMessageAsync("Gewinne kannste das Ding eh net, weil der Bob Tschigerillo macht mit! ");
        }
        else if (content == "/shutdown")
        {
            await message.Channel.SendMessageAsync("Der Bot wird jetzt heruntergefahren. Auf Wiedersehen! 👋");
            
            await Task.Delay(1000);
            Environment.Exit(0); 
        }
        else if (content == "/restart")
        {
            await message.Channel.SendMessageAsync("Der Bot wird jetzt neu gestartet. Bitte warten... 🔄");
            
            await Task.Delay(1000); 
            System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            Environment.Exit(0);
        }
        else if (content == "/reload")
        {
            if (_currentMessage != null)
            {
                await _currentMessage.DeleteAsync();
                _currentMessage = null;
            }

            _currentMessage = await _channel.SendMessageAsync(embed: CreateEmbed("Lade Server-Status neu..."));
            await PingServers();
        }
    }

    private static async Task UpdateEmbedForRegion(string regionName, bool isActive)
    {
        // Holt aktuelle Embed
        var embed = _currentMessage?.Embeds.FirstOrDefault();
        if (embed == null) return;

        var builder = embed.ToEmbedBuilder();
        var fields = builder.Fields;

        // Entfernt oder fügt die Region basierend auf dem Status hinzu
        var fieldIndex = fields.FindIndex(field => field.Name.Equals(regionName, StringComparison.OrdinalIgnoreCase));
        if (!isActive)
        {
            // Region deaktivieren: Feld entfernen
            if (fieldIndex >= 0)
                fields.RemoveAt(fieldIndex);
        }
        else
        {
            // Region aktivieren: Feld hinzufügen, falls es noch nicht existiert
            if (fieldIndex == -1)
                fields.Add(new EmbedFieldBuilder { Name = regionName, Value = "Initialisiere Server-Status...", IsInline = false });
        }

        
        builder.Fields = fields;
        await _currentMessage.ModifyAsync(msg => msg.Embed = builder.Build());

        // Server neu pingen, wenn aktiviert
        if (isActive)
        {
            await PingServers();
        }
    }


    private static void LoadRegionPingStatus()
    {
        if (!File.Exists(ConfigFilePath))
        {
            _regionPingStatus = _regions.ToDictionary(r => r.Name.ToLower(), _ => true);
            SaveRegionPingStatus();
        }
        else
        {
            var json = File.ReadAllText(ConfigFilePath);
            _regionPingStatus = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
        }
    }

    private static void SaveRegionPingStatus()
    {
        var json = JsonSerializer.Serialize(_regionPingStatus, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFilePath, json);
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
