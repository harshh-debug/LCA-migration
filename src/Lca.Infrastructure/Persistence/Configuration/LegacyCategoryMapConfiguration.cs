using Lca.Core.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class LegacyCategoryMapConfiguration : IEntityTypeConfiguration<LegacyCategoryMap>
{
    public void Configure(EntityTypeBuilder<LegacyCategoryMap> builder)
    {
        builder.ToTable("LegacyCategoryMaps", "migration");
        builder.HasKey(value => new { value.TenantId, value.LegacyCategoryId });
        builder.Property(value => value.LegacyCategoryId).HasColumnType("decimal(18,0)");
        builder.HasIndex(value => new { value.TenantId, value.CategoryId }).IsUnique();
        builder.HasOne(value => value.Category).WithMany()
            .HasForeignKey(value => new { value.TenantId, value.CategoryId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
