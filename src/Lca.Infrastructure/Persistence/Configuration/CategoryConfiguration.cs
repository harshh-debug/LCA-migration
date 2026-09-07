using Lca.Core.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories", "dbo");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedOnAdd();
        builder.Property(value => value.Name).HasMaxLength(500).IsRequired();
        builder.Property(value => value.LegacyIconPath).HasMaxLength(1000);
        builder.Property(value => value.LegacyNotificationImagePath).HasMaxLength(1000);
        builder.HasAlternateKey(value => new { value.TenantId, value.Id });
        builder.HasIndex(value => new { value.TenantId, value.ParentCategoryId });
        builder.HasIndex(value => new { value.TenantId, value.Name });
        builder.HasOne<Lca.Core.Tenancy.Tenant>().WithMany()
            .HasForeignKey(value => value.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.ParentCategory).WithMany(value => value.Children)
            .HasForeignKey(value => new { value.TenantId, value.ParentCategoryId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
