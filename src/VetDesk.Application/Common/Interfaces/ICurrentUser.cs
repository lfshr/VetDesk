using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.Common.Interfaces;

/// <summary>The member of staff signed in at this desk. Commands check permissions against it.</summary>
public interface ICurrentUser
{
    int? StaffId { get; }

    /// <summary>How the person is written into records, e.g. "Dr Patel".</summary>
    string Name { get; }

    bool Has(Permission permission);
}
