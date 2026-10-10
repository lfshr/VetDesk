using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Infrastructure;
using VetDesk.Infrastructure.Persistence;
using VetDesk.Infrastructure.Seeding;
using VetDesk.Infrastructure.Services;

namespace VetDesk.Application.UnitTests.Handlers;

[TestFixture]
public class BackupTests
{
    private TestDatabase _database = null!;

    [SetUp]
    public async Task SetUp() => _database = await TestDatabase.CreateAsync();

    [TearDown]
    public void TearDown() => _database.Dispose();

    [Test]
    public async Task Startup_UsesTheDefaultBackupFolder_OnlyWhileNoneIsSet()
    {
        await InitialiseAsync("/var/opt/mssql/backup");
        (await FolderAsync()).ShouldBe("/var/opt/mssql/backup");

        var settings = await _database.Db.ClinicSettings.FirstAsync();
        settings.BackupFolder = "/mnt/usb";
        await _database.Db.SaveChangesAsync();
        await InitialiseAsync("/var/opt/mssql/backup");

        (await FolderAsync()).ShouldBe("/mnt/usb");
    }

    [Test]
    public async Task Nightly_WithNoFolderSet_IsRecordedAsAFailureWithANotice_OncePerDay()
    {
        (await FolderAsync()).ShouldBeNull();
        var scheduler = Scheduler();

        await scheduler.CheckAsync(CancellationToken.None);
        await scheduler.CheckAsync(CancellationToken.None);

        var db = _database.NewContext();
        var record = (await db.BackupRecords.ToListAsync()).ShouldHaveSingleItem();
        record.Succeeded.ShouldBeFalse();
        record.Error.ShouldBe("No backup folder is set.");
        (await db.Notifications.CountAsync(n => n.Text.StartsWith("Backup failed"))).ShouldBe(1);
    }

    private async Task InitialiseAsync(string defaultFolder) =>
        await new DatabaseInitializer(_database.Db, new DemoDataSeeder(_database.Db, _database.Clock, _database.Hasher),
            new DatabaseOptions(false, defaultFolder), NullLogger<DatabaseInitializer>.Instance).InitialiseAsync();

    private async Task<string?> FolderAsync() =>
        (await _database.NewContext().ClinicSettings.AsNoTracking().FirstAsync()).BackupFolder;

    private BackupScheduler Scheduler()
    {
        var services = new ServiceCollection()
            .AddApplicationServices()
            .AddScoped(_ => _database.NewContext())
            .AddScoped<IVetDeskDbContext>(sp => sp.GetRequiredService<VetDeskDbContext>())
            .AddSingleton<ICurrentUser>(_database.User)
            .AddSingleton<IClock>(_database.Clock)
            .AddSingleton<IBackupService, UnusedBackupService>();
        return new BackupScheduler(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), _database.Clock,
            NullLogger<BackupScheduler>.Instance);
    }

    private sealed class UnusedBackupService : IBackupService
    {
        public Task<string> BackUpAsync(string folder, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("No folder is set, so nothing should be written.");
    }
}
