using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetDesk.Domain.Entities.Invoices;
using VetDesk.Domain.Entities.Practice;
using VetDesk.Domain.Entities.Purchasing;

namespace VetDesk.Infrastructure.Persistence.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasIndex(i => i.Number).IsUnique();
        builder.HasIndex(i => i.IssuedUtc);
        builder.Property(i => i.Net);
        builder.Property(i => i.Vat);
        builder.Property(i => i.Total);
        builder.HasMany(i => i.Lines).WithOne(l => l.Invoice).HasForeignKey("InvoiceId").IsRequired();
        builder.HasMany(i => i.Payments).WithOne(p => p.Invoice).HasForeignKey("InvoiceId").IsRequired();
        builder.HasOne(i => i.Claim).WithOne(c => c.Invoice).HasForeignKey<InsuranceClaim>("InvoiceId").IsRequired();
    }
}

public class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.Property(l => l.Quantity).HasPrecision(18, 3);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasIndex(p => p.TakenUtc);
        builder.HasOne(p => p.DayClose).WithMany().HasForeignKey(p => p.DayCloseId);
    }
}

public class DayCloseConfiguration : IEntityTypeConfiguration<DayClose>
{
    public void Configure(EntityTypeBuilder<DayClose> builder)
    {
        builder.HasIndex(d => d.Date).IsUnique();
    }
}

public class NumberSequenceConfiguration : IEntityTypeConfiguration<NumberSequence>
{
    public void Configure(EntityTypeBuilder<NumberSequence> builder)
    {
        builder.HasIndex(s => s.Name).IsUnique();
    }
}

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.HasIndex(o => o.Number).IsUnique();
        builder.HasMany(o => o.Lines).WithOne(l => l.Order).HasForeignKey("PurchaseOrderId").IsRequired();
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> builder)
    {
        builder.Property(l => l.Quantity).HasPrecision(18, 3);
    }
}

public class BackupRecordConfiguration : IEntityTypeConfiguration<BackupRecord>
{
    public void Configure(EntityTypeBuilder<BackupRecord> builder)
    {
        builder.Property(b => b.Error).HasMaxLength(2000);
        builder.Property(b => b.FilePath).HasMaxLength(1000);
    }
}

public class ClinicSettingsConfiguration : IEntityTypeConfiguration<ClinicSettings>
{
    public void Configure(EntityTypeBuilder<ClinicSettings> builder)
    {
        builder.Property(s => s.BackupFolder).HasMaxLength(1000);
        builder.Property(s => s.CompanyName).HasMaxLength(200);
        builder.Property(s => s.CompanyNumber).HasMaxLength(8);
        builder.Property(s => s.RegisteredOffice).HasMaxLength(400);
        builder.Property(s => s.BankAccountName).HasMaxLength(200);
        builder.Property(s => s.BankSortCode).HasMaxLength(8);
        builder.Property(s => s.BankAccountNumber).HasMaxLength(8);
        builder.Property(s => s.LastBackupResult).HasMaxLength(2000);
    }
}
