using Lca.Core.Governance;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class ApprovalQueueConfiguration : IEntityTypeConfiguration<ApprovalQueueItem>
{
    public void Configure(EntityTypeBuilder<ApprovalQueueItem> builder)
    {
        builder.ToTable("ApprovalQueue", "dbo", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_ApprovalQueue_EntityType", "[EntityType] IN ('Product','Media','MarketingPost','LogisticsBooking')");
            tableBuilder.HasCheckConstraint("CK_ApprovalQueue_Status", "[Status] IN ('Pending','Approved','Rejected')");
        });
        builder.HasKey(item => item.Id).HasName("PK_ApprovalQueue");
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.TenantId).HasMaxLength(100).IsRequired();
        builder.Property(item => item.EntityType).HasConversion<string>().HasColumnType("varchar(30)").IsRequired();
        builder.Property(item => item.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(item => item.DraftPayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(item => item.Status).HasConversion<string>().HasColumnType("varchar(20)").HasDefaultValue(ApprovalStatus.Pending);
        builder.Property(item => item.CreatedByAgent).HasColumnType("varchar(50)").IsRequired();
        builder.Property(item => item.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(item => item.ReviewedBy).HasMaxLength(50);
        builder.Property(item => item.ReviewedAt).HasColumnType("datetime2");
        builder.HasIndex(item => new { item.TenantId, item.Status, item.CreatedAt });
    }
}
