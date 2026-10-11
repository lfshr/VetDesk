using VetDesk.Domain.Entities.Products;

namespace VetDesk.Domain.Entities.Invoices;

public record VatBreakdown(decimal Net, decimal Vat)
{
    public decimal Total => Net + Vat;
}

/// <summary>The net and VAT for one VAT rate on an invoice, as a VAT invoice must show them.</summary>
public record VatRateTotal(VatRate Rate, decimal Net, decimal Vat);
