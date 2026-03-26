# FnUI Foundation

`FnUI` is the visual application lens within the broader [`FnHCI`](concepts/fnhci.md) concern line.

The governing decision is:

- [`docs/decisions/0015-fnui-is-the-visual-application-lens.md`](decisions/0015-fnui-is-the-visual-application-lens.md)

## Purpose

FnUI is the path to a real NEXUS GUI.

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
- a replacement for CLI workflows
- the source of canonical truth
- equivalent to one concrete rendering stack

FnUI is:

- the visual shell
- the navigable operator surface
- the place where important operational and conceptual views can coexist
- the place where public-use obligations such as attribution can be made prominent

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
- graph scope and batch exploration
- help/about with attribution obligations

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

## Current Branch

The current foundation branch for this concern line is:

- `fnui-foundation`
