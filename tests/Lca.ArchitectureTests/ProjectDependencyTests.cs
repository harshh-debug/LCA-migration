using Lca.Application.Security;
using Lca.Domain.Tenancy;
using Lca.Infrastructure;

namespace Lca.ArchitectureTests;

public sealed class ProjectDependencyTests
{
    [Fact]
    public void DomainDoesNotReferenceApplicationOrInfrastructure()
    {
        string[] references = typeof(TenantId).Assembly.GetReferencedAssemblies()
            .Select(static assembly => assembly.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain("Lca.Application", references);
        Assert.DoesNotContain("Lca.Infrastructure", references);
    }

    [Fact]
    public void ApplicationDoesNotReferenceInfrastructure()
    {
        string[] references = typeof(ICurrentUser).Assembly.GetReferencedAssemblies()
            .Select(static assembly => assembly.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(typeof(InfrastructureAssembly).Assembly.GetName().Name, references);
    }
}
