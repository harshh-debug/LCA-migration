using Lca.Core.Governance;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class AIImageConfiguration : IEntityTypeConfiguration<AIImage>
{
    public void Configure(EntityTypeBuilder<AIImage> builder)
    {
        builder.ToTable("AIImages", "dbo", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_AIImages_SlotPosition", "[SlotPosition] BETWEEN 1 AND 9");
            tableBuilder.HasCheckConstraint("CK_AIImages_Status", "[Status] IN ('Draft','Approved','Rejected')");
        });
        builder.HasKey(image => image.Id).HasName("PK_AIImages");
        builder.Property(image => image.Id).ValueGeneratedOnAdd();
        builder.Property(image => image.ProductId).HasMaxLength(50).IsRequired();
        builder.Property(image => image.ImageUrl).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(image => image.Status).HasConversion<string>().HasColumnType("varchar(20)").HasDefaultValue(AIImageStatus.Draft);
        builder.Property(image => image.AgentId).HasColumnType("varchar(50)").IsRequired();
        builder.Property(image => image.ApprovedBy).HasMaxLength(50);
        builder.Property(image => image.ApprovedAt).HasColumnType("datetime2");

        builder.HasIndex(image => new { image.ProductId, image.SlotPosition, image.Status });
        builder.HasOne(image => image.Product)
            .WithMany()
            .HasForeignKey(image => image.ProductId)
            .HasPrincipalKey(product => product.ItemCode)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_AIImages_Mobile_ItemMaster");
    }
}
