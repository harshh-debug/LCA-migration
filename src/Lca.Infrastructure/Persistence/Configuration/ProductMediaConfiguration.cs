using Lca.Core.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class ProductMediaConfiguration : IEntityTypeConfiguration<ProductMedia>
{
    public void Configure(EntityTypeBuilder<ProductMedia> builder)
    {
        builder.ToTable("ProductMedia", "dbo");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.LegacyPath).HasMaxLength(1000).IsRequired();
        builder.HasIndex(value => new { value.TenantId, value.ProductId, value.SortOrder });
        builder.HasOne(value => value.Product).WithMany(value => value.Media)
            .HasForeignKey(value => new { value.TenantId, value.ProductId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
