using MediatR;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Invoices.Queries;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Invoices.Commands;

public enum BillInsurerError { NotAllowed, NotFound, InvalidRequest, AlreadyClaimed }

/// <summary>Records that an insurance claim has been sent for this invoice. The claim form itself goes outside VetDesk in v1.</summary>
public class BillInsurerCommand : IRequest<Result>
{
    public int InvoiceId { get; init; }
    public string Insurer { get; init; } = string.Empty;
    public string PolicyNumber { get; init; } = string.Empty;
    public decimal Excess { get; init; }
    public string Condition { get; init; } = string.Empty;
}

public class BillInsurerCommandHandler : IRequestHandler<BillInsurerCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public BillInsurerCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result> Handle(BillInsurerCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.TakePayments))
            return Result.Failure(BillInsurerError.NotAllowed, Access.Denied(Permission.TakePayments));
        if (!Text.Has(request.Insurer) || !Text.Has(request.PolicyNumber) || !Text.Has(request.Condition))
            return Result.Failure(BillInsurerError.InvalidRequest, "Enter the insurer, policy number and condition.");
        if (request.Excess < 0)
            return Result.Failure(BillInsurerError.InvalidRequest, "The excess can't be negative.");

        var invoice = await InvoiceQueries.LoadAsync(_dbContext, request.InvoiceId, tracking: true, cancellationToken);
        if (invoice is null)
            return Result.Failure(BillInsurerError.NotFound, "That invoice no longer exists.");
        if (invoice.Claim is not null)
            return Result.Failure(BillInsurerError.AlreadyClaimed, $"A claim is already recorded for {invoice.Number}.");

        invoice.Claim = new InsuranceClaim
        {
            Invoice = invoice,
            Insurer = request.Insurer.Trim(),
            PolicyNumber = request.PolicyNumber.Trim(),
            Excess = Money.Round(request.Excess),
            Condition = request.Condition.Trim(),
            CreatedUtc = _clock.UtcNow,
        };
        invoice.Version = Guid.NewGuid();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
