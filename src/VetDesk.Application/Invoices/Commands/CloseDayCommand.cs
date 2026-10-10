using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Invoices.Queries;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Invoices.Commands;

public enum CloseDayError { NotAllowed, InvalidRequest, AlreadyClosed, NoteRequired }

/// <summary>
/// "Close the day": records the takings and the cash counted, and stamps each payment so it can't be counted
/// again. A variance (over or short) needs a note explaining it.
/// </summary>
public class CloseDayCommand : IRequest<Result<decimal>>
{
    public DateOnly Date { get; init; }
    public decimal CashCounted { get; init; }
    public string? Note { get; init; }
}

public class CloseDayCommandHandler : IRequestHandler<CloseDayCommand, Result<decimal>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public CloseDayCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    /// <returns>The variance: positive when over, negative when short.</returns>
    public async Task<Result<decimal>> Handle(CloseDayCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.TakePayments))
            return Result.Failure(CloseDayError.NotAllowed, Access.Denied(Permission.TakePayments));
        if (request.CashCounted < 0)
            return Result.Failure(CloseDayError.InvalidRequest, "Cash counted can't be negative.");
        if (request.Date > ClinicTime.Today(_clock.UtcNow))
            return Result.Failure(CloseDayError.InvalidRequest, "You can't close a day that hasn't happened yet.");
        if (await _dbContext.DayCloses.AnyAsync(d => d.Date == request.Date, cancellationToken))
            return Result.Failure(CloseDayError.AlreadyClosed, $"{request.Date:dddd d MMMM} has already been closed.");

        var payments = await CashUpPayments.ForDayAsync(_dbContext, request.Date, cancellationToken);
        var settings = await _dbContext.ClinicSettings.AsNoTracking().FirstAsync(cancellationToken);

        var close = new DayClose
        {
            Date = request.Date,
            ClosedUtc = _clock.UtcNow,
            ClosedBy = _user.Name,
            CardTotal = payments.Where(p => p.Method == PaymentMethod.Card).Sum(p => p.Amount),
            CashTotal = payments.Where(p => p.Method == PaymentMethod.Cash).Sum(p => p.Amount),
            TransferTotal = payments.Where(p => p.Method == PaymentMethod.Transfer).Sum(p => p.Amount),
            OpeningFloat = settings.OpeningFloat,
            CashCounted = Money.Round(request.CashCounted),
            Note = Text.Clean(request.Note),
        };
        if (close.Variance != 0 && close.Note is null)
            return Result.Failure(CloseDayError.NoteRequired,
                $"The till is {(close.Variance > 0 ? "over" : "short")} by £{Math.Abs(close.Variance):N2}. Add a note saying why.");

        _dbContext.DayCloses.Add(close);
        foreach (var payment in payments)
            payment.DayClose = close;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(close.Variance);
    }
}
