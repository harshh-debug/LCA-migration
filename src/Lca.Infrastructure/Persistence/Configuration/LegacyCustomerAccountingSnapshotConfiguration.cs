using Lca.Core.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class LegacyCustomerAccountingSnapshotConfiguration : IEntityTypeConfiguration<LegacyCustomerAccountingSnapshot>
{
    public void Configure(EntityTypeBuilder<LegacyCustomerAccountingSnapshot> builder)
    {
        builder.ToTable("LegacyCustomerAccountingSnapshots", "migration");
        builder.HasKey(value => new { value.TenantId, value.CustomerId });
        builder.Property(value => value.DueBalance).HasColumnType("decimal(18,2)");
        builder.Property(value => value.OpeningBalance).HasColumnType("decimal(18,2)");
        builder.Property(value => value.BlockLevel).HasMaxLength(20);
        builder.Property(value => value.PreviousBlockLevel).HasMaxLength(20);
        builder.HasOne(value => value.Customer).WithOne(value => value.LegacyAccountingSnapshot)
            .HasForeignKey<LegacyCustomerAccountingSnapshot>(value => new { value.TenantId, value.CustomerId })
            .HasPrincipalKey<Customer>(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
