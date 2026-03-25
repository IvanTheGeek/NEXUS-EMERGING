# Import Provider Exports

This command takes a provider export zip, archives it into the NEXUS object layer, extracts a working raw snapshot, parses the provider payload, and writes canonical observed-history events plus an import manifest into the event store.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  import-provider-export \
  --provider <chatgpt|claude|grok> \
  --zip <path-to-export.zip> \
  --window full
```

## Examples

Claude:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  import-provider-export \
  --provider claude \
  --zip RawDataExports/data-2026-03-22-12-13-53-batch-0000.zip \
  --window full
```

ChatGPT:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  import-provider-export \
  --provider chatgpt \
  --zip RawDataExports/6dcabe6366235ba54dd09835c0f06f95c4ba2daa260f5ccac73dff295d2f1bda-2026-03-21-00-59-59-9c115c057bfc4f5f9a779548b8846d7d.zip \
  --window full
```

Grok:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  import-provider-export \
  --provider grok \
  --zip "RawDataExports/ab6110cc-45f1-48f3-8e4a-091c0a0440e5 (1).zip" \
  --window 30d
```

## What It Does

1. Copies the source zip into `NEXUS-Objects/providers/<provider>/archive/...`
2. Updates the stable latest zip and extracted latest snapshot under `NEXUS-Objects/providers/<provider>/latest/`
3. Extracts the provider payload and the rest of the zip contents into the raw object layer
4. Parses provider conversations and messages
5. Writes canonical events into `NEXUS-EventStore/events/...`
6. Writes an import manifest into `NEXUS-EventStore/imports/...`
7. Writes a normalized import snapshot under `NEXUS-EventStore/snapshots/imports/<import-id>/...`
8. Materializes an import-local graph working batch under `NEXUS-EventStore/graph/working/imports/<import-id>/...`
9. Updates the graph working catalog under `NEXUS-EventStore/graph/working/catalog/import-batches.toml`
10. Refreshes the SQLite working index under `NEXUS-EventStore/graph/working/index/graph-working.sqlite`

## Progress Output

The CLI prints import phases as it works so you can tell the difference between a quiet wait and active processing.

Typical phases:

- preparing the import request
- archiving the raw export zip
- parsing the provider payload file
- loading the event-store dedupe index
- processing conversations into canonical history
- writing canonical events
- writing the import manifest
- writing the normalized import snapshot
- completion summary with elapsed time and counts

Larger imports also emit periodic conversation-processing updates with running message, artifact, duplicate, revision, and reparse counts.

The import summary also reports the normalized import-snapshot paths, the graph-working manifest path, the graph working catalog path, the SQLite working-index path, and the assertion count for the batch-local materialization.

## Current v0 Scope

The importer currently normalizes:

- provider artifact received
- raw snapshot extracted
- provider conversation observed
- provider message observed
- provider message revision observed
- artifact referenced
- import completed

Current provider parsing scope:

- ChatGPT: conversations, messages, attachment references from message metadata
- Claude: conversations, messages, file/attachment references from message payloads
- Grok: conversations, responses, message text, and file-attachment references from `prod-grok-backend.json`

Still deferred:

- full canonical modeling for Claude projects, memories, and users
- richer normalization of non-text provider content
- automatic artifact payload hydration
- live API capture

## Defaults and Overrides

Defaults:

- `--window full`
- objects root: `NEXUS-Objects/`
- event-store root: `NEXUS-EventStore/`

Optional overrides:

```bash
--objects-root /some/other/objects-root
--event-store-root /some/other/event-store-root
```

## Notes

- The raw object layer is ignored by Git in this repo for now.
- The canonical event store is intended to be committed.
- Each import records a `normalization_version` so parser/canonicalizer changes can be tracked explicitly.
- Each provider import now also records a normalized per-import snapshot so later full exports and rolling windows can be compared without confusing additive dedupe behavior for snapshot truth.
- Older imports that predate this snapshot layer can be backfilled with `rebuild-import-snapshots`.
- The current importer baseline is `provider-export-v1`. Earlier history without explicit versioning is treated as the legacy `provider-export-v0` baseline for reparse comparisons.
- Re-importing the same provider objects under the same normalization version will skip duplicates and emit revision events only when a known provider message is observed with changed canonical content.
- Re-importing the same provider objects under a different normalization version appends new message observations instead of pretending the provider message itself changed.
