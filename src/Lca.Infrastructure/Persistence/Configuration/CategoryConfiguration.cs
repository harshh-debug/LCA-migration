using Lca.Core.Catalog;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("CategoryMastertbl", "dbo");
        builder.HasKey(category => category.Id).HasName("PK_CategoryMastertbl");
        builder.Property(category => category.Id)
            .HasColumnName("CategoryID")
            .HasColumnType("numeric(18,0)")
            .ValueGeneratedOnAdd();
        builder.Property(category => category.Name).HasColumnName("CategoryName").HasMaxLength(1000);
        builder.Property(category => category.Icon).HasColumnName("CategoryIcon").HasColumnType("nvarchar(max)");
        builder.Property(category => category.ParentCategoryId).HasColumnName("ParentCategoryID").HasColumnType("numeric(18,0)");
        builder.Property(category => category.NotificationImage).HasColumnType("nvarchar(max)");

        // The exported self-FK is anomalous; do not infer a parent navigation from ParentCategoryID.
    }
}
