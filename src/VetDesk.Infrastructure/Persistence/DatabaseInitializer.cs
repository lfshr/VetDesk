using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Practice;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Infrastructure.Seeding;

namespace VetDesk.Infrastructure.Persistence;

/// <summary>
/// Brings the database up to date when the app starts: migrations on SQL Server, create-if-missing on SQLite,
/// then the rows every practice needs (permissions grid, clinic settings, appointment types).
/// </summary>
public class DatabaseInitializer
{
    private readonly VetDeskDbContext _dbContext;
    private readonly DemoDataSeeder _demo;
    private readonly DatabaseOptions _options;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(VetDeskDbContext dbContext, DemoDataSeeder demo, DatabaseOptions options, ILogger<DatabaseInitializer> logger)
    {
        _dbContext = dbContext;
        _demo = demo;
        _options = options;
        _logger = logger;
    }

    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        if (_dbContext.Database.IsSqlServer())
            await _dbContext.Database.MigrateAsync(cancellationToken);
        else
            await _dbContext.Database.EnsureCreatedAsync(cancellationToken);

        await SeedRequiredAsync(cancellationToken);

        if (_options.SeedDemoData && !await _dbContext.StaffMembers.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Empty database and VetDesk:SeedDemoData is on: adding demo data.");
            await _demo.SeedAsync(cancellationToken);
        }
    }

    private async Task SeedRequiredAsync(CancellationToken cancellationToken)
    {
        if (!await _dbContext.RolePermissions.AnyAsync(cancellationToken))
            _dbContext.RolePermissions.AddRange(RolePermission.Defaults());

        if (!await _dbContext.ClinicSettings.AnyAsync(cancellationToken))
            _dbContext.ClinicSettings.Add(new ClinicSettings());

        if (!await _dbContext.AppointmentTypes.AnyAsync(cancellationToken))
            _dbContext.AppointmentTypes.AddRange(DefaultAppointmentTypes());

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>The list on the design's Settings › Appointment types page.</summary>
    internal static IEnumerable<AppointmentType> DefaultAppointmentTypes() =>
    [
        new() { Name = "Consultation", Kind = AppointmentKind.Consultation, Minutes = 15, SeenBy = "Vet", OnlineBooking = true, Colour = AppointmentColour.Teal },
        new() { Name = "Vaccination", Kind = AppointmentKind.Consultation, Minutes = 15, SeenBy = "Vet", OnlineBooking = true, Colour = AppointmentColour.Teal },
        new() { Name = "Nurse clinic", Kind = AppointmentKind.Consultation, Minutes = 15, SeenBy = "Nurse", OnlineBooking = true, Colour = AppointmentColour.Blue },
        new() { Name = "Recheck", Kind = AppointmentKind.Consultation, Minutes = 15, SeenBy = "Vet", Colour = AppointmentColour.Teal },
        new() { Name = "Dental", Kind = AppointmentKind.Surgery, Minutes = 60, SeenBy = "Vet", Colour = AppointmentColour.Purple },
        new() { Name = "Surgery", Kind = AppointmentKind.Surgery, Minutes = 120, SeenBy = "Vet", Colour = AppointmentColour.Purple },
        new() { Name = "Euthanasia", Kind = AppointmentKind.Consultation, Minutes = 30, SeenBy = "Vet", Colour = AppointmentColour.Amber },
    ];
}
