# NEXUS Core Conceptual Layers v0

NEXUS is grounded in the idea that there is one underlying connected reality, while different people and purposes need different ways to localize, name, and work with that reality.

The system should therefore separate source truth, interpreted structure, semantic boundaries, and viewpoint.

## 1. Observed History

This is the acquisition and provenance layer.

It records what NEXUS actually encountered from:

- provider exports
- manual artifact additions
- later API capture
- later browser or session capture

This layer is append-only and epistemically conservative. It should say what was observed, referenced, captured, revised, or imported, without pretending to know more than the source provides.

Example event family:

- `ProviderArtifactReceived`
- `RawSnapshotExtracted`
- `ProviderConversationObserved`
- `ProviderMessageObserved`
- `ProviderMessageRevisionObserved`
- `ArtifactReferenced`
- `ArtifactPayloadCaptured`
- `ImportCompleted`

Purpose:

- preserve evidence
- preserve provenance
- support reparsing
- avoid premature ontology decisions

## 2. Graph Substrate

This is the normalized structural layer derived from observed history.

It represents:

- nodes
- edges
- claims or assertions
- provenance back to observed history

The graph substrate is the shared underlying reality inside NEXUS. It is where relationships can accumulate across providers, imports, artifacts, and later interpretations.

Important rule:

- not everything must be stored as graph first
- observed history remains the foundation
- graph structure is derived from that foundation

## 3. Domain

A domain says what area of reality or work a part of the graph concerns.

Examples:

- `Ingestion`
- `KnowledgeManagement`
- `SoftwareDevelopment`
- `UX`
- `BusinessOperations`
- `PersonalSystems`

A domain is broader than a bounded context. It helps organize major areas of meaning, but does not by itself define exact language or rules.

## 4. Bounded Context

A bounded context defines a specific meaning boundary within a domain.

It determines:

- vocabulary
- rules
- responsibilities
- interpretation discipline

Examples within `SoftwareDevelopment`:

- `EventModeling`
- `Requirements`
- `Implementation`
- `Architecture`

Examples near the foundation:

- `Ingestion`
- `CanonicalHistory`
- `ArtifactReconciliation`

Guidance:

- use `BoundedContext`, not bare `Context`, in the model wherever possible

## 5. Lens

A lens is a way of viewing or working with the underlying graph for a purpose.

A lens does not change the truth underneath. It:

- aliases vocabulary
- groups related structures
- filters what is relevant
- emphasizes certain relationships
- supports a particular user perspective or task

Examples:

- `DeveloperLens`
- `BusinessLens`
- `UXLens`
- `PersonalKnowledgeLens`
- `EventModelingLens`

## Relationship Between the Layers

- `ObservedHistory` records evidence.
- `GraphSubstrate` expresses normalized structure.
- `Domain` says what part of reality or work this concerns.
- `BoundedContext` says what meanings and rules apply.
- `Lens` says how we are looking at it right now.

## Initial v0 Guidance

For the first importer foundation:

- implement `ObservedHistory` first
- keep `GraphSubstrate` thin but planned
- introduce `Domain`, `BoundedContext`, and `Lens` conceptually now
- avoid overcommitting the graph ontology too early
- let richer NEXUS semantics emerge through later interpretation and reparsing

## Suggested Minimal Type Concepts

- `DomainId`
- `BoundedContextId`
- `LensId`
- `NodeId`
- `EdgeId`
- `FactId`
- `GraphAssertion`
- `FactProvenance`

## Working Principle

NEXUS should preserve one evolving underlying reality while allowing many bounded meanings and many valid lenses over that same reality.

The foundation should therefore favor provenance first, structure second, and interpretation as an explicit later layer rather than an implicit assumption baked into ingestion.

## Related Direction

For the current longer-term direction from semantic substrate toward specs and running artifacts, see [Graph, Spec, And Running Artifact Direction](graph-spec-artifact-direction.md).
