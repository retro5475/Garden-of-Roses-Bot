using System.Collections.Generic;

public static class RegionData
{
    public static readonly List<RegionInfo> Regions = new()
    {
        new RegionInfo
        {
            Name = "USA",
            Servers = new List<ServerInfo>
            {
                new ServerInfo { Name = "Aether: LOGIN", IP = "204.2.29.80" }
            }
        },
        new RegionInfo
        {
            Name = "Europe",
            Servers = new List<ServerInfo>
            {
                new ServerInfo { Name = "🌼Chaos: LOGIN", IP = "80.239.145.6" },
                new ServerInfo { Name = "🌸Light: LOGIN", IP = "80.239.145.7" },
                new ServerInfo { Name = "🌸Light: Alpha", IP = "80.239.145.91" },
                new ServerInfo { Name = "🌸Light: Lich", IP = "80.239.145.92" },
                new ServerInfo { Name = "🌸Light: Odin", IP = "80.239.145.93" },
                new ServerInfo { Name = "🌸Light: Phönix", IP = "80.239.145.94" },
                new ServerInfo { Name = "🌸Light: Raiden", IP = "80.239.145.95" },
                new ServerInfo { Name = "🌸Light: Shiva", IP = "80.239.145.96" },
                new ServerInfo { Name = "🌸Light: Twin", IP = "80.239.145.97" }
            }
        },
        new RegionInfo
        {
            Name = "Japan",
            Servers = new List<ServerInfo>
            {
                // Add Japanese server IPs here when available
            }
        }
    };
}

public class ServerInfo
{
    public string? Name { get; set; }
    public string? IP { get; set; }
}

public class RegionInfo
{
    public string? Name { get; set; }
    public List<ServerInfo> Servers { get; set; } = new List<ServerInfo>();
}