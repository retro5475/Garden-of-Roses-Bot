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

namespace Blauer_Marlin.Properties
{
    public static class ServerPingManager
    {
        private static IUserMessage? _currentMessage;

        public static async Task PingServersAsync(DiscordSocketClient client, ulong guildId, bool forceNewEmbed = false)
        {
            try
            {
                Log.Information("Pinging servers...");
                var channel = await ChannelConfigManager.GetSavedChannelAsync(client, guildId);

                if (channel == null)
                {
                    Log.Error("No valid channel found. Please set the channel using /setchannel.");
                    return;
                }

                var embed = new EmbedBuilder()
                    .WithTitle("FFXIV Server Status")
                    .WithColor(Color.Blue)
                    .WithImageUrl("https://lds-img.finalfantasyxiv.com/h/e/2a9GxMb6zta1aHsi8u-Pw9zByc.jpg")
                    .WithTimestamp(DateTimeOffset.Now);

                Dictionary<string, bool> regionStatuses = LoadRegionStatuses();

                foreach (var region in RegionData.GetRegions()) // Ensure RegionData is correctly implemented
                {
                    if (!regionStatuses.TryGetValue(region.Name.ToLower(), out var isActive) || !isActive)
                    {
                        Log.Information($"Skipping region {region.Name} (inactive).");
                        continue;
                    }

                    embed.AddField(region.Name, await GetRegionPingTable(region), false);
                }

                await SendOrUpdateEmbed(channel, embed, forceNewEmbed);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error pinging servers.");
            }
        }

        private static Dictionary<string, bool> LoadRegionStatuses()
        {
            string configPath = "regionconfig.json"; // Ensure this path is correct

            if (!File.Exists(configPath))
            {
                Log.Warning($"Config file {configPath} not found. Defaulting to active for all regions.");
                return new Dictionary<string, bool>();
            }

            try
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<Dictionary<string, bool>>(json) ?? new Dictionary<string, bool>();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error reading the region status configuration file.");
                return new Dictionary<string, bool>();
            }
        }

        private static async Task<string> GetRegionPingTable(Region region)
        {
            string table = "```\nServer         | Ping (ms) | Loss | Status\n" +
                           "--------------|-----------|------|-------\n";

            foreach (var server in region.Servers)
            {
                (string status, string responseTime, string packetLoss, string statusEmoji) = await PingServerAsync(server.IP);
                table += $"{server.Name.PadRight(15)}| {responseTime.PadLeft(5)} ms  | {packetLoss.PadLeft(5)}| {status} {statusEmoji}\n";
            }

            return table + "```";
        }

        private static async Task<(string status, string responseTime, string packetLoss, string statusEmoji)> PingServerAsync(string serverIp)
        {
            int successfulPings = 0;
            int totalPings = 5;
            string packetLoss = "0%";

            try
            {
                for (int i = 0; i < totalPings; i++)
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(serverIp);

                    if (reply.Status == IPStatus.Success)
                        successfulPings++;
                }

                int loss = totalPings - successfulPings;
                packetLoss = loss > 0 ? $"{(loss * 100 / totalPings)}%" : "0%";

                using var lastPing = new Ping();
                var finalReply = await lastPing.SendPingAsync(serverIp);

                if (finalReply.Status == IPStatus.Success)
                    return ("Online", finalReply.RoundtripTime.ToString(), packetLoss, "🟢");
                else
                    return ("Offline", "N/A", packetLoss, "🔴");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Error pinging server {serverIp}.");
                return ("Error", "N/A", packetLoss, "⚪");
            }
        }

        private static async Task SendOrUpdateEmbed(ISocketMessageChannel channel, EmbedBuilder embed, bool forceNewEmbed)
        {
            if (forceNewEmbed || _currentMessage == null)
            {
                if (_currentMessage != null)
                {
                    try
                    {
                        Log.Information("Deleting the old message.");
                        await _currentMessage.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to delete the old message.");
                    }
                }

                try
                {
                    Log.Information("Sending a new embed message.");
                    _currentMessage = await channel.SendMessageAsync(embed: embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error sending the new embed message.");
                }
            }
            else
            {
                try
                {
                    Log.Information("Modifying the existing embed message.");
                    await _currentMessage.ModifyAsync(msg => msg.Embed = embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Error modifying the existing message. Sending a new one instead.");
                    try
                    {
                        _currentMessage = await channel.SendMessageAsync(embed: embed.Build());
                    }
                    catch (Exception retryEx)
                    {
                        Log.Error(retryEx, "Error sending the new embed message after modification failure.");
                    }
                }
            }
        }
    }
}
