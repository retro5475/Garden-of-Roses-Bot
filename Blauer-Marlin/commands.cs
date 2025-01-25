using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Serilog;

internal class commands
{
    public static async Task RegisterSlashCommands(DiscordSocketClient client)
    {
        try
        {
            Log.Information("Registering slash commands...");

            if (client == null)
            {
                Log.Error("Error: client is null. Ensure the bot is started first.");
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
                    await client.Rest.CreateGuildCommand(commandProperties, guildId);
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
}
