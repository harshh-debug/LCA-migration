using Lca.Api.Contracts;
using Lca.Core.Catalog;
using Lca.Core.Governance;
using Lca.Core.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lca.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public sealed class ProductsController(
    ICatalogService catalogService,
    ITenantContext tenantContext,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.CatalogRead)]
    [ProducesResponseType<PagedResponse<ProductResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResponse<ProductResponse>>> GetProducts(
        [FromQuery] ProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.IncludeDrafts
            && !currentUser.Permissions.Contains(Permissions.ApprovalQueueRead, StringComparer.Ordinal))
        {
            return Forbid();
        }

        string tenantId = tenantContext.TenantId!.Value.Value;
        PagedResult<Product> products = await catalogService.SearchProductsAsync(
            new ProductSearch(tenantId, request.Search, request.CategoryId, request.IncludeDrafts, request.Page, request.PageSize),
            cancellationToken);
        IReadOnlyCollection<Category> categories = await catalogService.GetCategoriesAsync(tenantId, cancellationToken);
        Dictionary<decimal, Category> categoriesById = categories.ToDictionary(category => category.Id);

        ProductResponse[] response = products.Items.Select(product => MapProduct(product, categoriesById)).ToArray();
        return Ok(new PagedResponse<ProductResponse>(response, products.Page, products.PageSize, products.TotalCount));
    }

    [HttpPost("draft")]
    [Authorize(Policy = Policies.ProductDraftCreate)]
    [ProducesResponseType<ProductDraftCreatedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ProductDraftCreatedResponse>> CreateDraft(
        [FromBody] CreateProductDraftRequest request,
        CancellationToken cancellationToken)
    {
        ApprovalQueueItem item = await catalogService.CreateProductDraftAsync(
            new ProductDraft(
                tenantContext.TenantId!.Value.Value,
                request.Name.Trim(),
                request.Description,
                request.Specification,
                request.CategoryId,
                currentUser.UserId!),
            cancellationToken);

        ProductDraftCreatedResponse response = new(item.Id, item.EntityId, item.Status.ToString(), item.CreatedAt);
        return Created($"/api/v1/approval-queue/{item.Id}", response);
    }

    private static ProductResponse MapProduct(Product product, Dictionary<decimal, Category> categories)
    {
        CategoryReferenceResponse? category = product.CategoryId.HasValue
            && categories.TryGetValue(product.CategoryId.Value, out Category? match)
                ? new CategoryReferenceResponse(match.Id, match.Name)
                : null;
        string?[] imageSlots =
        [
            product.Image1,
            product.Image2,
            product.Image3,
            product.Image4,
            product.Image5,
            product.Image6,
            product.Image7,
            product.Image8,
            product.Image9,
        ];
        string[] images = imageSlots
            .Where(static image => !string.IsNullOrWhiteSpace(image))
            .Select(static image => image!)
            .ToArray();

        return new ProductResponse(
            product.ItemCode,
            product.Name,
            product.Description,
            product.Specification,
            category,
            new ProductPriceResponse(product.RetailRate, product.WholesaleRate, product.DealerRate),
            product.IsDisabled == true,
            product.IsDraft,
            product.CreatedSource,
            images,
            product.ThumbnailImage);
    }
}
