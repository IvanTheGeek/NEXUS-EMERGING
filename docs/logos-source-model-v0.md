# LOGOS Source Model v0

LOGOS is the NEXUS concept area for intake, refinement, and retention of knowledge-bearing signals.

This document defines the first small structural model for that area.

## Purpose

The goal of this first pass is not to define the whole LOGOS ontology.

The goal is to name the smallest stable concepts needed to represent where a signal came from and what kind of signal it is, so future intake paths can converge without losing meaning.

This is especially relevant for later sources such as:

- AI conversations
- forum threads
- email threads
- bug reports
- support requests
- deployed-app feedback

## Stable Concepts

The `v0` LOGOS source model introduces:

- `SourceSystemId`
  the stable slug for the originating system or source surface
- `IntakeChannelId`
  the stable slug for the intake channel or path
- `SignalKindId`
  the stable slug for the kind of knowledge-bearing signal
- `LogosLocator`
  a concrete locator back to the originating source item
- `LogosSourceRef`
  the source-system + intake-channel + locator grouping
- `LogosSignal`
  a small semantic envelope for a captured signal
- `LogosHandlingPolicy`
  a small explicit handling envelope covering sensitivity, sharing scope, sanitization status, and retention class
- pool boundary types
  explicit `raw`, `private`, and `public-safe` handling boundaries for downstream use

## Relation To Existing NEXUS Layers

This model does not replace canonical ingestion history.

Instead:

- canonical history still records what was observed or imported
- LOGOS source types classify the source and meaning of knowledge-bearing intake
- handling policy describes the material
- pool boundary types constrain what downstream workflows may do with it
- provider and Codex imports can now carry LOGOS metadata from the moment they enter NEXUS
- later curation, refinement, retrieval, and doctrine can build on both

In other words:

- canonical history answers: what did NEXUS observe?
- LOGOS source modeling answers: what kind of source or signal is this?

## Overlap

LOGOS must expect source overlap.

The same underlying interaction may later be seen through multiple acquisition paths:

- local session capture
- provider export
- API capture
- copied or quoted text in another system

That overlap should be preserved separately first and reconciled explicitly later.

See:

- `docs/decisions/0009-overlap-reconciliation-is-explicit.md`

## v0 Scope

The `v0` scope is intentionally narrow:

- stable source-system identifiers
- stable intake-channel identifiers
- stable signal-kind identifiers
- minimal source references and signal envelopes
- a small durable note workflow for seeding non-chat intake before full ingestion exists
- a first explicit derived-note workflow for redacted, anonymized, or shareable LOGOS derivatives
- a first handling-policy audit report over LOGOS note material
- first explicit pool boundary types for `raw`, `private`, and `public-safe` use
- explicit pool-aware intake and derived note paths under `docs/logos-intake/<pool>/` and `docs/logos-intake-derived/<pool>/`
- provider and Codex imports entering the system with restricted-by-default LOGOS handling metadata

Deferred:

- full LOGOS ontology
- vector or embedding storage
- retrieval ranking
- clustering
- doctrine/refinement workflows
- broader redaction and anonymization workflows beyond the first derived-note path
- cross-source overlap reconciliation implementation

## Design Notes

`v0` LOGOS identifiers use explicit allowlisted slug validation rather than permissive acceptance.

That keeps this layer aligned with the repo rule that stable recognized forms should be narrow and deterministic.

Current concrete source-system allowlist:

- `chatgpt`
- `claude`
- `grok`
- `codex`
- `forum`
- `email`
- `issue-tracker`
- `app-feedback-surface`

Current concrete intake-channel allowlist:

- `ai-conversation`
- `forum-thread`
- `email-thread`
- `bug-report`
- `app-feedback`

Current concrete signal-kind allowlist:

- `conversation`
- `message`
- `bug-report`
- `feedback`
- `support-question`

Current handling-policy allowlists:

- sensitivities:
  - `personal-private`
  - `customer-confidential`
  - `internal-restricted`
  - `public`
- sharing scopes:
  - `owner-only`
  - `case-team`
  - `project-team`
  - `public`
- sanitization statuses:
  - `raw`
  - `redacted`
  - `anonymized`
  - `approved-for-sharing`
- retention classes:
  - `ephemeral`
  - `case-bound`
  - `durable`

Current provider-import baseline:

- provider and Codex imports enter with restricted-by-default handling metadata
- current default handling is:
  - `sensitivity = internal-restricted`
  - `sharing_scope = owner-only`
  - `sanitization_status = raw`
  - `retention_class = durable`
  - `entry_pool = raw`

Current non-chat LOGOS note baseline:

- manual intake notes now enter a declared pool at creation time
- the default entry pool is:
  - `entry_pool = raw`
- derived sanitized notes resolve into:
  - `private` when the resulting policy is still restricted
  - `public-safe` only when the explicit public-safe policy boundary is crossed
