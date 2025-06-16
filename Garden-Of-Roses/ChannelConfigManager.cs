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
        Log.Information($"Fetching saved channel for Guild {guildId}...");

        var configData = await LoadChannelConfigAsync();
        if (configData.TryGetValue(guildId, out var channelId))
        {
            var channel = client.GetChannel(channelId) as SocketTextChannel;
            if (channel == null)
            {
                Log.Error($"The saved channel (ID: {channelId}) for Guild {guildId} could not be found.");
                return null;
            }
            Log.Information($"Successfully retrieved saved channel: {channel.Name} (ID: {channelId}) for Guild {guildId}.");
            return channel;
        }

        Log.Warning($"No saved channel ID found for Guild {guildId}.");
        return null;
    }

    /// <summary>
    /// Loads the saved channel ID for a specific guild.
    /// If the guild does not have a saved channel, returns 0.
    /// </summary>
    public static async Task<ulong> LoadChannelIdForGuildAsync(ulong guildId)
    {
        Log.Information($"Loading channel ID for Guild {guildId}...");

        var configData = await LoadChannelConfigAsync();
        if (configData.TryGetValue(guildId, out var savedChannelId))
        {
            Log.Information($"Found saved channel ID {savedChannelId} for Guild {guildId}.");
            return savedChannelId;
        }

        Log.Warning($"No saved channel ID found for Guild {guildId}. Returning 0.");
        return 0; // Return 0 if no channel ID is saved
    }

    /// <summary>
    /// Loads the channel configuration from a JSON file.
    /// If the file does not exist, it creates a new one.
    /// </summary>
    public static async Task<Dictionary<ulong, ulong>> LoadChannelConfigAsync()
    {
        Log.Information("Loading channel configuration...");

        if (!File.Exists(ConfigFilePath))
        {
            Log.Warning($"Config file '{ConfigFilePath}' not found. Creating a new one with default values.");
            return await CreateDefaultConfigAsync();
        }

        try
        {
            var json = await File.ReadAllTextAsync(ConfigFilePath);
            var configData = JsonSerializer.Deserialize<Dictionary<ulong, ulong>>(json);

            if (configData == null)
            {
                Log.Warning($"Config file '{ConfigFilePath}' was empty or invalid. Creating a new default file.");
                return await CreateDefaultConfigAsync();
            }

            Log.Information("Successfully loaded channel configuration.");
            return configData;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error reading the channel configuration file. Creating a new default file.");
            return await CreateDefaultConfigAsync();
        }
    }

    /// <summary>
    /// Creates a default empty configuration file.
    /// </summary>
    private static async Task<Dictionary<ulong, ulong>> CreateDefaultConfigAsync()
    {
        var defaultConfig = new Dictionary<ulong, ulong>();

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

        return defaultConfig;
    }

    /// <summary>
    /// Saves the given guild-channel mapping to the config file.
    /// </summary>
    public static async Task<bool> SaveChannelConfigAsync(ulong guildId, ulong channelId)
    {
        Log.Information($"Saving channel ID {channelId} for Guild {guildId}...");

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
            Log.Error(ex, $"Error saving channel ID {channelId} for Guild {guildId}.");
            return false;
        }
    }

    /// <summary>
    /// Saves the entire dictionary to the config file.
    /// </summary>
    private static async Task SaveChannelConfigAsync(Dictionary<ulong, ulong> configData)
    {
        Log.Information("Saving channel configuration to file...");

        try
        {
            var json = JsonSerializer.Serialize(configData, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(ConfigFilePath, json);
            Log.Information("Channel configuration successfully saved.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error writing to the channel configuration file.");
        }
    }
}
