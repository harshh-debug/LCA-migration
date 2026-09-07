# Tenant 1 legacy Customer import

This offline compatibility tool reads the legacy LCA SQL database and reconciles Customer master rows, normalized contacts, the meaningful Updated Contact review state, and read-only legacy accounting snapshots into canonical `TenantId = 1`.

Safety invariants:

- no tenant argument exists;
- active Tenant 1 must already exist;
- the legacy connection is used only for `SELECT` operations;
- normal APIs never call this tool or the legacy database;
- `migration.LegacyCustomerMaps` preserves legacy Customer ID mapping;
- dry-run profiling/reconciliation is the default; writes require `--apply`.

Provide `LEGACY_LCA_CONNECTION` (read-only legacy SQL) and `LCA_MIGRATION_CONNECTION` (new platform SQL), then run:

```text
dotnet run --project src/Lca.LegacyCustomerImport/Lca.LegacyCustomerImport.csproj
dotnet run --project src/Lca.LegacyCustomerImport/Lca.LegacyCustomerImport.csproj -- --apply
```

Future tenants are created directly in the platform and must never use this importer.
