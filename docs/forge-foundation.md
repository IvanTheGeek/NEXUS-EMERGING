# FORGE Foundation

`FORGE` is the NEXUS line concerned with turning repeated useful work into more deterministic, reviewable, and teachable system behavior.

The short version:

- AI can help discover and prototype the work
- NEXUS should then functionalize the important repeated parts
- the resulting surfaces should be inspectable by humans and AI alike

## Why This Matters

Without this step, too much of the system would depend on:

- one model's habits
- one collaborator's memory
- one-off prompt craftsmanship

That is not a strong enough foundation for NEXUS.

FORGE is the direction that says:

- move useful repeated work into functions, tools, workflows, and schemas
- make the behavior reviewable
- make the behavior testable where practical
- let later systems use the same surfaces instead of rediscovering them from scratch

## What Counts As A FORGE Surface

Examples include:

- stable CLI commands
- reviewed import and export transforms
- deterministic generation or conversion workflows
- bootstrap and maintenance scripts
- schema-guided production paths
- compiler-like behavior when the rules are explicit enough
- Event Modeling tooling that turns repeated modeling work into inspectable surfaces instead of leaving it in one-off diagrams or prompts

## Relationship To AI

FORGE is not anti-AI.

It is the step after AI-assisted discovery.

The goal is that future work depends less on the capabilities of a particular AI and more on stable shared surfaces that any collaborator can inspect and improve.

## Relationship To Other Lines

- `NEXUS` holds the doctrine and system direction
- `FORGE` is one part of that NEXUS foundation direction
- `FnTools` may host reusable tools that embody FORGE ideas
- downstream apps such as `CheddarBooks` should benefit from those surfaces instead of re-solving the same work ad hoc
- Event Modeling tool work is one concrete place where FORGE should move from AI-assisted exploration toward deterministic, reviewable behavior

## Related

- [`decisions/0022-functionalize-repeatable-work-into-deterministic-surfaces.md`](decisions/0022-functionalize-repeatable-work-into-deterministic-surfaces.md)
- [`decisions/0024-build-our-own-event-modeling-tool-and-use-penpot-transitionally.md`](decisions/0024-build-our-own-event-modeling-tool-and-use-penpot-transitionally.md)
- [`event-modeling-tool-foundation.md`](event-modeling-tool-foundation.md)
- [`repository-concern-lines.md`](repository-concern-lines.md)
- [`collaboration-protocol.md`](collaboration-protocol.md)
