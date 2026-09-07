namespace Lca.Core.Catalog;

public sealed record WorkbookImportIssue(int Row, string Field, string Message);
public sealed record WorkbookImportPreview(int RowsRead, int ValidRows, IReadOnlyCollection<WorkbookImportIssue> Issues);
public sealed record WorkbookImportResult(int RowsRead, int Created, int Updated, int Disabled, IReadOnlyCollection<WorkbookImportIssue> Issues);

public interface IProductWorkbookImportService
{
    Task<WorkbookImportPreview> PreviewAsync(Stream workbook, CancellationToken cancellationToken);
    Task<WorkbookImportResult> ApplySnapshotAsync(Stream workbook, bool confirmSnapshot, CancellationToken cancellationToken);
}
