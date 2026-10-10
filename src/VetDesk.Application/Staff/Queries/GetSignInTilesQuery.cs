using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Staff.Queries;

/// <summary>Everyone who can sign in, for the name tiles. Needs no sign-in: the desks are inside the practice.</summary>
public class GetSignInTilesQuery : IRequest<Result<IReadOnlyList<SignInTileDto>>>
{
}

public class GetSignInTilesQueryHandler : IRequestHandler<GetSignInTilesQuery, Result<IReadOnlyList<SignInTileDto>>>
{
    private readonly IVetDeskDbContext _dbContext;

    public GetSignInTilesQueryHandler(IVetDeskDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<IReadOnlyList<SignInTileDto>>> Handle(GetSignInTilesQuery request, CancellationToken cancellationToken)
    {
        var staff = await _dbContext.StaffMembers.AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.FirstName)
            .ToListAsync(cancellationToken);
        IReadOnlyList<SignInTileDto> tiles = staff.Select(s => new SignInTileDto(s.Id, s.ShortName, s.Initials, s.JobTitle, s.HasPin, !s.HasPin && s.HasPassword)).ToList();
        return Result.Success(tiles);
    }
}
