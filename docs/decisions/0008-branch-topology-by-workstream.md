# 0008 Branch Topology By Workstream

## Status

Accepted

## Context

NEXUS uses Git history as durable project memory.

That means branch history is not disposable noise. It is part of the record of:

- what line of work was attempted
- where a stream of work started
- what was merged when
- what experiments or implementations were grouped together

So a single forever-branch for all work is too flat. It keeps commit history, but it does not show the branching and merging shape that helps later understanding.

## Decision

Use branch topology to reflect workstreams.

Rules:

- `main` is accepted truth.
- merge accepted branches into `main` with `--no-ff`.
- do not rely on fast-forward merges for accepted work.
- prefer focused topic branches for coherent slices of work.
- use longer-lived epic branches only when a stream truly spans multiple merges over time.
- when a topic branch is complete, merge it and then delete it.
- when a branch represents an ongoing stream, keep it alive across multiple merges until that stream is actually done.
- tag important milestones before or at merge points when the milestone itself should remain easy to find later.

## Working Shape

Preferred branch names should describe the workstream itself.

Examples:

- `export-window-analysis`
  short-lived, one coherent slice, merged and usually deleted
- `logos-intake-foundation`
  a longer-running stream when the work genuinely spans multiple merges
- `graph-lens-spike`
  exploratory or experimental work that may or may not graduate

Avoid agent-qualified prefixes unless they add real meaning.

The branch graph should primarily describe the work, not which tool or agent happened to perform it.

## Initial Application

The bootstrap branch:

- `codex/nexus-ingestion-foundation`

is treated as the first milestone branch for the ingestion foundation.

It should be merged into `main` with `--no-ff` and tagged as a milestone so the bootstrap line remains easy to identify in the graph.

## Consequences

Positive:

- Git graph becomes readable as actual work topology.
- Merge points show when a line of work became accepted truth.
- Branches remain visible in history even after deletion because `--no-ff` merge commits preserve the shape.
- Milestone tags make major waypoints easy to recover.

Tradeoff:

- Branch planning becomes a deliberate part of work.
- Slightly more Git overhead is accepted in exchange for a more meaningful durable history.
