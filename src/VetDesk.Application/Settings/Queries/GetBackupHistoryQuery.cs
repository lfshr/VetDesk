using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Staff;
using SimpleResult;

namespace VetDesk.Application.Settings.Queries;

public enum GetBackupHistoryError { NotAllowed }

public class GetBackupHistoryQuery : IRequest<Result<IReadOnlyList<BackupDto>>>
{
    public int Take { get; init; } = 10;
}

public class GetBackupHistoryQueryHandler : IRequestHandler<GetBackupHistoryQuery, Result<IReadOnlyList<BackupDto>>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetBackupHistoryQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<IReadOnlyList<BackupDto>>> Handle(GetBackupHistoryQuery request, CancellationToken cancellationToken)
    {
        if (_user.Lacks(Permission.ManageSettings))
            return Result.Failure(GetBackupHistoryError.NotAllowed, Access.Denied(Permission.ManageSettings));

        var rows = await _dbContext.BackupRecords.AsNoTracking().OrderByDescending(b => b.StartedUtc).Take(request.Take).ToListAsync(cancellationToken);
        IReadOnlyList<BackupDto> dtos = rows.Select(b => new BackupDto(Utc.Mark(b.StartedUtc), b.Succeeded, b.FilePath, b.Error, b.TriggeredBy)).ToList();
        return Result.Success(dtos);
    }
}
