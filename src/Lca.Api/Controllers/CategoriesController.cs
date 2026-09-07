using Lca.Api.Contracts;
using Lca.Core.Catalog;
using Lca.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lca.Api.Controllers;

[ApiController, Route("api/v1/categories"), Authorize(Policy = Policies.TenantAccess)]
public sealed class CategoriesController(ICatalogService catalogService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<CategoryResponse>>> GetCategories(CancellationToken cancellationToken) =>
        Ok((await catalogService.GetCategoriesAsync(cancellationToken)).Select(Map).ToArray());

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        Category category = await catalogService.CreateCategoryAsync(request.Name, request.ParentCategoryId, request.DisplaySubCategory, cancellationToken);
        return Created($"/api/v1/categories/{category.Id}", Map(category));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(long id, CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        Category? category = await catalogService.UpdateCategoryAsync(id, request.Name, request.ParentCategoryId, request.DisplaySubCategory, cancellationToken);
        return category is null ? NotFound() : Ok(Map(category));
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCategory(long id, CancellationToken cancellationToken) =>
        await catalogService.DeleteCategoryAsync(id, cancellationToken) ? NoContent() : NotFound();

    private static CategoryResponse Map(Category value) => new(value.Id, value.Name, value.ParentCategoryId, value.DisplaySubCategory, value.LegacyIconPath, value.LegacyNotificationImagePath);
}
