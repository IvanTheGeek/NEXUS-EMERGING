# Glossary

## Artifact

A file or binary-like object related to a conversation or message. An artifact may be present in a provider export, referenced without payload, or added later through manual capture.

## Bounded Context

A semantic boundary that defines vocabulary, rules, and responsibility for a part of the system.

## Canonical History

The append-only normalized event history maintained by NEXUS. It is derived from acquisition inputs and intended to be durable and reparsable.

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
