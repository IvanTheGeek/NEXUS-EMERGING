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
- a single local `main` worktree is the normal steady state when no independent concern line is active.
- merge accepted branches into `main` with `--no-ff`.
- do not rely on fast-forward merges for accepted work.
- prefer focused topic branches for coherent work items.
- use longer-lived epic branches only when a stream truly spans multiple merges over time.
- multiple long-running branches may coexist when they represent genuinely different concerns.
- when a topic branch is complete, merge it and then delete it.
- when a branch represents an ongoing stream, keep it alive across multiple merges until that stream is actually done.
- periodically merge active long-running branches with `main` or with each other when convergence matters.
- use linked Git worktrees as a temporary operating tool for active side branches or merge/convergence work, not as evidence that a branch should remain alive indefinitely.
- after a branch is merged and no longer needs an independent cadence, remove any extra worktree for it and delete the remote branch when the shared remote no longer needs that ref.
- tag important milestones before or at merge points when the milestone itself should remain easy to find later.

## Working Shape

Preferred branch names should describe the workstream itself.

Examples:

- `export-window-analysis`
  short-lived, one coherent work item, merged and usually deleted
- `logos-intake-foundation`
  a longer-running stream when the work genuinely spans multiple merges
- `graph-lens-spike`
  exploratory or experimental work that may or may not graduate

Avoid agent-qualified prefixes unless they add real meaning.

The branch graph should primarily describe the work, not which tool or agent happened to perform it.

## Initial Application

The first ingestion-foundation workstream has already been preserved as a milestone:

- merge milestone tag: `ingestion-foundation-v0`

That milestone marks the bootstrap phase without keeping the older agent-qualified branch name as the ongoing naming pattern.

## Consequences

Positive:

- Git graph becomes readable as actual work topology.
- Merge points show when a line of work became accepted truth.
- Branches remain visible in history even after deletion because `--no-ff` merge commits preserve the shape.
- Milestone tags make major waypoints easy to recover.
- The repo can return to a simple `main`-only steady state between independent workstreams instead of accumulating stale branch/worktree clutter.

Tradeoff:

- Branch planning becomes a deliberate part of work.
- Slightly more Git overhead is accepted in exchange for a more meaningful durable history.
- Worktrees and remote branch refs also need intentional cleanup after convergence.
