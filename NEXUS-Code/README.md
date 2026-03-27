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

- `src/Nexus.FnHCI/`
  current project path for the `FnTools.FnHCI` interaction namespace and shared interaction-line primitives
- `src/Nexus.FnHCI.UI/`
  current project path for the `FnTools.FnHCI.UI` renderer-neutral visual shell and navigation boundary
- `src/Nexus.FnHCI.UI.Blazor/`
  current project path for the `FnTools.FnHCI.UI.Blazor` host seam
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
- `capture-codex-commit-checkpoint`
- `report-codex-commit-checkpoint`
- `install-codex-commit-checkpoint-hook`
- `import-logos-blog-repo`
- `capture-artifact-payload`
- `rebuild-graph-assertions`
- `export-graphviz-dot`
- `create-concept-note`
- `rebuild-artifact-projections`
- `report-unresolved-artifacts`
- `rebuild-conversation-projections`
- `./NEXUS-Code/tests/run-all-tests.sh`

Use built-in CLI help for quick guidance:

- `dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- --help`
- `dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- help <command>`

See:

- [`docs/how-to/cli-commands.md`](../docs/how-to/cli-commands.md)
- [`docs/how-to/capture-artifact-payload.md`](../docs/how-to/capture-artifact-payload.md)
- [`docs/how-to/capture-codex-commit-checkpoint.md`](../docs/how-to/capture-codex-commit-checkpoint.md)
- [`docs/how-to/install-codex-commit-checkpoint-hook.md`](../docs/how-to/install-codex-commit-checkpoint-hook.md)
- [`docs/how-to/import-provider-export.md`](../docs/how-to/import-provider-export.md)
- [`docs/how-to/import-codex-sessions.md`](../docs/how-to/import-codex-sessions.md)
- [`docs/how-to/import-logos-blog-repo.md`](../docs/how-to/import-logos-blog-repo.md)
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
- [`docs/decisions/0015-fnhci-owns-the-top-interaction-namespace.md`](../docs/decisions/0015-fnhci-owns-the-top-interaction-namespace.md)
- [`docs/fnhci-namespace-map.md`](../docs/fnhci-namespace-map.md)
- [`docs/fnui-foundation.md`](../docs/fnui-foundation.md)
- [`docs/fnhci-ui-blazor-requirements.md`](../docs/fnhci-ui-blazor-requirements.md)
- [`docs/repository-concern-lines.md`](../docs/repository-concern-lines.md)
- [`docs/fsharp-documentation-convention.md`](../docs/fsharp-documentation-convention.md)
- [`docs/decisions/0017-docs-and-tests-ship-with-work.md`](../docs/decisions/0017-docs-and-tests-ship-with-work.md)

## Working Expectations

When changing code here, treat docs and tests as part of the work rather than optional follow-up.

- update or add tests when runtime behavior changes
- update CLI help and runbooks when commands or workflow expectations change
- update source xmldoc when public API surfaces change
- if a change is docs-only or tests are not applicable, say that explicitly

## Testing Approach

The current testing stack is:

- `Expecto` for standard tests
- `Expecto.FsCheck` for invariants and property-style tests
- `Verify.Expecto` for snapshot verification of generated TOML artifacts

The suite is now split by concern line:

- `Nexus.Foundation.Tests`
- `FnTools.Tests`
- `CheddarBooks.Tests`

When learning a library used here, prefer its repo docs and examples first, then source, with XML/API inspection used only when exact behavior needs confirmation.
