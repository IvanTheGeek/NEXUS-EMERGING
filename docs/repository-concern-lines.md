# Repository Concern Lines

This document is the practical map of the major concern lines inside NEXUS.

Use it when deciding:

- where a new decision belongs
- where a new concept note belongs
- which branch a change should live on
- whether work is foundational or specific to one domain or platform

## Current Concern Lines

### NEXUS Core

Scope:

- the meaning of NEXUS itself
- ontology boundaries
- cross-project principles
- core vocabulary

Primary docs:

- [`README.md`](../README.md)
- [`docs/glossary.md`](glossary.md)
- [`docs/nexus-core-conceptual-layers.md`](nexus-core-conceptual-layers.md)
- [`docs/nexus-ontology-imprint-alignment.md`](nexus-ontology-imprint-alignment.md)

Typical branch names:

- `nexus-core`
- `ontology-imprint-alignment`

### Engineering Conventions

Scope:

- F# modeling practices
- testing strategy
- documentation conventions
- explicit allowlists and deterministic coding rules

Primary docs:

- [`docs/fsharp-documentation-convention.md`](fsharp-documentation-convention.md)
- [`docs/decisions/0003-testing-stack-and-library-onboarding.md`](decisions/0003-testing-stack-and-library-onboarding.md)
- [`docs/decisions/0005-explicit-allowlists-over-catchalls.md`](decisions/0005-explicit-allowlists-over-catchalls.md)
- [`docs/decisions/0017-docs-and-tests-ship-with-work.md`](decisions/0017-docs-and-tests-ship-with-work.md)

Typical branch names:

- `engineering-conventions`
- `testing-foundation`

### Repository Governance

Scope:

- branch topology
- project-memory structure
- terminology governance
- decision-record discipline

Primary docs:

- [`docs/decisions/0002-no-fast-forward-merges.md`](decisions/0002-no-fast-forward-merges.md)
- [`docs/decisions/0008-branch-topology-by-workstream.md`](decisions/0008-branch-topology-by-workstream.md)
- [`docs/decisions/0010-terminology-flow-slice-batch-scope.md`](decisions/0010-terminology-flow-slice-batch-scope.md)
- [`docs/decisions/0014-repository-concern-lines-and-documentation-spine.md`](decisions/0014-repository-concern-lines-and-documentation-spine.md)

Typical branch names:

- `repository-governance`
- `terminology-governance`

### Ingestion And Canonical History

Scope:

- raw acquisition
- canonical observed history
- projections
- graph derivation and materialization
- export-window analysis

Primary docs:

- [`docs/nexus-ingestion-architecture.md`](nexus-ingestion-architecture.md)
- [`docs/nexus-graph-materialization-plan.md`](nexus-graph-materialization-plan.md)
- [`docs/how-to/import-provider-export.md`](how-to/import-provider-export.md)
- [`docs/how-to/rebuild-graph-assertions.md`](how-to/rebuild-graph-assertions.md)
- [`NEXUS-Code/README.md`](../NEXUS-Code/README.md)

Typical branch names:

- `ingestion-foundation`
- `export-window-analysis`
- `graph-materialization`

### LOGOS Intake And Handling

Scope:

- source systems
- intake channels
- signal kinds
- pools
- sensitivity, sharing, sanitization
- access context and rights policy

Primary docs:

- [`docs/logos-source-model-v0.md`](logos-source-model-v0.md)
- [`docs/decisions/0011-restricted-by-default-intake-and-explicit-publication.md`](decisions/0011-restricted-by-default-intake-and-explicit-publication.md)
- [`docs/decisions/0012-pool-based-handling-boundaries.md`](decisions/0012-pool-based-handling-boundaries.md)
- [`docs/decisions/0013-access-context-and-rights-aware-intake.md`](decisions/0013-access-context-and-rights-aware-intake.md)

Typical branch names:

- `logos-intake-foundation`
- `public-safe-publication`
- `rights-aware-intake`

### Interaction And UI

Scope:

- FnHCI
- FnUI
- live capture UX
- later NEXUS user-facing interaction surfaces

Primary docs:

- [`docs/concepts/fnhci.md`](concepts/fnhci.md)
- [`docs/fnui-foundation.md`](fnui-foundation.md)
- [`docs/fnhci-ui-blazor-requirements.md`](fnhci-ui-blazor-requirements.md)
- [`docs/fnhci-conversation-reading-surface.md`](fnhci-conversation-reading-surface.md)
- [`docs/fnhci-ui-web-requirements.md`](fnhci-ui-web-requirements.md)
- [`docs/fnhci-ui-native-host-requirements.md`](fnhci-ui-native-host-requirements.md)
- [`docs/laundrylog-fnui-proving-ground.md`](laundrylog-fnui-proving-ground.md)
- [`docs/fnhci-namespace-map.md`](fnhci-namespace-map.md)
- [`docs/decisions/0015-fnhci-owns-the-top-interaction-namespace.md`](decisions/0015-fnhci-owns-the-top-interaction-namespace.md)

Typical branch names:

- `fnhci-foundation`
- `fnui-foundation`
- `live-capture-foundation`

### External Integrations

Scope:

- Talkyard
- Discord
- GitHub
- forum/wiki/issue-tracker connections
- scrape vs API reconciliation

Primary docs:

- [`docs/logos-source-model-v0.md`](logos-source-model-v0.md)
- [`docs/public-content-publishing-and-talkyard-comments.md`](public-content-publishing-and-talkyard-comments.md)
- future platform-specific integration notes

Typical branch names:

- `talkyard-integration`
- `discord-integration`
- `github-ingestion`

### App And Tool Lines

Scope:

- concrete product and tool lines built on top of NEXUS
- application-domain modeling beyond the platform foundation
- branding/division boundaries that affect branch shape and documentation placement

Examples:

- Cheddar
- CheddarBooks
- LaundryLog within CheddarBooks
- PerDiemLog within CheddarBooks
- FnTools
- FnAPI.Penpot and FnMCP.Penpot within FnTools
- future CheddarBooks support/debugging flows
- other downstream applications

Primary docs:

- [`docs/application-domains/README.md`](application-domains/README.md)
- [`docs/application-domains/cheddar/README.md`](application-domains/cheddar/README.md)
- [`docs/application-domains/cheddarbooks-foundation.md`](application-domains/cheddarbooks-foundation.md)
- [`docs/fntools-foundation.md`](fntools-foundation.md)
- [`docs/decisions/0018-namespace-and-repo-boundaries-by-line.md`](decisions/0018-namespace-and-repo-boundaries-by-line.md)

Typical branch names:

- `cheddar-foundation`
- `cheddarbooks-foundation`
- `cheddarbooks-laundrylog-tool`
- `cheddarbooks-support-model`
- `fntools-foundation`
- `penpot-integration`

## How To Use This Map

For documentation:

- put the decision where its concern line lives
- if a change spans concern lines, place the main decision in the dominant line and cross-link from the others

For branching:

- start from `main` for short focused work
- use a longer-running branch only when a concern line is still actively evolving across multiple merges
- if a subtask clearly belongs under an active concern-line branch, branch from that concern-line branch instead of directly from `main`

For AI and human onboarding:

1. read [`README.md`](../README.md)
2. read this concern-line map
3. read [`docs/glossary.md`](glossary.md)
4. read the decision records and how-to docs for the relevant concern line
5. then inspect source and tests

## Near-Term Implementation Plan

1. Keep [`README.md`](../README.md) and [`NEXUS-Code/README.md`](../NEXUS-Code/README.md) linked to this map.
2. Continue naming branches by workstream or concern line, not by agent/tool.
3. When new decisions are added, sanity-check which concern line they belong to before writing them.
4. When FnHCI/FnUI and external integrations grow, give them their own explicit architecture docs rather than burying them under ingestion notes.
5. Add a grouped index for `docs/decisions/` later if the decision set grows enough to need another navigation layer.
