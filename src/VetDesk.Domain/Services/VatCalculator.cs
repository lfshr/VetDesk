using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Domain.Services;

/// <summary>
/// Works out VAT for a set of lines. Prices are held net of VAT; each line's net is rounded to the penny, then VAT
/// is calculated once per rate on the invoice total for that rate (HMRC allows invoice-level rounding, and it avoids
/// the pennies drifting that per-line VAT causes). Consultations and invoices both use this, so the total a vet
/// sees on the consultation is exactly what the invoice says.
/// </summary>
public static class VatCalculator
{
    public static VatBreakdown Calculate(IEnumerable<(decimal Net, VatRate Rate)> lines)
    {
        var byRate = lines
            .GroupBy(l => l.Rate)
            .Select(g => (Rate: g.Key, Net: g.Sum(l => Money.Round(l.Net))))
            .ToList();

        var net = byRate.Sum(g => g.Net);
        var vat = byRate.Sum(g => Money.Round(g.Net * g.Rate.Percentage()));
        return new VatBreakdown(net, vat);
    }

    public static VatBreakdown For(IEnumerable<InvoiceLine> lines) =>
        Calculate(lines.Select(l => (l.Net, l.VatRate)));

    /// <summary>A single price including VAT, for showing gross prices next to a product.</summary>
    public static decimal Gross(decimal net, VatRate rate) => Money.Round(net) + Money.Round(Money.Round(net) * rate.Percentage());

    /// <summary>
    /// The net unit price for a price typed including VAT. Prices are held to the penny, so now and then (e.g. £10.05
    /// at 20%) no net price gives exactly the typed gross; <see cref="Gross"/> of the result is then a penny out.
    /// </summary>
    public static decimal NetFromGross(decimal gross, VatRate rate) => Money.Round(gross / (1 + rate.Percentage()));
}
