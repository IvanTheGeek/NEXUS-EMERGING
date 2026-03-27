# 0023. Use UTC For Durable Time And Localize In Views

## Status

Accepted

## Context

NEXUS projects increasingly deal with:

- event history
- imported observations
- cross-device activity
- future cross-repo and cross-system analysis
- user-facing views that may be rendered in different local contexts

If durable time is stored in local time by default, several problems follow:

- timestamps become harder to compare across systems
- offsets can be lost or inconsistently applied
- event ordering and analysis become harder to reason about
- view concerns leak into the durable model

NEXUS wants a clearer separation:

- durable/model time should be stable and comparable
- views should be responsible for human-friendly localization

## Decision

Default to UTC for durable time in NEXUS projects.

Working rule:

- store durable timestamps in UTC
- name time fields clearly when explicitness helps, for example `recorded_at_utc`
- convert or localize time in views, reports, and other human-facing surfaces
- do not let view-local time formatting define the durable model

## Consequences

Positive:

- durable timestamps are easier to compare and analyze
- cross-device and cross-system ordering is simpler
- localization concerns stay in the view layer
- event models stay more deterministic

Tradeoffs:

- human-facing displays need an explicit localization step
- some examples may need both UTC storage form and user-local rendered form to stay understandable

## Related

- [`0005-explicit-allowlists-over-catchalls.md`](0005-explicit-allowlists-over-catchalls.md)
- [`0017-docs-and-tests-ship-with-work.md`](0017-docs-and-tests-ship-with-work.md)
- [`../collaboration-protocol.md`](../collaboration-protocol.md)
- [`../repository-concern-lines.md`](../repository-concern-lines.md)
