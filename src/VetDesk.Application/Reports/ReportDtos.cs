namespace VetDesk.Application.Reports;

public enum ReportPeriod { Week, Month, Quarter }

public enum StandardReport { DaySheet, VatSummary, AgedDebtors, ControlledDrugs, ClinicianProductivity, StockValuation }

/// <param name="IsCurrent">The revenue bar for the day, week or month that today falls in; the chart picks it out.</param>
public record ReportBar(string Label, decimal Value, bool IsCurrent = false);

public record ReportSummary(
    ReportPeriod Period,
    DateOnly From,
    DateOnly To,
    decimal Revenue,
    decimal? RevenueChange,
    int Consultations,
    decimal? ConsultationsChange,
    int NewClients,
    decimal? NewClientsChange,
    decimal AverageInvoice,
    decimal? AverageInvoiceChange,
    IReadOnlyList<ReportBar> RevenueBars,
    IReadOnlyList<ReportBar> AppointmentsByType);

/// <summary>A report as rows of text, ready for the on-screen table, printing and CSV.</summary>
public record ReportTable(string Title, string Subtitle, IReadOnlyList<string> Columns, IReadOnlyList<IReadOnlyList<string>> Rows, IReadOnlyList<string>? Totals);

public record ReminderDue(int PatientId, string PatientName, string OwnerName, string? Phone, string? Email, string Vaccine, DateOnly DueDate, bool OwnerOptedIn);

public static class ReportNames
{
    public static string Title(this StandardReport report) => report switch
    {
        StandardReport.DaySheet => "Day sheet",
        StandardReport.VatSummary => "VAT summary",
        StandardReport.AgedDebtors => "Aged debtors",
        StandardReport.ControlledDrugs => "Controlled drugs",
        StandardReport.ClinicianProductivity => "Clinician productivity",
        _ => "Stock valuation"
    };

    public static string Description(this StandardReport report) => report switch
    {
        StandardReport.DaySheet => "Appointments, consultations and takings for one day",
        StandardReport.VatSummary => "Net, VAT and gross by rate for the period",
        StandardReport.AgedDebtors => "Unpaid balances by age: current, 30, 60 and 90+ days",
        StandardReport.ControlledDrugs => "Register entries for the period, with balances",
        StandardReport.ClinicianProductivity => "Consultations and revenue by clinician",
        _ => "Stock on hand at cost and at sale price"
    };
}
