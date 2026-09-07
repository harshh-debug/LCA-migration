using System.Globalization;
using System.Text;
using ExcelDataReader;
using ExcelDataReader.Exceptions;
using Lca.Core.Catalog;
using Lca.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Lca.Infrastructure.Catalog;

internal sealed class ProductWorkbookImportService(LcaDbContext context, TimeProvider timeProvider) : IProductWorkbookImportService
{
    static ProductWorkbookImportService() => Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

    public Task<WorkbookImportPreview> PreviewAsync(Stream workbook, CancellationToken cancellationToken)
    {
        ParsedWorkbook parsed = ParseSafe(workbook, cancellationToken);
        return Task.FromResult(new WorkbookImportPreview(parsed.RowsRead, parsed.Rows.Count, parsed.Issues));
    }

    public async Task<WorkbookImportResult> ApplySnapshotAsync(Stream workbook, bool confirmSnapshot, CancellationToken cancellationToken)
    {
        if (!confirmSnapshot) throw new CatalogConflictException("Snapshot replacement must be explicitly confirmed.");
        ParsedWorkbook parsed = ParseSafe(workbook, cancellationToken);
        if (parsed.Issues.Count > 0) return new(parsed.RowsRead, 0, 0, 0, parsed.Issues);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        Product[] existing = await context.Products.Include(value => value.Pricing).Include(value => value.Inventory).ToArrayAsync(cancellationToken);
        Dictionary<string, Product> byCode = existing.ToDictionary(value => value.ItemCode, StringComparer.OrdinalIgnoreCase);
        HashSet<string> importedCodes = new(StringComparer.OrdinalIgnoreCase);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        int created = 0, updated = 0, disabled = 0;

        foreach (ParsedProduct row in parsed.Rows)
        {
            importedCodes.Add(row.ItemCode);
            if (!byCode.TryGetValue(row.ItemCode, out Product? product))
            {
                product = new Product { ItemCode = row.ItemCode, Name = row.Name, CreatedAtUtc = now };
                context.Products.Add(product);
                byCode.Add(row.ItemCode, product);
                created++;
            }
            else updated++;

            product.Name = row.Name; product.Unit = row.Unit; product.AlternateItemCode = row.AlternateItemCode;
            product.GujaratiName = row.GujaratiName; product.UnitKilograms = row.UnitKilograms;
            product.Group1 = row.Group1; product.Group2 = row.Group2; product.ChapterNumber = row.ChapterNumber;
            product.HsnNumber = row.HsnNumber; product.ItemType = row.ItemType; product.Packing = row.Packing;
            product.ManufacturerName = row.ManufacturerName; product.Location = row.Location; product.AlternateLocation = row.AlternateLocation;
            product.WarrantyYears = row.WarrantyYears; product.WarrantyMonths = row.WarrantyMonths;
            product.Description = row.Description; product.Remark = row.Remark; product.SalesmanCommission = row.SalesmanCommission;
            product.IsDisabled = false; product.UpdatedAtUtc = now;

            product.Pricing ??= new ProductPricing();
            ApplyPricing(product.Pricing, row, now);
            product.Inventory ??= new ProductInventory();
            ApplyInventory(product.Inventory, row, now);
        }

        foreach (Product product in existing.Where(value => !importedCodes.Contains(value.ItemCode) && !value.IsDisabled))
        {
            product.IsDisabled = true; product.UpdatedAtUtc = now; disabled++;
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new(parsed.RowsRead, created, updated, disabled, []);
    }

    private static ParsedWorkbook Parse(Stream workbook, CancellationToken cancellationToken)
    {
        using IExcelDataReader reader = ExcelReaderFactory.CreateReader(workbook);
        do
        {
            if (!string.Equals(reader.Name, "ITEMMAST", StringComparison.OrdinalIgnoreCase)) continue;
            if (!reader.Read()) return new(0, [], [new(1, "ITEMMAST", "The ITEMMAST sheet has no header row.")]);
            Dictionary<string, int> headers = Enumerable.Range(0, reader.FieldCount)
                .Select(index => (Name: Text(reader.GetValue(index)), Index: index))
                .Where(value => value.Name is not null)
                .ToDictionary(value => value.Name!, value => value.Index, StringComparer.OrdinalIgnoreCase);
            List<WorkbookImportIssue> issues = [];
            foreach (string required in new[] { "Item_no", "Itemname" })
                if (!headers.ContainsKey(required)) issues.Add(new(1, required, "Required column is missing."));
            if (issues.Count > 0) return new(0, [], issues);

            List<ParsedProduct> rows = [];
            HashSet<string> codes = new(StringComparer.OrdinalIgnoreCase);
            int rowNumber = 1;
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                rowNumber++;
                string? code = Cell("Item_no");
                string? name = Cell("Itemname");
                if (code is null && name is null) continue;
                if (code is null) { issues.Add(new(rowNumber, "Item_no", "Item code is required.")); continue; }
                if (code.Length > 20) { issues.Add(new(rowNumber, "Item_no", "Item code exceeds 20 characters.")); continue; }
                if (name is null) { issues.Add(new(rowNumber, "Itemname", "Product name is required.")); continue; }
                if (!codes.Add(code)) { issues.Add(new(rowNumber, "Item_no", "Duplicate ItemCode in workbook.")); continue; }

                int before = issues.Count;
                decimal? Number(string column, bool blankAsZero = false)
                {
                    string? raw = Cell(column);
                    if (raw is null) return blankAsZero ? 0m : null;
                    if (decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out decimal value)
                        || decimal.TryParse(raw, NumberStyles.Number, CultureInfo.CurrentCulture, out value)) return value;
                    issues.Add(new(rowNumber, column, $"'{raw}' is not a valid number.")); return null;
                }
                int? Whole(string column, int maximum)
                {
                    decimal? value = Number(column);
                    if (!value.HasValue) return null;
                    if (value.Value != decimal.Truncate(value.Value) || value < 0 || value > maximum)
                    { issues.Add(new(rowNumber, column, $"Expected a whole number between 0 and {maximum}.")); return null; }
                    return (int)value.Value;
                }

                ParsedProduct parsed = new(code, name, Cell("Unit"), Cell("Altitemcode"), Cell("Gujitem"), Number("Unit_kgs"),
                    Cell("Group1"), Cell("Group2"), Cell("Chapter_no"), Cell("Hsnno"), Cell("Itemtype"), Cell("Packing"),
                    Cell("Mfg_name"), Cell("Location"), Cell("Altloction"), Whole("Warr_yr", 100), Whole("Warr_mon", 11),
                    Cell("Itemdisc"), Cell("Itremark"), Number("Salesmancommission"),
                    Number("Purrate", true) ?? 0, Number("Delearrt", true) ?? 0, Number("Wholesalert", true) ?? 0,
                    Number("Retailrate", true) ?? 0, Number("Otherrt", true) ?? 0, Number("Vatrt"), Number("Addivatrt"), Number("Cstrt"),
                    Number("Igstrt", true) ?? 0, Number("Sgstrt", true) ?? 0, Number("Cgstrt", true) ?? 0,
                    Number("Curstock"), Number("Max_stock"), Number("Min_stock"), Number("God01stock"), Number("God02stock"));
                CheckLength("Itemname", parsed.Name, 500); CheckLength("Unit", parsed.Unit, 50);
                CheckLength("Altitemcode", parsed.AlternateItemCode, 100); CheckLength("Gujitem", parsed.GujaratiName, 500);
                CheckLength("Group1", parsed.Group1, 200); CheckLength("Group2", parsed.Group2, 200);
                CheckLength("Chapter_no", parsed.ChapterNumber, 100); CheckLength("Hsnno", parsed.HsnNumber, 100);
                CheckLength("Itemtype", parsed.ItemType, 100); CheckLength("Packing", parsed.Packing, 100);
                CheckLength("Mfg_name", parsed.ManufacturerName, 500); CheckLength("Location", parsed.Location, 500);
                CheckLength("Altloction", parsed.AlternateLocation, 500);
                ReadOnlySpan<(string Field, decimal Value)> prices = [("Purrate", parsed.PurchaseRate), ("Delearrt", parsed.DealerRate), ("Wholesalert", parsed.WholesaleRate), ("Retailrate", parsed.RetailRate), ("Otherrt", parsed.OtherRate)];
                foreach ((string field, decimal value) in prices)
                    if (value < 0) issues.Add(new(rowNumber, field, "Price cannot be negative."));
                ReadOnlySpan<(string Field, decimal? Value)> taxes = [("Vatrt", parsed.VatRate), ("Addivatrt", parsed.AdditionalVatRate), ("Cstrt", parsed.CstRate), ("Igstrt", parsed.IgstRate), ("Sgstrt", parsed.SgstRate), ("Cgstrt", parsed.CgstRate)];
                foreach ((string field, decimal? value) in taxes)
                    if (value is < 0 or > 100) issues.Add(new(rowNumber, field, "Tax rate must be between 0 and 100."));
                if (issues.Count == before) rows.Add(parsed);

                void CheckLength(string field, string? value, int maximum)
                {
                    if (value?.Length > maximum) issues.Add(new(rowNumber, field, $"Value exceeds {maximum} characters."));
                }
            }
            return new(rowNumber - 1, rows, issues);

            string? Cell(string name) => headers.TryGetValue(name, out int index) ? Text(reader.GetValue(index)) : null;
        } while (reader.NextResult());
        return new(0, [], [new(0, "ITEMMAST", "Required ITEMMAST sheet was not found.")]);
    }

    private static ParsedWorkbook ParseSafe(Stream workbook, CancellationToken cancellationToken)
    {
        try { return Parse(workbook, cancellationToken); }
        catch (HeaderException exception) { return InvalidWorkbook(exception); }
        catch (InvalidDataException exception) { return InvalidWorkbook(exception); }
    }

    private static ParsedWorkbook InvalidWorkbook(Exception exception) =>
        new(0, [], [new(0, "Workbook", $"The file is not a readable Excel workbook: {exception.Message}")]);

    private static string? Text(object? value)
    {
        string? text = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static void ApplyPricing(ProductPricing value, ParsedProduct row, DateTime now)
    {
        value.PurchaseRate = row.PurchaseRate; value.DealerRate = row.DealerRate; value.WholesaleRate = row.WholesaleRate;
        value.RetailRate = row.RetailRate; value.OtherRate = row.OtherRate; value.VatRate = row.VatRate;
        value.AdditionalVatRate = row.AdditionalVatRate; value.CstRate = row.CstRate; value.IgstRate = row.IgstRate;
        value.SgstRate = row.SgstRate; value.CgstRate = row.CgstRate; value.UpdatedAtUtc = now;
    }
    private static void ApplyInventory(ProductInventory value, ParsedProduct row, DateTime now)
    {
        value.Balance = row.CurrentStock; value.CurrentStock = row.CurrentStock; value.MaximumStock = row.MaximumStock;
        value.MinimumStock = row.MinimumStock; value.Godown1Stock = row.Godown1Stock; value.Godown2Stock = row.Godown2Stock; value.UpdatedAtUtc = now;
    }

    private sealed record ParsedWorkbook(int RowsRead, List<ParsedProduct> Rows, List<WorkbookImportIssue> Issues);
    private sealed record ParsedProduct(
        string ItemCode, string Name, string? Unit, string? AlternateItemCode, string? GujaratiName, decimal? UnitKilograms,
        string? Group1, string? Group2, string? ChapterNumber, string? HsnNumber, string? ItemType, string? Packing,
        string? ManufacturerName, string? Location, string? AlternateLocation, int? WarrantyYears, int? WarrantyMonths,
        string? Description, string? Remark, decimal? SalesmanCommission,
        decimal PurchaseRate, decimal DealerRate, decimal WholesaleRate, decimal RetailRate, decimal OtherRate,
        decimal? VatRate, decimal? AdditionalVatRate, decimal? CstRate, decimal IgstRate, decimal SgstRate, decimal CgstRate,
        decimal? CurrentStock, decimal? MaximumStock, decimal? MinimumStock, decimal? Godown1Stock, decimal? Godown2Stock);
}
