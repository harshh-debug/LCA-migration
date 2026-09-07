using Lca.Core.Tenancy;

namespace Lca.Core.Catalog;

public sealed class ProductMedia : ITenantOwned
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public long ProductId { get; set; }
    public required string LegacyPath { get; set; }
    public int SortOrder { get; set; }
    public bool IsThumbnail { get; set; }
    public Product Product { get; set; } = null!;
}
