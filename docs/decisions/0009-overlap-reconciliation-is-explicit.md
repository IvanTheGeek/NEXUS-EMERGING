# 0009 Overlap Reconciliation Is Explicit

## Status

Accepted

## Context

NEXUS is already preserving multiple acquisition sources for similar kinds of human and AI interaction:

- provider exports such as ChatGPT, Claude, and Grok
- local Codex session capture
- later, likely API capture
- later, likely forum, email, bug-report, and feedback intake

As these sources grow, some of them will overlap.

Examples:

- a locally captured Codex session may later appear in a provider export or API history
- the same discussion may appear as both a forum thread and a copied email exchange
- user feedback may appear in an app-feedback intake path and also in a support ticket system

That overlap is important, but it should not be collapsed too early.

If NEXUS silently merges overlapping acquisitions during import, it becomes harder to preserve provenance, harder to reason about what was actually captured where, and harder to correct a bad match later.

## Decision

Overlap reconciliation is an explicit, traceable, and reversible concern.

Working rule:

- preserve each acquisition source separately first
- treat possible overlap as a later interpretation or linking concern
- do not silently collapse separate acquisitions into one canonical record only because they look similar

Future reconciliation may use signals such as:

- time-window overlap
- provider or source thread identity
- title similarity
- message similarity
- provenance alignment
- later, richer semantic or embedding-assisted comparison

But the result of reconciliation must remain explicit.

## Consequences

- import remains conservative and provenance-first
- the system can support cross-source overlap checks without losing original acquisition history
- future reconciliation links can be reviewed, revised, or removed
- later live-capture and export-capture paths can coexist without forcing premature identity collapse

## Notes

This aligns with the broader NEXUS preference to preserve observed history first and let stronger interpretation happen as an explicit later layer rather than as an implicit importer side effect.
