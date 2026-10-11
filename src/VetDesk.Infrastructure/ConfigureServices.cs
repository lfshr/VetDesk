using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Infrastructure.Persistence;
using VetDesk.Infrastructure.Seeding;
using VetDesk.Infrastructure.Services;

namespace VetDesk.Infrastructure;

public static class ConfigureServices
{
    /// <summary>
    /// Registers the database and services. "VetDesk:DatabaseProvider" picks SqlServer (the practice setup, SQL Server
    /// on the practice server) or Sqlite (a single PC, development and demos).
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["VetDesk:DatabaseProvider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString("VetDesk")
            ?? throw new InvalidOperationException("Set ConnectionStrings:VetDesk in appsettings.json.");

        services.AddDbContext<VetDeskDbContext>(options =>
        {
            if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
                options.UseSqlite(ExpandPath(connectionString));
            else if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
            else
                throw new InvalidOperationException($"VetDesk:DatabaseProvider must be SqlServer or Sqlite, not '{provider}'.");
        });
        services.AddScoped<IVetDeskDbContext>(sp => sp.GetRequiredService<VetDeskDbContext>());

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<DatabaseInitializer>();
        services.AddScoped<DemoDataSeeder>();
        services.AddSingleton(new DatabaseOptions(
            configuration.GetValue("VetDesk:SeedDemoData", false),
            configuration["VetDesk:DefaultBackupFolder"]));
        services.AddSingleton<BackupScheduler>();
        services.AddSingleton<WrongPinTracker>();
        services.AddScoped<IDevicePinLimit, DevicePinLimit>();

        return services;
    }

    /// <summary>Lets a SQLite connection string say "%LOCALAPPDATA%\VetDesk\vetdesk.db" and creates the folder.</summary>
    private static string ExpandPath(string connectionString)
    {
        var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(Environment.ExpandEnvironmentVariables(connectionString));
        var folder = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);
        return builder.ToString();
    }
}

/// <param name="DefaultBackupFolder">Backup folder to use while Settings › Backup has none, so a new install is backed up
/// from its first night. The server install sets it to the backup volume.</param>
public record DatabaseOptions(bool SeedDemoData, string? DefaultBackupFolder = null);
