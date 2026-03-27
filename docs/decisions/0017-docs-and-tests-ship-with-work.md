# 0017. Docs And Tests Ship With Work

## Status

Accepted

## Context

NEXUS is being built in a way that future humans and future AI agents must be able to recover intent, behavior, and boundaries without relying on fragile chat memory alone.

That means code by itself is not enough.

In practice, most meaningful work here changes at least one of these:

- behavior
- workflow
- public command or API surface
- terminology
- architecture or branch/concern placement

When those changes land without the matching docs or tests, the repository loses too much of its recoverable meaning.

## Decision

Treat docs and tests as part of the work, not as optional cleanup.

Working rule:

- update the relevant docs when behavior, structure, terminology, workflow, or architecture changes
- update or add tests when code behavior changes
- update CLI help, runbooks, and xmldoc when public command or API surfaces change
- if a change is docs-only or tests are not applicable, say that explicitly rather than leaving the omission ambiguous

## Consequences

This means:

- documentation updates are expected as part of normal implementation
- tests are expected as part of normal implementation
- docs-only changes do not require fake tests
- behavior-changing code without tests should be treated as incomplete unless there is a clearly stated reason
- user-facing CLI additions should normally bring:
  - command help
  - a runbook
  - tests
- public API additions should normally bring:
  - source xmldoc
  - tests where behavior exists

## Related

- [`0003-testing-stack-and-library-onboarding.md`](0003-testing-stack-and-library-onboarding.md)
- [`0014-repository-concern-lines-and-documentation-spine.md`](0014-repository-concern-lines-and-documentation-spine.md)
- [`../fsharp-documentation-convention.md`](../fsharp-documentation-convention.md)
- [`../how-to/run-tests.md`](../how-to/run-tests.md)
