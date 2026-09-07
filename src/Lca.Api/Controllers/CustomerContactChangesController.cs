using Lca.Api.Contracts;
using Lca.Core.Catalog;
using Lca.Core.Customers;
using Lca.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lca.Api.Controllers;

[ApiController, Route("api/v1/customer-contact-changes"), Authorize(Policy = Policies.TenantAccess)]
public sealed class CustomerContactChangesController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerContactChangeResponse>>> GetChanges(
        [FromQuery] ContactChangeSearchRequest request, CancellationToken cancellationToken)
    {
        PagedResult<CustomerContactChange> result = await customerService.SearchContactChangesAsync(
            new(request.ReviewStatus, request.Page, request.PageSize), null, cancellationToken);
        return Ok(new PagedResponse<CustomerContactChangeResponse>(
            result.Items.Select(CustomerContactChangeResponse.From).ToArray(), result.Page, result.PageSize, result.TotalCount));
    }

    [HttpPut("{id:long}/acknowledge")]
    public async Task<ActionResult<CustomerContactChangeResponse>> Acknowledge(long id, CancellationToken cancellationToken)
    {
        CustomerContactChange? change = await customerService.AcknowledgeContactChangeAsync(id, cancellationToken);
        return change is null ? NotFound() : Ok(CustomerContactChangeResponse.From(change));
    }
}
