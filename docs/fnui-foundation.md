# FnUI Foundation

`FnUI` is the concrete visual/UI system line within the broader [`FnHCI`](concepts/fnhci.md) concern line.

The governing decision is:

- [`docs/decisions/0015-fnhci-owns-the-top-interaction-namespace.md`](decisions/0015-fnhci-owns-the-top-interaction-namespace.md)
- [`docs/fnhci-namespace-map.md`](fnhci-namespace-map.md)
- [`docs/fnhci-ui-blazor-requirements.md`](fnhci-ui-blazor-requirements.md)
- [`docs/fnhci-conversation-reading-surface.md`](fnhci-conversation-reading-surface.md)
- [`docs/laundrylog-fnui-proving-ground.md`](laundrylog-fnui-proving-ground.md)

## Purpose

FnUI is the path to a real NEXUS GUI, and also the likely public/package line for the visual system that replaces Bolero while sitting on top of Blazor.

Its job is to provide an operator-facing application layer that can make NEXUS usable without reducing NEXUS to a GUI-first system.

That means FnUI should sit over:

- canonical history and projections
- LOGOS intake and handling state
- graph scopes, batches, and reports
- concept and discovery memory
- later live-capture workflows

## Boundary

FnUI is not:

- the whole of `FnHCI`
- the top-most interaction namespace
- a replacement for CLI workflows
- the source of canonical truth
- equivalent to one concrete rendering stack

FnUI is:

- the concrete visual system line inside `FnHCI`
- the likely public/package identity for the Blazor-based replacement layer
- the visual shell and navigable operator surface for NEXUS
- the place where important operational and conceptual views can coexist
- the place where public-use obligations such as attribution can be made prominent

## Namespace Direction

The target reusable-library namespace direction should nest under the broader `FnHCI` namespace line:

- `FnTools.FnHCI`
- `FnTools.FnHCI.UI`
- `FnTools.FnHCI.UI.Blazor`

while public package naming may still use the narrower `FnUI` line:

- `FnUI`
- `FnUI.Blazor`

The current code namespace now uses `FnTools.FnHCI.*`.

Project and filesystem paths may still temporarily use the older `Nexus.FnHCI` project-path names until the `FnTools` extraction work is ready.

## First Foundation Targets

The first FnUI foundation should define:

1. application shell
   what the top-level frame, navigation, workspace regions, and global actions are
2. host boundary
   what stable UI-facing state and command surfaces exist independently of any renderer
3. primary operational surfaces
   the first views for current ingestion, concept memory, LOGOS handling, graph exploration, and help/about
4. provenance visibility
   how users can move from a view back toward projections, canonical history, and source references
5. attribution visibility
   how rights and attribution obligations become easy to inspect from the GUI

## Likely Early Views

The first practical views likely include:

- current-ingestion dashboard
- provider/import history
- concept notes and LOGOS intake notes
- conversation reading surfaces over canonical projection TOML files
- graph scope and batch exploration
- help/about with attribution obligations

CheddarBooks LaundryLog is currently the clearest first proving ground for this line because it is small, mobile-friendly, event-heavy, and pressures offline and convergence concerns early.

## Relationship To Other Concern Lines

- ingestion-and-canonical-history
  FnUI should present this clearly, not redefine it
- logos-intake-and-handling
  FnUI should surface handling, rights, and pool boundaries prominently
- external-integrations
  FnUI will later host Talkyard, Discord, GitHub, and similar intake/output workflows
- application-domains
  downstream apps may build on the same FnUI substrate, but FnUI should stay platform-level first

## Near-Term Questions

- What is the smallest stable host boundary that lets us support CLI, GUI, and later live capture coherently?
- Which current CLI reports should become the first GUI views?
- What should the first navigation model be?
- Which runtime candidates should be evaluated after the host boundary is clearer?

The first concrete runtime requirements for this line now live in [`docs/fnhci-ui-blazor-requirements.md`](fnhci-ui-blazor-requirements.md).

The first explicit reading-surface requirement now lives in [`docs/fnhci-conversation-reading-surface.md`](fnhci-conversation-reading-surface.md).

## Current Branch

The current foundation branch for this concern line is:

- `fnui-foundation`
