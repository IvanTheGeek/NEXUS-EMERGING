# NEXUS-EMERGING

This repository is the early foundation workspace for NEXUS.

NEXUS is being shaped as a system where:

- source truth is preserved
- canonical history is append-only
- graph structure is derived from observed history
- multiple domains, bounded contexts, and lenses can coexist over one underlying reality

The current focus is the ingestion foundation:

- preserve provider artifacts from systems like ChatGPT and Claude
- normalize those artifacts into a canonical append-only history
- keep provenance strong enough to support reparsing later
- avoid forcing the final NEXUS ontology too early

## Workspace Boundaries

This workspace intentionally separates three concerns, even though they currently live under one root:

- `NEXUS-Code/`
  F# code, adapters, importer workflow, tests, later GUI/tools
- `NEXUS-Objects/`
  provider zips, extracted raw payloads, attachments, manually added artifacts
- `NEXUS-EventStore/`
  append-only canonical events, manifests, projections, later graph assertions

These boundaries are real design seams now and can become separate systems later.

## Documentation Spine

Core project memory lives in the repo so humans and AI agents can recover intent quickly:

- `docs/nexus-core-conceptual-layers.md`
- `docs/nexus-ingestion-architecture.md`
- `docs/glossary.md`
- `docs/how-to/`
- `docs/decisions/`

## Current Status

The project is in architecture and scaffolding mode.

Already established:

- Git is the durable history mechanism for canonical event history.
- Conversations are domain entities and streams, not Git branches.
- Provider exports are acquisition inputs, not absolute truth.
- Raw artifacts must be preserved.
- Canonical history should prefer `Observed` language at the ingestion layer.

Not yet established:

- final graph ontology
- final domain taxonomy
- final live capture workflow
- final storage split into separate deployed systems
