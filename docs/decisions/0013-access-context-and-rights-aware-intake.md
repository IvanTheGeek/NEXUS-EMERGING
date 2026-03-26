# 0013: Access-Context And Rights-Aware Intake

## Status

Accepted

## Context

NEXUS will ingest material from sources whose technical visibility and legal reuse boundaries are not the same thing.

Examples:

- a forum thread may be visible differently to an anonymous reader, a logged-in user, and an admin
- a Discord API or bot may expose data under explicit developer-contract limits
- a source may allow personal/private training or analysis without allowing public redistribution
- CC-BY and CC-BY-SA material may be reusable, but only if attribution obligations are carried forward

So NEXUS needs to model more than source-system and handling metadata.

It also needs to model:

- which concrete source instance was observed
- under which access context it was observed
- by which acquisition kind it was captured
- which rights policy governs reuse
- whether attribution must be surfaced later in public UX

## Decision

LOGOS intake adopts explicit access and rights metadata:

- `SourceInstanceId`
- `AccessContextId`
- `AcquisitionKindId`
- `RightsPolicyId`
- optional `AttributionReference`

Manual LOGOS intake notes now persist that metadata from entry time.

Restricted defaults remain conservative:

- `access_context = owner`
- `acquisition_kind = manual-note`
- `rights_policy = review-required`

Public-facing use now requires both:

- a handling-safe boundary
- a rights-safe boundary

In current code, public-safe/public export requires:

- `sanitization_status = approved-for-sharing`
- `sensitivity = public`
- `sharing_scope = public`
- a `rights_policy` that explicitly allows public distribution
- an `attribution_reference` when the rights policy requires attribution

## Consequences

### Technical Access Is Not Publication Permission

NEXUS keeps the rule explicit:

- public visibility does not automatically imply reuse permission
- API access does not automatically imply redistribution permission
- private/personal training permission does not automatically imply public distribution permission

### Personal And Public Use Can Diverge

Material classified as `personal-training-only` may remain useful in private pools or private training contexts, while still being blocked from public-safe export.

### Attribution Becomes A First-Class Output Concern

Public export manifests now surface attribution requirements so later UI/help/about surfaces can expose them prominently instead of relying on memory or ad hoc notes.

### Future Source Adapters Should Carry This Metadata Early

Forum, wiki, issue-tracker, Discord, Talkyard, and similar adapters should preserve access and rights metadata from the moment material enters NEXUS.
