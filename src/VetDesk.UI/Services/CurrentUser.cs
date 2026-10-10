using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Staff;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.UI.Services;

/// <summary>
/// The <see cref="ICurrentUser"/> handlers see. Each request runs in its own DI scope, and <see cref="Backend"/>
/// copies the desk's signed-in person into that scope before sending.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private SignedInStaff? _staff;

    public int? StaffId => _staff?.StaffId;
    public string Name => _staff?.ShortName ?? string.Empty;
    public bool Has(Permission permission) => _staff?.Permissions.Contains(permission) == true;

    public void Set(SignedInStaff? staff) => _staff = staff;
}
