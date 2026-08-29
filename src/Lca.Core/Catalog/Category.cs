namespace Lca.Core.Catalog;

public sealed class Category
{
    public decimal Id { get; set; }

    public string? Name { get; set; }

    public string? Icon { get; set; }

    public bool? DisplaySubCategory { get; set; }

    public decimal? ParentCategoryId { get; set; }

    public string? NotificationImage { get; set; }
}
