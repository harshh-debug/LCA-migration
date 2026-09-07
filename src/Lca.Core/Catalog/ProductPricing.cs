using Lca.Core.Tenancy;

namespace Lca.Core.Catalog;

public sealed class ProductPricing : ITenantOwned
{
    public long ProductId { get; set; }
    public long TenantId { get; set; }
    public decimal PurchaseRate { get; set; }
    public decimal DealerRate { get; set; }
    public decimal WholesaleRate { get; set; }
    public decimal RetailRate { get; set; }
    public decimal OtherRate { get; set; }
    public decimal? VatRate { get; set; }
    public decimal? AdditionalVatRate { get; set; }
    public decimal? CstRate { get; set; }
    public decimal IgstRate { get; set; }
    public decimal SgstRate { get; set; }
    public decimal CgstRate { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public Product Product { get; set; } = null!;
}
