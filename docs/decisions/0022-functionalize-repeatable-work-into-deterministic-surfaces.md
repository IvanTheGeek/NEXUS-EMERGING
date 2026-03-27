# 0022. Functionalize Repeatable Work Into Deterministic Surfaces

## Status

Accepted

## Context

NEXUS is being developed with help from multiple AI systems and humans.

That creates a real risk:

- useful work patterns can remain trapped inside the habits or capabilities of one particular AI
- repeated tasks can stay ad hoc instead of becoming inspectable system behavior
- later collaborators may get different results because they are relying on model improvisation instead of stable reviewed surfaces

NEXUS is explicitly trying to move in the opposite direction.

When a task pattern becomes important and repeatable, the goal is to turn it into something more concrete:

- a function
- a tool command
- a script
- a workflow
- a schema
- a deterministic transformation surface

That keeps the work reviewable, teachable, and improvable over time.

This is part of the emerging direction behind `FORGE`, and it also fits interaction-heavy tool lines such as Penpot-related work.

## Decision

Prefer functionalizing important repeated work into deterministic and reviewable surfaces instead of leaving that work dependent on the capabilities of a particular AI model.

Working rule:

- let AI help discover and prototype the work first
- once the pattern proves useful and repeatable, capture it in a stable surface
- prefer surfaces that can be inspected, reviewed, tested, and improved by later collaborators
- treat ad hoc AI execution as a starting point, not the desired long-term endpoint, when the task is important enough to recur

## Examples

Examples of the kinds of surfaces this points toward:

- CLI commands
- importer adapters
- repo bootstrap workflows
- repeatable extraction or publishing scripts
- reviewed transformation code
- deterministic schema-driven conversions
- compiler-like generation behavior when the rules are stable enough

Examples of areas this should influence:

- FORGE
- Penpot interaction and integration work
- cross-repo bootstrap and maintenance workflows
- ingestion transforms that begin as exploratory AI work and later become stable pipelines

## Consequences

Positive:

- future humans and AI systems can use the same reviewed surfaces
- behavior becomes less dependent on a specific model's strengths or quirks
- repeated work becomes easier to evaluate, compare, and improve
- deterministic and testable behavior grows over time

Tradeoffs:

- some exploratory speed is traded for formalization effort
- collaborators must decide when a pattern is mature enough to encode
- not every useful one-off should become a framework too early

## Related

- [`0005-explicit-allowlists-over-catchalls.md`](0005-explicit-allowlists-over-catchalls.md)
- [`0017-docs-and-tests-ship-with-work.md`](0017-docs-and-tests-ship-with-work.md)
- [`0021-important-discoveries-become-durable-repo-memory.md`](0021-important-discoveries-become-durable-repo-memory.md)
- [`../collaboration-protocol.md`](../collaboration-protocol.md)
- [`../forge-foundation.md`](../forge-foundation.md)
