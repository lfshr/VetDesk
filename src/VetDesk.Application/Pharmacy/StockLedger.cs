using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Application.Pharmacy;

/// <summary>
/// The only place stock levels change. Every change writes a stock movement; controlled drugs also get a
/// register entry carrying the running balance. Callers must load the product with its batches.
/// </summary>
internal static class StockLedger
{
    public static string? CannotTake(Product product, decimal quantity) =>
        product.OnHand < quantity
            ? $"Only {product.OnHand:0.##} {product.Unit} of {product.Name} in stock; {quantity:0.##} needed."
            : null;

    /// <summary>Takes stock from the batches that expire first. Check <see cref="CannotTake"/> first.</summary>
    public static void Take(IVetDeskDbContext db, Product product, decimal quantity, StockMovementReason reason, string reference, string by, DateTime nowUtc)
    {
        var remaining = quantity;
        foreach (var batch in product.Batches.Where(b => b.Quantity > 0).OrderBy(b => b.Expiry ?? DateOnly.MaxValue).ThenBy(b => b.Id))
        {
            if (remaining <= 0)
                break;
            var taken = Math.Min(batch.Quantity, remaining);
            batch.Quantity -= taken;
            remaining -= taken;
            db.StockMovements.Add(new StockMovement { Product = product, Batch = batch, Change = -taken, Reason = reason, Reference = reference, By = by, Utc = nowUtc });
        }
    }

    public static StockBatch Receive(IVetDeskDbContext db, Product product, string batchNumber, DateOnly? expiry, decimal quantity, string reference, string by, DateTime nowUtc)
    {
        var batch = product.Batches.FirstOrDefault(b => b.BatchNumber.Equals(batchNumber, StringComparison.OrdinalIgnoreCase) && b.Expiry == expiry);
        if (batch is null)
        {
            batch = new StockBatch { Product = product, BatchNumber = batchNumber, Expiry = expiry, ReceivedUtc = nowUtc };
            product.Batches.Add(batch);
        }

        batch.Quantity += quantity;
        db.StockMovements.Add(new StockMovement { Product = product, Batch = batch, Change = quantity, Reason = StockMovementReason.Received, Reference = reference, By = by, Utc = nowUtc });
        return batch;
    }

    /// <summary>Sets the stock to what was counted, taking the difference off (or adding it to) the batches in expiry order.</summary>
    public static decimal Adjust(IVetDeskDbContext db, Product product, decimal counted, string reference, string by, DateTime nowUtc)
    {
        var difference = counted - product.OnHand;
        if (difference < 0)
        {
            Take(db, product, -difference, StockMovementReason.StockTake, reference, by, nowUtc);
        }
        else if (difference > 0)
        {
            var batch = product.Batches.OrderByDescending(b => b.Expiry ?? DateOnly.MaxValue).FirstOrDefault()
                        ?? Receive(db, product, "UNRECORDED", null, 0, reference, by, nowUtc);
            batch.Quantity += difference;
            db.StockMovements.Add(new StockMovement { Product = product, Batch = batch, Change = difference, Reason = StockMovementReason.StockTake, Reference = reference, By = by, Utc = nowUtc });
        }

        return difference;
    }

    public static void Register(IVetDeskDbContext db, Product product, ControlledDrugEntryKind kind, decimal change, string reason, string by, string? witness, DateTime nowUtc) =>
        db.ControlledDrugEntries.Add(new ControlledDrugEntry
        {
            Product = product, Kind = kind, Change = change, Balance = product.OnHand, Reason = reason, By = by, Witness = witness, Utc = nowUtc,
        });
}
