namespace VetDesk.Application.Common.Interfaces;

/// <summary>
/// The network address of the device a request came from, e.g. "192.168.1.23". Null when it isn't known, as in the
/// handler tests.
/// </summary>
public interface IDeviceAddress
{
    string? Address { get; }
}
