using MediatR;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Invoices.Queries;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Invoices.Commands;

public enum RefundPaymentError { NotAllowed, NotFound, InvalidRequest, MoreThanPaid, ChangedElsewhere }

/// <summary>Gives money back. Recorded as a negative payment so the day's cash-up nets it off.</summary>
public class RefundPaymentCommand : IRequest<Result>
{
    public int InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public PaymentMethod Method { get; init; } = PaymentMethod.Card;
    public string Reason { get; init; } = string.Empty;
}

public class RefundPaymentCommandHandler : IRequestHandler<RefundPaymentCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public RefundPaymentCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.TakePayments))
            return Result.Failure(RefundPaymentError.NotAllowed, Access.Denied(Permission.TakePayments));

        var amount = Money.Round(request.Amount);
        if (amount <= 0)
            return Result.Failure(RefundPaymentError.InvalidRequest, "Enter an amount more than £0.00.");
        if (Text.Clean(request.Reason) is not { } reason)
            return Result.Failure(RefundPaymentError.InvalidRequest, "Give a reason for the refund.");

        var invoice = await InvoiceQueries.LoadAsync(_dbContext, request.InvoiceId, tracking: true, cancellationToken);
        if (invoice is null)
            return Result.Failure(RefundPaymentError.NotFound, "That invoice no longer exists.");
        if (amount > invoice.Paid)
            return Result.Failure(RefundPaymentError.MoreThanPaid, $"Only £{invoice.Paid:N2} has been paid on {invoice.Number}.");

        invoice.Payments.Add(new Payment
        {
            Invoice = invoice,
            Method = request.Method,
            Amount = -amount,
            Reference = reason,
            TakenBy = _user.Name,
            TakenUtc = _clock.UtcNow,
        });
        invoice.Version = Guid.NewGuid();

        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(RefundPaymentError.ChangedElsewhere, Saving.ChangedElsewhere);
        return Result.Success();
    }
}
