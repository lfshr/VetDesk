using Microsoft.EntityFrameworkCore;
using VetDesk.Application.Common.Interfaces;
using VetDesk.Application.Common.Live;
using VetDesk.Domain.Common;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Consultations;
using VetDesk.Domain.Entities.Dispensing;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Patients;
using VetDesk.Domain.Entities.Practice;
using VetDesk.Domain.Entities.Products;
using VetDesk.Domain.Entities.Purchasing;
using VetDesk.Domain.Entities.Staff;
using VetDesk.Domain.Entities.Tasks;

namespace VetDesk.Infrastructure.Persistence;

public class VetDeskDbContext : DbContext, IVetDeskDbContext
{
    private readonly ICurrentUser? _user;
    private readonly IClock? _clock;
    private readonly RecordChangeFeed? _feed;
    private readonly IDesk? _desk;

    public VetDeskDbContext(DbContextOptions<VetDeskDbContext> options, ICurrentUser? user = null, IClock? clock = null,
        RecordChangeFeed? feed = null, IDesk? desk = null)
        : base(options)
    {
        _user = user;
        _clock = clock;
        _feed = feed;
        _desk = desk;
    }

    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientAlert> PatientAlerts => Set<PatientAlert>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<WeightRecord> WeightRecords => Set<WeightRecord>();
    public DbSet<Vaccination> Vaccinations => Set<Vaccination>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<ControlledDrugEntry> ControlledDrugEntries => Set<ControlledDrugEntry>();
    public DbSet<AppointmentType> AppointmentTypes => Set<AppointmentType>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Consultation> Consultations => Set<Consultation>();
    public DbSet<ConsultationCharge> ConsultationCharges => Set<ConsultationCharge>();
    public DbSet<DispenseItem> DispenseItems => Set<DispenseItem>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<InsuranceClaim> InsuranceClaims => Set<InsuranceClaim>();
    public DbSet<DayClose> DayCloses => Set<DayClose>();
    public DbSet<ClinicSettings> ClinicSettings => Set<ClinicSettings>();
    public DbSet<NumberSequence> NumberSequences => Set<NumberSequence>();
    public DbSet<BackupRecord> BackupRecords => Set<BackupRecord>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Money everywhere unless a configuration says otherwise (quantities use three places).
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<string>().HaveMaxLength(400);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VetDeskDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IHasVersion).IsAssignableFrom(entityType.ClrType))
                modelBuilder.Entity(entityType.ClrType).Property(nameof(IHasVersion.Version)).IsConcurrencyToken();

            // Enums are stored by name so the database stays readable and reordering an enum can't corrupt data.
            foreach (var property in entityType.GetProperties())
            {
                var type = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
                if (type.IsEnum)
                {
                    property.SetProviderClrType(typeof(string));
                    property.SetMaxLength(40);
                }
            }

            // Clinical and financial records are never removed by a cascade.
            foreach (var foreignKey in entityType.GetForeignKeys())
                foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        Stamp();
        var topics = _feed is null ? null : ChangeTopics.Capture(ChangeTracker);
        var saved = await base.SaveChangesAsync(cancellationToken);
        Publish(topics);
        return saved;
    }

    public override int SaveChanges()
    {
        Stamp();
        var topics = _feed is null ? null : ChangeTopics.Capture(ChangeTracker);
        var saved = base.SaveChanges();
        Publish(topics);
        return saved;
    }

    // Only after the save succeeded, so no desk refreshes for a change that was refused (e.g. a version conflict).
    private void Publish(ChangeTopics? topics)
    {
        if (topics is null)
            return;
        var who = _user?.StaffId is not null ? _user.Name : "System";
        _feed!.Publish(new RecordChange(topics.Complete(), who, _desk?.DeskId));
    }

    private void Stamp()
    {
        var now = _clock?.UtcNow ?? DateTime.UtcNow;
        var who = _user?.StaffId is not null ? _user.Name : "System";

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            if (entry.Entity is IHasVersion versioned)
                versioned.Version = Guid.NewGuid();

            if (entry.Entity is BaseAuditableEntity audited)
            {
                if (entry.State == EntityState.Added)
                {
                    audited.CreatedUtc = now;
                    audited.CreatedBy = who;
                }
                else
                {
                    audited.LastModifiedUtc = now;
                    audited.LastModifiedBy = who;
                }
            }
        }
    }
}
