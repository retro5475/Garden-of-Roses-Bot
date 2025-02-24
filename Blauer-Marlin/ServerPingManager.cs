using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Serilog;

public static class ServerPingManager
{
    private static readonly Dictionary<ulong, IUserMessage?> _currentMessages = new();
    
    private static string GetConfigPath(ulong guildId) => $"regionPingStatus_{guildId}.json";

    /// <summary>
    /// Pings servers and updates the embed message for each guild.
    /// </summary>
    public static async Task PingServersAsync(DiscordSocketClient client, ulong guildId, bool forceNewEmbed = false)
    {
        try
        {
            Log.Information($"Pinging servers for guild {guildId}...");
            var channel = await ChannelConfigManager.GetSavedChannelAsync(client, guildId);

            if (channel == null)
            {
                Log.Error($"No valid channel found for guild {guildId}. Please set the channel using /setchannel.");
                return;
            }

            var embed = new EmbedBuilder()
                .WithTitle("FFXIV Server Status")
                .WithColor(Color.Blue)
                .WithImageUrl("https://lds-img.finalfantasyxiv.com/h/e/2a9GxMb6zta1aHsi8u-Pw9zByc.jpg")
                .WithTimestamp(DateTimeOffset.Now);

            var regionStatuses = LoadRegionStatuses(guildId);

            foreach (var region in RegionData.Regions)
            {
                if (!regionStatuses.TryGetValue(region.Name.ToLower(), out var isActive) || !isActive)
                {
                    Log.Information($"Skipping region {region.Name} (inactive) for guild {guildId}.");
                    continue;
                }

                string regionTable = await GetRegionPingTable(region);
                if (!string.IsNullOrWhiteSpace(regionTable))
                {
                    embed.AddField(region.Name, regionTable, false);
                }
                else
                {
                    Log.Warning($"No server data available for {region.Name} in guild {guildId}.");
                }
            }

            await SendOrUpdateEmbed(channel, guildId, embed, forceNewEmbed);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error pinging servers for guild {guildId}.");
        }
    }

    /// <summary>
    /// Loads or creates the region ping status configuration file for a specific guild.
    /// </summary>
    private static Dictionary<string, bool> LoadRegionStatuses(ulong guildId)
    {
        string configPath = GetConfigPath(guildId);

        if (!File.Exists(configPath))
        {
            Log.Warning($"Config file '{configPath}' not found. Creating a new one with default values.");

            var defaultStatuses = RegionData.Regions.ToDictionary(region => region.Name.ToLower(), _ => true);

            try
            {
                var json = JsonSerializer.Serialize(defaultStatuses, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(configPath, json);
                Log.Information($"New region status configuration file created at {configPath}.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error creating the region status configuration file for guild {guildId}.");
            }

            return defaultStatuses;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error reading the region status configuration file for guild {guildId}.");
            return new Dictionary<string, bool>();
        }
    }

    /// <summary>
    /// Sends or updates the embed message with server status for a specific guild.
    /// </summary>
    private static async Task SendOrUpdateEmbed(ISocketMessageChannel channel, ulong guildId, EmbedBuilder embed, bool forceNewEmbed)
    {
        try
        {
            if (!_currentMessages.ContainsKey(guildId) || forceNewEmbed || _currentMessages[guildId] == null)
            {
                if (_currentMessages.ContainsKey(guildId) && _currentMessages[guildId] != null)
                {
                    try
                    {
                        Log.Information($"Deleting the old message for guild {guildId}.");
                        await _currentMessages[guildId]!.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, $"Failed to delete the old message for guild {guildId}.");
                    }
                }

                Log.Information($"Sending a new embed message for guild {guildId}.");
                _currentMessages[guildId] = await channel.SendMessageAsync(embed: embed.Build());
            }
            else
            {
                Log.Information($"Modifying the existing embed message for guild {guildId}.");
                await _currentMessages[guildId]!.ModifyAsync(msg => msg.Embed = embed.Build());
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Error updating the embed message for guild {guildId}.");
            try
            {
                Log.Information($"Retrying by sending a new embed message for guild {guildId}.");
                _currentMessages[guildId] = await channel.SendMessageAsync(embed: embed.Build());
            }
            catch (Exception retryEx)
            {
                Log.Error(retryEx, $"Error sending the new embed message after modification failure for guild {guildId}.");
            }
        }
    }
}
