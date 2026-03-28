# Agent README

This file is the AI-first orientation surface for `NEXUS-EMERGING`.

Read it after [`README.md`](../README.md) when you need to recover how this repo is meant to work without depending on prior chat memory.

## Mission

`NEXUS-EMERGING` is the foundation workspace for NEXUS.

Its job is to hold the doctrine, architecture, ingestion direction, protocol surfaces, and durable memory needed to support both humans and AI collaborators.

## Current Role Of This Repo

Right now this repo is primarily:

- the foundation repo for NEXUS
- the ingestion and canonical-history repo
- the place where shared collaboration rules should become durable and inspectable
- the place where upstream doctrine should remain recoverable even when app lines are extracted elsewhere

It is not only a code repo.

It is also an operating-memory surface for the broader NEXUS line.

## Read Next

Use this order unless the task is extremely narrow:

1. [`README.md`](../README.md)
2. [`current-focus.md`](current-focus.md)
3. [`index.md`](index.md)
4. [`glossary.md`](glossary.md)
5. [`cortex-repo-memory-protocol.md`](cortex-repo-memory-protocol.md)
6. the relevant decision notes, architecture notes, how-to docs, and code/tests

High-value next docs in this branch usually include:

- [`nexus-core-conceptual-layers.md`](nexus-core-conceptual-layers.md)
- [`nexus-ingestion-architecture.md`](nexus-ingestion-architecture.md)
- [`nexus-graph-materialization-plan.md`](nexus-graph-materialization-plan.md)
- [`nexus-ontology-imprint-alignment.md`](nexus-ontology-imprint-alignment.md)
- [`logos-source-model-v0.md`](logos-source-model-v0.md)

## Memory Rules

This repo distinguishes between:

- scratch working memory
- durable project memory
- canonical historical record
- derived views

Use [`cortex-repo-memory-protocol.md`](cortex-repo-memory-protocol.md) for the full practical rule set.

Short version:

- do not leave critical learnings only in chat
- do not silently turn scratch into doctrine
- do not rewrite canonical history to simplify the story
- do keep durable docs current when a learning should guide future work

## Contribution Expectations

When meaningful work changes behavior, terminology, architecture, or repo workflow:

- update the relevant durable docs
- update tests when code behavior changes
- update runbooks and help surfaces when public commands change
- add durable memory for discoveries that will matter later

When work is docs-only or tests are not applicable, say so explicitly.

## How To Distinguish The Main Memory Surfaces

### Scratch

Use for temporary, session-local working notes and handoff fragments.

Scratch is operational, not authoritative.

### Durable Docs

Use for:

- doctrine
- glossary terms
- decisions
- architecture notes
- requirements
- current-focus views
- context packs

If it should help the next collaborator orient correctly, it likely belongs here.

### Canonical History

Use for append-only or historically traceable records such as:

- canonical event history
- import manifests
- commit history
- checkpoint records

Canonical history should be corrected additively, not rewritten for convenience.

### Derived Views

Use for summaries, projections, current-focus notes, and read-optimized bundles.

Derived views should remain clearly secondary to stronger sources.

## If You Are Unsure

Prefer:

- adding a small durable note
- linking to the stronger source
- preserving traceability

over:

- assuming the chat transcript will be enough later
- compressing away history that may matter

## Related

- [`current-focus.md`](current-focus.md)
- [`index.md`](index.md)
- [`cortex-repo-memory-protocol.md`](cortex-repo-memory-protocol.md)
- [`context-packs/README.md`](context-packs/README.md)
- [`session-handoffs/README.md`](session-handoffs/README.md)
