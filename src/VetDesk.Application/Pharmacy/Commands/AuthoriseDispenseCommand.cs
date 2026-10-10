using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Dispensing;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Pharmacy.Commands;

public enum AuthoriseDispenseError { NotAllowed, NotFound, NotNeeded }

public class AuthoriseDispenseCommand : IRequest<Result>
{
    public int Id { get; init; }
}

public class AuthoriseDispenseCommandHandler : IRequestHandler<AuthoriseDispenseCommand, Result>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public AuthoriseDispenseCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result> Handle(AuthoriseDispenseCommand request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.Prescribe))
            return Result.Failure(AuthoriseDispenseError.NotAllowed, Access.Denied(Permission.Prescribe));

        var item = await _dbContext.DispenseItems.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);
        if (item is null || item.Status != DispenseStatus.Pending)
            return Result.Failure(AuthoriseDispenseError.NotFound, "That item is no longer in the queue.");
        if (!item.AwaitingAuthorisation)
            return Result.Failure(AuthoriseDispenseError.NotNeeded, "This item doesn't need authorising.");

        item.AuthorisedBy = _user.Name;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
