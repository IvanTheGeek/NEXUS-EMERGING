# Compare Import Snapshots

Use this when you want to compare two normalized import snapshots after provider import.

This is useful for questions like:

- which provider-native conversations were present in import A vs import B?
- did a shared conversation gain more normalized messages between imports?
- which conversations are new to the later import snapshot?
- how do full exports and later rolling windows differ at the normalized layer?

Important:

- this is a **normalized import-snapshot comparison**
- it is derived from the parsed provider payload before canonical dedupe
- it is **not** canonical deletion logic
- absence from the current snapshot means absence from that import payload only

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  compare-import-snapshots \
  --base-import-id <uuid> \
  --current-import-id <uuid>
```

Limit detailed rows:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  compare-import-snapshots \
  --base-import-id <uuid> \
  --current-import-id <uuid> \
  --limit 10
```

Use a non-default event store root:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  compare-import-snapshots \
  --event-store-root /tmp/nexus-event-store \
  --base-import-id <uuid> \
  --current-import-id <uuid>
```

## Output

The report includes:

- base/current import IDs
- provider and window labels
- imported timestamps
- added, removed, changed, and unchanged conversation counts

Detailed rows are grouped into:

- `Added`
- `Removed`
- `Changed`

Each detailed row is keyed by `provider_conversation_id`.

Changed rows show normalized deltas like:

- `canonical_conversation_id=<base> -> <current>`
- `messages=2 -> 3`
- `artifacts=1 -> 2`

## How It Differs From Other Compare Commands

`compare-provider-exports`

- raw source-layer comparison
- compares zip artifacts before import
- best for checking vendor export behavior directly

`compare-import-snapshots`

- normalized snapshot comparison after import
- compares parsed provider payload membership before canonical dedupe
- best for full-export and rolling-window reasoning inside the NEXUS pipeline

`compare-working-import-conversations`

- derived batch-local working-slice comparison
- compares additive graph-working contributions, not full snapshot truth

## Notes

- Snapshot files are written during `import-provider-export`.
- Older imports may predate snapshot materialization and therefore may not be comparable with this command yet.
- Use `rebuild-import-snapshots` to backfill those normalized snapshot files from preserved raw exports.
- Normalized import snapshots are derived working artifacts, not canonical truth.
- Canonical history still remains the durable append-only authority.

## Related Commands

- `compare-provider-exports`
- `rebuild-import-snapshots`
- `import-provider-export`
- `compare-working-import-conversations`
