namespace Lca.Core.Tenancy;

public interface ITenantOwned
{
    long TenantId { get; set; }
}
