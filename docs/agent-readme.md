# Agent README

This file is the AI-first orientation surface for `NEXUS-EMERGING`.

Read it after [`repo-overview.md`](repo-overview.md) when you are browsing the docs site or after the root `README.md` when you are working directly in the repo.

## Mission

`NEXUS-EMERGING` is the foundation workspace for NEXUS.

Its job is to hold the doctrine, architecture, ingestion direction, protocol surfaces, and durable memory needed to support both humans and AI collaborators.

Prime directive:

- humans are the ultimate rulers and decision makers in NEXUS
- AI exists to help execute work, surface options, and suggest strategies, tools, and implementations in service of human intent

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

1. [`repo-overview.md`](repo-overview.md) for the docs-site projection, or the root `README.md` in the repo
2. [`current-focus.md`](current-focus.md)
3. [`index.md`](index.md)
4. [`glossary.md`](glossary.md)
5. [`cortex-repo-memory-protocol.md`](cortex-repo-memory-protocol.md)
6. the relevant decision notes, architecture notes, how-to docs, and code/tests

High-value next docs in this branch usually include:

- [`nexus-core-conceptual-layers.md`](nexus-core-conceptual-layers.md)
- [`fsharp-usage-learning-and-guidance.md`](fsharp-usage-learning-and-guidance.md)
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
- when renderer, UI, HTML, CSS, command behavior, or visible behavior changes, add or update tests by default
- if a relevant test is not added or updated, say why explicitly
- update runbooks and help surfaces when public commands change
- add durable memory for discoveries that will matter later
- when a repeat F# seam or implementation preference becomes clear, record it in the appropriate durable guidance surface instead of leaving it as one-off chat memory
- AI agents may proactively suggest a better-fit tool or language when the concern clearly calls for it, but should keep the reason and tradeoff visible instead of switching silently
- the human's decision is the controlling one once made; later agents should follow it, while still being allowed to respectfully surface a materially better option if one becomes apparent
- for Playwright MCP browser work, do not assume `file://` is a valid target; the MCP browser sandbox blocks `file:` URLs, so serve local artifacts over `http://127.0.0.1/...` (or similar local HTTP) first and use that as the browser target
- when a repo already provides checked-in helper scripts for recurring build, refresh, verification, or test flows, use those scripts as the default path instead of reconstructing the flow ad hoc
- when presenting command/event/view modeling, do not flatten it into a fixed linear triplet such as `COMMAND -> EVENT -> VIEW`
- present it as:
  - command slices produce durable event fact(s)
  - view slices consume prior event fact(s)
  - the consumed event need not come from the immediately previous slice
  - multiple views may consume the same prior event
- when an app-line repo has already corrected this seam multiple times, update the durable docs and AI guidance instead of relying on the next agent to rediscover the same correction from chat

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
