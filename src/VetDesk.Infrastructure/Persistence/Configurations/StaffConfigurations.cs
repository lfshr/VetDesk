using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetDesk.Domain.Entities.Staff;

namespace VetDesk.Infrastructure.Persistence.Configurations;

public class StaffMemberConfiguration : IEntityTypeConfiguration<StaffMember>
{
    public void Configure(EntityTypeBuilder<StaffMember> builder)
    {
        // Email is optional; SQL Server only enforces uniqueness on rows that have one.
        builder.HasIndex(s => s.Email).IsUnique();
        builder.Property(s => s.PasswordHash).HasMaxLength(200);
        builder.Property(s => s.PinHash).HasMaxLength(200);
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.HasIndex(rp => new { rp.Role, rp.Permission }).IsUnique();
    }
}
