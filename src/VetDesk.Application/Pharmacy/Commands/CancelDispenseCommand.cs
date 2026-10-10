using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Dispensing;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Commands;

public enum CancelDispenseError { NotAllowed, NotFound }

/// <summary>Takes an item off the queue without touching stock, e.g. the owner declined it.</summary>
public class CancelDispenseCommand : IRequest<Result>
{
    public int Id { get; init; }
}

public class CancelDispenseCommandHandler : IRequestHandler<CancelDispenseCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public CancelDispenseCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result> Handle(CancelDispenseCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.WriteClinicalNotes))
            return Result.Failure(CancelDispenseError.NotAllowed, Access.Denied(Permission.WriteClinicalNotes));

        var item = await _dbContext.DispenseItems.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
        if (item is null || item.Status != DispenseStatus.Pending)
            return Result.Failure(CancelDispenseError.NotFound, "That item is no longer in the queue.");

        item.Status = DispenseStatus.Cancelled;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
