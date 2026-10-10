using VetDesk.Application.Staff;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.UI.Services;

/// <summary>
/// Who is signed in at this desk. One per window (desktop) or browser tab (web host). Locked means the screen is
/// covered after inactivity and the same person must re-enter their password; signing out clears it.
/// </summary>
public class UserSession
{
    public SignedInStaff? Staff { get; private set; }
    public bool IsLocked { get; private set; }
    public string? Email { get; private set; }

    public bool IsSignedIn => Staff is not null;

    public event Action? Changed;

    public bool Can(Permission permission) => Staff?.Permissions.Contains(permission) == true;

    public void SignIn(SignedInStaff staff, string email)
    {
        Staff = staff;
        Email = email;
        IsLocked = false;
        Changed?.Invoke();
    }

    public void Lock()
    {
        if (Staff is null || IsLocked)
            return;
        IsLocked = true;
        Changed?.Invoke();
    }

    public void Unlock(SignedInStaff staff)
    {
        Staff = staff;
        IsLocked = false;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        Staff = null;
        Email = null;
        IsLocked = false;
        Changed?.Invoke();
    }
}
