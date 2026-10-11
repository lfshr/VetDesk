using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Reports.Queries;

public enum GetStandardReportError { NotAllowed, InvalidRange }

/// <summary>The standard reports list. Each comes back as a table so the screen can show, print or export it the same way.</summary>
public class GetStandardReportQuery : IRequest<Result<ReportTable>>
{
    public StandardReport Report { get; init; }
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
}

public class GetStandardReportQueryHandler : IRequestHandler<GetStandardReportQuery, Result<ReportTable>>
{
    private static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetStandardReportQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<ReportTable>> Handle(GetStandardReportQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.TakePayments))
            return Result.Failure(GetStandardReportError.NotAllowed, Access.Denied(Permission.TakePayments));
        if (request.To < request.From || request.To.DayNumber - request.From.DayNumber > 400)
            return Result.Failure(GetStandardReportError.InvalidRange, "Choose a range of up to about a year.");

        var start = ClinicTime.DayRange(request.From).StartUtc;
        var end = ClinicTime.DayRange(request.To).EndUtc;
        var range = request.From == request.To
            ? request.From.ToString("dddd d MMMM yyyy", Uk)
            : $"{request.From.ToString("d MMM yyyy", Uk)} to {request.To.ToString("d MMM yyyy", Uk)}";

        var table = request.Report switch
        {
            StandardReport.DaySheet => await DaySheetAsync(request.From, cancellationToken),
            StandardReport.VatSummary => await VatSummaryAsync(start, end, range, cancellationToken),
            StandardReport.AgedDebtors => await AgedDebtorsAsync(cancellationToken),
            StandardReport.ControlledDrugs => await ControlledDrugsAsync(start, end, range, cancellationToken),
            StandardReport.ClinicianProductivity => await ProductivityAsync(start, end, range, cancellationToken),
            _ => await StockValuationAsync(cancellationToken)
        };
        return Result.Success(table);
    }

    private static string Gbp(decimal value) => value.ToString("C", Uk);

    private async Task<ReportTable> DaySheetAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var (start, end) = ClinicTime.DayRange(date);
        var appointments = await _dbContext.Appointments.AsNoTracking()
            .Include(a => a.Patient).ThenInclude(p => p.Client).Include(a => a.Type).Include(a => a.Clinician)
            .Where(a => a.StartUtc >= start && a.StartUtc < end)
            .OrderBy(a => a.StartUtc)
            .ToListAsync(cancellationToken);
        var invoices = await _dbContext.Invoices.AsNoTracking().Include(i => i.Patient)
            .Where(i => i.IssuedUtc >= start && i.IssuedUtc < end)
            .ToListAsync(cancellationToken);
        var invoicedByPatient = invoices.Where(i => i.Patient != null).GroupBy(i => i.Patient!.Id).ToDictionary(g => g.Key, g => g.Sum(i => i.Total));

        var rows = appointments.Select(a => (IReadOnlyList<string>)new[]
        {
            ClinicTime.ToLocal(Utc.Mark(a.StartUtc)).ToString("HH:mm", Uk), a.Patient.Name, a.Patient.Client.FullName, a.Type.Name,
            a.Clinician?.ShortName ?? "", a.Status.Label(a.Type.Kind), invoicedByPatient.TryGetValue(a.Patient.Id, out var t) ? Gbp(t) : ""
        }).ToList();

        return new ReportTable(StandardReport.DaySheet.Title(), date.ToString("dddd d MMMM yyyy", Uk),
            ["Time", "Patient", "Owner", "Type", "Clinician", "Status", "Invoiced"], rows,
            [$"{appointments.Count} appointments", "", "", "", "", "", Gbp(invoices.Sum(i => i.Total))]);
    }

    private async Task<ReportTable> VatSummaryAsync(DateTime start, DateTime end, string range, CancellationToken cancellationToken)
    {
        var invoices = await _dbContext.Invoices.AsNoTracking().Include(i => i.Lines)
            .Where(i => i.IssuedUtc >= start && i.IssuedUtc < end)
            .ToListAsync(cancellationToken);

        // VAT is shown as it was charged on each invoice (worked out per rate on the invoice), so the report agrees with them.
        var rows = invoices
            .SelectMany(i => i.Lines.GroupBy(l => l.VatRate).Select(g => (Rate: g.Key, Net: g.Sum(l => l.Net))))
            .GroupBy(x => x.Rate)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var net = g.Sum(x => x.Net);
                var vat = g.Sum(x => Money.Round(x.Net * g.Key.Percentage()));
                return (Rate: g.Key, Net: net, Vat: vat);
            })
            .ToList();

        return new ReportTable(StandardReport.VatSummary.Title(), range, ["VAT rate", "Net", "VAT", "Gross"],
            rows.Select(r => (IReadOnlyList<string>)new[] { r.Rate.Label(), Gbp(r.Net), Gbp(r.Vat), Gbp(r.Net + r.Vat) }).ToList(),
            ["Total", Gbp(invoices.Sum(i => i.Net)), Gbp(invoices.Sum(i => i.Vat)), Gbp(invoices.Sum(i => i.Total))]);
    }

    private async Task<ReportTable> AgedDebtorsAsync(CancellationToken cancellationToken)
    {
        var today = ClinicTime.Today(_clock.UtcNow);
        var invoices = await _dbContext.Invoices.AsNoTracking().Include(i => i.Client).Include(i => i.Payments)
            .ToListAsync(cancellationToken);

        var rows = invoices.Where(i => i.AmountDue > 0)
            .GroupBy(i => i.Client)
            .Select(g =>
            {
                decimal Bucket(int min, int max) => g.Where(i =>
                {
                    var age = today.DayNumber - ClinicTime.Today(Utc.Mark(i.IssuedUtc)).DayNumber;
                    return age >= min && age <= max;
                }).Sum(i => i.AmountDue);
                return (Client: g.Key, Current: Bucket(0, 29), D30: Bucket(30, 59), D60: Bucket(60, 89), D90: Bucket(90, int.MaxValue), Total: g.Sum(i => i.AmountDue));
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        return new ReportTable(StandardReport.AgedDebtors.Title(), $"As at {today.ToString("d MMMM yyyy", Uk)}",
            ["Client", "Phone", "Current", "30+ days", "60+ days", "90+ days", "Total"],
            rows.Select(r => (IReadOnlyList<string>)new[] { r.Client.FullName, r.Client.Phone, Gbp(r.Current), Gbp(r.D30), Gbp(r.D60), Gbp(r.D90), Gbp(r.Total) }).ToList(),
            ["Total", "", Gbp(rows.Sum(r => r.Current)), Gbp(rows.Sum(r => r.D30)), Gbp(rows.Sum(r => r.D60)), Gbp(rows.Sum(r => r.D90)), Gbp(rows.Sum(r => r.Total))]);
    }

    private async Task<ReportTable> ControlledDrugsAsync(DateTime start, DateTime end, string range, CancellationToken cancellationToken)
    {
        var entries = await _dbContext.ControlledDrugEntries.AsNoTracking().Include(e => e.Product)
            .Where(e => e.Utc >= start && e.Utc < end)
            .OrderBy(e => e.Product.Name).ThenBy(e => e.Utc).ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);

        return new ReportTable(StandardReport.ControlledDrugs.Title(), range,
            ["Date", "Drug", "Entry", "Amount", "Balance", "Details", "By", "Witness"],
            entries.Select(e => (IReadOnlyList<string>)new[]
            {
                ClinicTime.ToLocal(Utc.Mark(e.Utc)).ToString("dd/MM/yyyy HH:mm", Uk), e.Product.Name, e.Kind.ToString(),
                $"{e.Change:+0.##;-0.##;0} {e.Product.Unit}", $"{e.Balance:0.##} {e.Product.Unit}", e.Reason, e.By, e.Witness ?? ""
            }).ToList(), null);
    }

    private async Task<ReportTable> ProductivityAsync(DateTime start, DateTime end, string range, CancellationToken cancellationToken)
    {
        var consultations = await _dbContext.Consultations.AsNoTracking()
            .Include(c => c.Clinician).Include(c => c.Invoice).Include(c => c.Appointment).ThenInclude(a => a!.Type)
            .Where(c => c.StartedUtc >= start && c.StartedUtc < end)
            .ToListAsync(cancellationToken);

        var rows = consultations.GroupBy(c => c.Clinician)
            .Select(g => (Name: g.Key.ShortName, Count: g.Count(),
                Surgery: g.Count(c => c.Appointment?.Type.Kind == AppointmentKind.Surgery),
                Revenue: g.Sum(c => c.Invoice?.Net ?? 0)))
            .OrderByDescending(r => r.Revenue)
            .ToList();

        return new ReportTable(StandardReport.ClinicianProductivity.Title(), range,
            ["Clinician", "Consultations", "Of which surgery", "Revenue (net)", "Average"],
            rows.Select(r => (IReadOnlyList<string>)new[] { r.Name, r.Count.ToString(Uk), r.Surgery.ToString(Uk), Gbp(r.Revenue), Gbp(r.Count == 0 ? 0 : Money.Round(r.Revenue / r.Count)) }).ToList(),
            ["Total", rows.Sum(r => r.Count).ToString(Uk), rows.Sum(r => r.Surgery).ToString(Uk), Gbp(rows.Sum(r => r.Revenue)), ""]);
    }

    private async Task<ReportTable> StockValuationAsync(CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products.AsNoTracking().Include(p => p.Batches)
            .Where(p => p.IsStocked && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var rows = products.Select(p => (Product: p, OnHand: p.OnHand, Cost: Money.Round(p.OnHand * p.CostPrice), Sale: Money.Round(p.OnHand * p.UnitPrice))).ToList();
        return new ReportTable(StandardReport.StockValuation.Title(), $"As at {ClinicTime.Today(_clock.UtcNow).ToString("d MMMM yyyy", Uk)}, prices net of VAT",
            ["Product", "Category", "On hand", "Cost price", "Value at cost", "Value at sale"],
            rows.Select(r => (IReadOnlyList<string>)new[]
            {
                r.Product.Name, r.Product.Category.ToString(), $"{r.OnHand:0.##} {r.Product.Unit}", Gbp(r.Product.CostPrice), Gbp(r.Cost), Gbp(r.Sale)
            }).ToList(),
            ["Total", "", "", "", Gbp(rows.Sum(r => r.Cost)), Gbp(rows.Sum(r => r.Sale))]);
    }
}
