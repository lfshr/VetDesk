using Microsoft.Extensions.Logging;

namespace VetDesk.UI.Services.Live;

/// <summary>
/// The shared drafts of every form open at any desk, one per record. The first desk to open a record's form starts its
/// draft from the saved record; the next ones join it, seeing anything already typed and not yet saved. The draft goes
/// when the last desk closes the form.
/// </summary>
public sealed class LiveDrafts
{
    /// <summary>How long a field stays someone's after their last keystroke, if they don't move on first.</summary>
    public static readonly TimeSpan HoldFor = TimeSpan.FromSeconds(4);

    private readonly Dictionary<string, LiveDraft> _drafts = [];
    private readonly ILogger<LiveDrafts> _logger;
    private readonly TimeProvider _time;

    public LiveDrafts(ILogger<LiveDrafts> logger, TimeProvider? time = null)
    {
        _logger = logger;
        _time = time ?? TimeProvider.System;
    }

    internal DateTime UtcNow() => _time.GetUtcNow().UtcDateTime;

    /// <param name="topic">The record and form, e.g. "Consultation:40".</param>
    /// <param name="saved">The saved record's values, used only if nobody has the form open yet.</param>
    public LiveDraft Join(string topic, IReadOnlyDictionary<string, string?> saved, Guid version)
    {
        lock (_drafts)
        {
            if (!_drafts.TryGetValue(topic, out var draft))
                _drafts[topic] = draft = new LiveDraft(this, topic, saved, version);
            draft.Users++;
            return draft;
        }
    }

    public void Leave(LiveDraft draft)
    {
        lock (_drafts)
        {
            if (--draft.Users <= 0 && _drafts.GetValueOrDefault(draft.Topic) == draft)
                _drafts.Remove(draft.Topic);
        }
    }

    internal void ReleaseLater(LiveDraft draft, string field)
    {
        _ = ReleaseLaterAsync();

        async Task ReleaseLaterAsync()
        {
            // A little after the hold runs out, so the check sees it expired.
            await Task.Delay(HoldFor + TimeSpan.FromMilliseconds(100), _time);
            draft.Expire(field);
        }
    }

    internal void Failed(LiveDraft draft, Exception ex) =>
        _logger.LogError(ex, "A screen failed to handle a change to the draft of {Topic}", draft.Topic);
}
