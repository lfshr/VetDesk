using VetDesk.Application.Common.Interfaces;

namespace VetDesk.UI.Services.Live;

/// <summary>
/// This open copy of VetDesk: one per desktop window or browser tab, like <see cref="UserSession"/>. Its id tells this
/// desk's own saves and typing apart from other desks', and <see cref="Backend"/> copies it into each request's scope.
/// </summary>
public class Desk : IDesk
{
    public Guid Id { get; set; } = Guid.NewGuid();

    Guid? IDesk.DeskId => Id;

    /// <summary>
    /// False while the desk's connection to the server is down (its window closed, or the network dropped). The server
    /// keeps a dropped desk's screens for a few minutes in case it comes back, so without this a closed window would go
    /// on showing as "Heather (editing)" and holding the box she was typing in.
    /// </summary>
    public bool IsConnected { get; private set; } = true;

    /// <summary>Raised from the connection, not a screen's thread: handlers hop onto their own.</summary>
    public event Action? ConnectionChanged;

    public void SetConnected(bool connected)
    {
        if (connected == IsConnected)
            return;
        IsConnected = connected;
        ConnectionChanged?.Invoke();
    }
}
