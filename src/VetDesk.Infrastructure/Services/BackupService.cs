using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Infrastructure.Persistence;

namespace VetDesk.Infrastructure.Services;

/// <summary>
/// A full copy of the database. On SQL Server this is a native BACKUP, so the folder is on the database server's
/// disk (or a share its service account can write to). On SQLite it is VACUUM INTO, a consistent copy taken while
/// the app keeps running.
/// </summary>
public class BackupService : IBackupService
{
    private readonly VetDeskDbContext _dbContext;
    private readonly IClock _clock;

    public BackupService(VetDeskDbContext dbContext, IClock clock)
    {
        _dbContext = dbContext;
        _clock = clock;
    }

    public async Task<string> BackUpAsync(string folder, CancellationToken cancellationToken)
    {
        var stamp = _clock.UtcNow.ToString("yyyyMMdd-HHmmss");

        if (_dbContext.Database.IsSqlServer())
        {
            var path = Path.Combine(folder, $"VetDesk-{stamp}.bak");
            // BACKUP can't take the database name as a parameter, so the server quotes its own name with QUOTENAME and
            // only the file path is passed in, as a parameter.
            await _dbContext.Database.ExecuteSqlRawAsync("""
                DECLARE @sql nvarchar(max) = N'BACKUP DATABASE ' + QUOTENAME(DB_NAME()) + N' TO DISK = @path WITH INIT, CHECKSUM';
                EXEC sp_executesql @sql, N'@path nvarchar(4000)', @path = {0};
                """, [path], cancellationToken);
            return path;
        }

        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"VetDesk-{stamp}.db");
        await _dbContext.Database.ExecuteSqlRawAsync("VACUUM INTO {0}", [file], cancellationToken);
        return file;
    }
}
