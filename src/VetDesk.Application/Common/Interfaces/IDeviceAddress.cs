namespace VetDesk.Application.Common.Interfaces;

/// <summary>
/// The network address of the device a request came from, e.g. "192.168.1.23", in the browser setup. Null in the
/// desktop app, where every request comes from the PC itself.
/// </summary>
public interface IDeviceAddress
{
    string? Address { get; }
}
