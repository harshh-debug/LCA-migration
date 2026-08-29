using Lca.Api.Contracts;
using Lca.Core.Catalog;
using Lca.Core.Security;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lca.Api.Controllers;

[ApiController]
[Route("api/v1/categories")]
public sealed class CategoriesController(ICatalogService catalogService, ITenantContext tenantContext) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.CatalogRead)]
    [ProducesResponseType<IReadOnlyCollection<CategoryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> GetCategories(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Category> categories = await catalogService.GetCategoriesAsync(
            tenantContext.TenantId!.Value.Value,
            cancellationToken);
        CategoryResponse[] response = categories.Select(category => new CategoryResponse(
            category.Id,
            category.Name,
            category.ParentCategoryId,
            category.DisplaySubCategory == true,
            category.Icon,
            category.NotificationImage)).ToArray();
        return Ok(response);
    }
}
