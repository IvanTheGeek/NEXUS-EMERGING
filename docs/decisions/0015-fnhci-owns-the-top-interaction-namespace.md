# 0015 FnHCI Owns The Top Interaction Namespace

## Status

Accepted

## Context

NEXUS now has enough ingestion, LOGOS, graph, and curation capability that interaction and GUI work need a stable naming boundary.

The earlier discussions around Bolero, Blazor, `FnUI`, and later `FnHCI` clarified an important distinction:

- `FnHCI` is the broader interaction concern
- `FnUI` is a more concrete visual/UI line inside that broader concern
- the eventual Bolero replacement on top of Blazor is likely part of that `FnUI` line

Without a boundary, these can easily become muddled:

- all of FnHCI
- the broader interaction namespace
- the concrete visual system
- public package naming
- internal code namespace structure

That would make later implementation choices harder, not easier.

## Decision

Treat `FnHCI` as the top interaction concern and top interaction namespace.

Treat `FnUI` as the outward-facing visual/UI system and package line within that broader `FnHCI` space.

The concrete Bolero-replacement system that sits on top of Blazor belongs under the `FnHCI` top namespace, not beside it as a separate peer concern.

## Meaning

`FnHCI` owns the broad interaction meaning, including:

- visual UI
- CLI and terminal interaction
- API and contract surfaces
- accessibility and cross-surface behavior
- later live capture and user-facing workflow design

`FnUI` is narrower.

It represents the visual/UI-specific subsystem and likely the public-facing package/story for the Blazor-based replacement layer.

## Namespace And Package Consequences

`FnHCI` still owns the top interaction namespace line.

The current reusable-library namespace line is now tracked as `FnTools.FnHCI.*`.

See:

- [`0018-namespace-and-repo-boundaries-by-line.md`](0018-namespace-and-repo-boundaries-by-line.md)

So the reusable line should favor `FnTools.FnHCI.*` as the top namespace line.

That means the visual system belongs conceptually under shapes such as:

- `FnTools.FnHCI.UI`
- `FnTools.FnHCI.UI.Blazor`
- later sibling lines such as `FnTools.FnHCI.Cli` or `FnTools.FnHCI.Api`

Project and filesystem paths may temporarily lag behind that namespace while the repo split is being prepared.

Public package naming may still use `FnUI` where that is the clearest outward-facing name.

That means names such as these may make sense later:

- `FnUI`
- `FnUI.Blazor`
- `FnUI.Components`

while still mapping back to the broader `FnHCI` namespace and concern line.

## FnUI Is Not

- the whole of `FnHCI`
- the top-most interaction namespace
- all future interaction modes
- the canonical source of truth for NEXUS state

## Consequences

Positive:

- `FnHCI` keeps the broader interaction namespace clean
- `FnUI` can remain a concrete product/package story without swallowing all interaction concerns
- the Bolero-replacement layer has an intentional place in the namespace hierarchy
- later package branding and internal namespace structure do not need to be identical

Tradeoff:

- some naming now needs an explicit mapping layer
- package names and namespaces may intentionally differ when that gives a better public story
