namespace VetDesk.Application.Common.Interfaces;

/// <summary>
/// Which open copy of VetDesk (a desktop window or browser tab) a request came from, so a screen can tell its own saves
/// from another desk's. Null when it isn't known, as in the handler tests.
/// </summary>
public interface IDesk
{
    Guid? DeskId { get; }
}
