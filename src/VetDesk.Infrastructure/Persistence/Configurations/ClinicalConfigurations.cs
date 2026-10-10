using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetDesk.Domain.Entities.Appointments;
using VetDesk.Domain.Entities.Consultations;
using VetDesk.Domain.Entities.Dispensing;

namespace VetDesk.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasIndex(a => a.StartUtc);
        builder.Property(a => a.Status);
        builder.Property(a => a.CheckedInUtc);
    }
}

public class ConsultationConfiguration : IEntityTypeConfiguration<Consultation>
{
    public void Configure(EntityTypeBuilder<Consultation> builder)
    {
        foreach (var soap in new[] { nameof(Consultation.Subjective), nameof(Consultation.Objective), nameof(Consultation.Assessment), nameof(Consultation.Plan) })
            builder.Property(soap).HasMaxLength(8000);
        builder.Property(c => c.TemperatureC).HasPrecision(4, 1);
        builder.Property(c => c.WeightKg).HasPrecision(7, 3);
        builder.Property(c => c.Status);
        builder.Property(c => c.CompletedUtc);
        builder.HasOne(c => c.Invoice).WithMany().HasForeignKey(c => c.InvoiceId);
        builder.HasMany(c => c.Charges).WithOne(ch => ch.Consultation).HasForeignKey("ConsultationId").IsRequired();
        builder.HasIndex(c => c.StartedUtc);
    }
}

public class ConsultationChargeConfiguration : IEntityTypeConfiguration<ConsultationCharge>
{
    public void Configure(EntityTypeBuilder<ConsultationCharge> builder)
    {
        builder.Property(c => c.Quantity).HasPrecision(18, 3);
    }
}

public class DispenseItemConfiguration : IEntityTypeConfiguration<DispenseItem>
{
    public void Configure(EntityTypeBuilder<DispenseItem> builder)
    {
        builder.Property(d => d.Quantity).HasPrecision(18, 3);
        builder.HasIndex(d => d.Status);
    }
}
