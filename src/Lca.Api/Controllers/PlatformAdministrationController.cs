using System.ComponentModel.DataAnnotations;

using Lca.Api.Contracts;
using Lca.Core.Platform;
using Lca.Core.Security;
using Lca.Core.Tenancy;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lca.Api.Controllers;

[ApiController]
[Route("api/v1/platform")]
[Authorize(Policy = Policies.PlatformAdmin)]
public sealed class PlatformAdministrationController(
    IPlatformAdministrationService service,
    ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("dashboard")]
    public Task<PlatformDashboard> Dashboard(CancellationToken cancellationToken) =>
        service.GetDashboardAsync(cancellationToken);

    [HttpGet("tenants")]
    public Task<PlatformPage<TenantSummary>> Tenants(
        [FromQuery] string? search,
        [FromQuery] TenantStatus? status,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        service.GetTenantsAsync(search, status, page, pageSize, cancellationToken);

    [HttpGet("tenants/{tenantId:long}")]
    public async Task<ActionResult<TenantSummary>> Tenant(long tenantId, CancellationToken cancellationToken)
    {
        TenantSummary? tenant = await service.GetTenantAsync(tenantId, cancellationToken);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPost("tenants")]
    public async Task<ActionResult<TenantSummary>> CreateTenant(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        TenantSummary tenant = await service.CreateTenantAsync(request.Name, request.Slug, Actor, HttpContext.TraceIdentifier, cancellationToken);
        return CreatedAtAction(nameof(Tenant), new { tenantId = tenant.Id }, tenant);
    }

    [HttpPut("tenants/{tenantId:long}")]
    public async Task<ActionResult<TenantSummary>> UpdateTenant(long tenantId, UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        TenantSummary? tenant = await service.UpdateTenantAsync(tenantId, request.Name, request.Slug, Actor, HttpContext.TraceIdentifier, cancellationToken);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpPut("tenants/{tenantId:long}/status")]
    public async Task<ActionResult<TenantSummary>> SetTenantStatus(long tenantId, SetTenantStatusRequest request, CancellationToken cancellationToken)
    {
        TenantSummary? tenant = await service.SetTenantStatusAsync(tenantId, request.Status, Actor, HttpContext.TraceIdentifier, cancellationToken);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    [HttpGet("tenants/{tenantId:long}/users")]
    public async Task<ActionResult<IReadOnlyCollection<TenantUserSummary>>> TenantUsers(long tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TenantUserSummary>? users = await service.GetTenantUsersAsync(tenantId, cancellationToken);
        return users is null ? NotFound() : Ok(users);
    }

    [HttpPost("tenants/{tenantId:long}/users")]
    public async Task<ActionResult<ProvisionedTenantUser>> ProvisionTenantUser(long tenantId, ProvisionTenantUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            ProvisionedTenantUser result = await service.ProvisionTenantUserAsync(tenantId, request.Email, Actor, HttpContext.TraceIdentifier, cancellationToken);
            return CreatedAtAction(nameof(TenantUser), new { userId = result.User.Id }, result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("users/{userId}")]
    public async Task<ActionResult<TenantUserSummary>> TenantUser(string userId, CancellationToken cancellationToken)
    {
        TenantUserSummary? user = await service.GetTenantUserAsync(userId, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("users/{userId}/status")]
    public async Task<ActionResult<TenantUserSummary>> SetTenantUserStatus(string userId, SetTenantUserStatusRequest request, CancellationToken cancellationToken)
    {
        TenantUserSummary? user = await service.SetTenantUserStatusAsync(userId, request.IsActive, Actor, HttpContext.TraceIdentifier, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("tenants/{tenantId:long}/users/{userId}/membership-status")]
    public async Task<ActionResult<TenantUserSummary>> SetMembershipStatus(long tenantId, string userId, SetMembershipStatusRequest request, CancellationToken cancellationToken)
    {
        TenantUserSummary? user = await service.SetMembershipStatusAsync(tenantId, userId, request.Status, Actor, HttpContext.TraceIdentifier, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost("users/{userId}/initial-password-setup-email")]
    public async Task<ActionResult<object>> ResendInitialSetupEmail(string userId, CancellationToken cancellationToken)
    {
        AccountEmailDeliveryResult? result = await service.ResendInitialSetupEmailAsync(userId, Actor, HttpContext.TraceIdentifier, cancellationToken);
        return result is null ? NotFound() : Ok(new { delivery = result });
    }

    [HttpGet("audit")]
    public Task<PlatformPage<PlatformAuditRecord>> Audit(
        [FromQuery] long? tenantId,
        [FromQuery] string? action,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, 100)] int pageSize = 50,
        CancellationToken cancellationToken = default) =>
        service.GetAuditAsync(tenantId, action, fromUtc, toUtc, page, pageSize, cancellationToken);

    private string Actor => currentUser.UserId ?? throw new InvalidOperationException("Platform user is unavailable.");
}
