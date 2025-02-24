using System.Text.Json;
using Discord.WebSocket;
using Serilog;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

public class ChannelConfigManager
{
    private static readonly string ConfigFilePath = "channelConfig.json";

    /// <summary>
    /// Retrieves the saved channel for a given guild.
    /// </summary>
    public static async Task<SocketTextChannel?> GetSavedChannelAsync(DiscordSocketClient client, ulong guildId)
    {
        var configData = await LoadChannelConfigAsync();
        if (configData.TryGetValue(guildId, out var channelId))
        {
            var channel = client.GetChannel(channelId) as SocketTextChannel;
            if (channel == null)
            {
                Log.Error($"The saved channel (ID: {channelId}) for Guild {guildId} could not be found.");
                return null;
            }
            return channel;
        }
        else
        {
            Log.Warning($"No saved channel ID found for Guild {guildId}.");
        }

        return null;
    }

    /// <summary>
    /// Loads the saved channel ID for a specific guild.
    /// If the guild does not have a saved channel, returns 0.
    /// </summary>
    public static async Task<ulong> LoadChannelIdForGuildAsync(ulong guildId)
    {
        var configData = await LoadChannelConfigAsync(); // Load the full config dictionary

        if (configData.TryGetValue(guildId, out var savedChannelId))
        {
            return savedChannelId; // Return the saved channel ID for this guild
        }
        
        Log.Warning($"No saved channel ID found for Guild {guildId}.");
        return 0; // Return 0 if no channel ID is saved
    }

    /// <summary>
    /// Loads the channel configuration from a JSON file.
    /// If the file does not exist, it creates a new one.
    /// </summary>
public static async Task<Dictionary<ulong, ulong>> LoadChannelConfigAsync()
{
    if (!File.Exists(ConfigFilePath))
    {
        Log.Warning($"Config file '{ConfigFilePath}' not found. Creating a new one with default values.");

        var defaultConfig = new Dictionary<ulong, ulong>(); // Empty dictionary

        try
        {
            var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(ConfigFilePath, json);
            Log.Information($"New channel configuration file created at {ConfigFilePath}.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error creating the channel configuration file.");
        }

        return defaultConfig; // Return the newly created empty dictionary
    }

    try
    {
        var json = await File.ReadAllTextAsync(ConfigFilePath);
        return JsonSerializer.Deserialize<Dictionary<ulong, ulong>>(json) ?? new Dictionary<ulong, ulong>();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Error reading the channel configuration file.");
        return new Dictionary<ulong, ulong>(); // Return empty if file is corrupted
    }
}


    /// <summary>
    /// Saves the given guild-channel mapping to the config file.
    /// </summary>
    public static async Task<bool> SaveChannelConfigAsync(ulong guildId, ulong channelId)
    {
        try
        {
            var configData = await LoadChannelConfigAsync();
            configData[guildId] = channelId; // Save or update entry

            await SaveChannelConfigAsync(configData);
            Log.Information($"Channel ID {channelId} successfully saved for Guild {guildId}.");
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error saving the channel configuration file.");
            return false;
        }
    }

    /// <summary>
    /// Saves the entire dictionary to the config file.
    /// </summary>
    private static async Task SaveChannelConfigAsync(Dictionary<ulong, ulong> configData)
    {
        try
        {
            var json = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(ConfigFilePath, json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error writing to the channel configuration file.");
        }
    }
}
