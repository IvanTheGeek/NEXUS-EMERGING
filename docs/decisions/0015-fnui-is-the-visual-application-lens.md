# 0015 FnUI Is The Visual Application Lens

## Status

Accepted

## Context

NEXUS now has enough ingestion, LOGOS, graph, and curation capability that a real GUI is no longer a distant concern.

At the same time, the broader interaction concept is already forming under `FnHCI`.

Without a boundary, `FnUI` can easily become muddled with:

- all of FnHCI
- one specific rendering framework
- style and theming only
- live capture itself
- application-specific downstream UI work

That would make later implementation choices harder, not easier.

## Decision

Treat `FnUI` as the visual application lens within the broader `FnHCI` concern line.

`FnUI` is responsible for the visual, navigable, operator-facing GUI layer of NEXUS.

It is not:

- the whole of `FnHCI`
- the canonical source of truth for NEXUS state
- tied to one rendering/runtime choice yet
- limited to component styling

## Meaning

`FnHCI` remains the wider interaction concern, spanning:

- visual UI
- CLI and terminal interaction
- API and contract surfaces
- accessibility and cross-surface behavior
- later live capture and user-facing operational workflows

`FnUI` is the visual application-oriented sub-area that should give NEXUS a real GUI for:

- navigation
- panels and views
- command surfaces
- state presentation
- operator workflows
- prominent attribution/help/about surfaces

## Initial Responsibilities

The first `FnUI` foundation should cover:

- application shell boundaries
- navigation and view composition
- a host boundary between NEXUS state/workflows and any one UI runtime
- presentation of current ingestion, LOGOS, graph, and concept-memory surfaces
- future room for live capture workflows without collapsing those workflows into the UI itself

## Deferred Decisions

This decision does not yet choose:

- Avalonia vs web vs hybrid host
- a rendering component model
- a theming system
- concrete accessibility mechanics
- downstream application-specific UI such as LaundryLog

Those should follow after the `FnUI` foundation defines the stable application and host seams.

## Consequences

Positive:

- `FnUI` gets a clear place in the repo and branch topology
- GUI work can begin without pretending the renderer choice is already settled
- later `FnCLI`, `FnAPI`, and accessibility work can remain sibling interaction lenses under `FnHCI`
- future attribution/help/about obligations already have an intentional UI home

Tradeoff:

- some design questions stay intentionally open a little longer
- the first work should focus on boundaries and host seams before visual polish
