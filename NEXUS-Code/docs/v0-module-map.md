# NEXUS-Code v0 Module Map

This is the first F# shape for the canonical ingestion layer.

The goal is to stabilize naming and module responsibilities before importer logic is added.

## Project

- `src/Nexus.Domain/Nexus.Domain.fsproj`
  the initial domain-only project

## Modules

- `Ids.fs`
  canonical identifiers and broad classification IDs
- `Time.fs`
  timestamp wrappers for occurred, observed, and imported times
- `Provenance.fs`
  provider identity, raw object references, acquisition kinds, and provenance types
- `CanonicalHistory.fs`
  append-only canonical event envelope, event bodies, and import manifest types
- `Graph.fs`
  a thin graph-compatible layer for nodes, edges, values, and assertions

## Design Notes

- the canonical history layer uses `Observed` language
- the graph layer is intentionally thin and derived-friendly
- `Domain`, `BoundedContext`, and `Lens` are present as concepts without forcing a final ontology
- provider adapters and importer workflow are explicitly deferred to the next slice
