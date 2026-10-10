using VetDesk.Application.Common.Interfaces;

namespace VetDesk.UI.Services;

/// <summary>
/// The <see cref="IDeviceAddress"/> handlers see. The web host sets it for each browser tab when it connects, and
/// <see cref="Backend"/> copies it into each request's scope, as it does the signed-in person. The desktop app
/// leaves it empty.
/// </summary>
public class DeviceAddress : IDeviceAddress
{
    public string? Address { get; set; }
}
