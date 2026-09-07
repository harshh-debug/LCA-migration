using Lca.Core.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class CustomerContactConfiguration : IEntityTypeConfiguration<CustomerContact>
{
    public void Configure(EntityTypeBuilder<CustomerContact> builder)
    {
        builder.ToTable("CustomerContacts", "dbo");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedOnAdd();
        builder.HasAlternateKey(value => new { value.TenantId, value.Id });
        builder.Property(value => value.Name).HasMaxLength(200).IsRequired();
        builder.Property(value => value.Designation).HasMaxLength(200);
        builder.Property(value => value.Mobile).HasMaxLength(100);
        builder.Property(value => value.Email).HasMaxLength(200);
        builder.HasIndex(value => new { value.TenantId, value.CustomerId });
        builder.HasOne(value => value.Customer).WithMany(value => value.Contacts)
            .HasForeignKey(value => new { value.TenantId, value.CustomerId })
            .HasPrincipalKey(value => new { value.TenantId, value.Id })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
