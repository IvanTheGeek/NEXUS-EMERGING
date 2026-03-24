# 0004 Explicit Heavyweight Graph Rebuilds

## Status

Accepted

## Context

The first real graph rebuilds over the working NEXUS event store showed a clear split:

- canonical imports are incremental and relatively cheap
- full graph rebuild from canonical history is much more expensive

On March 24, 2026:

- a full rebuild over `17,820` canonical event files and `89,254` graph assertions took `01:20:31`
- an additive ChatGPT canonical import that appended `515` new events completed in about `00:00:09.44` end to end

This means the current graph rebuild path is correctness-preserving but too expensive to be treated as the normal day-to-day refresh loop.

## Decision

Full graph rebuild from canonical history is an explicit heavyweight operation.

It remains necessary because it is:

- the correctness fallback
- the rebuildability proof for the derived graph layer
- the protection against treating a mutable cache as source truth

But it is no longer treated as the default operational path after ordinary imports or during ordinary interactive work.

NEXUS should move toward:

- incremental graph materialization
- scoped graph slices
- a secondary graph working layer optimized for practical use

## Consequences

- `rebuild-graph-assertions` should be documented as a heavyweight rewrite of the full derived graph layer
- future UX may require explicit confirmation or a deliberate override for large real stores
- the current durable TOML graph-assertion layer remains valid and important
- interactive graph work should increasingly rely on scoped slices and a secondary working layer rather than full rebuilds
- import workflows should eventually be able to feed incremental graph materialization directly

## Notes

This decision does not demote correctness.

It preserves full rebuilds precisely because correctness matters, while recognizing that practical human workflows need a more efficient path for day-to-day use.
