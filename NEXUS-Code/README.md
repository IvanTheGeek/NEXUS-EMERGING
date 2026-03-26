# NEXUS-Code

This folder will hold the F# solution, importer core, provider adapters, CLI entry points, tests, and later supporting tools plus FnHCI/FnUI host/runtime work.

Near-term focus:

- domain types for canonical ingestion history
- provider adapters for ChatGPT and Claude
- importer pipeline
- TOML serialization
- manual CLI workflow
- raw-session preservation tools for local AI work logs

## v0 Shape

The first code scaffold is intentionally small and centered on the canonical ingestion layer.

- `src/Nexus.Kernel/`
  small semantic kernel for ontology-level role and relation primitives
- `src/Nexus.Logos/`
  small source-model scaffold for knowledge-bearing intake systems, channels, and signals
- `src/Nexus.Domain/`
  stable IDs, provenance, observed-history events, and a thin graph layer
- `src/Nexus.EventStore/`
  TOML serialization and append-only file writers for canonical events and manifests
- `src/Nexus.Importers/`
  raw zip intake, provider adapters, event-store dedupe index, and import workflow orchestration
- `src/Nexus.Curation/`
  concept-note seeding over conversation projections for durable repo memory
- `src/Nexus.Cli/`
  manual command-line entry points for smoke tests and provider imports
- [`docs/v0-module-map.md`](docs/v0-module-map.md)
  a short module and responsibility map for the first pass
- `scripts/export_codex_sessions.fsx`
  copies local Codex session transcripts into the NEXUS object layer as raw source artifacts

Working commands now include:

- `write-sample-event-store`
- `import-provider-export`
- `import-codex-sessions`
- `capture-artifact-payload`
- `rebuild-graph-assertions`
- `export-graphviz-dot`
- `create-concept-note`
- `rebuild-artifact-projections`
- `report-unresolved-artifacts`
- `rebuild-conversation-projections`
- `dotnet run --project NEXUS-Code/tests/Nexus.Tests/Nexus.Tests.fsproj`

Use built-in CLI help for quick guidance:

- `dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- --help`
- `dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- help <command>`

See:

- [`docs/how-to/cli-commands.md`](../docs/how-to/cli-commands.md)
- [`docs/how-to/capture-artifact-payload.md`](../docs/how-to/capture-artifact-payload.md)
- [`docs/how-to/import-provider-export.md`](../docs/how-to/import-provider-export.md)
- [`docs/how-to/import-codex-sessions.md`](../docs/how-to/import-codex-sessions.md)
- [`docs/how-to/rebuild-artifact-projections.md`](../docs/how-to/rebuild-artifact-projections.md)
- [`docs/how-to/rebuild-conversation-projections.md`](../docs/how-to/rebuild-conversation-projections.md)
- [`docs/how-to/create-concept-note.md`](../docs/how-to/create-concept-note.md)
- [`docs/how-to/rebuild-graph-assertions.md`](../docs/how-to/rebuild-graph-assertions.md)
- [`docs/how-to/export-graphviz-dot.md`](../docs/how-to/export-graphviz-dot.md)
- [`docs/how-to/report-unresolved-artifacts.md`](../docs/how-to/report-unresolved-artifacts.md)
- [`docs/how-to/export-codex-sessions.md`](../docs/how-to/export-codex-sessions.md)
- [`docs/how-to/run-tests.md`](../docs/how-to/run-tests.md)
- [`docs/nexus-ontology-imprint-alignment.md`](../docs/nexus-ontology-imprint-alignment.md)
- [`docs/logos-source-model-v0.md`](../docs/logos-source-model-v0.md)
- [`docs/fnhci-namespace-map.md`](../docs/fnhci-namespace-map.md)
- [`docs/fnui-foundation.md`](../docs/fnui-foundation.md)
- [`docs/repository-concern-lines.md`](../docs/repository-concern-lines.md)
- [`docs/fsharp-documentation-convention.md`](../docs/fsharp-documentation-convention.md)

## Testing Approach

The current testing stack is:

- `Expecto` for standard tests
- `Expecto.FsCheck` for invariants and property-style tests
- `Verify.Expecto` for snapshot verification of generated TOML artifacts

When learning a library used here, prefer its repo docs and examples first, then source, with XML/API inspection used only when exact behavior needs confirmation.
