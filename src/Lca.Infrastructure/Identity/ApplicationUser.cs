using Lca.Core.Security;

using Microsoft.AspNetCore.Identity;

namespace Lca.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public AccountType AccountType { get; set; }

    public bool IsActive { get; set; } = true;

    public bool RequiresPasswordSetup { get; set; }
}
