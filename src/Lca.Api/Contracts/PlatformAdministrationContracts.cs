using System.ComponentModel.DataAnnotations;

using Lca.Core.Tenancy;

namespace Lca.Api.Contracts;

public sealed class CreateTenantRequest
{
    [Required, MaxLength(200)] public required string Name { get; init; }
    [Required, MaxLength(100), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public required string Slug { get; init; }
}

public sealed class UpdateTenantRequest
{
    [Required, MaxLength(200)] public required string Name { get; init; }
    [Required, MaxLength(100), RegularExpression("^[a-z0-9]+(?:-[a-z0-9]+)*$")]
    public required string Slug { get; init; }
}

public sealed class SetTenantStatusRequest
{
    [Required] public required TenantStatus Status { get; init; }
}

public sealed class ProvisionTenantUserRequest
{
    [Required, EmailAddress, MaxLength(256)] public required string Email { get; init; }
}

public sealed class SetTenantUserStatusRequest
{
    public bool IsActive { get; init; }
}

public sealed class SetMembershipStatusRequest
{
    [Required] public required TenantMembershipStatus Status { get; init; }
}
