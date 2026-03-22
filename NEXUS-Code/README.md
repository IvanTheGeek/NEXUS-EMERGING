# NEXUS-Code

This folder will hold the F# solution, importer core, provider adapters, CLI entry points, tests, and later supporting tools.

Near-term focus:

- domain types for canonical ingestion history
- provider adapters for ChatGPT and Claude
- importer pipeline
- TOML serialization
- manual CLI workflow
- raw-session preservation tools for local AI work logs

## v0 Shape

The first code scaffold is intentionally small and centered on the canonical ingestion layer.

- `src/Nexus.Domain/`
  stable IDs, provenance, observed-history events, and a thin graph layer
- `src/Nexus.EventStore/`
  TOML serialization and append-only file writers for canonical events and manifests
- `docs/v0-module-map.md`
  a short module and responsibility map for the first pass
- `scripts/export_codex_sessions.fsx`
  copies local Codex session transcripts into the NEXUS object layer as raw source artifacts

Importer workflow, provider parsing, and file writing come after the domain surface is agreed.
