using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Staff;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Application.Pharmacy;

/// <summary>
/// A controlled drug entry needs a second person. They confirm with their own PIN at the same desk, so the
/// register shows a witness who was actually there, not just a name picked from a list.
/// </summary>
internal static class WitnessCheck
{
    /// <remarks>Call before the request changes anything: a wrong PIN is counted and saved straight away.</remarks>
    public static async Task<(StaffMember? Witness, string? Problem)> VerifyAsync(
        IVetDeskDbContext db, IPasswordHasher hasher, ICurrentUser user, int? witnessId, string? pin, DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (witnessId is null)
            return (null, "Choose a witness. Controlled drugs need a second person.");
        if (witnessId == user.StaffId)
            return (null, "The witness must be someone other than you.");

        var witness = await db.StaffMembers.FirstOrDefaultAsync(s => s.Id == witnessId && s.IsActive, cancellationToken);
        if (witness is null)
            return (null, "That witness isn't an active member of staff.");

        var permissions = await StaffRules.PermissionsForAsync(db, witness, cancellationToken);
        if (!permissions.Contains(Permission.WitnessControlledDrugs))
            return (null, $"{witness.ShortName}'s role can't witness controlled drugs.");

        if (await StaffRules.CheckPinAsync(db, hasher, witness, pin, nowUtc, cancellationToken) is { } problem)
            return (null, problem);

        return (witness, null);
    }
}
