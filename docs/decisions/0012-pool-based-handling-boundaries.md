# 0012: Pool-Based Handling Boundaries

## Status

Accepted

## Context

NEXUS needs to preserve sensitive material while also allowing safer derived material to be reused more broadly.

Handling metadata such as:

- sensitivity
- sharing scope
- sanitization status
- retention class

is necessary, but policy properties alone are not enough for high-risk downstream use.

Some operations should only be possible when data has explicitly crossed a stronger boundary.

## Decision

NEXUS adopts a pool-based handling model:

- `raw` pool
  preserved intake with maximal fidelity and provenance
- `private` pool
  owner or restricted internal use
- `public-safe` pool
  explicitly approved material for public-facing or broadly shared downstream use

The rule is:

- properties describe handling
- types permit operations

So:

- handling policy remains explicit typed metadata
- pool membership is represented by explicit F# boundary types
- future public/export flows should depend on `PublicSafePoolItem<_>` rather than ad hoc property checks

## Consequences

### Raw

Raw intake may exist for preservation and traceability even when it is not fit for wider reuse.

### Private

Private material may still carry sensitive or customer-confidential detail for owner or restricted team use.

### Public-Safe

Public-safe material must cross an explicit promotion boundary.

In the early implementation, public-safe promotion requires:

- `sanitization_status = approved-for-sharing`
- `sensitivity = public`
- `sharing_scope = public`
- a `rights_policy` that explicitly allows public distribution
- an `attribution_reference` when the rights policy requires attribution

### Future Work

Public-facing export, retrieval, embedding, or sharing workflows should accept the public-safe type as input instead of re-validating loose properties everywhere.

## Notes

This keeps the model explicit in two ways:

- the policy remains visible and auditable
- the dangerous operations gain compile-time boundaries

See also:

- `docs/decisions/0013-access-context-and-rights-aware-intake.md`
