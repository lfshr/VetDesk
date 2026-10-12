using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using VetDesk.Application.Common.Live;

namespace VetDesk.Infrastructure.Persistence;

/// <summary>
/// Works out the <see cref="RecordChange.Topics"/> of a save: each changed row's type and id, and the rows its foreign
/// keys point at, so a new clinical note (whose patient is 12) is heard by the screens showing "Patient:12".
/// </summary>
internal sealed class ChangeTopics
{
    private readonly HashSet<string> _topics = [];
    private readonly List<EntityEntry> _added = [];

    /// <summary>Before saving: deleted rows are detached by the save, so read them (and changed rows) now.</summary>
    public static ChangeTopics Capture(ChangeTracker tracker)
    {
        var topics = new ChangeTopics();
        foreach (var entry in tracker.Entries())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    topics._added.Add(entry); // ids and foreign keys are only known once saved
                    break;
                case EntityState.Modified:
                    topics.Add(entry, original: true);
                    topics.Add(entry, original: false);
                    break;
                case EntityState.Deleted:
                    topics.Add(entry, original: true);
                    break;
            }
        }

        return topics;
    }

    /// <summary>After saving: the new rows now have their ids.</summary>
    public IReadOnlySet<string> Complete()
    {
        foreach (var entry in _added)
            Add(entry, original: false);
        return _topics;
    }

    private void Add(EntityEntry entry, bool original)
    {
        var type = entry.Metadata.ClrType.Name;
        _topics.Add(type);
        if (entry.Metadata.FindProperty("Id") is { } idProperty && Value(entry, idProperty.Name, original) is { } id)
            _topics.Add(RecordChange.Topic(type, id));

        foreach (var foreignKey in entry.Metadata.GetForeignKeys())
        {
            if (foreignKey.Properties.Count == 1 && Value(entry, foreignKey.Properties[0].Name, original) is { } principalId)
                _topics.Add(RecordChange.Topic(foreignKey.PrincipalEntityType.ClrType.Name, principalId));
        }
    }

    private static int? Value(EntityEntry entry, string property, bool original)
    {
        var p = entry.Property(property);
        var value = original ? p.OriginalValue : p.CurrentValue;
        return value is int id && id > 0 ? id : null;
    }
}
