using Lca.Core.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class ProductInventoryConfiguration : IEntityTypeConfiguration<ProductInventory>
{
    public void Configure(EntityTypeBuilder<ProductInventory> builder)
    {
        builder.ToTable("ProductInventory", "dbo");
        builder.HasKey(value => new { value.TenantId, value.ProductId });
        foreach (string propertyName in new[] { nameof(ProductInventory.Balance), nameof(ProductInventory.CurrentStock), nameof(ProductInventory.MaximumStock), nameof(ProductInventory.MinimumStock), nameof(ProductInventory.Godown1Stock), nameof(ProductInventory.Godown2Stock) })
        {
            builder.Property(propertyName).HasColumnType("decimal(18,4)");
        }
        builder.HasOne(value => value.Product).WithOne(value => value.Inventory)
            .HasForeignKey<ProductInventory>(value => new { value.TenantId, value.ProductId })
            .HasPrincipalKey<Product>(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
