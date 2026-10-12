namespace VetDesk.UI.Services.Live;

public enum LiveDraftEventKind
{
    /// <summary>Someone typed into a field; <see cref="LiveDraftEvent.Field"/> says which.</summary>
    Typed,

    /// <summary>A field was let go of (the person moved on, or stopped typing for a while).</summary>
    Released,

    /// <summary>The draft was saved to the record.</summary>
    Saved,

    /// <summary>Someone started or stopped finishing the record (e.g. completing a consultation): nobody else may type.</summary>
    Frozen,

    /// <summary>The record changed some other way and the draft took the new values.</summary>
    Reset,
}

public sealed record LiveDraftEvent(LiveDraftEventKind Kind, Guid Desk, string Name, string? Field = null);

/// <summary>
/// The shared, unsaved state of one record's form, which every desk with that form open types into. Each keystroke
/// updates the draft and goes to the other desks straight away, so they see it as it is typed rather than when it is
/// saved. A field someone is typing in is theirs until they move on or pause for <see cref="LiveDrafts.HoldFor"/>:
/// meanwhile it is read-only elsewhere, showing who is typing, so two people never type over each other in one box.
/// Fields are kept as the text in the box ("38.", "true", "2024-05-01"), so half-typed numbers travel too; each form
/// converts them when it saves.
/// </summary>
public sealed class LiveDraft
{
    private readonly object _gate = new();
    private readonly LiveDrafts _owner;
    private readonly Dictionary<string, string?> _values;
    private Dictionary<string, string?> _saved;
    private readonly Dictionary<string, Hold> _holds = [];
    private Hold? _frozen;

    private record Hold(Guid Desk, string Name, DateTime UntilUtc);

    internal LiveDraft(LiveDrafts owner, string topic, IReadOnlyDictionary<string, string?> values, Guid version)
    {
        _owner = owner;
        Topic = topic;
        _values = new Dictionary<string, string?>(values);
        _saved = new Dictionary<string, string?>(values);
        Version = version;
    }

    public string Topic { get; }

    /// <summary>The record's version the draft was last saved at (or loaded from), for the save's conflict check.</summary>
    public Guid Version { get; private set; }

    /// <summary>
    /// One save at a time per record, from whichever desk: two desks saving the same draft together would otherwise
    /// both send the version they started from, and the second would be refused.
    /// </summary>
    public SemaphoreSlim SaveGate { get; } = new(1, 1);

    internal int Users { get; set; }

    /// <summary>Raised on the desk that made the change; handlers must hop onto their own screen's thread.</summary>
    public event Action<LiveDraftEvent>? Changed;

    public string? this[string field]
    {
        get
        {
            lock (_gate)
                return _values.GetValueOrDefault(field);
        }
    }

    public IReadOnlyDictionary<string, string?> Values
    {
        get
        {
            lock (_gate)
                return new Dictionary<string, string?>(_values);
        }
    }

    /// <summary>Fields typed since the last save, in no particular order.</summary>
    public IReadOnlyList<string> Unsaved
    {
        get
        {
            lock (_gate)
                return _values.Keys.Where(f => !Same(_values[f], _saved.GetValueOrDefault(f))).ToList();
        }
    }

    public bool IsDirty => Unsaved.Count > 0;

    /// <summary>Who else is typing in this field right now, or null if it is free for <paramref name="desk"/>.</summary>
    public string? TypingBy(string field, Guid desk)
    {
        lock (_gate)
        {
            if (_frozen is { } f && f.Desk != desk)
                return f.Name;
            return _holds.TryGetValue(field, out var h) && h.Desk != desk && h.UntilUtc > _owner.UtcNow() ? h.Name : null;
        }
    }

    /// <summary>Who else is finishing the record (nobody may type meanwhile), or null.</summary>
    public string? FrozenBy(Guid desk)
    {
        lock (_gate)
            return _frozen is { } f && f.Desk != desk ? f.Name : null;
    }

    /// <summary>Puts what <paramref name="desk"/> typed into the draft, unless someone else holds the field.</summary>
    /// <returns>False, changing nothing, when another desk is typing in the field or finishing the record.</returns>
    public bool Type(string field, string? value, Guid desk, string name)
    {
        lock (_gate)
        {
            if (_frozen is { } f && f.Desk != desk)
                return false;
            var now = _owner.UtcNow();
            if (_holds.TryGetValue(field, out var h) && h.Desk != desk && h.UntilUtc > now)
                return false;
            _holds[field] = new Hold(desk, name, now + LiveDrafts.HoldFor);
            _values[field] = value;
        }

        Raise(new LiveDraftEvent(LiveDraftEventKind.Typed, desk, name, field));
        _owner.ReleaseLater(this, field);
        return true;
    }

    /// <summary>The person left the field, so others can type in it at once.</summary>
    public void Release(string field, Guid desk)
    {
        Hold? hold;
        lock (_gate)
        {
            if (!_holds.TryGetValue(field, out hold) || hold.Desk != desk)
                return;
            _holds.Remove(field);
        }

        Raise(new LiveDraftEvent(LiveDraftEventKind.Released, desk, hold.Name, field));
    }

    /// <summary>The desk closed the form: free everything it held.</summary>
    public void ReleaseAll(Guid desk)
    {
        List<string> fields;
        lock (_gate)
            fields = _holds.Where(h => h.Value.Desk == desk).Select(h => h.Key).ToList();
        foreach (var field in fields)
            Release(field, desk);
        Unfreeze(desk);
    }

    /// <summary>A hold ran out without the person leaving the field: tell the others it is free.</summary>
    internal void Expire(string field)
    {
        Hold? hold;
        lock (_gate)
        {
            if (!_holds.TryGetValue(field, out hold) || hold.UntilUtc > _owner.UtcNow())
                return;
            _holds.Remove(field);
        }

        Raise(new LiveDraftEvent(LiveDraftEventKind.Released, hold.Desk, hold.Name, field));
    }

    /// <summary>
    /// The values in <paramref name="savedValues"/> (what the save sent) are now the record's, at <paramref name="version"/>.
    /// Anything typed while the save was on its way stays unsaved.
    /// </summary>
    public void MarkSaved(IReadOnlyDictionary<string, string?> savedValues, Guid version, Guid desk, string name)
    {
        lock (_gate)
        {
            _saved = new Dictionary<string, string?>(savedValues);
            Version = version;
        }

        Raise(new LiveDraftEvent(LiveDraftEventKind.Saved, desk, name));
    }

    /// <summary>
    /// The record was saved some other way (or reloaded): untouched fields take its values, and the draft's version moves
    /// on. Fields with unsaved typing keep it, so nobody's typing disappears under them.
    /// </summary>
    public void Reset(IReadOnlyDictionary<string, string?> recordValues, Guid version)
    {
        lock (_gate)
        {
            if (version == Version)
                return;
            foreach (var (field, value) in recordValues)
            {
                if (Same(_values.GetValueOrDefault(field), _saved.GetValueOrDefault(field)))
                    _values[field] = value;
            }

            _saved = new Dictionary<string, string?>(recordValues);
            Version = version;
        }

        Raise(new LiveDraftEvent(LiveDraftEventKind.Reset, Guid.Empty, "System"));
    }

    /// <summary>Stops everyone else typing while <paramref name="desk"/> finishes the record. False if someone else is.</summary>
    public bool Freeze(Guid desk, string name)
    {
        lock (_gate)
        {
            if (_frozen is { } f && f.Desk != desk)
                return false;
            _frozen = new Hold(desk, name, DateTime.MaxValue);
        }

        Raise(new LiveDraftEvent(LiveDraftEventKind.Frozen, desk, name));
        return true;
    }

    public void Unfreeze(Guid desk)
    {
        Hold? frozen;
        lock (_gate)
        {
            frozen = _frozen;
            if (frozen is null || frozen.Desk != desk)
                return;
            _frozen = null;
        }

        Raise(new LiveDraftEvent(LiveDraftEventKind.Frozen, desk, frozen.Name));
    }

    /// <summary>A box left empty and a field never filled in are the same thing.</summary>
    private static bool Same(string? a, string? b) => (a ?? "") == (b ?? "");

    private void Raise(LiveDraftEvent e)
    {
        if (Changed is not { } handlers)
            return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<LiveDraftEvent>>())
        {
            try
            {
                handler(e);
            }
            catch (Exception ex)
            {
                _owner.Failed(this, ex);
            }
        }
    }
}
