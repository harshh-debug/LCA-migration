using Lca.Core.Catalog;

namespace Lca.Core.Customers;

public sealed record CustomerWorkbookImportPreview(
    int RowsRead,
    int ValidRows,
    IReadOnlyCollection<WorkbookImportIssue> Issues);

public sealed record CustomerWorkbookImportResult(
    int RowsRead,
    int Created,
    int Updated,
    int Reactivated,
    int Disabled,
    int ContactsCreated,
    int ContactsUpdated,
    IReadOnlyCollection<WorkbookImportIssue> Issues);

public interface ICustomerWorkbookImportService
{
    Task<CustomerWorkbookImportPreview> PreviewAsync(Stream workbook, CancellationToken cancellationToken);
    Task<CustomerWorkbookImportResult> ApplySnapshotAsync(Stream workbook, bool confirmSnapshot, CancellationToken cancellationToken);
}
