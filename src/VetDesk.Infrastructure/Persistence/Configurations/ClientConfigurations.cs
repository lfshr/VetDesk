using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetDesk.Domain.Entities.Clients;
using VetDesk.Domain.Entities.Patients;

namespace VetDesk.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.Property(c => c.Phone).HasField("_phone").HasMaxLength(40);
        builder.Property(c => c.PhoneDigits).HasMaxLength(20);
        builder.HasIndex(c => c.PhoneDigits);
        builder.HasIndex(c => c.LastName);
        builder.HasMany(c => c.Patients).WithOne(p => p.Client).HasForeignKey("ClientId").IsRequired();
    }
}

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.HasIndex(p => p.Name);
        builder.HasIndex(p => p.Microchip);
        builder.HasMany(p => p.Alerts).WithOne(a => a.Patient).HasForeignKey("PatientId").IsRequired();
        builder.HasMany(p => p.Allergies).WithOne(a => a.Patient).HasForeignKey("PatientId").IsRequired();
    }
}

public class WeightRecordConfiguration : IEntityTypeConfiguration<WeightRecord>
{
    public void Configure(EntityTypeBuilder<WeightRecord> builder)
    {
        builder.Property(w => w.Kg).HasPrecision(7, 3);
    }
}

public class ClinicalNoteConfiguration : IEntityTypeConfiguration<ClinicalNote>
{
    public void Configure(EntityTypeBuilder<ClinicalNote> builder)
    {
        builder.Property(n => n.Body).HasMaxLength(8000);
    }
}
