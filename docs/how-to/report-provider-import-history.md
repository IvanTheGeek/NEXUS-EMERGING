# Report Provider Import History

Use this when you want to inspect one provider's normalized import snapshots in chronological order.

This is especially useful for export-window analysis, because it shows:

- which imports exist for a provider
- the normalized snapshot totals for each import
- the delta from the previous snapshot for that provider

Important:

- this is based on normalized import snapshots
- it is not based on additive working-batch contributions
- it reflects parsed provider payload membership before canonical dedupe

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-provider-import-history \
  --provider chatgpt
```

Limit the report to the newest `N` rows:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-provider-import-history \
  --provider claude \
  --limit 10
```

Use a non-default event store root:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-provider-import-history \
  --event-store-root /tmp/nexus-event-store \
  --provider chatgpt
```

Use a non-default objects root when the preserved raw artifacts live somewhere else:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-provider-import-history \
  --event-store-root /tmp/nexus-event-store \
  --objects-root /tmp/nexus-objects \
  --provider chatgpt
```

## Output

The report includes:

- provider slug
- available snapshot count
- reported entry count
- one row per reported import snapshot

Each row includes:

- `import_id`
- `imported_at`
- `window`
- conversation, message, and artifact-reference totals
- normalization version, when available
- source artifact relative path, when available
- source artifact SHA-256, when the preserved raw artifact is still available
- whether the preserved raw artifact matches the previous snapshot's preserved raw artifact
- delta from the previous snapshot for that provider

The first row for a provider reports:

- `delta_from_previous=none (first snapshot for provider)`

Later rows report adjacent delta counts like:

- `added=<n>`
- `removed=<n>`
- `changed=<n>`
- `unchanged=<n>`

When the preserved raw artifact still exists under the objects root, each row also reports:

- `source_artifact_sha256=<hash>`
- `source_artifact_matches_previous=<true|false>`

The first row for a provider reports:

- `source_artifact_matches_previous=none (first raw artifact for provider)`

If the preserved raw artifact path is missing, the row reports:

- `source_artifact_sha256=missing`
- `source_artifact_matches_previous=unknown`

## Notes

- If older imports are missing normalized snapshot files, run `rebuild-import-snapshots` first.
- This command is best for historical window analysis after import.
- Use `--objects-root <path>` when the preserved provider artifacts are not under the repository-default object store.
- Use `compare-provider-exports` when you want to compare raw vendor artifacts before import.
- Use `compare-import-snapshots` when you want a detailed pairwise snapshot comparison between two specific imports.

## Related Commands

- `compare-provider-exports`
- `compare-import-snapshots`
- `rebuild-import-snapshots`
