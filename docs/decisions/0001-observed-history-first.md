# Decision 0001: Observed History First

## Status

Accepted

## Decision

The first implementation layer for NEXUS ingestion will prioritize observed history and provenance before a richer graph ontology.

## Why

- provider exports are partial acquisition inputs, not guaranteed-complete truth
- raw artifacts need to be preserved for reparsing
- canonical history must stay append-only
- the graph ontology is still evolving
- forcing the final graph model too early would make reparsing and reinterpretation harder

## Consequences

- ingestion event names use `Observed` language where appropriate
- canonical events focus first on imports, conversations, messages, revisions, and artifact references
- graph assertions remain a thinner later layer
- richer domain-specific meaning is expected to emerge through later interpretation
