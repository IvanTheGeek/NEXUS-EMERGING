# Codex Session Export

This script preserves local Codex session transcripts as raw source artifacts inside the NEXUS object layer.

## Purpose

It is not a canonical importer.

It exists so that Codex conversations themselves can be archived as source material before any normalization work happens.

## What It Copies

From the local Codex home, the script copies:

- `session_index.jsonl`
- all transcript files under `sessions/`

It does not currently copy:

- runtime SQLite logs
- auth or config files
- shell snapshots

## Destination Layout

The default destination is:

- `NEXUS-Objects/providers/codex/latest/`
- `NEXUS-Objects/providers/codex/archive/<timestamp>/`

Each run:

- refreshes the `latest/` mirror
- creates a new immutable timestamped archive snapshot
- writes a simple TOML manifest describing exported files and hashes

## Usage

From the repository root:

```bash
dotnet fsi NEXUS-Code/scripts/export_codex_sessions.fsx
```

Optional arguments:

```bash
dotnet fsi NEXUS-Code/scripts/export_codex_sessions.fsx -- \
  --source-root /home/ivan/.codex \
  --destination-root /home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-Objects/providers/codex \
  --snapshot-name 2026-03-22T20-10-00Z
```

Dry run:

```bash
dotnet fsi NEXUS-Code/scripts/export_codex_sessions.fsx -- --dry-run
```
