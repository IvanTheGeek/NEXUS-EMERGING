# 0018. Namespace And Repo Boundaries By Line

## Status

Accepted

## Context

NEXUS is now carrying several real concern lines at once:

- foundation system and paradigm work
- reusable interaction and tooling libraries
- concrete application and product lines

Keeping all of those under `Nexus.*` indefinitely would blur an important distinction:

- what is foundational doctrine or system work
- what is a reusable library/tool line
- what is a concrete downstream product or app

That distinction matters for:

- namespaces
- repo boundaries
- package boundaries
- human and AI understanding
- dependency direction

## Decision

Use namespaces to express conceptual ownership first, then let repo boundaries follow those lines.

The intended line boundaries are:

- `Nexus.*`
  foundation system and paradigm work
- `FnTools.*`
  reusable technical and interaction/tooling libraries
- `CheddarBooks.*`
  concrete CheddarBooks product and app lines
- `Cheddar.*`
  broader Cheddar umbrella concerns only when that line grows real shared code or publishing surfaces

## Current Boundary Interpretation

### NEXUS

`Nexus.*` is for the foundation system itself.

This includes areas such as:

- LOGOS
- CORTEX
- ATLAS
- FORGE
- ontology, ingestion, doctrine, and project-memory rules

### FnTools

`FnTools.*` is for reusable technical and interaction/tooling libraries.

This includes the intended future home of:

- `FnTools.FnHCI`
- `FnTools.FnHCI.UI`
- `FnTools.FnHCI.UI.Blazor`
- later `FnTools.FnAPI.*`
- later `FnTools.FnMCP.*`

### CheddarBooks

`CheddarBooks.*` is for concrete CheddarBooks products and apps.

This includes:

- `CheddarBooks.LaundryLog`
- `CheddarBooks.LaundryLog.UI`
- later `CheddarBooks.PerDiemLog`
- later broader CheddarBooks application lines

### Cheddar

`Cheddar.*` is not yet the next code split.

For now it remains mostly the umbrella brand and portfolio line above CheddarBooks.

It should become a code repo only when it has real shared assets, runtime code, or publishing surfaces of its own.

## Migration Order

The migration order is intentionally phased:

1. correct concrete app namespaces first
   example: `Nexus.CheddarBooks.*` -> `CheddarBooks.*`
2. keep reusable interaction/tooling scaffolds working while the repo split is prepared
3. extract `FnTools.*` into its own repo when the library surface is ready
4. extract `CheddarBooks.*` into its own repo when the product/app line is ready
5. leave `Nexus.*` as the foundation umbrella repo

## Consequences

Positive:

- dependency direction becomes clearer
- product code no longer looks like a NEXUS subsystem
- reusable library code gets a cleaner future home
- repo splits become easier because namespaces already express the intended ownership

Tradeoffs:

- temporary mismatch may exist while scaffolds are being migrated
- some project names and filesystem paths may lag behind namespaces for a while
- docs must be explicit about whether something is the current scaffold or the target boundary

## Current Implementation Note

The first concrete namespace correction pass should move CheddarBooks app code away from `Nexus.CheddarBooks.*` toward `CheddarBooks.*`.

The reusable FnHCI/FnUI scaffold may remain temporarily under `Nexus.FnHCI.*` inside this repo until the `FnTools` extraction work is ready, but the intended future boundary is `FnTools.FnHCI.*`.

## Related

- [`0014-repository-concern-lines-and-documentation-spine.md`](0014-repository-concern-lines-and-documentation-spine.md)
- [`0015-fnhci-owns-the-top-interaction-namespace.md`](0015-fnhci-owns-the-top-interaction-namespace.md)
- [`../repository-concern-lines.md`](../repository-concern-lines.md)
- [`../application-domains/cheddarbooks-foundation.md`](../application-domains/cheddarbooks-foundation.md)
- [`../fntools-foundation.md`](../fntools-foundation.md)
