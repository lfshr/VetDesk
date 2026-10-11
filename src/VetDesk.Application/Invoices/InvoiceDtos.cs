using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Practice;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Application.Invoices;

public record InvoiceRow(
    int Id,
    string Number,
    DateTime IssuedUtc,
    int ClientId,
    string ClientName,
    string? PatientName,
    decimal Total,
    decimal AmountDue,
    InvoiceStatus Status);

public record InvoiceLineDto(string Description, decimal Quantity, decimal UnitPrice, VatRate VatRate, decimal Net);

public record PaymentDto(int Id, DateTime TakenUtc, PaymentMethod Method, decimal Amount, string? Reference, string TakenBy, bool IsRefund);

public record ClaimDto(string Insurer, string PolicyNumber, decimal Excess, string Condition, DateTime CreatedUtc, ClaimStatus Status);

public record InvoiceDetail(
    int Id,
    string Number,
    DateTime IssuedUtc,
    DateOnly DueDate,
    int ClientId,
    string ClientName,
    string ClientNumber,
    string? ClientAddress,
    string? ClientPostcode,
    string? ClientEmail,
    int? PatientId,
    string? PatientName,
    IReadOnlyList<InvoiceLineDto> Lines,
    decimal Net,
    decimal Vat,
    decimal Total,
    decimal Paid,
    decimal AmountDue,
    InvoiceStatus Status,
    IReadOnlyList<PaymentDto> Payments,
    ClaimDto? Claim,
    Guid Version);

public static class InvoiceStatusLabels
{
    public static string Label(this InvoiceStatus status) => status switch
    {
        InvoiceStatus.PartPaid => "Part paid",
        InvoiceStatus.ClaimSent => "Claim sent",
        _ => status.ToString()
    };
}

/// <summary>Everything the A4 invoice printout shows: the invoice itself, the practice's details and the VAT by rate.</summary>
public record InvoicePrintout(
    InvoiceDetail Invoice,
    PracticeDetails Practice,
    PrintoutPatient? Patient,
    IReadOnlyList<VatRateTotal> VatByRate);

public record PrintoutPatient(string Name, string PatientNumber, string SpeciesAndBreed, string? SeenBy, DateTime? SeenUtc);

public record PracticeDetails(
    string Name,
    string? Address,
    string? Phone,
    string? Email,
    string? VatNumber,
    string? CompanyName,
    string? CompanyNumber,
    CompanyRegistration? CompanyRegisteredIn,
    string? RegisteredOffice,
    string? BankAccountName,
    string? BankSortCode,
    string? BankAccountNumber,
    int PaymentTermsDays)
{
    public bool IsVatRegistered => !string.IsNullOrWhiteSpace(VatNumber);
    public bool HasCompanyDetails => !string.IsNullOrWhiteSpace(CompanyNumber);
    public bool HasBankDetails => !string.IsNullOrWhiteSpace(BankSortCode) && !string.IsNullOrWhiteSpace(BankAccountNumber);
}

public static class CompanyRegistrationLabels
{
    public static string Label(this CompanyRegistration place) => place switch
    {
        CompanyRegistration.Scotland => "Scotland",
        CompanyRegistration.NorthernIreland => "Northern Ireland",
        _ => "England and Wales"
    };
}
