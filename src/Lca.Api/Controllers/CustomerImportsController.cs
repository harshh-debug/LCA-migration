using Lca.Core.Customers;
using Lca.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lca.Api.Controllers;

[ApiController, Route("api/v1/customers/imports"), Authorize(Policy = Policies.TenantAccess)]
public sealed class CustomerImportsController(ICustomerWorkbookImportService importService) : ControllerBase
{
    private const long MaximumWorkbookBytes = 20 * 1024 * 1024;
    private static readonly HashSet<string> AcceptedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".xls", ".xlsx" };

    [HttpPost("preview"), Consumes("multipart/form-data")]
    public async Task<ActionResult<CustomerWorkbookImportPreview>> Preview(
        [FromForm] IFormFile file, CancellationToken cancellationToken)
    {
        BadRequestObjectResult? rejection = Validate(file);
        if (rejection is not null) return rejection;
        await using Stream stream = file.OpenReadStream();
        return Ok(await importService.PreviewAsync(stream, cancellationToken));
    }

    [HttpPost("apply"), Consumes("multipart/form-data")]
    public async Task<ActionResult<CustomerWorkbookImportResult>> Apply(
        [FromForm] IFormFile file, [FromForm] bool confirmSnapshot, CancellationToken cancellationToken)
    {
        BadRequestObjectResult? rejection = Validate(file);
        if (rejection is not null) return rejection;
        await using Stream stream = file.OpenReadStream();
        CustomerWorkbookImportResult result = await importService.ApplySnapshotAsync(stream, confirmSnapshot, cancellationToken);
        return result.Issues.Count == 0 ? Ok(result) : UnprocessableEntity(result);
    }

    private BadRequestObjectResult? Validate(IFormFile file)
    {
        if (file.Length == 0) return BadRequest(new ProblemDetails { Title = "Workbook is empty." });
        if (file.Length > MaximumWorkbookBytes) return BadRequest(new ProblemDetails { Title = "Workbook exceeds the 20 MB limit." });
        if (!AcceptedExtensions.Contains(Path.GetExtension(file.FileName)))
            return BadRequest(new ProblemDetails { Title = "Only legacy .xls and .xlsx workbooks are accepted." });
        return null;
    }
}
