using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Services;

namespace VetDesk.Domain.UnitTests.Services;

[TestFixture]
public class VatCalculatorTests
{
    [Test]
    public void Calculate_StandardRatedLines_AddsTwentyPercent()
    {
        var totals = VatCalculator.Calculate([(52m, VatRate.Standard), (38m, VatRate.Standard)]);

        totals.Net.ShouldBe(90m);
        totals.Vat.ShouldBe(18m);
        totals.Total.ShouldBe(108m);
    }

    [Test]
    public void Calculate_MixedRates_OnlyChargesVatOnStandardRated()
    {
        var totals = VatCalculator.Calculate([(52m, VatRate.Standard), (24m, VatRate.Zero), (10m, VatRate.Exempt)]);

        totals.Net.ShouldBe(86m);
        totals.Vat.ShouldBe(10.40m);
        totals.Total.ShouldBe(96.40m);
    }

    [Test]
    public void Calculate_RoundsVatOncePerRate_NotPerLine()
    {
        // Per line, 20% of £0.08 is £0.016, rounded to £0.02, so £0.06 for three lines.
        // On the rate total, 20% of £0.24 is £0.048, rounded to £0.05: what the invoice must show.
        var lines = Enumerable.Repeat((0.08m, VatRate.Standard), 3);

        var totals = VatCalculator.Calculate(lines);

        totals.Net.ShouldBe(0.24m);
        totals.Vat.ShouldBe(0.05m);
    }

    [Test]
    public void Calculate_HalfPenny_RoundsAwayFromZero()
    {
        // 20% of £0.125 net (after line rounding £0.13) is £0.026 → £0.03.
        var totals = VatCalculator.Calculate([(0.125m, VatRate.Standard)]);

        totals.Net.ShouldBe(0.13m);
        totals.Vat.ShouldBe(0.03m);
    }

    [Test]
    public void Calculate_NoLines_IsZero()
    {
        var totals = VatCalculator.Calculate([]);

        totals.Total.ShouldBe(0m);
    }

    [Test]
    public void Gross_StandardRate_IncludesVat()
    {
        VatCalculator.Gross(52m, VatRate.Standard).ShouldBe(62.40m);
        VatCalculator.Gross(24m, VatRate.Zero).ShouldBe(24m);
    }
}
