# NEXUS-Code v0 Module Map

This is the first F# shape for the canonical ingestion layer.

The goal is to stabilize naming and module responsibilities before importer logic is added.

## Project

- `src/Nexus.Domain/Nexus.Domain.fsproj`
  the initial domain-only project
- `src/Nexus.EventStore/Nexus.EventStore.fsproj`
  event-store serialization and file writing on top of `Nexus.Domain`

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
- `Nexus.EventStore/CanonicalStore.fs`
  canonical event, import manifest, and graph assertion serialization plus file layout rules

## Design Notes

- the canonical history layer uses `Observed` language
- internal event, import, conversation, message, artifact, node, edge, and fact IDs default to UUIDv7
- semantic taxonomy IDs like `DomainId`, `BoundedContextId`, and `LensId` stay human-readable slugs
- the graph layer is intentionally thin and derived-friendly
- `Domain`, `BoundedContext`, and `Lens` are present as concepts without forcing a final ontology
- provider adapters and source parsing are explicitly deferred to the next slice
