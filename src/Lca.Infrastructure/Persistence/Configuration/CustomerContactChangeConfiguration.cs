using Lca.Core.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class CustomerContactChangeConfiguration : IEntityTypeConfiguration<CustomerContactChange>
{
    public void Configure(EntityTypeBuilder<CustomerContactChange> builder)
    {
        builder.ToTable("CustomerContactChanges", "dbo");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedOnAdd();
        builder.HasAlternateKey(value => new { value.TenantId, value.Id });
        builder.Property(value => value.Operation).HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.ReviewStatus).HasConversion<string>().HasMaxLength(30);
        builder.Property(value => value.BeforeJson).HasColumnType("nvarchar(max)");
        builder.Property(value => value.AfterJson).HasColumnType("nvarchar(max)");
        builder.Property(value => value.ChangedByUserId).HasMaxLength(450);
        builder.Property(value => value.ReviewedByUserId).HasMaxLength(450);
        builder.Property(value => value.Source).HasMaxLength(50);
        builder.HasIndex(value => new { value.TenantId, value.ReviewStatus, value.ChangedAtUtc });
        builder.HasIndex(value => new { value.TenantId, value.CustomerId, value.ChangedAtUtc });
        builder.HasOne(value => value.Customer).WithMany(value => value.ContactChanges)
            .HasForeignKey(value => new { value.TenantId, value.CustomerId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
