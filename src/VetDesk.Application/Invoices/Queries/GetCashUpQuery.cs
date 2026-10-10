using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Invoices.Queries;

public enum GetCashUpError { NotAllowed }

public record CashUp(
    DateOnly Date,
    decimal Card,
    decimal Cash,
    decimal Transfer,
    int PaymentCount,
    decimal OpeningFloat,
    bool IsClosed,
    decimal? CashCounted,
    string? ClosedBy,
    DateTime? ClosedUtc)
{
    public decimal Total => Card + Cash + Transfer;
    public decimal ExpectedCash => OpeningFloat + Cash;
}

/// <summary>Takings for one day by method, for the end-of-day cash-up.</summary>
public class GetCashUpQuery : IRequest<Result<CashUp>>
{
    public DateOnly? Date { get; init; }
}

public class GetCashUpQueryHandler : IRequestHandler<GetCashUpQuery, Result<CashUp>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetCashUpQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<CashUp>> Handle(GetCashUpQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.TakePayments))
            return Result.Failure(GetCashUpError.NotAllowed, Access.Denied(Permission.TakePayments));

        var date = request.Date ?? ClinicTime.Today(_clock.UtcNow);
        var closed = await _dbContext.DayCloses.AsNoTracking().FirstOrDefaultAsync(d => d.Date == date, cancellationToken);
        if (closed is not null)
            return Result.Success(new CashUp(date, closed.CardTotal, closed.CashTotal, closed.TransferTotal, 0, closed.OpeningFloat,
                true, closed.CashCounted, closed.ClosedBy, Utc.Mark(closed.ClosedUtc)));

        var payments = await CashUpPayments.ForDayAsync(_dbContext, date, cancellationToken);
        var settings = await _dbContext.ClinicSettings.AsNoTracking().FirstAsync(cancellationToken);
        return Result.Success(new CashUp(date,
            payments.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.Amount),
            payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Amount),
            payments.Where(p => p.Method == PaymentMethod.Transfer).Sum(p => p.Amount),
            payments.Count, settings.OpeningFloat, false, null, null, null));
    }
}

internal static class CashUpPayments
{
    public static Task<List<Payment>> ForDayAsync(IVetDeskDbContext db, DateOnly date, CancellationToken cancellationToken)
    {
        var (start, end) = ClinicTime.DayRange(date);
        return db.Payments.Where(p => p.TakenUtc >= start && p.TakenUtc < end && p.DayCloseId == null).ToListAsync(cancellationToken);
    }
}
