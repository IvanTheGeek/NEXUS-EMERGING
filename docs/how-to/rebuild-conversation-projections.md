# Rebuild Conversation Projections

This command rebuilds per-conversation projection files from the canonical event store.

It does not change canonical history. It only rewrites the derived projection layer under `NEXUS-EventStore/projections/conversations/`.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-conversation-projections
```

Optional override:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-conversation-projections \
  --event-store-root /some/other/event-store-root
```

## What It Produces

For each canonical conversation stream, the rebuild writes one projection file:

```text
NEXUS-EventStore/projections/conversations/<conversation-id>.toml
```

Each projection currently includes:

- conversation title when known
- providers and provider conversation IDs
- import IDs that touched the conversation
- message count
- artifact reference count
- revision count
- first/last occurred timestamps
- last observed timestamp
- lightweight message previews with role, sequence, excerpt, and artifact count

## Notes

- Projections are rebuildable derived views.
- The rebuild deletes and rewrites the existing conversation projection folder.
- If canonical event history changes, run this again to refresh the read side.
- Message excerpts prefer human-visible text and only fall back to tool or reasoning content when no visible reply text is available.
