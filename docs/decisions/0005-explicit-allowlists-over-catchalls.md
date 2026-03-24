# 0005 Explicit Allowlists Over Catchalls

## Status

Accepted

## Context

NEXUS is aiming for deterministic behavior, strong domain boundaries, and a smaller known attack surface.

That affects both domain modeling and validation style.

A permissive catchall such as:

- a wildcard branch in pattern matching
- or a validation rule that only blocks a few bad characters while implicitly allowing everything else

often hides ambiguity rather than removing it.

This conflicts with the NEXUS direction that invalid states should be hard to represent or impossible to reach.

## Decision

Prefer explicit allowlists over permissive catchalls.

In practice:

- prefer explicit pattern matches over `_` branches when the allowed cases are known
- prefer explicit allowed-character sets over broad "anything except X" validation rules
- prefer deterministic recognized forms over permissive fallback acceptance

This does not forbid every use of `_`.

But it does mean wildcard branches should be treated as a deliberate exception that needs justification, not as the default style.

## Consequences

- domain and validation code should aim for explicit recognized forms
- parser and CLI input handling should prefer allowlisted accepted values where practical
- future hardening work should review wildcard and permissive validation paths through this lens
- the repo will favor determinism, narrower attack surface, and more explicit failure modes over permissive acceptance

## Notes

This aligns with the broader NEXUS preference for making invalid states hard to express and for keeping behavior predictable across both human and AI collaboration.
