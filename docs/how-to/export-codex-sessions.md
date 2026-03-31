# Export Codex Sessions

This guide explains how to archive local Codex chat transcripts into the NEXUS object layer.

Use this when you want to preserve your Codex prompts and responses as raw source material before any normalization or canonical import work.

## What This Does

The exporter copies local Codex session history from `~/.codex` into:

- `NEXUS-Objects/providers/codex/latest/`
- `NEXUS-Objects/providers/codex/archive/<timestamp>/`

It preserves:

- `session_index.jsonl`
- session transcript files under `sessions/`

It also writes an `export-manifest.toml` that records what was copied and the file hashes.

## Command

Run this from the repository root:

```bash
dotnet fsi NEXUS-Code/scripts/export_codex_sessions.fsx
```

## Dry Run

If you want to preview what would happen without writing files:

```bash
dotnet fsi NEXUS-Code/scripts/export_codex_sessions.fsx -- --dry-run
```

## Optional Arguments

You can override the source, destination, or archive snapshot name:

```bash
dotnet fsi NEXUS-Code/scripts/export_codex_sessions.fsx -- \
  --source-root /home/ivan/.codex \
  --destination-root /home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-Objects/providers/codex \
  --snapshot-name 2026-03-22T20-10-00Z
```

## Where To Look After Running It

Latest mirror:

- `NEXUS-Objects/providers/codex/latest/`

Immutable snapshot archive:

- `NEXUS-Objects/providers/codex/archive/`

Manifest example:

- `NEXUS-Objects/providers/codex/latest/export-manifest.toml`

## Notes

- This is raw preservation, not canonical import.
- After exporting, use `import-codex-sessions` to append Codex conversations into the canonical event store.
- If you want the Codex snapshot explicitly linked to a Git commit, use `capture-codex-commit-checkpoint` instead of running export and import as separate manual steps.
- The local session JSONL files contain the actual turn history, including your prompts and Codex responses.
- Runtime app logs are not the same thing and are not what this exporter is preserving.

## Related Files

- `NEXUS-Code/scripts/export_codex_sessions.fsx`
- [`NEXUS-Code/docs/codex-session-export.md`](https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/main/NEXUS-Code/docs/codex-session-export.md)
