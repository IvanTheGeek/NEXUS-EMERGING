# Export LOGOS Public Notes

Use this when you want a dedicated public-safe output set from LOGOS notes that truly cross the public-safe boundary.

This is intentionally stricter than a handling audit.

The command does not export notes just because they are derived or redacted. It only exports notes that successfully cross the explicit `PublicSafe` pool boundary.

## What It Does

`export-logos-public-notes`:

- scans `docs/logos-intake/` and `docs/logos-intake-derived/` recursively
- evaluates each eligible note against the explicit public-safe pool rules
- exports only eligible notes into a dedicated output folder
- writes a `manifest.toml` describing what was exported and what was skipped

## Public-Safe Boundary

For a note to be exported, its handling policy must be:

- `sanitization_status = approved-for-sharing`
- `sensitivity = public`
- `sharing_scope = public`

Anything else is skipped.

That means this command is protected by the same typed boundary we wanted in code, not just by a loose report or convention.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  export-logos-public-notes
```

## Useful Options

- `--docs-root <path>`
  Optional. Override the docs root. Defaults to `docs/`.
- `--output-root <path>`
  Optional. Override the public export folder. Defaults to `docs/logos-public/`.

## Example

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  export-logos-public-notes \
  --docs-root /tmp/nexus-docs \
  --output-root /tmp/nexus-public
```

## Result

The command writes:

- one Markdown file per exported public-safe note
- `manifest.toml` with exported and skipped note rows

It reports:

- eligible notes scanned
- exported note count
- skipped note count

## Important Rules

- import permission is not publication permission
- sanitized is not automatically public-safe
- being in the intake tree is not automatically disqualifying if the note already crosses the public-safe boundary
- public-facing export depends on the explicit `PublicSafe` pool boundary
- a skipped note is not an error if it was never meant for public use
