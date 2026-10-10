using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.Common;

public static class Access
{
    public static bool IsSignedIn(this ICurrentUser user) => user.StaffId is not null;

    public static bool Lacks(this ICurrentUser user, Permission permission) =>
        user.StaffId is null || !user.Has(permission);

    public static string Denied(Permission permission) =>
        $"Your role doesn't allow this ({permission.Label().ToLowerInvariant()}). Ask a practice administrator.";

    public const string NotSignedIn = "Sign in to continue.";
}
