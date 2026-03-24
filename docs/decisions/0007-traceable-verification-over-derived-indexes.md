# 0007 Traceable Verification Over Derived Indexes

## Status

Accepted

## Context

NEXUS now has multiple derived layers over canonical history:

- projections
- durable graph assertions
- graph working slices
- a persisted SQLite working index over graph working slices

These layers exist to make practical work faster.

They do not replace source truth.

As NEXUS grows, some flows will matter more than others.
For ordinary exploration, the index-first path is appropriate.
For important slices, sensitive decisions, or higher-confidence workflows, NEXUS must preserve the ability to verify results against more authoritative layers.

The current architecture already separates:

- raw/object inputs
- canonical append-only history
- derived assertions
- derived working indexes

This decision makes the verification requirement explicit.

## Decision

Derived indexes and projections may accelerate access, but they may not become the final authority.

NEXUS must preserve the ability to verify important slice flows by tracing from derived working results back through authoritative layers.

The intended verification chain is:

1. working index
2. working slice assertions
3. canonical events
4. raw source references
5. provider artifact or preserved source object when needed

This does not mean every workflow must always pay the full verification cost.

It does mean:

- important workflows must be able to do so
- the design must not sever the provenance chain
- future optimization must not erase the verification path

## Working Rule

Fast path and authoritative path are distinct.

### Fast Path

Use derived indexes and derived read models for:

- interactive exploration
- quick slice summaries
- operator-facing reports
- visualization preparation
- normal exploratory graph work

### Authoritative Verification Path

Use deeper verification when confidence matters more than latency, such as:

- validating an important slice before acting on it
- checking whether derived state is stale or inconsistent
- auditing provenance for a concept, note, or claim
- troubleshooting surprising graph results
- later, supporting trust-sensitive automation

## Consequences

- every derived working layer should preserve identifiers and provenance links needed to trace back to canonical history
- indexes and projections should be treated as accelerators, not authorities
- future command design may introduce explicit verification modes instead of assuming one fixed level of confidence
- important NEXUS flows should be able to trade speed for stronger validation intentionally

## Deferred Implementation

This decision does not require immediate implementation of a full verification UX.

Possible future command-level semantics may include modes such as:

- fast
- assertion-verified
- canonical-verified
- raw-verified

Those modes are not yet implemented.

What is being locked in now is the architectural requirement that such verification remains possible.

## Notes

This aligns with the broader NEXUS principle that derived structures should help with practical work without collapsing provenance, truth, and interpretation into one opaque layer.
