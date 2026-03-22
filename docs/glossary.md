# Glossary

## Artifact

A file or binary-like object related to a conversation or message. An artifact may be present in a provider export, referenced without payload, or added later through manual capture.

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

## Import

A single acquisition run that processes one or more provider artifacts and appends canonical history.

## Lens

A perspective over the underlying graph that localizes meaning, naming, grouping, and emphasis for a purpose or audience.

## Observed History

The provenance-first layer that records what NEXUS actually encountered from exports, manual additions, and later capture paths.

## Projection

A rebuildable materialized view derived from canonical history.

## Provider Artifact

An original source object received from a provider, such as a ChatGPT or Claude export zip.

## Raw Object

A preserved source or derived extraction stored in the object layer.
