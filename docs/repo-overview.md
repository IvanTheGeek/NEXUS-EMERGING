---
title: Repo Overview
---

# Repo Overview

This page is a docs-site projection of the repository-level orientation for `NEXUS-EMERGING`.

Use it when browsing the rendered docs site.

When working directly in the repo, the stronger onboarding surface is still the root `README.md`.

## Workspace Boundaries

This workspace intentionally separates three concerns across one local repo plus one sibling data repo:

- `NEXUS-Code/`
  F# code, adapters, importer workflow, tests, and later GUI and tools
- `NEXUS-Objects/`
  provider zips, extracted raw payloads, attachments, and manually added artifacts
- sibling repo `../NEXUS-EventStore/`
  append-only canonical events, manifests, projections, graph assertions, working batches, and derived local indexes

## Documentation Spine

Core project memory lives in the repo so humans and AI agents can recover intent quickly:

- [`index.md`](index.md)
- [`agent-readme.md`](agent-readme.md)
- [`current-focus.md`](current-focus.md)
- [`glossary.md`](glossary.md)
- [`nexus-core-conceptual-layers.md`](nexus-core-conceptual-layers.md)
- [`nexus-ingestion-architecture.md`](nexus-ingestion-architecture.md)
- [`logos-source-model-v0.md`](logos-source-model-v0.md)
- [`how-to/README.md`](how-to/README.md)
- [`decisions/0001-observed-history-first.md`](decisions/0001-observed-history-first.md)

## Current Status

The project is past pure scaffolding and into first working ingestion.

Already established:

- Git is the durable history mechanism for canonical event history.
- Conversations are domain entities and streams, not Git branches.
- Provider exports are acquisition inputs, not absolute truth.
- Raw artifacts must be preserved.
- Canonical history should prefer `Observed` language at the ingestion layer.
- A first working CLI importer exists for ChatGPT and Claude full-export zips.
- Canonical events and import manifests can be written into the sibling `NEXUS-EventStore` repo.
- A first concept-note curation workflow exists for promoting conversation material into durable repo memory.

Not yet established:

- the final graph ontology
- the final domain taxonomy
- the final live capture workflow
- the final storage split into separate deployed systems

## Git Workflow

For now, `main` is the steady-state branch.

- start from `main`
- use short-lived side branches only when they materially help
- merge accepted work back quickly
- retire the extra branch and worktree once that need ends

For the fuller operating context, continue with [`agent-readme.md`](agent-readme.md) and [`current-focus.md`](current-focus.md).
