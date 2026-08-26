using Lca.Domain.Tenancy;

namespace Lca.Domain.UnitTests.Tenancy;

public sealed class TenantIdTests
{
    [Fact]
    public void ConstructorTrimsValidIdentifier()
    {
        TenantId tenantId = new("  tenant-a  ");

        Assert.Equal("tenant-a", tenantId.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorRejectsEmptyIdentifier(string value)
    {
        Assert.Throws<ArgumentException>(() => new TenantId(value));
    }
}
