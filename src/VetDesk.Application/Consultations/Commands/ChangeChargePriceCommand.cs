using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Consultations.Commands;

public enum ChangeChargePriceError { NotAllowed, NotFound, Locked, InvalidRequest }

/// <summary>Changes the price of a line already on an open consultation (clicking its price on the Charges list).</summary>
public class ChangeChargePriceCommand : IRequest<Result>
{
    public int ChargeId { get; init; }

    /// <summary>Net unit price.</summary>
    public decimal UnitPrice { get; init; }

    /// <summary>Why the price is below the list price (required then).</summary>
    public string? Reason { get; init; }
}

public class ChangeChargePriceCommandHandler : IRequestHandler<ChangeChargePriceCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public ChangeChargePriceCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result> Handle(ChangeChargePriceCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(ChangeChargePriceError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));

        var charge = await _dbContext.ConsultationCharges.Include(c => c.Consultation).Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == request.ChargeId, cancellationToken);
        if (charge is null)
            return Result.Failure(ChangeChargePriceError.NotFound, "That item has been removed from the consultation.");
        if (charge.Consultation.IsLocked)
            return Result.Failure(ChangeChargePriceError.Locked, "This consultation is completed and locked.");

        if (ChargePrice.Set(charge, request.UnitPrice, request.Reason, _user.Name) is { } error)
            return Result.Failure(ChangeChargePriceError.InvalidRequest, error);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
