# Tenant 1 legacy catalog import

This offline compatibility tool reads the legacy LCA SQL database and reconciles Product, Category, Pricing, Inventory, and legacy media references into canonical `TenantId = 1` in the new database.

Safety invariants:

- no tenant argument exists;
- active Tenant 1 must already exist;
- the legacy connection is used only for `SELECT` operations;
- normal APIs never call this tool or the legacy database;
- `ItemCode` is the repeatable Product key and `migration.LegacyCategoryMaps` preserves Category ID mapping;
- validation/reconciliation runs without writes unless `--apply` is supplied.

Provide secrets through environment variables:

```text
LEGACY_LCA_CONNECTION=<read-only legacy SQL connection>
LCA_MIGRATION_CONNECTION=<new platform SQL connection>
```

Dry run:

```text
dotnet run --project src/Lca.LegacyCatalogImport/Lca.LegacyCatalogImport.csproj
```

Apply after reviewing the report:

```text
dotnet run --project src/Lca.LegacyCatalogImport/Lca.LegacyCatalogImport.csproj -- --apply
```

The tool never removes files from the legacy filesystem and does not implement dual writes.
