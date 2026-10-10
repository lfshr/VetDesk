using Microsoft.Extensions.Logging;

namespace VetDesk.Infrastructure.Services;

/// <summary>
/// Counts wrong PINs per device address across the whole practice. <see cref="MaxWrongPins"/> wrong PINs within
/// <see cref="Window"/> block that address for <see cref="BlockFor"/>. Kept in memory: there is one server, and a
/// restart clearing the count is acceptable.
/// </summary>
public sealed class WrongPinTracker
{
    public const int MaxWrongPins = 20;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan BlockFor = TimeSpan.FromMinutes(15);

    private readonly Dictionary<string, Device> _devices = new(StringComparer.Ordinal);
    private readonly Lock _lock = new();
    private readonly ILogger<WrongPinTracker> _logger;

    public WrongPinTracker(ILogger<WrongPinTracker> logger)
    {
        _logger = logger;
    }

    public bool IsBlocked(string address, DateTime nowUtc)
    {
        lock (_lock)
            return _devices.TryGetValue(address, out var device) && device.BlockedUntilUtc > nowUtc;
    }

    public void RecordWrongPin(string address, DateTime nowUtc)
    {
        lock (_lock)
        {
            Forget(nowUtc);
            if (!_devices.TryGetValue(address, out var device))
                _devices[address] = device = new Device();

            device.WrongPinsUtc.Enqueue(nowUtc);
            if (device.WrongPinsUtc.Count < MaxWrongPins)
                return;

            device.BlockedUntilUtc = nowUtc + BlockFor;
            device.WrongPinsUtc.Clear();
            _logger.LogWarning("{Count} wrong PINs from {Address} within {Window}: blocked PIN entry from it until {Until:u}.",
                MaxWrongPins, address, Window, device.BlockedUntilUtc);
        }
    }

    /// <summary>Drops wrong PINs older than the window, and devices with nothing left to remember, so memory stays small.</summary>
    private void Forget(DateTime nowUtc)
    {
        foreach (var (address, device) in _devices)
        {
            while (device.WrongPinsUtc.TryPeek(out var first) && first <= nowUtc - Window)
                device.WrongPinsUtc.Dequeue();
            if (device.WrongPinsUtc.Count == 0 && !(device.BlockedUntilUtc > nowUtc))
                _devices.Remove(address);
        }
    }

    private sealed class Device
    {
        public Queue<DateTime> WrongPinsUtc { get; } = new();
        public DateTime? BlockedUntilUtc { get; set; }
    }
}
