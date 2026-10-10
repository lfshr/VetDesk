namespace VetDesk.Domain.Entities.Invoices;

public record VatBreakdown(decimal Net, decimal Vat)
{
    public decimal Total => Net + Vat;
}
