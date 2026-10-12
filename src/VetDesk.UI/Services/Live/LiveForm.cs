namespace VetDesk.UI.Services.Live;

/// <summary>
/// One screen's handle on a record's <see cref="LiveDraft"/>: types as this desk's person, and tells the screen when
/// another desk's typing, saving or finishing needs it to re-render. Dispose it when the form closes.
/// </summary>
public sealed class LiveForm : IDisposable
{
    private readonly LiveDrafts _drafts;
    private readonly Desk _desk;
    private readonly UserSession _session;
    private readonly Func<LiveDraftEvent, Task> _onOtherDesk;
    private bool _disposed;

    /// <param name="onOtherDesk">
    /// Called for each change made at another desk, on that desk's thread: wrap the work in the component's InvokeAsync.
    /// </param>
    public LiveForm(LiveDrafts drafts, Desk desk, UserSession session, string topic, IReadOnlyDictionary<string, string?> saved,
        Guid version, Func<LiveDraftEvent, Task> onOtherDesk)
    {
        _drafts = drafts;
        _desk = desk;
        _session = session;
        _onOtherDesk = onOtherDesk;
        Draft = drafts.Join(topic, saved, version);
        Draft.Changed += OnChanged;
        _desk.ConnectionChanged += OnConnectionChanged;
    }

    public LiveDraft Draft { get; }

    public Guid DeskId => _desk.Id;

    public string Me => _session.Staff?.ShortName ?? "";

    public string? this[string field] => Draft[field];

    public bool IsDirty => Draft.IsDirty;

    public bool Type(string field, string? value) => Draft.Type(field, value, _desk.Id, Me);

    public void Release(string field) => Draft.Release(field, _desk.Id);

    /// <summary>Who else is typing in the field (it is read-only here meanwhile), or null.</summary>
    public string? TypingBy(string field) => Draft.TypingBy(field, _desk.Id);

    public string? FrozenBy => Draft.FrozenBy(_desk.Id);

    public void Revert() => Draft.Revert(_desk.Id, Me);

    public bool Freeze() => Draft.Freeze(_desk.Id, Me);

    public void Unfreeze() => Draft.Unfreeze(_desk.Id);

    public void MarkSaved(IReadOnlyDictionary<string, string?> values, Guid version) => Draft.MarkSaved(values, version, _desk.Id, Me);

    private void OnChanged(LiveDraftEvent e)
    {
        if (e.Desk != _desk.Id && !_disposed)
            _ = _onOtherDesk(e);
    }

    // A desk that dropped off can't be typing or completing: free its boxes for the others straight away.
    private void OnConnectionChanged()
    {
        if (!_desk.IsConnected && !_disposed)
            Draft.ReleaseAll(_desk.Id);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        Draft.Changed -= OnChanged;
        _desk.ConnectionChanged -= OnConnectionChanged;
        Draft.ReleaseAll(_desk.Id);
        _drafts.Leave(Draft);
    }
}
