using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;

namespace VetDesk.Application.Common;

internal static class Balances
{
    /// <summary>
    /// Amount owed per client across all invoices, as <see cref="Domain.Entities.Invoices.Invoice.AmountDue"/> works it
    /// out: refunds lower what is owed, so only money never received counts. Summed in memory because SQLite can't
    /// aggregate decimals.
    /// </summary>
    public static async Task<Dictionary<int, decimal>> ForClientsAsync(IVetDeskDbContext db, IReadOnlyCollection<int> clientIds, CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0)
            return new Dictionary<int, decimal>();

        var invoices = await db.Invoices.AsNoTracking()
            .Where(i => clientIds.Contains(i.Client.Id))
            .Select(i => new { ClientId = i.Client.Id, i.Total, Payments = i.Payments.Select(p => p.Amount).ToList() })
            .ToListAsync(cancellationToken);

        return invoices
            .GroupBy(i => i.ClientId)
            .ToDictionary(g => g.Key, g => g.Sum(i => Math.Max(0, i.Total - i.Payments.Where(a => a > 0).Sum())));
    }

    public static async Task<decimal> ForClientAsync(IVetDeskDbContext db, int clientId, CancellationToken cancellationToken) =>
        (await ForClientsAsync(db, [clientId], cancellationToken)).GetValueOrDefault(clientId);
}
