using Lca.Core.Catalog;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Mobile_ItemMaster", "dbo");
        builder.HasKey(product => product.ItemCode).HasName("PK_Mobile_ItemMaster");
        builder.Property(product => product.ItemCode).HasMaxLength(20).IsRequired().ValueGeneratedNever();
        builder.Property(product => product.Name).HasColumnName("Item").HasMaxLength(500);
        builder.Property(product => product.Description).HasColumnType("nvarchar(max)");
        builder.Property(product => product.Specification).HasColumnType("nvarchar(max)");
        builder.Property(product => product.CategoryId).HasColumnName("CategoryID").HasColumnType("numeric(18,0)");
        builder.Property(product => product.RetailRate).HasColumnName("Rretailrt").HasColumnType("numeric(18,2)");
        builder.Property(product => product.WholesaleRate).HasColumnName("Wholesaler").HasColumnType("numeric(18,2)");
        builder.Property(product => product.DealerRate).HasColumnName("Delearrt").HasColumnType("numeric(18,2)");
        builder.Property(product => product.IsDisabled).HasColumnName("Disable");
        builder.Property(product => product.Image1).HasMaxLength(500);
        builder.Property(product => product.Image2).HasMaxLength(500);
        builder.Property(product => product.Image3).HasMaxLength(500);
        builder.Property(product => product.Image4).HasMaxLength(500);
        builder.Property(product => product.Image5).HasMaxLength(500);
        builder.Property(product => product.Image6).HasMaxLength(500);
        builder.Property(product => product.Image7).HasMaxLength(500);
        builder.Property(product => product.Image8).HasMaxLength(500);
        builder.Property(product => product.Image9).HasMaxLength(500);
        builder.Property(product => product.ThumbnailImage).HasColumnName("ThumbImage").HasMaxLength(500);
        builder.Property(product => product.IsDraft).HasDefaultValue(false);
        builder.Property(product => product.CreatedSource).HasColumnType("varchar(50)").HasDefaultValue("Manual").IsRequired();

        // CategoryID is an application-observed lookup only. No FK is declared in the legacy database.
    }
}
