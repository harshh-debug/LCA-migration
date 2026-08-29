namespace Lca.Core.Security;

public static class Policies
{
    public const string TenantRequired = "tenant-required";

    public const string CatalogRead = "catalog-read";

    public const string ProductDraftCreate = "product-draft-create";

    public const string ApprovalQueueRead = "approval-queue-read";

    public const string ApprovalQueueApprove = "approval-queue-approve";
}
