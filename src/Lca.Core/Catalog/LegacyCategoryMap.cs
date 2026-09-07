using Lca.Core.Tenancy;

namespace Lca.Core.Catalog;

public sealed class LegacyCategoryMap : ITenantOwned
{
    public long TenantId { get; set; }
    public decimal LegacyCategoryId { get; set; }
    public long CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
