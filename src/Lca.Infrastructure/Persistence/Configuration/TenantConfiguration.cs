using Lca.Core.Tenancy;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    private static readonly DateTime InitialTimestamp = new(2026, 9, 3, 0, 0, 0, DateTimeKind.Utc);

    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", "dbo");
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).ValueGeneratedOnAdd();
        builder.Property(tenant => tenant.Name).HasMaxLength(200).IsRequired();
        builder.Property(tenant => tenant.Slug).HasMaxLength(100).IsRequired();
        builder.Property(tenant => tenant.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(tenant => tenant.Slug).IsUnique();
        builder.HasData(new Tenant
        {
            Id = 1,
            Name = "Laxmi Ceramic",
            Slug = "lca",
            Status = TenantStatus.Active,
            CreatedAtUtc = InitialTimestamp,
            UpdatedAtUtc = InitialTimestamp,
        });
    }
}
