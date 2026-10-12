namespace VetDesk.UI.Services.Live;

/// <summary>
/// Who has which record open, at every desk. A screen joins with the record's topic ("Consultation:40") while it shows
/// it, and says when its person is editing, so the others can see "Heather (editing)" above the record.
/// </summary>
public sealed class Presence
{
    private readonly object _gate = new();
    private readonly List<Entry> _entries = [];

    /// <summary>Raised with the topic whose viewers changed. Handlers run on the desk that changed it.</summary>
    public event Action<string>? Changed;

    public record Viewer(Guid Desk, string Name, bool Editing);

    public Entry Join(string topic, Guid desk, string name)
    {
        var entry = new Entry(this, topic, desk, name);
        lock (_gate)
            _entries.Add(entry);
        Changed?.Invoke(topic);
        return entry;
    }

    /// <summary>Everyone else with the record open, one per desk, in the order they opened it.</summary>
    public IReadOnlyList<Viewer> Others(string topic, Guid desk)
    {
        lock (_gate)
        {
            return _entries.Where(e => e.Topic == topic && e.Desk != desk)
                .GroupBy(e => e.Desk)
                .Select(g => new Viewer(g.Key, g.First().Name, g.Any(e => e.Editing)))
                .ToList();
        }
    }

    private void Update(Entry entry, Action change)
    {
        lock (_gate)
            change();
        Changed?.Invoke(entry.Topic);
    }

    private void Remove(Entry entry)
    {
        bool removed;
        lock (_gate)
            removed = _entries.Remove(entry);
        if (removed)
            Changed?.Invoke(entry.Topic);
    }

    public sealed class Entry : IDisposable
    {
        private readonly Presence _owner;

        internal Entry(Presence owner, string topic, Guid desk, string name)
        {
            _owner = owner;
            Topic = topic;
            Desk = desk;
            Name = name;
        }

        public string Topic { get; }
        public Guid Desk { get; }
        public string Name { get; private set; }
        public bool Editing { get; private set; }

        public void SetEditing(bool editing)
        {
            if (editing != Editing)
                _owner.Update(this, () => Editing = editing);
        }

        /// <summary>Someone else signed in at this desk.</summary>
        public void SetName(string name)
        {
            if (name != Name)
                _owner.Update(this, () => Name = name);
        }

        public void Dispose() => _owner.Remove(this);
    }
}
