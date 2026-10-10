using VetDesk.Application.Common.Interfaces;

namespace VetDesk.Infrastructure.Services;

/// <summary>The <see cref="IDevicePinLimit"/> for the device this request came from. No limit without an address (desktop app).</summary>
public class DevicePinLimit : IDevicePinLimit
{
    private readonly IDeviceAddress _device;
    private readonly WrongPinTracker _tracker;

    public DevicePinLimit(IDeviceAddress device, WrongPinTracker tracker)
    {
        _device = device;
        _tracker = tracker;
    }

    public bool IsBlocked(DateTime nowUtc) => _device.Address is { } address && _tracker.IsBlocked(address, nowUtc);

    public void RecordWrongPin(DateTime nowUtc)
    {
        if (_device.Address is { } address)
            _tracker.RecordWrongPin(address, nowUtc);
    }
}
