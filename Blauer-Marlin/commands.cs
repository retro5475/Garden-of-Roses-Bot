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
    new SlashCommandBuilder()
        .WithName("setnickname")
        .WithDescription("Sets a user's nickname.")
        .AddOption("user", ApplicationCommandOptionType.User, "The user to set the nickname for.", isRequired: true) // Allows selecting a user
        .AddOption("nickname", ApplicationCommandOptionType.String, "The new nickname.", isRequired: true), // Input for the new nickname
    new SlashCommandBuilder()
        .WithName("warn")
        .WithDescription("Warn a user")
        .AddOption("user", ApplicationCommandOptionType.User, "The name of the user to warn", isRequired: true),

    new SlashCommandBuilder()
        .WithName("ban")  // Ensure 'ban' is lowercase
        .WithDescription("Ban a user")
        .AddOption("user", ApplicationCommandOptionType.User, "The name of the user to ban", isRequired: true),  // 'user' should be lowercase
};
            if (commands == null || commands.Count == 0)
            {
                Log.Error("Error: No commands to register.");
                return;
            }

            // Log all commands being registered
            Log.Information("Commands to be registered:");
            foreach (var command in commands)
            {
                Log.Information($"- {command.Name}: {command.Description}");
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

