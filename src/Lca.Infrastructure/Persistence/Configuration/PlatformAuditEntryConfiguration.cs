using Lca.Core.Platform;
using Lca.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class PlatformAuditEntryConfiguration : IEntityTypeConfiguration<PlatformAuditEntry>
{
    public void Configure(EntityTypeBuilder<PlatformAuditEntry> builder)
    {
        builder.ToTable("PlatformAuditEntries", "dbo");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.ActorUserId).HasMaxLength(450).IsRequired();
        builder.Property(value => value.Action).HasMaxLength(100).IsRequired();
        builder.Property(value => value.TargetType).HasMaxLength(100).IsRequired();
        builder.Property(value => value.TargetId).HasMaxLength(450).IsRequired();
        builder.Property(value => value.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(value => value.Details).HasMaxLength(2000);
        builder.HasIndex(value => value.OccurredAtUtc);
        builder.HasIndex(value => new { value.TenantId, value.OccurredAtUtc });
        builder.HasIndex(value => new { value.Action, value.OccurredAtUtc });
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(value => value.ActorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
