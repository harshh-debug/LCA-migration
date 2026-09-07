using Lca.Core.Security;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class IdentityRoleConfiguration : IEntityTypeConfiguration<IdentityRole>
{
    public const string PlatformAdminRoleId = "9f3a6866-4605-43c6-9174-9a37c63cd361";

    public void Configure(EntityTypeBuilder<IdentityRole> builder) => builder.HasData(new IdentityRole
    {
        Id = PlatformAdminRoleId,
        Name = PlatformRoles.PlatformAdmin,
        NormalizedName = PlatformRoles.PlatformAdmin.ToUpperInvariant(),
        ConcurrencyStamp = "3a0523aa-2c72-4a2c-bf5e-3f68e880ce72",
    });
}
