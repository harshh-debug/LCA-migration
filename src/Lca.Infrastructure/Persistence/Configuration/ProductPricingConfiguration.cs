using Lca.Core.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class ProductPricingConfiguration : IEntityTypeConfiguration<ProductPricing>
{
    public void Configure(EntityTypeBuilder<ProductPricing> builder)
    {
        builder.ToTable("ProductPricing", "dbo");
        builder.HasKey(value => new { value.TenantId, value.ProductId });
        foreach (string propertyName in new[] { nameof(ProductPricing.PurchaseRate), nameof(ProductPricing.DealerRate), nameof(ProductPricing.WholesaleRate), nameof(ProductPricing.RetailRate), nameof(ProductPricing.OtherRate), nameof(ProductPricing.VatRate), nameof(ProductPricing.AdditionalVatRate), nameof(ProductPricing.CstRate), nameof(ProductPricing.IgstRate), nameof(ProductPricing.SgstRate), nameof(ProductPricing.CgstRate) })
        {
            builder.Property(propertyName).HasColumnType("decimal(18,4)");
        }
        builder.HasOne(value => value.Product).WithOne(value => value.Pricing)
            .HasForeignKey<ProductPricing>(value => new { value.TenantId, value.ProductId })
            .HasPrincipalKey<Product>(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
