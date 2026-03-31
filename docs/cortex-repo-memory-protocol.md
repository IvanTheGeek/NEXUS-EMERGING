# CORTEX Repo Memory Protocol

This document explains how NEXUS treats the repository as shared operating memory for humans and AI agents.

It is meant to stay practical.

It should help a collaborator decide:

- where a piece of information belongs
- what should become durable
- what should stay scratch
- what must remain append-only
- what counts as a derived view instead of source truth

## Purpose

NEXUS work is not supposed to depend on one collaborator remembering a prior chat.

The repo is the working memory surface.

That working memory still serves a clear authority structure:

- humans are the final decision makers
- AI agents are supporting collaborators that help execute, recover context, and suggest options

That means:

- critical learnings should become durable docs
- canonical history should remain traceable
- projections and summaries should stay recognizably derived
- scratch can exist, but it should not silently become doctrine

## The Four Memory Layers

### Scratch

Scratch is temporary working memory.

Examples:

- quick notes for an active thread
- temporary checklists
- local exploratory fragments
- session handoff notes that are only operational

Scratch is useful, but it is not durable truth by default.

### Durable Project Memory

Durable project memory is the human-and-agent-readable layer that should survive beyond one session.

Examples in this branch:

- `README.md`
- [agent-readme.md](agent-readme.md)
- [current-focus.md](current-focus.md)
- [glossary.md](glossary.md)
- architecture notes
- decision notes
- requirements and concept notes

If a learning is likely to matter later, this is usually where it belongs.

### Canonical Historical Record

Canonical history is the append-only trace of what was actually observed or decided in durable form.

Examples in NEXUS:

- canonical append-only event history in `NEXUS-EventStore/`
- Git commit history
- import manifests
- durable checkpoint manifests

Canonical history should not be rewritten just to tell a cleaner story.

Corrections should be additive and traceable.

### Derived Views

Derived views are read-optimized, operational, or presentation-oriented surfaces built from more primary material.

Examples:

- projections
- indexes
- reports
- context packs
- current-focus summaries

Derived views are valuable, but they are not the deepest source truth.

## Where Information Belongs

Use these simple rules:

- if it only matters for the current working moment, keep it in scratch
- if it should guide future work, put it in durable project memory
- if it is a historical observation or append-only system record, preserve it in canonical history
- if it is an optimized summary, onboarding bundle, or operational convenience surface, treat it as a derived view

When in doubt:

- prefer durable docs over chat memory
- prefer additive correction over silent replacement
- prefer traceable derivation over unexplained summaries
- for concrete code, renderer, HTML, CSS, and UI work, inspect the actual local source and current artifacts before changing behavior; do not rely on memory or inferred structure when the code can be read directly

## Agent Startup Protocol

When an AI agent or human collaborator starts work here:

1. read [`repo-overview.md`](repo-overview.md) when browsing the docs site, or the root `README.md` when working directly in the repo
2. read [`docs/agent-readme.md`](agent-readme.md)
3. read [`docs/current-focus.md`](current-focus.md)
4. read [`docs/glossary.md`](glossary.md)
5. read the relevant decision notes, architecture notes, and runbooks
6. inspect the relevant code, tests, and branch context

If the task is narrow, do not read the whole repo.

Read enough to recover:

- mission
- current truths
- vocabulary
- applicable concern line
- applicable operating rules

## Agent Completion Protocol

Before finishing a meaningful piece of work:

- if terminology changed, update the glossary or the relevant terminology doc
- if architecture or behavior changed, update the durable docs that explain it
- if code, renderer, command behavior, or visible behavior changed, add or update tests by default
- if a relevant test was not added or updated, say why explicitly
- if a discovery will matter later, record it durably instead of leaving it only in chat
- if a note is only scratch, leave it in scratch and do not pretend it is doctrine
- if canonical history is affected, append or add correction records rather than rewriting it

If the work is docs-only, say so.

If tests are not applicable, say so.

## Mutation Discipline

NEXUS prefers immutable evolution where practical.

That means:

- preserve what was observed
- add corrections explicitly
- do not hide prior states when they are part of the meaningful history
- do not flatten historical nuance just to make the present view look cleaner

This applies to:

- canonical event history
- import records
- commit history
- durable decisions

It does not mean every scratch note must live forever.

It means canonical and durable layers should evolve additively and traceably.

## Cross-Repo Continuity

When a concrete repo is extracted from NEXUS:

- it should carry enough local durable memory to be understandable on its own
- it may still point back to upstream NEXUS doctrine where appropriate
- it should not rely on chat memory or one agent's private recollection of the extraction

So:

- NEXUS keeps doctrine, protocol, architecture, and foundation memory
- extracted repos keep local mission, current focus, and concrete context packs

## Anti-Patterns

Avoid these:

- critical decisions only living in chat
- scratch and doctrine mixed together without being marked
- mutating canonical history to fit the latest interpretation
- letting generated summaries become authoritative without traceability
- expecting future collaborators to reconstruct the rules from commit messages alone
- treating derived views as if they were the deepest source truth

## Related Surfaces

- [`docs/agent-readme.md`](agent-readme.md)
- [`docs/current-focus.md`](current-focus.md)
- [`docs/index.md`](index.md)
- [`docs/context-packs/README.md`](context-packs/README.md)
- [`docs/session-handoffs/README.md`](session-handoffs/README.md)

## Operating Mantra

Record what matters.

Keep scratch visible as scratch.

Keep history additive.

Keep derived views traceable.
