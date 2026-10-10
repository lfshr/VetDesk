using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using SimpleResult;

namespace VetDesk.Application.Settings.Queries;

public enum GetClinicSettingsError { NotSignedIn }

public class GetClinicSettingsQuery : IRequest<Result<ClinicSettingsDto>>
{
}

public class GetClinicSettingsQueryHandler : IRequestHandler<GetClinicSettingsQuery, Result<ClinicSettingsDto>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;

    public GetClinicSettingsQueryHandler(IVetDeskDbContext dbContext, ICurrentUser user)
    {
        _dbContext = dbContext;
        _user = user;
    }

    public async Task<Result<ClinicSettingsDto>> Handle(GetClinicSettingsQuery request, CancellationToken cancellationToken)
    {
        if (!_user.IsSignedIn())
            return Result.Failure(GetClinicSettingsError.NotSignedIn, Access.NotSignedIn);

        var s = await _dbContext.ClinicSettings.AsNoTracking().FirstAsync(cancellationToken);
        return Result.Success(new ClinicSettingsDto(s.Name, s.Address, s.Phone, s.Email, s.VatNumber, s.WeekdayOpen, s.WeekdayClose,
            s.SaturdayOpen, s.SaturdayClose, s.LunchStart, s.LunchEnd, s.PaymentTermsDays, s.OpeningFloat, s.AutoLockMinutes,
            s.BackupFolder, s.BackupTime, Utc.Mark(s.LastBackupUtc), s.LastBackupResult, s.Version));
    }
}
