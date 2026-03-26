# Report LOGOS Handling

Use this when you want a quick audit of the handling state of LOGOS notes.

It scans:

- `docs/logos-intake/`
- `docs/logos-intake-derived/`

and reports:

- note counts by kind
- entry-pool counts
- sensitivity counts
- sharing-scope counts
- sanitization-status counts
- retention-class counts
- flagged note rows for:
  - still raw
  - personal-private
  - customer-confidential
  - approved-for-sharing

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-logos-handling
```

## Useful Options

- `--docs-root <path>`
  Override the docs root.
- `--limit <n>`
  Limit the note rows shown in each flagged section. Defaults to `10`.

## Example

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-logos-handling \
  --docs-root /tmp/nexus-docs \
  --limit 5
```

## Why This Matters

This report helps answer operational questions like:

- which notes are still `raw`?
- which notes are still `personal-private` or `customer-confidential`?
- which derived notes are already marked `approved-for-sharing`?

It is an audit report, not a publication gate by itself.

The intended rule still stands:

- import permission is not publication permission
- restricted source notes remain the source note of record
- safer derivatives should be explicit

The report scans both note trees recursively, so pool subfolders like `raw/`, `private/`, and `public-safe/` are all included.
