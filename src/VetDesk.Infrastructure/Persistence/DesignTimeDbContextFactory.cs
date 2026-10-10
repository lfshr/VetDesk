using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace VetDesk.Infrastructure.Persistence;

/// <summary>
/// Used only by "dotnet ef migrations add". Migrations are for SQL Server (the practice database); SQLite installs
/// are created from the model instead. The connection string is never opened when adding a migration.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VetDeskDbContext>
{
    public VetDeskDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<VetDeskDbContext>()
            .UseSqlServer("Server=localhost\\SQLEXPRESS;Database=VetDesk;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new VetDeskDbContext(options);
    }
}
