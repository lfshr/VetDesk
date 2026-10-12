using Microsoft.AspNetCore.Components.Server.Circuits;
using VetDesk.UI.Services.Live;

namespace VetDesk.Web.Services;

/// <summary>
/// Tells the desk when its browser tab or desktop window loses or regains its connection, so the other desks stop
/// showing it as having records open (and stop keeping boxes read-only for it) as soon as it goes.
/// </summary>
public sealed class DeskConnectionCircuitHandler : CircuitHandler
{
    private readonly Desk _desk;

    public DeskConnectionCircuitHandler(Desk desk)
    {
        _desk = desk;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _desk.SetConnected(true);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken cancellationToken)
    {
        _desk.SetConnected(false);
        return Task.CompletedTask;
    }
}
