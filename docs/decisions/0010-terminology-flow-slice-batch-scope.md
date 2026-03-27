# 0010: Terminology for Flow, Slice, Batch, and Scope

## Status

Accepted

## Context

NEXUS uses language from Event Modeling, canonical history, graph derivation, and operational CLI tooling.

The word `slice` had started drifting across those contexts, which creates confusion because in Event Modeling a `Slice` has a specific meaning and should not become a vague synonym for any small unit or filtered chunk.

## Decision

NEXUS adopts the following terminology, with the Event Modeling-specific terms scoped to Event Modeling contexts and lenses:

- `Slice`
  an Event Modeling unit of change or read
- `CommandSlice`
  an Event Modeling slice centered on intent that should lead to one or more durable events
- `ViewSlice`
  an Event Modeling slice centered on what an actor can see and decide from the current business state
- `Flow`
  an ordered sequence of slices that accomplishes something useful
- `View`
  the dataset or structure shown in the Event Modeling business/UI lens behind a `ViewSlice`
- `Batch`
  an import-bounded or materialization-bounded contribution unit
- `Scope`
  an explicit filtered view or export boundary

## Consequences

### Event Modeling

Use `Slice` only in the strict Event Modeling sense.

Within that Event Modeling language:

- prefer `CommandSlice` for the intent-side slice
- prefer `ViewSlice` for the business/read-side slice
- prefer `View` for the dataset/structure behind a `ViewSlice`
- treat `read model` and `ReadSlice` only as reference/search terms when useful, not as the preferred NEXUS wording

These terms do not redefine every NEXUS context or lens. They are the preferred vocabulary when NEXUS is working explicitly in an Event Modeling frame.

### Import and Working Graph Materialization

Use `Batch` for:

- import-local graph contributions
- import-bounded materialization units
- per-import graph working updates

### Reporting and Export

Use `Scope` for:

- filtered Graphviz exports
- provider, conversation, import, or node-bounded views

### CLI and Docs

Prefer `batch` and `scope` in user-facing commands and documentation.

Backward-compatible aliases may remain where earlier commands already used `slice`.

### Internal Code

Internal names may continue using older `slice` identifiers temporarily when renaming would create excessive churn.

Those internal names do not redefine the user-facing terminology.

## Notes

This keeps Event Modeling language precise while allowing NEXUS operational tooling to use words that better match what the system is actually doing.
