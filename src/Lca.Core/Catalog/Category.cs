using Lca.Core.Tenancy;

namespace Lca.Core.Catalog;

public sealed class Category : ITenantOwned
{
    public long Id { get; set; }
    public long TenantId { get; set; }
    public required string Name { get; set; }
    public long? ParentCategoryId { get; set; }
    public bool DisplaySubCategory { get; set; }
    public string? LegacyIconPath { get; set; }
    public string? LegacyNotificationImagePath { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Category? ParentCategory { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<Product> Products { get; set; } = [];
}
