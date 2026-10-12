namespace VetDesk.UI.Services;

/// <summary>
/// The patients open as tabs at this desk (one list per desktop window or browser tab, like <see cref="UserSession"/>).
/// Opening a patient's record or consultation adds its tab, or brings the existing one forward. Each tab remembers the
/// screen it was last on (the record or a consultation), the record's section and any note being written there, so
/// switching back returns to it as it was.
/// </summary>
public class OpenPatients
{
    private readonly List<OpenPatient> _tabs = [];

    public IReadOnlyList<OpenPatient> Tabs => _tabs;

    public event Action? Changed;

    /// <summary>A patient screen is showing: add its tab, or update the existing one's name and screen.</summary>
    /// <param name="path">The screen's address relative to the app, e.g. <c>patients/12</c> or <c>consultations/40</c>.</param>
    public void Show(int patientId, string name, string ownerName, string path)
    {
        var tab = Find(patientId);
        if (tab is null)
        {
            _tabs.Add(new OpenPatient(patientId) { Name = name, OwnerName = ownerName, Path = path });
        }
        else
        {
            if (tab.Name == name && tab.OwnerName == ownerName && tab.Path == path)
                return;
            tab.Name = name;
            tab.OwnerName = ownerName;
            tab.Path = path;
        }

        Changed?.Invoke();
    }

    public OpenPatient? Find(int patientId) => _tabs.Find(t => t.PatientId == patientId);

    /// <summary>The tab whose screen is on view, if the current address is one of them.</summary>
    public OpenPatient? ActiveFor(string currentPath)
    {
        var path = currentPath.Split('?', '#')[0].Trim('/');
        return _tabs.Find(t => string.Equals(t.Path, path, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Closes a tab and returns the tab to show in its place: the one to its right, else its left, else none.</summary>
    public OpenPatient? Close(int patientId)
    {
        var index = _tabs.FindIndex(t => t.PatientId == patientId);
        if (index < 0)
            return null;
        _tabs.RemoveAt(index);
        Changed?.Invoke();
        return _tabs.Count == 0 ? null : _tabs[Math.Min(index, _tabs.Count - 1)];
    }

    /// <summary>On sign-out, so the next person at the desk starts with no patients open.</summary>
    public void Clear()
    {
        if (_tabs.Count == 0)
            return;
        _tabs.Clear();
        Changed?.Invoke();
    }
}

public class OpenPatient(int patientId)
{
    public int PatientId { get; } = patientId;
    public string Name { get; set; } = "";
    public string OwnerName { get; set; } = "";

    /// <summary>The screen this tab was last on, relative to the app.</summary>
    public string Path { get; set; } = "";

    /// <summary>Where the record screen was left: its section and any note being written. Null until it is left.</summary>
    public RecordViewState? RecordView { get; set; }

    public record RecordViewState(string Section, bool NoteOpen, string NoteTitle, string NoteBody);
}
