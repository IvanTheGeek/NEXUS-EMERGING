# Import Codex Sessions

This guide explains how to take preserved Codex session JSONL snapshots from the NEXUS object layer and append them into the canonical event store.

Use this after running the raw exporter:

```bash
dotnet fsi NEXUS-Code/scripts/export_codex_sessions.fsx
```

## What This Does

The importer reads a preserved snapshot root such as:

- `NEXUS-Objects/providers/codex/latest/`
- `NEXUS-Objects/providers/codex/archive/<timestamp>/`

It then:

- reads `session_index.jsonl` when present
- reads transcript files under `sessions/`
- canonicalizes `user_message` and `agent_message` records
- writes append-only canonical events into `NEXUS-EventStore/events/`
- writes an import manifest into `NEXUS-EventStore/imports/`
- materializes an import-local graph working slice under `NEXUS-EventStore/graph/working/imports/<import-id>/`
- updates the graph working catalog under `NEXUS-EventStore/graph/working/catalog/import-batches.toml`
- refreshes the SQLite working index under `NEXUS-EventStore/graph/working/index/graph-working.sqlite`

In v1, richer Codex runtime records such as function calls, tool outputs, reasoning, token counts, and other non-message events remain preserved only in raw JSONL.

## Command

Run this from the repository root:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- import-codex-sessions
```

## Optional Arguments

Import from a specific snapshot or override roots:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- import-codex-sessions \
  --snapshot-root /home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-Objects/providers/codex/archive/2026-03-22T16-03-56Z \
  --objects-root /home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-Objects \
  --event-store-root /home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore
```

## Normalization Behavior

- conversations are keyed by Codex session ID
- message identities are stable per transcript line, so re-imports dedupe cleanly
- repeated imports of the same snapshot should mostly append only import-stream events
- future parser upgrades append re-observations under a different normalization version rather than pretending the provider revised older messages

## Recommended Follow-Up

After importing, rebuild conversation projections:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- rebuild-conversation-projections
```

You can also inspect or visualize the fresh graph working slice without a full durable graph rebuild:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-working-graph-imports
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-working-graph-slice --import-id <import-id>
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --working-import-id <import-id>
```
