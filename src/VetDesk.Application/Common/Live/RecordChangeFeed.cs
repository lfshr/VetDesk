using Microsoft.Extensions.Logging;

namespace VetDesk.Application.Common.Live;

/// <summary>
/// Tells every open screen about each save, as it happens, so other desks see new appointments, payments, charges and
/// notes without reloading. Every desk is connected to the one VetDesk server (the desktop app is a window onto it), so
/// an in-memory feed reaches them all; Blazor then pushes the refreshed screens down each desk's SignalR connection.
/// </summary>
public sealed class RecordChangeFeed
{
    private readonly ILogger<RecordChangeFeed> _logger;

    public RecordChangeFeed(ILogger<RecordChangeFeed> logger)
    {
        _logger = logger;
    }

    public event Action<RecordChange>? Changed;

    public void Publish(RecordChange change)
    {
        if (change.Topics.Count == 0 || Changed is not { } handlers)
            return;

        // One screen failing to refresh mustn't stop the others hearing about the change, or fail the save that caused it.
        foreach (var handler in handlers.GetInvocationList().Cast<Action<RecordChange>>())
        {
            try
            {
                handler(change);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "A screen failed to handle a change to {Topics}", string.Join(", ", change.Topics));
            }
        }
    }
}
