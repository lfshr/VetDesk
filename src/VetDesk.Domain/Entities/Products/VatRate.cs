namespace VetDesk.Domain.Entities.Products;

/// <summary>UK VAT treatments a practice uses.</summary>
public enum VatRate
{
    Standard,
    Zero,
    Exempt
}

public static class VatRates
{
    public static decimal Percentage(this VatRate rate) => rate switch
    {
        VatRate.Standard => 0.20m,
        _ => 0m
    };

    public static string Label(this VatRate rate) => rate switch
    {
        VatRate.Standard => "Standard 20%",
        VatRate.Zero => "Zero-rated",
        _ => "Exempt"
    };
}
