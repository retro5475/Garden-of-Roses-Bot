using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Serilog;

internal static class commands //! Now static to prevent memory leaks
{
    public static async Task RegisterSlashCommands(DiscordSocketClient client)
    {
        try
        {
            Log.Information("Registering global slash commands...");

            if (client == null)
            {
                Log.Error("Error: client is null. Ensure the bot is started first.");
                return;
            }

            var commands = new List<SlashCommandBuilder>
            {
                new SlashCommandBuilder().WithName("help").WithDescription("Shows a list of available commands."),
                new SlashCommandBuilder()
                    .WithName("setchannel")
                    .WithDescription("Wähle einen Textkanal für Nachrichten")
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("channel")
                        .WithDescription("Wähle einen Textkanal")
                        .WithRequired(true)
                        .WithType(ApplicationCommandOptionType.Channel)),

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
                new SlashCommandBuilder()
                .WithName("warn")
                .WithDescription("Warns a user and sends a DM with details.")
                .AddOption("user", ApplicationCommandOptionType.User, "The user to warn.", isRequired: true)
                .AddOption("reason", ApplicationCommandOptionType.String, "The reason for the warning.", isRequired: false),

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

                new SlashCommandBuilder().WithName("slowmode").WithDescription("Sets a slow mode on a channel.")
                    .AddOption("duration", ApplicationCommandOptionType.Integer, "The duration for slow mode (in seconds).", isRequired: true),
               new SlashCommandBuilder()
              .WithName("announce")
              .WithDescription("Sends an announcement to a chosen channel.")
              .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel to send the announcement in.", isRequired: true)
              .AddOption("message", ApplicationCommandOptionType.String, "The message to announce.", isRequired: true),

                new SlashCommandBuilder().WithName("setprefix").WithDescription("Sets a custom prefix.")
                    .AddOption("prefix", ApplicationCommandOptionType.String, "The new prefix.", isRequired: true),
                new SlashCommandBuilder().WithName("clear").WithDescription("Clears messages in a channel.")
                    .AddOption("amount", ApplicationCommandOptionType.Integer, "The number of messages to clear.", isRequired: true),
            };

            Log.Information("Commands to be registered globally:");
            foreach (var command in commands)
            {
                Log.Information($"- {command.Name}: {command.Description}");
            }

            // Register as global commands, since bot is public now
            await client.Rest.BulkOverwriteGlobalCommands(commands.Select(cmd => cmd.Build()).ToArray());


            Log.Information("Slash commands registered successfully for all servers!");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error registering global slash commands.");
        }
    }
}
