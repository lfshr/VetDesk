using Microsoft.AspNetCore.Components.Server.Circuits;
using VetDesk.UI.Services;

namespace VetDesk.Web.Services;

/// <summary>
/// Records which device each browser tab is on, for the practice-wide limit on wrong PINs. Screens run inside the
/// tab's Blazor connection, which has no HttpContext of its own, so the address is taken when the connection opens
/// (and again when it reconnects, possibly from a new address). Behind Caddy this is the desk's address, because the
/// forwarded-headers middleware has already applied X-Forwarded-For.
/// </summary>
public sealed class DeviceAddressCircuitHandler : CircuitHandler
{
    private readonly IHttpContextAccessor _http;
    private readonly DeviceAddress _device;

    public DeviceAddressCircuitHandler(IHttpContextAccessor http, DeviceAddress device)
    {
        _http = http;
        _device = device;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        var ip = _http.HttpContext?.Connection.RemoteIpAddress;
        _device.Address = (ip is { IsIPv4MappedToIPv6: true } ? ip.MapToIPv4() : ip)?.ToString();
        return Task.CompletedTask;
    }
}
