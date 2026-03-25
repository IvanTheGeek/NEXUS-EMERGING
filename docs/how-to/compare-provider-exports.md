# Compare Provider Exports

Use this when you want to compare two raw ChatGPT, Claude, or Grok export zips before importing either one into canonical history.

This is useful for questions like:

- did this newer export actually add anything?
- how many provider-native conversations or messages are new?
- did a shared conversation gain more messages in the newer export?
- are these two vendor artifacts actually identical bytes?

Important:

- this is a **raw source-layer comparison**
- it does **not** import, archive, or append anything
- it compares provider-native conversations and messages
- it is meant to help you reason about export-window behavior before canonical import

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  compare-provider-exports \
  --provider <chatgpt|claude|grok> \
  --base-zip <path> \
  --current-zip <path>
```

Limit detailed rows:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  compare-provider-exports \
  --provider chatgpt \
  --base-zip RawDataExports/chatgpt-older.zip \
  --current-zip RawDataExports/chatgpt-newer.zip \
  --limit 10
```

## Output

The report includes:

- SHA-256 for both zip artifacts
- whether the zip bytes are identical
- base/current conversation counts
- base/current message counts
- base/current artifact-reference counts
- added, removed, changed, and unchanged shared conversation counts

Detailed rows are grouped into:

- `Added`
- `Removed`
- `Changed`

Changed rows show deltas like:

- `messages=2 -> 3 (added=1 removed=0)`
- `artifacts=1 -> 1 (added=0 removed=0)`

That means the same provider-native conversation ID appeared in both zips, but its message or artifact-reference set changed.

## Notes

- This comparison is based on provider-native IDs and the parsed provider payload content.
- It does not claim canonical or ontology-level meaning.
- If the two zips are byte-identical, the comparison will report that directly.
- After this check, use `import-provider-export` when you want to append canonical history.

## Related Commands

- `compare-import-snapshots`
- `import-provider-export`
- `report-working-import-conversations`
- `compare-working-import-conversations`
