# Compare Working Import Conversations

Use this when you want to compare the conversation-level contributions of two import-local graph working slices.

This is useful for questions like:

- what did this newer import batch add?
- which conversations contributed new graph-local structure?
- which previously seen conversations contributed different message or artifact counts in this batch?

Important:

- this compares **batch-local working-slice contributions**
- it does **not** claim to be full provider snapshot truth
- if you need stronger validation, follow up with canonical or raw-layer inspection

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  compare-working-import-conversations \
  --base-import-id <uuid> \
  --current-import-id <uuid>
```

Limit detailed rows per bucket:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  compare-working-import-conversations \
  --base-import-id <uuid> \
  --current-import-id <uuid> \
  --limit 10
```

Use a different event-store root:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  compare-working-import-conversations \
  --event-store-root /tmp/nexus-event-store \
  --base-import-id <uuid> \
  --current-import-id <uuid>
```

## Output

The report shows:

- base import metadata
- current import metadata
- added conversation count
- removed conversation count
- changed conversation count
- unchanged shared conversation count

Detailed rows are grouped into:

- `Added`
- `Removed`
- `Changed`

Changed rows show contribution deltas like:

- `messages=2 -> 1`
- `artifacts=1 -> 0`

That means the two import-local slices contributed different counts for the same canonical conversation node.

## Related Commands

- `report-working-import-conversations`
- `report-working-graph-imports`
- `verify-working-graph-slice`
- `export-graphviz-dot --working-import-id <uuid>`
