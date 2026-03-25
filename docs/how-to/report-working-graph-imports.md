# Report Working Graph Imports

This command summarizes the incremental graph working batches created by provider imports.

Use it when you want a quick operator view of the secondary graph working layer without rereading every graph assertion TOML file.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-working-graph-imports
```

Optional limit:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-working-graph-imports \
  --limit 10
```

## What It Shows

- the graph working catalog path
- how many import-batch slices are currently recorded
- total canonical events represented by those slices
- total graph assertions represented by those slices
- provider counts when the matching canonical import manifests are present
- a short recent-slice list with:
  - `import_id`
  - provider
  - window
  - canonical event count
  - graph assertion count
  - working manifest path

## Notes

- This command prefers `NEXUS-EventStore/graph/working/catalog/import-batches.toml`
- If the catalog file is missing, it falls back to scanning `graph/working/imports/*/manifest.toml`
- The graph working layer is a secondary practical layer, not canonical source truth
