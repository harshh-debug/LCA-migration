using Lca.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lca.Infrastructure.Persistence.Configuration;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(user => user.AccountType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(user => user.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(user => user.RequiresPasswordSetup).HasDefaultValue(false).IsRequired();
    }
}
