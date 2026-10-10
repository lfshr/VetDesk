using MediatR;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Invoices.Queries;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Invoices.Commands;

public enum TakePaymentError { NotAllowed, NotFound, InvalidRequest, Overpayment, ChangedElsewhere }

/// <summary>
/// Records money taken against an invoice. Card payments are taken on the practice's own terminal and recorded
/// here: VetDesk v1 doesn't talk to card terminals.
/// </summary>
public class TakePaymentCommand : IRequest<Result<decimal>>
{
    public int InvoiceId { get; init; }
    public PaymentMethod Method { get; init; }
    public decimal Amount { get; init; }

    /// <summary>Terminal name or transfer reference.</summary>
    public string? Reference { get; init; }

    public Guid? Version { get; init; }
}

public class TakePaymentCommandHandler : IRequestHandler<TakePaymentCommand, Result<decimal>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public TakePaymentCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    /// <returns>The amount still due afterwards.</returns>
    public async Task<Result<decimal>> Handle(TakePaymentCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.TakePayments))
            return Result.Failure(TakePaymentError.NotAllowed, Access.Denied(Permission.TakePayments));

        var amount = Money.Round(request.Amount);
        if (amount <= 0)
            return Result.Failure(TakePaymentError.InvalidRequest, "Enter an amount more than £0.00.");
        if (request.Method == PaymentMethod.Transfer && !Text.Has(request.Reference))
            return Result.Failure(TakePaymentError.InvalidRequest, "Enter the bank transfer reference.");

        var invoice = await InvoiceQueries.LoadAsync(_dbContext, request.InvoiceId, tracking: true, cancellationToken);
        if (invoice is null)
            return Result.Failure(TakePaymentError.NotFound, "That invoice no longer exists.");
        if (request.Version is { } version && invoice.Version != version)
            return Result.Failure(TakePaymentError.ChangedElsewhere, "This invoice was updated at another desk. Check the amount due and try again.");
        if (amount > invoice.AmountDue)
            return Result.Failure(TakePaymentError.Overpayment, $"Only £{invoice.AmountDue:N2} is due on {invoice.Number}.");

        invoice.Payments.Add(new Payment
        {
            Invoice = invoice,
            Method = request.Method,
            Amount = amount,
            Reference = Text.Clean(request.Reference),
            TakenBy = _user.Name,
            TakenUtc = _clock.UtcNow,
        });

        // Touching the invoice moves its version on, so two desks can't both take the last payment.
        invoice.Version = Guid.NewGuid();
        if (!await _dbContext.TrySaveChangesAsync(cancellationToken))
            return Result.Failure(TakePaymentError.ChangedElsewhere, "This invoice was updated at another desk. Check the amount due and try again.");
        return Result.Success(invoice.AmountDue);
    }
}
