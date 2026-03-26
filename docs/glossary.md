# Glossary

## Artifact

A file or binary-like object related to a conversation or message. An artifact may be present in a provider export, referenced without payload, or added later through manual capture.

## Artifact Hydration

The act of attaching a payload to an already known artifact reference by appending an `ArtifactPayloadCaptured` event. Hydration does not rewrite the earlier `ArtifactReferenced` history.

## Bounded Context

A semantic boundary that defines vocabulary, rules, and responsibility for a part of the system.

## Canonical

In NEXUS, `canonical` does not mean universally true or final.

It means:

- the preferred normalized representation inside a specific bounded context
- the form the system chooses so different source inputs can be treated consistently
- a stable working shape for storage, dedupe, projection, and later interpretation

In the current `Ingestion` and `CanonicalHistory` bounded contexts, `canonical` means:

- append-only
- provenance-preserving
- provider-neutral where practical
- reparsable from preserved raw source artifacts

It does not mean:

- raw provider truth
- the final NEXUS graph ontology
- the only valid interpretation for all later domains and lenses

## Canonical History

The append-only normalized event history maintained by NEXUS.

In the current bounded context, it is the canonical store for observed acquisition history. It is derived from acquisition inputs and intended to be durable, provenance-preserving, and reparsable.

## Domain

A broad area of reality or work that a part of the graph concerns, such as `Ingestion` or `SoftwareDevelopment`.

## Flow

An ordered sequence of Event Modeling slices that accomplishes something useful.

## Event

An append-only record in the canonical history that states what was observed, referenced, captured, or completed.

## Fact

A graph-level assertion derived from canonical history and tied back to provenance.

## Graph Substrate

The shared structural layer of nodes, edges, and assertions derived from observed history.

## Imprint

A domain-neutral meaning applied to structure when that structure is understood as the persistent result of causality and a source for later interpretation.

In the current ontology direction, `Imprint` is a role, not a structural primitive.

## Import

A single acquisition run that processes one or more provider artifacts and appends canonical history.

## Intake Channel

A stable classification for the path through which a LOGOS signal entered NEXUS, such as AI conversation, forum thread, email thread, bug report, or app feedback.

## Lens

A perspective over the underlying graph that localizes meaning, naming, grouping, and emphasis for a purpose or audience.

## LOGOS

The NEXUS concept area for intake, refinement, and retention of knowledge-bearing signals.

LOGOS is broader than any one storage or retrieval technology.

## LOGOS Intake Note

A durable Markdown seed note under `docs/logos-intake/<pool>/` that records explicit source-system, intake-channel, signal-kind, locator, and handling metadata for an intake signal before full ingestion exists for that source type.

## LOGOS Sanitized Note

A derived Markdown note under `docs/logos-intake-derived/<pool>/` that preserves source classification and handling-policy provenance while intentionally excluding raw locators and raw copied source text.

## LOGOS Handling Policy

An explicit handling classification for a LOGOS intake signal covering sensitivity, sharing scope, sanitization status, and retention class.

## LOGOS Handling Report

A derived audit view over `docs/logos-intake/` and `docs/logos-intake-derived/` that surfaces how LOGOS notes are currently classified for sensitivity, sharing scope, sanitization status, and retention.

## Entry Pool

The explicit pool path where a LOGOS note enters or lands: `raw`, `private`, or `public-safe`.

## Private Pool

A handling pool for owner or restricted internal use where sensitive detail may still be retained for legitimate work.

## Public-Safe Pool

A handling pool for explicitly approved material that is safe for public-facing or broadly shared downstream use.

## Raw Pool

A handling pool for preserved intake with maximal fidelity and provenance, regardless of whether the material is safe for broader sharing.

## Semantic Role

A meaning classification applied to a node without changing the node's structural identity.

## Normalized Import Snapshot

A per-import derived snapshot written from one parsed provider payload before canonical dedupe.

It captures what that import payload contained at the normalized layer, so imports can be compared with snapshot semantics instead of only additive canonical changes.

## Normalization Version

The version label of the parser/canonicalizer shape that produced a canonical observation.

It exists so NEXUS can distinguish:

- a provider object changing under the same normalization rules
- NEXUS re-observing the same provider object after its own parser or canonicalizer changed

## Observed History

The provenance-first layer that records what NEXUS actually encountered from exports, manual additions, and later capture paths.

## Projection

A rebuildable materialized view derived from canonical history.

## Retention Class

A stable classification for how long or how durably an intake signal should be retained, such as `ephemeral`, `case-bound`, or `durable`.

## Provider Artifact

An original source object received from a provider, such as a ChatGPT, Claude, or Grok export zip.

## Raw Object

A preserved source or derived extraction stored in the object layer.

## Sanitization Status

A stable classification for whether an intake signal or derived artifact is still raw, has been redacted, has been anonymized, or has been approved for wider sharing.

## Scope

An explicit filtered boundary over data or graph material, such as one provider, one conversation, one import batch, or one node neighborhood.

## Sensitivity

A stable classification for how sensitive an intake signal is, such as `personal-private`, `customer-confidential`, `internal-restricted`, or `public`.

## Sharing Scope

A stable classification for who may access or use an intake signal or derivative, such as `owner-only`, `case-team`, `project-team`, or `public`.

## Slice

In NEXUS terminology, `Slice` is reserved for the Event Modeling sense: a unit of change or read.

## Overlap Reconciliation

The explicit, traceable, and reversible process of linking multiple acquisition sources that may describe the same underlying interaction.

In NEXUS, overlap is preserved separately first and reconciled later by explicit logic rather than silently collapsed during import.

## Signal Kind

A stable classification for the kind of knowledge-bearing signal represented in LOGOS, such as conversation, message, feedback, bug report, or support question.

## Source System

A stable classification for the originating system or surface from which a LOGOS signal came, such as ChatGPT, Claude, Grok, Codex, a forum, or an app feedback surface.

## Batch

An import-bounded or materialization-bounded contribution unit, especially in the graph working layer.
