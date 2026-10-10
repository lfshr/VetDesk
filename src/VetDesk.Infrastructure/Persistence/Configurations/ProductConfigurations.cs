using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetDesk.Domain.Entities.Products;

namespace VetDesk.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasIndex(p => p.Name);
        builder.Property(p => p.ReorderLevel).HasPrecision(18, 3);
        builder.HasMany(p => p.Batches).WithOne(b => b.Product).HasForeignKey("ProductId").IsRequired();
    }
}

public class StockBatchConfiguration : IEntityTypeConfiguration<StockBatch>
{
    public void Configure(EntityTypeBuilder<StockBatch> builder)
    {
        builder.Property(b => b.Quantity).HasPrecision(18, 3);
    }
}

public class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.Property(m => m.Change).HasPrecision(18, 3);
    }
}

public class ControlledDrugEntryConfiguration : IEntityTypeConfiguration<ControlledDrugEntry>
{
    public void Configure(EntityTypeBuilder<ControlledDrugEntry> builder)
    {
        builder.Property(e => e.Change).HasPrecision(18, 3);
        builder.Property(e => e.Balance).HasPrecision(18, 3);
        builder.HasIndex(e => e.Utc);
    }
}
