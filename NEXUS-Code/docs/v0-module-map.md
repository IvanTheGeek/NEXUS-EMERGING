# NEXUS-Code v0 Module Map

This is the first F# shape for the canonical ingestion layer.

The goal is to stabilize naming and module responsibilities while the first real importer workflow is being built.

## Project

- `src/Nexus.Domain/Nexus.Domain.fsproj`
  the initial domain-only project
- `src/Nexus.EventStore/Nexus.EventStore.fsproj`
  event-store serialization and file writing on top of `Nexus.Domain`
- `src/Nexus.Importers/Nexus.Importers.fsproj`
  raw-intake, provider parsing, dedupe index loading, and import orchestration
- `src/Nexus.Cli/Nexus.Cli.fsproj`
  manual CLI entry points that exercise the domain and event-store layers

## Modules

- `Nexus.Domain/IDs.fs`
  canonical identifiers and broad classification IDs
- `Nexus.Domain/Time.fs`
  timestamp wrappers for occurred, observed, and imported times
- `Nexus.Domain/Provenance.fs`
  provider identity, raw object references, acquisition kinds, and provenance types
- `Nexus.Domain/CanonicalHistory.fs`
  append-only canonical event envelope, event bodies, and import manifest types
- `Nexus.Domain/Graph.fs`
  a thin graph-compatible layer for nodes, edges, values, and assertions
- `Nexus.EventStore/Toml.fs`
  low-level TOML rendering helpers
- `Nexus.EventStore/TomlDocument.fs`
  lightweight TOML reader for scanning canonical event files
- `Nexus.EventStore/CanonicalStore.fs`
  canonical event, import manifest, and graph assertion serialization plus file layout rules
- `Nexus.EventStore/ConversationProjections.fs`
  rebuildable read-model generation for conversation projections
- `Nexus.EventStore/ArtifactProjections.fs`
  rebuildable read-model generation for artifact projections and hydration status
- `Nexus.Importers/ImporterTypes.fs`
  provider naming, window naming, parsed-record shapes, and import request/result types
- `Nexus.Importers/EventStoreIndex.fs`
  lightweight event-store scan used for duplicate and revision detection
- `Nexus.Importers/Providers.fs`
  provider-specific parsers/adapters for ChatGPT and Claude exports
- `Nexus.Importers/ImportWorkflow.fs`
  archive/extract/import pipeline that turns raw exports into canonical events
- `Nexus.Importers/ManualArtifactWorkflow.fs`
  manual artifact hydration workflow for appending `ArtifactPayloadCaptured` events
- `Nexus.Cli/Program.fs`
  manual commands such as writing sample canonical history, importing provider exports, and capturing artifact payloads

## Design Notes

- the canonical history layer uses `Observed` language
- internal event, import, conversation, message, artifact, node, edge, and fact IDs default to UUIDv7
- semantic taxonomy IDs like `DomainId`, `BoundedContextId`, and `LensId` stay human-readable slugs
- the graph layer is intentionally thin and derived-friendly
- `Domain`, `BoundedContext`, and `Lens` are present as concepts without forcing a final ontology
- provider adapters are isolated from the shared canonical event-writing core
- dedupe is currently driven by provider object identity plus canonical content hash
- the importer currently supports first real full-export imports for ChatGPT and Claude
- conversation projections are rebuildable from canonical history and do not carry source-of-truth authority
- artifact projections are rebuildable from canonical artifact streams and make unresolved versus hydrated payload state easy to inspect
