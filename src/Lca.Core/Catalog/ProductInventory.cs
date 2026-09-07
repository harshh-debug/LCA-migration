using Lca.Core.Tenancy;

namespace Lca.Core.Catalog;

public sealed class ProductInventory : ITenantOwned
{
    public long ProductId { get; set; }
    public long TenantId { get; set; }
    public decimal? Balance { get; set; }
    public decimal? CurrentStock { get; set; }
    public decimal? MaximumStock { get; set; }
    public decimal? MinimumStock { get; set; }
    public decimal? Godown1Stock { get; set; }
    public decimal? Godown2Stock { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Product Product { get; set; } = null!;
}
