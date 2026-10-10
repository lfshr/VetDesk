using System.ComponentModel.DataAnnotations.Schema;

namespace VetDesk.Domain.Common;

public abstract class BaseEntity
{
    public int Id { get; init; }

    private readonly List<BaseEvent> _domainEvents = new();

    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(BaseEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();

    public bool IsPersisted() => Id > 0;
}
