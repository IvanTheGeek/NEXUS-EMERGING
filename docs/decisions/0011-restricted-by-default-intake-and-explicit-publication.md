# 0011: Restricted-By-Default Intake and Explicit Publication

## Status

Accepted

## Context

NEXUS will ingest many kinds of source material:

- personal AI chats
- provider exports
- customer support threads
- bug reports
- deployed-app feedback
- confidential customer datasets used for debugging or repair

Importing data into NEXUS does not automatically make that data safe to share, publish, or embed into public-facing derived artifacts.

## Decision

NEXUS adopts these rules:

1. import permission is not publication permission
2. raw intake defaults to restricted handling
3. canonical or derived does not imply public
4. sanitization is an explicit derived step
5. public-safe outputs come only from explicitly approved derivatives

## Handling Dimensions

Early LOGOS intake policy must track, at minimum:

- sensitivity
- sharing scope
- sanitization status
- retention class

These values are explicit allowlists, not free-form text.

## Consequences

### Raw Intake

Raw provider artifacts, customer datasets, personal chats, and similar acquisition inputs should be treated as restricted unless explicitly classified otherwise.

### Canonical History

Canonical history remains provenance-preserving source truth for ingestion, but it is not automatically share-safe.

### Derived Layers

Projections, graph assertions, snapshots, exports, embeddings, and indexes may all still leak sensitive information if their inputs are sensitive.

### Publication

Publication or wider sharing must be explicit.

Derived artifacts intended for public or wider team use should be:

- redacted
- anonymized
- or otherwise approved-for-sharing

with provenance back to the underlying intake.

### Future Retrieval

Vector or embedding stores are derived artifacts too.

They must follow the same sensitivity and sharing rules as other derived layers.

## Notes

This decision protects the distinction between:

- what NEXUS may ingest
- what NEXUS may retain
- what NEXUS may share
- what NEXUS may publish
