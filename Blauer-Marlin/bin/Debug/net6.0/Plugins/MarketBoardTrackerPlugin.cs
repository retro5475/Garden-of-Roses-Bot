using Discord.WebSocket;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

public class MarketBoardTrackerPlugin
{
    private readonly DiscordSocketClient _client;
    private readonly ConcurrentDictionary<ulong, List<TrackedItem>> _trackedItems = new();
    private readonly HttpClient _httpClient = new();
    private readonly string _universalisApi = "https://universalis.app/api/v2/"; // Universalis API for FFXIV market data

    public MarketBoardTrackerPlugin(DiscordSocketClient client)
    {
        _client = client;
        _client.MessageReceived += OnMessageReceivedAsync;
    }

    private async Task OnMessageReceivedAsync(SocketMessage message)
    {
        if (message.Author.IsBot) return;
        var content = message.Content.ToLower();

        if (content.StartsWith("!trackitem"))
        {
            await HandleTrackItemCommand(message);
        }
        else if (content.StartsWith("!untrackitem"))
        {
            await HandleUntrackItemCommand(message);
        }
        else if (content.StartsWith("!mytrackeditems"))
        {
            await HandleListTrackedItemsCommand(message);
        }
    }

    private async Task HandleTrackItemCommand(SocketMessage message)
    {
        var parts = message.Content.Split(' ', 3);
        if (parts.Length < 3 || !int.TryParse(parts[2], out int price))
        {
            await message.Channel.SendMessageAsync("Usage: !trackitem <item name> <desired price>");
            return;
        }

        var itemName = parts[1];
        var userId = message.Author.Id;

        if (!_trackedItems.ContainsKey(userId))
        {
            _trackedItems[userId] = new List<TrackedItem>();
        }

        _trackedItems[userId].Add(new TrackedItem(itemName, price));
        await message.Channel.SendMessageAsync($"Tracking {itemName} at {price} gil.");
    }

    private async Task HandleUntrackItemCommand(SocketMessage message)
    {
        var parts = message.Content.Split(' ', 2);
        if (parts.Length < 2)
        {
            await message.Channel.SendMessageAsync("Usage: !untrackitem <item name>");
            return;
        }

        var itemName = parts[1];
        var userId = message.Author.Id;

        if (_trackedItems.TryGetValue(userId, out var items))
        {
            items.RemoveAll(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            await message.Channel.SendMessageAsync($"Stopped tracking {itemName}.");
        }
        else
        {
            await message.Channel.SendMessageAsync("You are not tracking any items.");
        }
    }

    private async Task HandleListTrackedItemsCommand(SocketMessage message)
    {
        var userId = message.Author.Id;

        if (_trackedItems.TryGetValue(userId, out var items) && items.Count > 0)
        {
            var itemList = string.Join("\n", items.Select(i => $"{i.Name} - {i.Price} gil"));
            await message.Channel.SendMessageAsync($"You are tracking:\n{itemList}");
        }
        else
        {
            await message.Channel.SendMessageAsync("You are not tracking any items.");
        }
    }

    public async Task CheckMarketBoardAsync()
    {
        foreach (var (userId, items) in _trackedItems)
        {
            foreach (var item in items)
            {
                try
                {
                    var response = await _httpClient.GetStringAsync($"{_universalisApi}{Uri.EscapeDataString(item.Name)}");
                    var marketData = JsonSerializer.Deserialize<MarketData>(response);

                    if (marketData?.Listings != null && marketData.Listings.Any())
                    {
                        var lowestPrice = marketData.Listings.Min(l => l.PricePerUnit);
                        if (lowestPrice <= item.Price)
                        {
                            var user = _client.GetUser(userId);
                            if (user != null)
                            {
                                await user.SendMessageAsync($"{item.Name} is now available for {lowestPrice} gil or less!");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching market data for {item.Name}: {ex.Message}");
                }
            }
        }
    }
}

public class TrackedItem
{
    public string Name { get; }
    public int Price { get; }

    public TrackedItem(string name, int price)
    {
        Name = name;
        Price = price;
    }
}

public class MarketData
{
    public List<Listing> Listings { get; set; } = new();
}

public class Listing
{
    public int PricePerUnit { get; set; }
}
