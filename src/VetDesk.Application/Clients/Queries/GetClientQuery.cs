using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Clients.Queries;

public enum GetClientError { NotAllowed, NotFound }

public class GetClientQuery : IRequest<Result<ClientDetail>>
{
    public int Id { get; init; }
}

public class GetClientQueryHandler : IRequestHandler<GetClientQuery, Result<ClientDetail>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;

    public GetClientQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
    }

    public async Task<Result<ClientDetail>> Handle(GetClientQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ViewClinicalRecords))
            return Result.Failure(GetClientError.NotAllowed, Access.Denied(Permission.ViewClinicalRecords));

        var client = await _dbContext.Clients.AsNoTracking().Include(c => c.Patients)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
        if (client is null)
            return Result.Failure(GetClientError.NotFound, "That client no longer exists.");

        var balance = await Balances.ForClientAsync(_dbContext, client.Id, cancellationToken);
        return Result.Success(ClientMapping.ToDetail(client, balance, ClinicTime.Today(_clock.UtcNow)));
    }
}
