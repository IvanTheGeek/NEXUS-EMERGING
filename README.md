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
- `docs/nexus-graph-materialization-plan.md`
- `docs/nexus-ontology-imprint-alignment.md`
- `docs/logos-source-model-v0.md`
- `docs/fsharp-documentation-convention.md`
- `docs/glossary.md`
- `docs/concepts/`
- `docs/how-to/`
- `docs/how-to/cli-commands.md`
- `docs/decisions/`

Repository docs are the primary onboarding surface. Prefer:

1. docs/TOC first
2. examples and tests next
3. source after that
4. XML docs and API-level inspection as supporting detail

## Current Status

The project is past pure scaffolding and into first working ingestion.

Already established:

- Git is the durable history mechanism for canonical event history.
- Conversations are domain entities and streams, not Git branches.
- Provider exports are acquisition inputs, not absolute truth.
- Raw artifacts must be preserved.
- Canonical history should prefer `Observed` language at the ingestion layer.
- Working CLI importers exist for ChatGPT, Claude, Grok, and Codex capture.
- A first manual artifact hydration command exists for appending `ArtifactPayloadCaptured`.
- Canonical events and import manifests can be written into `NEXUS-EventStore/`.
- Normalized import snapshots exist for provider-export imports and can be rebuilt for older imports.
- A current-ingestion report exists for cross-provider operational status.
- A first external graph export exists through Graphviz DOT output over derived graph assertions.
- A first concept-note curation workflow exists for promoting conversation material into durable repo memory.
- A first LOGOS source-model scaffold exists for source systems, intake channels, and signal kinds.
- A first explicit LOGOS handling-policy model exists for sensitivity, sharing scope, sanitization status, and retention class.
- Concrete non-chat LOGOS source systems now exist for forum, email, issue-tracker, and app-feedback surfaces.
- A first LOGOS intake-note workflow exists for seeding non-chat intake into durable repo memory.
- Restricted-by-default intake and explicit publication are now named architectural rules.
- A first explicit overlap-candidate report exists without collapsing acquisition history automatically.
- Full graph rebuilds are now explicitly treated as heavyweight operations, and a secondary graph working layer is planned next.

Not yet established:

- final graph ontology
- final domain taxonomy
- final live capture workflow
- final storage split into separate deployed systems

## Git Workflow

This repository preserves branch history intentionally.

- merge feature and exploration branches into `main` with `--no-ff`
- do not rely on fast-forward merges for accepted work
- avoid squash merges when the branch history itself is part of the durable record

The goal is to keep implementation branches inspectable later as durable lines of work, thought, and experimentation.

Preferred branch shape:

- start focused work from `main` on a topic branch
- merge accepted work back with `--no-ff`
- delete completed topic branches after merge
- keep longer-lived branches only when a stream truly continues across multiple merges
- multiple long-running branches can coexist when they reflect genuinely different concerns
- periodically merge active long-running branches when convergence matters
- tag milestone merges when the checkpoint itself should stay easy to find in history

Historical milestone:

- `ingestion-foundation-v0` marks the first ingestion-foundation merge milestone

Current naming preference:

- use plain workstream branch names such as `export-window-analysis` or `logos-intake-foundation`
- avoid agent-qualified prefixes unless they add real meaning
