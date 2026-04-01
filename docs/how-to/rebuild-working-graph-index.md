# Rebuild Working Graph Index

This command rebuilds the persisted SQLite working index from the existing graph working import batches.

Use it when:

- `graph/working/index/graph-working.sqlite` is missing
- you intentionally deleted or reset the index
- you want to prove the SQLite layer is rebuildable from the working-batch files
- you pulled the sibling `NEXUS-EventStore` repo fresh and need to recreate the local derived index

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-working-graph-index
```

## Example

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-working-graph-index \
  --event-store-root /home/ivan/NEXUS/NEXUS-EventStore
```

## What It Does

1. Reads the current graph working catalog and import-batch manifests
2. Scans `graph/working/imports/<import-id>/assertions/`
3. Rebuilds `graph/working/index/graph-working.sqlite`
4. Restores per-import working-batch summaries for later CLI queries

## Notes

- This command rebuilds the SQLite working index only.
- It does not touch canonical history.
- It does not rebuild the durable `graph/assertions/` layer.
- The SQLite index remains a derived working structure, not source truth.
- The repository keeps the path stable, but the generated SQLite files under `graph/working/index/` are intentionally local and ignored by Git.

## Related Commands

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-working-graph-imports
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-working-graph-batch --import-id <uuid>
```
