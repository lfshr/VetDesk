using System.Net;

namespace VetDesk.Web.Services;

/// <summary>
/// Refuses requests from outside the practice's own network, as a second check behind the firewall.
/// "VetDesk:AllowedNetworks" lists the ranges, comma-separated, e.g. "192.168.1.0/24". When it is empty every
/// address is allowed (development, or a server not yet set up), and a warning says so at start-up.
/// </summary>
public sealed class NetworkAllowList
{
    private readonly IReadOnlyList<IPNetwork> _networks;

    private NetworkAllowList(IReadOnlyList<IPNetwork> networks)
    {
        _networks = networks;
    }

    public bool IsEmpty => _networks.Count == 0;

    /// <summary>Reads the setting. A range that doesn't parse stops start-up, rather than silently letting everyone in.</summary>
    public static NetworkAllowList FromSetting(string? setting)
    {
        var networks = new List<IPNetwork>();
        foreach (var entry in (setting ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!IPNetwork.TryParse(entry, out var network))
                throw new InvalidOperationException($"VetDesk:AllowedNetworks has '{entry}', which isn't a network range like 192.168.1.0/24.");
            networks.Add(network);
        }

        return new NetworkAllowList(networks);
    }

    public bool Allows(IPAddress? address)
    {
        if (IsEmpty)
            return true;
        if (address is null)
            return false;
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        return _networks.Any(network => network.Contains(address));
    }
}
