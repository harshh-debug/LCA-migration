using Lca.Core.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class LegacyCustomerMapConfiguration : IEntityTypeConfiguration<LegacyCustomerMap>
{
    public void Configure(EntityTypeBuilder<LegacyCustomerMap> builder)
    {
        builder.ToTable("LegacyCustomerMaps", "migration");
        builder.HasKey(value => new { value.TenantId, value.LegacyCustomerId });
        builder.Property(value => value.LegacyCustomerId).HasColumnType("decimal(18,0)");
        builder.Property(value => value.AccountNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(value => new { value.TenantId, value.CustomerId }).IsUnique();
        builder.HasOne(value => value.Customer).WithMany()
            .HasForeignKey(value => new { value.TenantId, value.CustomerId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LegacyCustomerContactMapConfiguration : IEntityTypeConfiguration<LegacyCustomerContactMap>
{
    public void Configure(EntityTypeBuilder<LegacyCustomerContactMap> builder)
    {
        builder.ToTable("LegacyCustomerContactMaps", "migration");
        builder.HasKey(value => new { value.TenantId, value.SourceFingerprint });
        builder.Property(value => value.SourceFingerprint).HasMaxLength(64);
        // Fixed-slot, normalized-table, and workbook sources may legitimately converge on one target contact.
        builder.HasIndex(value => new { value.TenantId, value.CustomerContactId });
        builder.HasOne(value => value.CustomerContact).WithMany()
            .HasForeignKey(value => new { value.TenantId, value.CustomerContactId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LegacyCustomerContactChangeMapConfiguration : IEntityTypeConfiguration<LegacyCustomerContactChangeMap>
{
    public void Configure(EntityTypeBuilder<LegacyCustomerContactChangeMap> builder)
    {
        builder.ToTable("LegacyCustomerContactChangeMaps", "migration");
        builder.HasKey(value => new { value.TenantId, value.SourceFingerprint });
        builder.Property(value => value.SourceFingerprint).HasMaxLength(64);
        builder.HasIndex(value => new { value.TenantId, value.CustomerContactChangeId }).IsUnique();
        builder.HasOne(value => value.CustomerContactChange).WithMany()
            .HasForeignKey(value => new { value.TenantId, value.CustomerContactChangeId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
