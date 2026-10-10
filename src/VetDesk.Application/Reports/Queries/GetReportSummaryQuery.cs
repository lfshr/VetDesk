using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Reports.Queries;

public enum GetReportSummaryError { NotAllowed }

/// <summary>
/// The figures at the top of Reports for this week, month or quarter so far, each compared with the same number of
/// days in the previous period.
/// </summary>
public class GetReportSummaryQuery : IRequest<Result<ReportSummary>>
{
    public ReportPeriod Period { get; init; }
}

public class GetReportSummaryQueryHandler : IRequestHandler<GetReportSummaryQuery, Result<ReportSummary>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetReportSummaryQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<ReportSummary>> Handle(GetReportSummaryQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.TakePayments))
            return Result.Failure(GetReportSummaryError.NotAllowed, Access.Denied(Permission.TakePayments));

        var today = ClinicTime.Today(_clock.UtcNow);
        var (from, previousFrom) = PeriodStart(request.Period, today);
        var days = today.DayNumber - from.DayNumber;
        var previousTo = previousFrom.AddDays(days);

        var current = await FiguresAsync(from, today, cancellationToken);
        var previous = await FiguresAsync(previousFrom, previousTo, cancellationToken);

        var invoices = await InvoicesAsync(from, today, cancellationToken);
        var bars = Buckets(request.Period, from).Select(b => new ReportBar(b.Label,
            invoices.Where(i => i.Date >= b.Start && i.Date <= b.End).Sum(i => i.Net),
            today >= b.Start && today <= b.End)).ToList();

        var (start, _) = ClinicTime.DayRange(from);
        var (_, end) = ClinicTime.DayRange(today);
        var byType = await _dbContext.Appointments.AsNoTracking()
            .Where(a => a.StartUtc >= start && a.StartUtc < end && a.Status != AppointmentStatus.Cancelled)
            .GroupBy(a => a.Type.Name)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return Result.Success(new ReportSummary(request.Period, from, today,
            current.Revenue, Change(current.Revenue, previous.Revenue),
            current.Consultations, Change(current.Consultations, previous.Consultations),
            current.NewClients, Change(current.NewClients, previous.NewClients),
            current.Average, Change(current.Average, previous.Average),
            bars,
            byType.OrderByDescending(t => t.Count).Select(t => new ReportBar(t.Name, t.Count)).ToList()));
    }

    internal static (DateOnly From, DateOnly PreviousFrom) PeriodStart(ReportPeriod period, DateOnly today)
    {
        switch (period)
        {
            case ReportPeriod.Week:
                var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
                return (monday, monday.AddDays(-7));
            case ReportPeriod.Month:
                var first = new DateOnly(today.Year, today.Month, 1);
                return (first, first.AddMonths(-1));
            default:
                var quarter = new DateOnly(today.Year, (today.Month - 1) / 3 * 3 + 1, 1);
                return (quarter, quarter.AddMonths(-3));
        }
    }

    private static IEnumerable<(string Label, DateOnly Start, DateOnly End)> Buckets(ReportPeriod period, DateOnly from)
    {
        var uk = CultureInfo.GetCultureInfo("en-GB");
        switch (period)
        {
            case ReportPeriod.Week:
                for (var i = 0; i < 7; i++)
                    yield return (from.AddDays(i).ToString("ddd", uk), from.AddDays(i), from.AddDays(i));
                break;
            case ReportPeriod.Month:
                for (var start = from; start.Month == from.Month; start = start.AddDays(7))
                {
                    var end = start.AddDays(6);
                    if (end.Month != from.Month)
                        end = new DateOnly(from.Year, from.Month, DateTime.DaysInMonth(from.Year, from.Month));
                    yield return ($"{start.Day}–{end.Day}", start, end);
                }
                break;
            default:
                for (var i = 0; i < 3; i++)
                {
                    var start = from.AddMonths(i);
                    yield return (start.ToString("MMM", uk), start, start.AddMonths(1).AddDays(-1));
                }
                break;
        }
    }

    private async Task<List<(DateOnly Date, decimal Net)>> InvoicesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var (start, _) = ClinicTime.DayRange(from);
        var (_, end) = ClinicTime.DayRange(to);
        var rows = await _dbContext.Invoices.AsNoTracking()
            .Where(i => i.IssuedUtc >= start && i.IssuedUtc < end)
            .Select(i => new { i.IssuedUtc, i.Net })
            .ToListAsync(cancellationToken);
        return rows.Select(r => (ClinicTime.Today(Utc.Mark(r.IssuedUtc)), r.Net)).ToList();
    }

    private async Task<(decimal Revenue, int Consultations, int NewClients, decimal Average)> FiguresAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var (start, _) = ClinicTime.DayRange(from);
        var (_, end) = ClinicTime.DayRange(to);
        var invoices = await InvoicesAsync(from, to, cancellationToken);
        var consultations = await _dbContext.Consultations.CountAsync(c => c.StartedUtc >= start && c.StartedUtc < end, cancellationToken);
        var newClients = await _dbContext.Clients.CountAsync(c => c.CreatedUtc >= start && c.CreatedUtc < end, cancellationToken);
        var revenue = invoices.Sum(i => i.Net);
        return (revenue, consultations, newClients, invoices.Count == 0 ? 0 : Money.Round(revenue / invoices.Count));
    }

    /// <summary>Percentage change, or null when there's nothing to compare with.</summary>
    internal static decimal? Change(decimal current, decimal previous) =>
        previous == 0 ? null : Math.Round((current - previous) / previous * 100, 1);
}
