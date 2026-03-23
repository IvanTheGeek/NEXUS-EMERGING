# NEXUS Ingestion Architecture v0

The first system is the acquisition and provenance foundation for NEXUS, not the final NEXUS graph itself.

The near-term job is to preserve source truth, normalize it into an append-only canonical history, and keep enough structure that higher layers can evolve later.

## Local System Boundaries

Use one workspace for now, but keep three explicit boundaries:

- `NEXUS-Code/`
  F# importer, provider adapters, serialization, CLI, tests, later GUI/tools
- `NEXUS-Objects/`
  immutable provider zips, extracted working snapshots, attachments, audio, manually added artifacts
- `NEXUS-EventStore/`
  Git-backed append-only canonical events, import manifests, projections, and later graph-derived views

## Core Rules

- provider exports are acquisition inputs, not absolute truth
- the original provider zip is preserved unchanged
- extracted working files are derived convenience data
- canonical history is append-only
- rolling-window absence never deletes prior facts
- referenced-but-missing artifacts are valid unresolved references
- later manual artifact capture appends new facts and does not rewrite history
- reparse from raw source is expected and designed for

## Minimum Stable Base Types

These are the types worth stabilizing early because they are about identity, provenance, and append-only history rather than final ontology:

- `ProviderKind`
- `SourceAcquisitionKind`
- `ImportId` as UUIDv7
- `CanonicalEventId` as UUIDv7
- `ConversationId` as UUIDv7
- `MessageId` as UUIDv7
- `ArtifactId` as UUIDv7
- `TurnId` as optional UUIDv7
- `ProviderRef`
- `RawObjectRef`
- `ContentHash`
- `OccurredAt`
- `ObservedAt`
- `ImportedAt`

Semantic taxonomy identifiers are intentionally different:

- `DomainId`, `BoundedContextId`, and `LensId` should be human-readable slugs unless there is a later reason to make them opaque

## Canonical History Layer

This is the stable base to build first.

- `ImportManifest`
  one record per import run with counts, source artifact, window kind, and results
- `CanonicalEventEnvelope`
  stable metadata like IDs, timestamps, provenance, hashes, and source refs
- `CanonicalEventBody`
  discriminated union for event meaning

Initial event kinds:

- `ProviderArtifactReceived`
- `RawSnapshotExtracted`
- `ProviderConversationObserved`
- `ProviderMessageObserved`
- `ProviderMessageRevisionObserved`
- `ArtifactReferenced`
- `ArtifactPayloadCaptured`
- `ImportCompleted`

Canonical imports should also stamp a `NormalizationVersion` so the system can distinguish:

- provider-side changes in the same normalization regime
- NEXUS parser/canonicalizer changes across reparses

Rule:

- same provider object + same normalization version + changed content hash => `ProviderMessageRevisionObserved`
- same provider object + different normalization version => append a new `ProviderMessageObserved`, not a revision

## What `Canonical` Means Here

`Canonical` in this repository is bounded-context-specific.

In the current `Ingestion` and `CanonicalHistory` bounded contexts, `canonical` means:

- the NEXUS-preferred normalized form for observed acquisition history
- one consistent shape across provider-specific inputs like ChatGPT, Claude, and later other sources
- append-only and provenance-preserving
- designed for dedupe, replay, projection, and reinterpretation later

It does not mean:

- the raw provider format
- the final graph model for all of NEXUS
- universal truth independent of domain, bounded context, or lens

So the current canonical layer should be understood as:

- canonical for ingestion
- canonical for append-only observed history
- not yet canonical for the whole NEXUS graph substrate or later domain semantics

That distinction matters because the same preserved raw source may later produce richer or different graph assertions as the NEXUS model evolves, without invalidating the canonical ingestion history that was recorded earlier.

## Artifact Strategy

The provider export is not guaranteed to contain every referenced artifact.

So the canonical model should support:

- artifact referenced in a message
- payload missing at import time
- later manual artifact hydration
- future automated capture as a separate acquisition path

The missing payload should not block import. It is an unresolved reference until a later capture event is appended.

## Recommended v0 Scope

Build the first version around:

- preserving provider zips
- extracting working raw snapshots
- importing conversations and messages
- recording artifact references, including unresolved ones
- supporting later manual artifact hydration
- keeping the graph layer intentionally thin and reparsable
