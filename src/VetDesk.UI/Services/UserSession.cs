using VetDesk.Application.Staff;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.UI.Services;

/// <summary>
/// Who is signed in at this desk. One per desktop window or browser tab (each is its own circuit). Locked means the
/// screen is covered after inactivity and the same person must re-enter their PIN; signing out clears it. Users and
/// settings open only after an administrator confirms their password, and close again when the desk locks.
/// </summary>
public class UserSession
{
    public SignedInStaff? Staff { get; private set; }
    public bool IsLocked { get; private set; }

    public bool IsSignedIn => Staff is not null;

    public event Action? Changed;

    public bool Can(Permission permission) => Staff?.Permissions.Contains(permission) == true;

    public void SignIn(SignedInStaff staff)
    {
        Staff = staff;
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

    /// <summary>Back in by PIN, so with settings closed again.</summary>
    public void Unlock(SignedInStaff staff)
    {
        Staff = staff;
        IsLocked = false;
        Changed?.Invoke();
    }

    /// <summary>The administrator confirmed their password: users and settings are open until the desk locks.</summary>
    public void UnlockSettings(SignedInStaff staff)
    {
        Staff = staff;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        Staff = null;
        IsLocked = false;
        Changed?.Invoke();
    }
}
