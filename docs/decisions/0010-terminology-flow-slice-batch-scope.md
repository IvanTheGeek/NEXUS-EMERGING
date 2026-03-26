# 0010: Terminology for Flow, Slice, Batch, and Scope

## Status

Accepted

## Context

NEXUS uses language from Event Modeling, canonical history, graph derivation, and operational CLI tooling.

The word `slice` had started drifting across those contexts, which creates confusion because in Event Modeling a `Slice` has a specific meaning and should not become a vague synonym for any small unit or filtered chunk.

## Decision

NEXUS adopts the following terminology:

- `Slice`
  an Event Modeling unit of change or read
- `Flow`
  an ordered sequence of slices that accomplishes something useful
- `Batch`
  an import-bounded or materialization-bounded contribution unit
- `Scope`
  an explicit filtered view or export boundary

## Consequences

### Event Modeling

Use `Slice` only in the strict Event Modeling sense.

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
