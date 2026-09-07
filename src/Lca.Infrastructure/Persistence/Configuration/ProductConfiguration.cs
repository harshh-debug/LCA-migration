using Lca.Core.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", "dbo");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedOnAdd();
        builder.HasAlternateKey(value => new { value.TenantId, value.Id });
        builder.Property(value => value.ItemCode).HasMaxLength(20).IsRequired();
        builder.Property(value => value.Name).HasMaxLength(500).IsRequired();
        builder.Property(value => value.Unit).HasMaxLength(50);
        builder.Property(value => value.AlternateItemCode).HasMaxLength(100);
        builder.Property(value => value.GujaratiName).HasMaxLength(500);
        builder.Property(value => value.UnitKilograms).HasColumnType("decimal(18,4)");
        builder.Property(value => value.Group1).HasMaxLength(200);
        builder.Property(value => value.Group2).HasMaxLength(200);
        builder.Property(value => value.ChapterNumber).HasMaxLength(100);
        builder.Property(value => value.HsnNumber).HasMaxLength(100);
        builder.Property(value => value.ItemType).HasMaxLength(100);
        builder.Property(value => value.Packing).HasMaxLength(100);
        builder.Property(value => value.ManufacturerName).HasMaxLength(500);
        builder.Property(value => value.Location).HasMaxLength(500);
        builder.Property(value => value.AlternateLocation).HasMaxLength(500);
        builder.Property(value => value.Description).HasColumnType("nvarchar(max)");
        builder.Property(value => value.Remark).HasColumnType("nvarchar(max)");
        builder.Property(value => value.SalesmanCommission).HasColumnType("decimal(18,4)");
        builder.HasIndex(value => new { value.TenantId, value.ItemCode }).IsUnique();
        builder.HasIndex(value => new { value.TenantId, value.CategoryId });
        builder.HasOne<Lca.Core.Tenancy.Tenant>().WithMany()
            .HasForeignKey(value => value.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(value => value.Category).WithMany(value => value.Products)
            .HasForeignKey(value => new { value.TenantId, value.CategoryId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
