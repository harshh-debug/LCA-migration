using Lca.Core.Tenancy;
using Lca.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("TenantMemberships", "dbo");
        builder.HasKey(membership => new { membership.UserId, membership.TenantId });
        builder.Property(membership => membership.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.HasIndex(membership => membership.UserId)
            .IsUnique()
            .HasFilter("[Status] = 'Active'");
        builder.HasIndex(membership => new { membership.TenantId, membership.Status });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(membership => membership.Tenant)
            .WithMany()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
