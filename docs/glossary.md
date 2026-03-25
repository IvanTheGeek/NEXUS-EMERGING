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

A durable Markdown seed note under `docs/logos-intake/` that records explicit source-system, intake-channel, signal-kind, and locator metadata for an intake signal before full ingestion exists for that source type.

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

## Provider Artifact

An original source object received from a provider, such as a ChatGPT, Claude, or Grok export zip.

## Raw Object

A preserved source or derived extraction stored in the object layer.

## Overlap Reconciliation

The explicit, traceable, and reversible process of linking multiple acquisition sources that may describe the same underlying interaction.

In NEXUS, overlap is preserved separately first and reconciled later by explicit logic rather than silently collapsed during import.

## Signal Kind

A stable classification for the kind of knowledge-bearing signal represented in LOGOS, such as conversation, message, feedback, bug report, or support question.

## Source System

A stable classification for the originating system or surface from which a LOGOS signal came, such as ChatGPT, Claude, Grok, Codex, a forum, or an app feedback surface.
