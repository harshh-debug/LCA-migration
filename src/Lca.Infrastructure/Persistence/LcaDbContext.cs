using Lca.Core.Catalog;
using Lca.Core.Customers;
using Lca.Core.Platform;
using Lca.Core.Security;
using Lca.Core.Tenancy;
using Lca.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lca.Infrastructure.Persistence;

public sealed class LcaDbContext(
    DbContextOptions<LcaDbContext> options,
    ITenantContext tenantContext) : IdentityDbContext<ApplicationUser>(options)
{
    private long? CurrentTenantId => tenantContext.TenantId?.Value;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<PlatformAuditEntry> PlatformAuditEntries => Set<PlatformAuditEntry>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<ProductPricing> ProductPricing => Set<ProductPricing>();

    public DbSet<ProductInventory> ProductInventory => Set<ProductInventory>();

    public DbSet<ProductMedia> ProductMedia => Set<ProductMedia>();

    public DbSet<LegacyCategoryMap> LegacyCategoryMaps => Set<LegacyCategoryMap>();

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerContact> CustomerContacts => Set<CustomerContact>();
    public DbSet<CustomerContactChange> CustomerContactChanges => Set<CustomerContactChange>();
    public DbSet<LegacyCustomerAccountingSnapshot> LegacyCustomerAccountingSnapshots => Set<LegacyCustomerAccountingSnapshot>();
    public DbSet<LegacyCustomerMap> LegacyCustomerMaps => Set<LegacyCustomerMap>();
    public DbSet<LegacyCustomerContactMap> LegacyCustomerContactMaps => Set<LegacyCustomerContactMap>();
    public DbSet<LegacyCustomerContactChangeMap> LegacyCustomerContactChangeMaps => Set<LegacyCustomerContactChangeMap>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(InfrastructureAssembly.Marker.Assembly);
        builder.Entity<Product>().HasQueryFilter(product =>
            CurrentTenantId.HasValue && product.TenantId == CurrentTenantId.Value);
        builder.Entity<Category>().HasQueryFilter(category =>
            CurrentTenantId.HasValue && category.TenantId == CurrentTenantId.Value);
        builder.Entity<ProductPricing>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
        builder.Entity<ProductInventory>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
        builder.Entity<ProductMedia>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
        builder.Entity<LegacyCategoryMap>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
        builder.Entity<Customer>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
        builder.Entity<CustomerContact>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
        builder.Entity<CustomerContactChange>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
        builder.Entity<LegacyCustomerAccountingSnapshot>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
        builder.Entity<LegacyCustomerMap>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
        builder.Entity<LegacyCustomerContactMap>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
        builder.Entity<LegacyCustomerContactChangeMap>().HasQueryFilter(value =>
            CurrentTenantId.HasValue && value.TenantId == CurrentTenantId.Value);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnforceTenantOwnership();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        EnforceTenantOwnership();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void EnforceTenantOwnership()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantOwned>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            long currentTenantId = CurrentTenantId
                ?? throw new TenantIsolationException("A trusted tenant context is required for tenant-owned writes.");

            if (entry.State == EntityState.Added && entry.Entity.TenantId == 0)
            {
                entry.Entity.TenantId = currentTenantId;
            }

            if (entry.Entity.TenantId != currentTenantId)
            {
                throw new TenantIsolationException("The entity does not belong to the active tenant.");
            }

            if (entry.State is EntityState.Modified or EntityState.Deleted)
            {
                long originalTenantId = entry.Property(entity => entity.TenantId).OriginalValue;
                if (originalTenantId != currentTenantId || entry.Property(entity => entity.TenantId).IsModified)
                {
                    throw new TenantIsolationException("Tenant ownership cannot be changed or bypassed.");
                }
            }
        }
    }
}

public sealed class TenantIsolationException(string message) : InvalidOperationException(message);
