using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;

namespace VetDesk.Application.Common;

public static class Saving
{
    public const string ChangedElsewhere =
        "Someone else changed this at another desk while you were working. Reopen it to see their changes.";

    /// <summary>
    /// Saves, returning false instead of throwing when another desk saved the same row first (its version token
    /// no longer matches). Callers turn that into a failure result the screen can show.
    /// </summary>
    public static async Task<bool> TrySaveChangesAsync(this IVetDeskDbContext db, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }
}
