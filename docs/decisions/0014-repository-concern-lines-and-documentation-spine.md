# 0014 Repository Concern Lines And Documentation Spine

## Status

Accepted

## Context

NEXUS is no longer one narrow ingestion prototype.

The repository is already carrying multiple kinds of knowledge:

- NEXUS as a concept and ontology
- F# coding and modeling conventions
- documentation conventions and project memory rules
- ingestion architecture and observed-history decisions
- LOGOS intake and handling policy
- graph and materialization concerns
- future UI and interaction work such as FnHCI and FnUI
- future application-specific domains that will build on top

If those concerns stay mixed together informally, later humans and AI collaborators will struggle to answer basic questions such as:

- what is fundamental to all NEXUS work?
- what is a coding convention versus a domain rule?
- what is specific to ingestion versus specific to FnHCI?
- which branch should new work start from?
- where should a new decision or concept note live?

## Decision

Treat the repository as a set of explicit concern lines with a matching documentation spine.

The broad concern lines are:

1. `nexus-core`
   The core concept of NEXUS, ontology boundaries, glossary meaning, and cross-project principles.

2. `engineering-conventions`
   F# coding practices, testing conventions, security/style constraints, and documentation rules.

3. `repository-governance`
   Branch topology, decision records, glossary discipline, and how project memory is organized.

4. `ingestion-and-canonical-history`
   Provider imports, raw preservation, canonical observed history, projections, graph derivation, and related indexing/materialization.

5. `logos-intake-and-handling`
   Source systems, intake channels, signal kinds, pools, sensitivity, sharing, sanitization, access context, and rights policy.

6. `interaction-and-ui`
   FnHCI, FnUI, live capture, presentation, navigation, and later user-facing workflow design.

7. `external-integrations`
   Talkyard, Discord, GitHub, forums, issue trackers, feedback surfaces, and similar platform-specific intake/output lines.

8. `application-domains`
   Concrete downstream systems such as LaundryLog or later product/project domains built on top of NEXUS.

## Documentation Spine

The documentation spine should make those concern lines easy to find.

Expected entry points:

- root `README.md`
  top-level orientation, active concern lines, and links into the deeper docs
- `docs/repository-concern-lines.md`
  the operator-facing map of the concern lines, their branch shape, and their canonical homes
- `docs/glossary.md`
  stable shared vocabulary
- `docs/decisions/`
  durable architectural and governance decisions
- `NEXUS-Code/README.md`
  code-focused orientation and current module map

Expected placement rules:

- core ontology or cross-project meaning belongs in `nexus-core` docs/decisions
- coding/testing/documentation rules belong in `engineering-conventions`
- branch/process/doc-memory rules belong in `repository-governance`
- importer/event-store/graph decisions belong in `ingestion-and-canonical-history`
- intake safety/rights/publication rules belong in `logos-intake-and-handling`
- FnHCI/FnUI interaction modeling belongs in `interaction-and-ui`
- platform-specific intake/output work belongs in `external-integrations`
- project-specific modeling belongs in `application-domains`

## Branching Consequences

Branch topology should reflect these concern lines when the workstream is broad enough to span multiple merges.

Rules:

- use `main` as accepted integrated truth
- keep short-lived topic branches for one coherent work item
- allow longer-running branches when they correspond to one of the concern lines above
- when a topic belongs clearly inside an active concern-line branch, it may branch from that concern line instead of directly from `main`
- periodically merge active concern-line branches with `main` or with each other when convergence matters
- keep branch names about the work itself, not the agent performing it

Examples:

- `logos-intake-foundation`
- `fnhci-foundation`
- `talkyard-integration`
- `discord-integration`
- `engineering-conventions`

## Consequences

Positive:

- future collaborators get a clearer map of what is foundational versus contextual
- decisions become easier to place and easier to find later
- branch history can show not only workstreams, but the concern line each workstream belongs to
- humans and AI can onboard faster without guessing where a rule lives

Tradeoff:

- the repository needs a little more explicit structure and discipline
- some workstreams will span concern lines and require deliberate merge/convergence choices
