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

- [`docs/nexus-core-conceptual-layers.md`](docs/nexus-core-conceptual-layers.md)
- [`docs/nexus-ingestion-architecture.md`](docs/nexus-ingestion-architecture.md)
- [`docs/nexus-graph-materialization-plan.md`](docs/nexus-graph-materialization-plan.md)
- [`docs/nexus-ontology-imprint-alignment.md`](docs/nexus-ontology-imprint-alignment.md)
- [`docs/logos-source-model-v0.md`](docs/logos-source-model-v0.md)
- [`docs/fnhci-namespace-map.md`](docs/fnhci-namespace-map.md)
- [`docs/fnui-foundation.md`](docs/fnui-foundation.md)
- [`docs/fnhci-ui-blazor-requirements.md`](docs/fnhci-ui-blazor-requirements.md)
- [`docs/laundrylog-fnui-proving-ground.md`](docs/laundrylog-fnui-proving-ground.md)
- [`docs/repository-concern-lines.md`](docs/repository-concern-lines.md)
- [`docs/fsharp-documentation-convention.md`](docs/fsharp-documentation-convention.md)
- [`docs/glossary.md`](docs/glossary.md)
- [`docs/concepts/`](docs/concepts/README.md)
- [`docs/how-to/`](docs/how-to/README.md)
- [`docs/how-to/cli-commands.md`](docs/how-to/cli-commands.md)
- [`docs/decisions/`](docs/decisions/)

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
- Provider and Codex imports now enter the system with restricted-by-default LOGOS source, signal, handling-policy, and entry-pool metadata.
- A first external graph export exists through Graphviz DOT output over derived graph assertions.
- A first concept-note curation workflow exists for promoting conversation material into durable repo memory.
- A first LOGOS source-model scaffold exists for source systems, intake channels, and signal kinds.
- A first explicit LOGOS handling-policy model exists for sensitivity, sharing scope, sanitization status, and retention class.
- A first explicit LOGOS access and rights model now exists for source instance, access context, acquisition kind, rights policy, and attribution reference.
- Concrete non-chat LOGOS source systems now exist for forum, Talkyard, Discord, email, issue-tracker, and app-feedback surfaces.
- A first LOGOS intake-note workflow exists for seeding non-chat intake into durable repo memory.
- A first LOGOS derived sanitization workflow exists for creating safer notes without widening access to restricted raw intake.
- A first LOGOS handling audit report exists for surfacing raw, restricted, and approved note states.
- A first LOGOS pool-boundary model exists so future public-facing flows can depend on explicit `public-safe` types instead of loose policy checks.
- A first LOGOS public-safe export workflow now exists and only emits notes that successfully cross that explicit `public-safe` boundary plus rights that allow public distribution.
- Public-safe export manifests now surface attribution obligations explicitly for later prominent UI/help/about exposure.
- FnHCI now explicitly owns the top interaction namespace, while FnUI is tracked as the narrower visual/UI system and likely package line for the Bolero-replacement path and the real NEXUS GUI.
- Non-chat LOGOS notes now enter explicit `raw`, `private`, or `public-safe` pool paths at creation time instead of relying on a later inferred layout.
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

Concern-line map:

- [`docs/repository-concern-lines.md`](docs/repository-concern-lines.md)
  use this when deciding whether work belongs to NEXUS core, engineering conventions, repository governance, ingestion, LOGOS, interaction/UI, integrations, or an application-specific domain

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
