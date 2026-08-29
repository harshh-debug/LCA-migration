namespace Lca.Core.Catalog;

public sealed class Product
{
    public required string ItemCode { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public string? Specification { get; set; }

    public decimal? CategoryId { get; set; }

    public decimal? RetailRate { get; set; }

    public decimal? WholesaleRate { get; set; }

    public decimal? DealerRate { get; set; }

    public bool? IsDisabled { get; set; }

    public string? Image1 { get; set; }

    public string? Image2 { get; set; }

    public string? Image3 { get; set; }

    public string? Image4 { get; set; }

    public string? Image5 { get; set; }

    public string? Image6 { get; set; }

    public string? Image7 { get; set; }

    public string? Image8 { get; set; }

    public string? Image9 { get; set; }

    public string? ThumbnailImage { get; set; }

    public bool IsDraft { get; set; }

    public required string CreatedSource { get; set; }
}
