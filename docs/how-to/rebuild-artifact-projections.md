# Rebuild Artifact Projections

This command rebuilds per-artifact projection files from the canonical artifact event streams.

It does not change canonical history. It only rewrites the derived projection layer under `NEXUS-EventStore/projections/artifacts/`.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-artifact-projections
```

Optional override:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-artifact-projections \
  --event-store-root /some/other/event-store-root
```

## What It Produces

For each canonical artifact stream, the rebuild writes one projection file:

```text
NEXUS-EventStore/projections/artifacts/<artifact-id>.toml
```

Each projection currently includes:

- conversation and message links when known
- provider, provider conversation, provider message, and provider artifact IDs
- file name and media type when known
- original reference disposition
- whether the payload has been captured
- reference count and capture count
- first and last observed timestamps
- last captured timestamp and latest captured object path
- captured content hashes

## Notes

- Projections are rebuildable derived views.
- The rebuild deletes and rewrites the existing artifact projection folder.
- This projection is especially useful for finding unresolved artifact references and verifying later manual hydration.
