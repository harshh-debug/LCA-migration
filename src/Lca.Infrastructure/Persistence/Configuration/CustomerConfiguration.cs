using Lca.Core.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "dbo");
        builder.HasKey(value => value.Id);
        builder.Property(value => value.Id).ValueGeneratedOnAdd();
        builder.HasAlternateKey(value => new { value.TenantId, value.Id });
        builder.Property(value => value.AccountNumber).HasMaxLength(50).IsRequired();
        builder.Property(value => value.CompanyName).HasMaxLength(500).IsRequired();
        ConfigureLengths(builder);
        builder.Property(value => value.PriceBand).HasConversion<string>().HasMaxLength(20);
        builder.Property(value => value.CreditLimit).HasColumnType("decimal(18,2)");
        builder.HasIndex(value => new { value.TenantId, value.AccountNumber }).IsUnique();
        builder.HasIndex(value => new { value.TenantId, value.IsDisabled, value.AccountNumber });
        builder.HasIndex(value => new { value.TenantId, value.PriceBand });
        builder.HasIndex(value => new { value.TenantId, value.Area });
        builder.HasOne<Lca.Core.Tenancy.Tenant>().WithMany()
            .HasForeignKey(value => value.TenantId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void ConfigureLengths(EntityTypeBuilder<Customer> builder)
    {
        foreach (string property in new[]
        {
            nameof(Customer.ContactPerson), nameof(Customer.MobileNumber), nameof(Customer.OfficePhone),
            nameof(Customer.ResidentialPhone), nameof(Customer.Email), nameof(Customer.ManagementContactPerson),
            nameof(Customer.ManagementMobileNumber), nameof(Customer.ManagementEmail), nameof(Customer.AddressLine1),
            nameof(Customer.AddressLine2), nameof(Customer.AddressLine3), nameof(Customer.City), nameof(Customer.District),
            nameof(Customer.State), nameof(Customer.PostalCode), nameof(Customer.DefaultShippingAddressLine1),
            nameof(Customer.DefaultShippingAddressLine2), nameof(Customer.DefaultShippingAddressLine3), nameof(Customer.Area),
            nameof(Customer.Gstin), nameof(Customer.TinNumber), nameof(Customer.CstNumber),
            nameof(Customer.DefaultTransportName), nameof(Customer.SalespersonReference),
        }) builder.Property(property).HasMaxLength(property.Contains("Address", StringComparison.Ordinal) ? 1000 : 200);
    }
}
