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
        var byRate = ByRate(lines);
        return new VatBreakdown(byRate.Sum(r => r.Net), byRate.Sum(r => r.Vat));
    }

    /// <summary>The net and VAT for each rate the lines use, in rate order. These always add up to <see cref="Calculate"/>.</summary>
    public static IReadOnlyList<VatRateTotal> ByRate(IEnumerable<(decimal Net, VatRate Rate)> lines) => lines
        .GroupBy(l => l.Rate)
        .OrderBy(g => g.Key)
        .Select(g =>
        {
            var net = g.Sum(l => Money.Round(l.Net));
            return new VatRateTotal(g.Key, net, Money.Round(net * g.Key.Percentage()));
        })
        .ToList();

    public static VatBreakdown For(IEnumerable<InvoiceLine> lines) =>
        Calculate(lines.Select(l => (l.Net, l.VatRate)));

    public static IReadOnlyList<VatRateTotal> ByRate(IEnumerable<InvoiceLine> lines) =>
        ByRate(lines.Select(l => (l.Net, l.VatRate)));

    /// <summary>A single price including VAT, for showing gross prices next to a product.</summary>
    public static decimal Gross(decimal net, VatRate rate) => Money.Round(net) + Money.Round(Money.Round(net) * rate.Percentage());
}
