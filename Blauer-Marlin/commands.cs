using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Serilog;

internal class commands //! maybe make this static in future, to prevent memorybreaks!
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
                new SlashCommandBuilder().WithName("reload").WithDescription("Reloads the server status."),
                new SlashCommandBuilder().WithName("europe").WithDescription("Toggles Europe server ping."),
                new SlashCommandBuilder().WithName("usa").WithDescription("Toggles North America server ping."),
                new SlashCommandBuilder().WithName("japan").WithDescription("Toggles Japan server ping."),
                new SlashCommandBuilder().WithName("ban").WithDescription("Bans a user.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to ban.", isRequired: true),
                new SlashCommandBuilder().WithName("unban").WithDescription("Unbans a user.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to unban.", isRequired: true),
                new SlashCommandBuilder().WithName("kick").WithDescription("Kicks a user.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to kick.", isRequired: true),
                new SlashCommandBuilder().WithName("mute").WithDescription("Mutes a user.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to mute.", isRequired: true),
                new SlashCommandBuilder().WithName("unmute").WithDescription("Unmutes a user.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to unmute.", isRequired: true),
                new SlashCommandBuilder().WithName("warn").WithDescription("Warns a user.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to warn.", isRequired: true),
                new SlashCommandBuilder().WithName("clearwarns").WithDescription("Clears warnings for a user.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to clear warnings for.", isRequired: true),
                new SlashCommandBuilder().WithName("userinfo").WithDescription("Displays user information.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to display information for.", isRequired: true),
                new SlashCommandBuilder().WithName("setnickname").WithDescription("Sets a user's nickname.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to set the nickname for.", isRequired: true)
                    .AddOption("nickname", ApplicationCommandOptionType.String, "The new nickname.", isRequired: true),
                new SlashCommandBuilder().WithName("lockdown").WithDescription("Locks the channel temporarily."),
                new SlashCommandBuilder().WithName("unlock").WithDescription("Unlocks the channel."),
                new SlashCommandBuilder().WithName("addrole").WithDescription("Adds a role to a user.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to add the role to.", isRequired: true)
                    .AddOption("role", ApplicationCommandOptionType.Role, "The role to add.", isRequired: true),
                new SlashCommandBuilder().WithName("removerole").WithDescription("Removes a role from a user.")
                    .AddOption("user", ApplicationCommandOptionType.User, "The user to remove the role from.", isRequired: true)
                    .AddOption("role", ApplicationCommandOptionType.Role, "The role to remove.", isRequired: true),
                new SlashCommandBuilder().WithName("mutechannel").WithDescription("Mutes a channel."),
                new SlashCommandBuilder().WithName("unmutechannel").WithDescription("Unmutes a channel."),
                new SlashCommandBuilder().WithName("slowmode").WithDescription("Sets a slow mode on a channel.")
                    .AddOption("duration", ApplicationCommandOptionType.Integer, "The duration for slow mode (in seconds).", isRequired: true),
                new SlashCommandBuilder().WithName("announce").WithDescription("Sends an announcement.")
                    .AddOption("message", ApplicationCommandOptionType.String, "The message to announce.", isRequired: true),
                new SlashCommandBuilder().WithName("setprefix").WithDescription("Sets a custom prefix.")
                    .AddOption("prefix", ApplicationCommandOptionType.String, "The new prefix.", isRequired: true),
                new SlashCommandBuilder().WithName("clear").WithDescription("Clears messages in a channel.")
                    .AddOption("amount", ApplicationCommandOptionType.Integer, "The number of messages to clear.", isRequired: true),
                new SlashCommandBuilder().WithName("poll").WithDescription("Creates a poll.")
                    .AddOption("question", ApplicationCommandOptionType.String, "The poll question.", isRequired: true)
                    .AddOption("options", ApplicationCommandOptionType.String, "Comma-separated options for the poll.", isRequired: true)
            };

            Log.Information("Commands to be registered:");
            foreach (var command in commands)
            {
                Log.Information($"- {command.Name}: {command.Description}");
            }

            ulong guildId = 1318269917769764884;
            foreach (var command in commands)
            {
                var commandProperties = command.Build();
                await client.Rest.CreateGuildCommand(commandProperties, guildId);
                Log.Information($"Command {command.Name} registered.");
            }

            Log.Information("Slash commands registered successfully for the guild!");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error registering slash commands.");
        }
    }
}
