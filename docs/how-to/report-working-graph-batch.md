# Report Working Graph Batch

This command summarizes one import-local graph working batch from the persisted SQLite working index.

Use it when you want a quick structural view of a fresh import batch without rereading all of its working assertion TOML files by hand.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-working-graph-batch \
  --import-id <uuid>
```

Legacy alias:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-working-graph-slice \
  --import-id <uuid>
```

## Example

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-working-graph-batch \
  --import-id 019d174e-e953-7e8b-b506-5f1475399fc7 \
  --limit 10
```

## What It Reads

The command reads the persisted working index at:

- `NEXUS-EventStore/graph/working/index/graph-working.sqlite`

That index is refreshed automatically by:

- `import-provider-export`
- `import-codex-sessions`

The index is a rebuildable working layer.
It is not canonical truth.
The repository does not need to keep that generated SQLite file under version control; rebuild it locally if it is missing.

## Output

The report includes:

- import ID
- provider and window when available
- imported and materialized timestamps
- canonical event count for the batch
- graph assertion count for the batch
- distinct subject count
- node-ref assertion count
- literal assertion count
- top predicates for the batch

## Options

- `--event-store-root <path>`
  Override the event-store root. Defaults to `NEXUS-EventStore/`.

- `--import-id <uuid>`
  Required. Selects the working import batch to summarize.

- `--limit <n>`
  Limits how many predicate-count rows are shown. Defaults to `10`.

## Notes

- This command is for the SQLite working index, not the durable `graph/assertions/` layer.
- If an import has not refreshed the index yet, the command will fail clearly instead of silently guessing.
- This is a working-layer read model, so it should stay rebuildable and subordinate to canonical history.
