using Discord;
using Discord.WebSocket;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class StatusManager
{
    private static readonly List<string> Statuses = new()
    {
        "Clearing Ultimates (in my dreams)",
        "Wiping to mechanics since 2013",
        "Waiting for Duty Finder to pop...",
        "ERPing in Limsa—just kidding (or am I?)",
        "Rolling Need on everything",
        "Savage prog: 0.1% wipe",
        "Trying to dodge AOEs... unsuccessfully",
        "Glamour is the true endgame",
        "Still can't clear P12S...",
        "Housing Savage, 10 minutes remain!",
        "Tank privilege activated",
        "Looking for a static... again",
        "One more Treasure Map... (copium)",
        "Mentor status but doesn't know mechanics",
        "Hunting A-rank marks like a madman",
        "Fishing in the Diadem for no reason",
        "Getting kicked from Party Finder",
        "Selling fake Ultimate carries",
        "Waiting for Island Sanctuary updates",
        "Relic grind pain intensifies",
        "Looking for FC buffs (again)",
        "Checking Market Board profits",
        "Running Expert Roulette for tomes",
        "AFK in Limsa like a true adventurer",
        "Pulling before the tank (oops)",
        "Spamming 'Dodge!' in chat",
        "My retainer made more gil than me",
        "Farming minions like a true collector",
        "Raiding with 200 ping, send help",
        "I am once again asking for healer LB3",
        "Glamour dresser is full (again)",
        "My relic weapon isn't glowing enough",
        "Aggro? Never heard of it",
        "Why do I have 200 unread hunt linkshell messages?",
        "Learning mechanics AFTER the pull",
        "Still mad about my 98 losing to a 99",
        "DPS parsing in casual content",
        "I just wanted to craft in peace...",
        "Tank forgot tank stance again",
        "Selling my soul for gil",
        "When in doubt, blame the healer",
        "Watching my co-healer 'DPS only'",
        "My parse was orange, I swear!",
        "What is this, a wipe?",
        "Waiting for my FC to log in",
        "Avoiding main scenario roulette",
        "Got kicked for watching cutscenes",
        "Playing 'dodge the AoE'... and failing",
        "Still trying to find a house plot",
        "Please, just let me buy a medium house",
        "Dying to Wall Boss in The Vault",
        "Titan, we meet again...",
        "Who needs a healer when you have pots?",
        "Oh no, I pulled the boss early",
        "Wiping to Cape Westwind (somehow)",
        "Where is my V&C dungeon portal?!",
        "I should be studying, but FFXIV",
        "Stuck in Gold in CC forever",
        "Waiting for my Raid Leader to come back",
        "Just one more dungeon, I swear...",
        "Why is this crafting macro so slow?",
        "Roulettes, roulettes, roulettes...",
        "Dying because I forgot Sprint exists",
        "My chocobo is tanking better than me",
        "I just queued for Castrum. Send help.",
        "This boss music is fire though",
        "Why does my tank have 5 vulnerability stacks?",
        "Main tanking in leveling roulette = pain",
        "I rolled a 1. Again.",
        "Another party finder disaster...",
        "Please, stop standing in bad",
        "My static is arguing over loot again",
        "This Duty Finder queue is taking years",
        "Is it a DPS check, or a skill issue?",
        "Getting carried through alliance raids",
        "Lost all my gil on the Jumbo Cactpot",
        "Pulling extra mobs... sorry healer!",
        "Another 'Oops! All Tanks' party",
        "Healers adjust!",
        "That was the pull timer, not the countdown!",
        "Taking my mandatory post-wipe stretch",
        "Tanking without tank stance? Bold.",
        "Oh look, my co-healer is AFK...",
        "I know the mechanics, I swear!",
        "This is a stack marker, right?",
        "I can solo this boss (famous last words)",
        "Limit Break 3 or wipe? Decisions...",
        "So... about that enrage timer...",
        "I forgot to repair before the raid",
        "30 minutes in and no healer LB3...",
        "Still stuck on the Nier raid...",
        "Mechs are easier when dead, right?",
        "When in doubt, blame the ping",
        "This PF group is actually good?!",
        "Dying to Leviathan knockback (again)",
        "Trusts don't judge my mistakes...",
        "Did someone say 'one last pull'?",
        "PVP for the wolf marks, not the wins",
        "I love my static (most of the time)",
        "Pressing buttons and hoping for the best",
        "Why does my DPS keep running away?",
        "That was totally lag, I swear",
        "My retainers are richer than me",
        "One more fate for my relic...",
        "So this is how we farm tomestones",
        "Expert roulette? More like pain roulette",
        "Help, I'm stuck in Haurchefant fan club",
        "I have 5 crafts at 90, and I still buy HQ mats",
        "Does anyone even know how to do this mechanic?",
        "Waiting for 8.XX content like...",
        "Still need my Triple Triad mount...",
        "Roulettes first, responsibilities later",
        "Do I *really* need another mount?",
        "When does the expansion drop again?!"
    };

    private static readonly Random Random = new();
    private static CancellationTokenSource? _cancellationTokenSource;

    public static void StartRotatingStatus(DiscordSocketClient client)
    {
        if (_cancellationTokenSource != null)
        {
            Log.Warning("Status rotation is already running.");
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _ = RotateStatusesAsync(client, _cancellationTokenSource.Token);
    }

    public static void StopRotatingStatus()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = null;
        Log.Information("Status rotation stopped.");
    }

    private static async Task RotateStatusesAsync(DiscordSocketClient client, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var randomStatus = Statuses[Random.Next(Statuses.Count)];
                await client.SetGameAsync(randomStatus);
                await Task.Delay(TimeSpan.FromMinutes(2), cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            Log.Information("Status rotation task canceled.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in status rotation task.");
        }
    }
}
