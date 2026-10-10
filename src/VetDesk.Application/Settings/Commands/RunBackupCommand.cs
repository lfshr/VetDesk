using MediatR;
using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Domain.Entities.Practice;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Domain.Entities.Tasks;
using SimpleResult;

namespace VetDesk.Application.Settings.Commands;

public enum RunBackupError { NotAllowed, NoFolder, Failed }

/// <summary>
/// Writes a full copy of the database to the backup folder. Used by "Back up now" and by the nightly timer.
/// A failure is recorded and raised as a notification so it can't go unnoticed.
/// </summary>
public class RunBackupCommand : IRequest<Result<string>>
{
    /// <summary>Set by the nightly timer, which runs without anyone signed in.</summary>
    public bool Scheduled { get; init; }
}

public class RunBackupCommandHandler : IRequestHandler<RunBackupCommand, Result<string>>
{
    private readonly IVetDeskDbContext _dbContext;
    private readonly ICurrentUser _user;
    private readonly IClock _clock;
    private readonly IBackupService _backup;

    public RunBackupCommandHandler(IVetDeskDbContext dbContext, ICurrentUser user, IClock clock, IBackupService backup)
    {
        _dbContext = dbContext;
        _user = user;
        _clock = clock;
        _backup = backup;
    }

    public async Task<Result<string>> Handle(RunBackupCommand request, CancellationToken cancellationToken)
    {
        if (!request.Scheduled && _user.Lacks(Permission.ManageSettings))
            return Result.Failure(RunBackupError.NotAllowed, Access.Denied(Permission.ManageSettings));

        var settings = await _dbContext.ClinicSettings.FirstAsync(cancellationToken);
        var by = request.Scheduled ? "Nightly backup" : _user.Name;
        var now = _clock.UtcNow;

        if (!Text.Has(settings.BackupFolder))
        {
            Record(settings, now, by, null, "No backup folder is set.");
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure(RunBackupError.NoFolder, "Choose a backup folder in Settings › Backup and data first.");
        }

        try
        {
            var file = await _backup.BackUpAsync(settings.BackupFolder!, cancellationToken);
            Record(settings, now, by, file, null);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success(file);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Record(settings, now, by, null, ex.Message);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure(RunBackupError.Failed, ex);
        }
    }

    private void Record(ClinicSettings settings, DateTime now, string by, string? file, string? error)
    {
        _dbContext.BackupRecords.Add(new BackupRecord { StartedUtc = now, Succeeded = error is null, FilePath = file, Error = error, TriggeredBy = by });
        settings.LastBackupResult = error is null ? "Succeeded" : $"Failed: {error}";
        if (error is null)
            settings.LastBackupUtc = now;
        else
            Notify.Add(_dbContext, now, $"Backup failed: {error}", NotificationKind.Warning, "/settings/backup");
    }
}
