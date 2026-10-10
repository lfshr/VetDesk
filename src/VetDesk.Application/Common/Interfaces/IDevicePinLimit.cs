namespace VetDesk.Application.Common.Interfaces;

/// <summary>
/// A limit on wrong PINs from one device, whoever they were entered for. Each person's own lock-out stops someone
/// guessing one PIN, but not trying a few PINs against every name in turn; this does.
/// </summary>
public interface IDevicePinLimit
{
    bool IsBlocked(DateTime nowUtc);

    void RecordWrongPin(DateTime nowUtc);
}
