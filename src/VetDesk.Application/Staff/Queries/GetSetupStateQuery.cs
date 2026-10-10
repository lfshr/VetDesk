using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Staff.Queries;

/// <summary>Whether anyone can sign in yet. A new database has no staff, so the sign-in screen offers set-up instead.</summary>
public class GetSetupStateQuery : IRequest<Result<bool>>
{
}

public class GetSetupStateQueryHandler : IRequestHandler<GetSetupStateQuery, Result<bool>>
{
    private readonly IVetDeskDbContext _dbContext;

    public GetSetupStateQueryHandler(IVetDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<bool>> Handle(GetSetupStateQuery request, CancellationToken cancellationToken) =>
        Result.Success(await _dbContext.StaffMembers.AnyAsync(cancellationToken));
}
