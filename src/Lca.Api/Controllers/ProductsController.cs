using Lca.Api.Contracts;
using Lca.Core.Catalog;
using Lca.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lca.Api.Controllers;

[ApiController, Route("api/v1/products"), Authorize(Policy = Policies.TenantAccess)]
public sealed class ProductsController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResponse<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetProducts([FromQuery] ProductSearchRequest request, CancellationToken cancellationToken)
    {
        PagedResult<Product> result = await catalogService.SearchProductsAsync(
            new(request.Search, request.CategoryId, request.Status, request.Page, request.PageSize), cancellationToken);
        return Ok(new PagedResponse<ProductResponse>(result.Items.Select(Map).ToArray(), result.Page, result.PageSize, result.TotalCount));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetProduct(long id, CancellationToken cancellationToken)
    {
        Product? product = await catalogService.GetProductAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(Map(product));
    }

    [HttpPost]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ProductResponse>> CreateProduct(ProductWriteRequest request, CancellationToken cancellationToken)
    {
        Product product = await catalogService.CreateProductAsync(ToModel(request), cancellationToken);
        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, Map(product));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ProductResponse>> UpdateProduct(long id, ProductWriteRequest request, CancellationToken cancellationToken)
    {
        Product? product = await catalogService.UpdateProductAsync(id, ToModel(request), cancellationToken);
        return product is null ? NotFound() : Ok(Map(product));
    }

    [HttpPut("{id:long}/pricing")]
    public async Task<ActionResult<ProductPricingResponse>> UpdatePricing(long id, PricingWriteRequest request, CancellationToken cancellationToken)
    {
        ProductPricing? pricing = await catalogService.UpdatePricingAsync(id, new(
            request.PurchaseRate, request.DealerRate, request.WholesaleRate, request.RetailRate, request.OtherRate,
            request.VatRate, request.AdditionalVatRate, request.CstRate, request.IgstRate, request.SgstRate, request.CgstRate), cancellationToken);
        return pricing is null ? NotFound() : Ok(Map(pricing));
    }

    [HttpPut("{id:long}/inventory")]
    public async Task<ActionResult<ProductInventoryResponse>> UpdateInventory(long id, InventoryWriteRequest request, CancellationToken cancellationToken)
    {
        ProductInventory? inventory = await catalogService.UpdateInventoryAsync(id, new(
            request.Balance, request.CurrentStock, request.MaximumStock, request.MinimumStock, request.Godown1Stock, request.Godown2Stock), cancellationToken);
        return inventory is null ? NotFound() : Ok(Map(inventory));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteProduct(long id, CancellationToken cancellationToken) =>
        await catalogService.DeleteProductAsync(id, cancellationToken) ? NoContent() : NotFound();

    private static ProductWriteModel ToModel(ProductWriteRequest value) => new(
        value.ItemCode, value.Name, value.Unit, value.AlternateItemCode, value.GujaratiName, value.UnitKilograms,
        value.Group1, value.Group2, value.ChapterNumber, value.HsnNumber, value.ItemType, value.Packing,
        value.ManufacturerName, value.Location, value.AlternateLocation, value.WarrantyYears, value.WarrantyMonths,
        value.Description, value.Remark, value.SalesmanCommission, value.CategoryId, value.IsDisabled);

    internal static ProductResponse Map(Product value) => new(
        value.Id, value.ItemCode, value.Name, value.Unit, value.AlternateItemCode, value.GujaratiName, value.UnitKilograms,
        value.Group1, value.Group2, value.ChapterNumber, value.HsnNumber, value.ItemType, value.Packing,
        value.ManufacturerName, value.Location, value.AlternateLocation, value.WarrantyYears, value.WarrantyMonths,
        value.Description, value.Remark, value.SalesmanCommission,
        value.Category is null ? null : new(value.Category.Id, value.Category.Name),
        value.Pricing is null ? null : Map(value.Pricing), value.Inventory is null ? null : Map(value.Inventory),
        value.IsDisabled, value.Media.OrderBy(media => media.SortOrder).Select(media => new ProductMediaResponse(media.Id, media.LegacyPath, media.SortOrder, media.IsThumbnail)).ToArray(),
        value.CreatedAtUtc, value.UpdatedAtUtc);

    private static ProductPricingResponse Map(ProductPricing value) => new(
        value.PurchaseRate, value.DealerRate, value.WholesaleRate, value.RetailRate, value.OtherRate,
        value.VatRate, value.AdditionalVatRate, value.CstRate, value.IgstRate, value.SgstRate, value.CgstRate);
    private static ProductInventoryResponse Map(ProductInventory value) => new(value.Balance, value.CurrentStock, value.MaximumStock, value.MinimumStock, value.Godown1Stock, value.Godown2Stock);
}
