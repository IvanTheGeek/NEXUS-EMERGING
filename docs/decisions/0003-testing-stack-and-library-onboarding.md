# 0003 Testing Stack And Library Onboarding

## Status

Accepted

## Context

NEXUS is using F# with a strong emphasis on:

- executable intent
- readable domain-oriented structure
- living documentation in the repository
- durable project memory for both humans and AI collaborators

As the codebase moved from scaffolding into working importer behavior, it became important to establish:

- a default testing stack
- how output stability is checked
- how invariants are checked
- how external libraries should be learned and evaluated inside this repo

This came into focus while integrating Expecto, property-based testing, and snapshot testing.

## Decision

The current default testing stack is:

- `Expecto` for the primary test runner and standard test composition
- `Expecto.FsCheck` for property and invariant testing
- `Verify.Expecto` for snapshot/golden-file verification of generated artifacts

The default library-onboarding approach is:

1. docs spine first
2. examples and tests next
3. source after that
4. XML docs, reflection, and low-level API inspection only as supporting detail when needed

Short rule of thumb:

- TOC docs teach meaning
- examples teach usage
- source teaches truth
- XML docs teach exact API details

## Consequences

For testing:

- behavior-driven and workflow tests remain centered in Expecto
- invariant tests should prefer `Expecto.FsCheck` over ad hoc property harnesses
- generated canonical artifacts should be protected with snapshot tests when output shape matters
- direct browser inspection is useful for understanding visible behavior, but it is not by itself a substitute for a repeatable browser automation harness when path behavior must be asserted deterministically

For documentation and onboarding:

- repo docs are the primary onboarding surface for humans and AI agents
- examples and tests should remain readable because they act as working documentation
- XML docs and reflection are still useful, but they are not the preferred first entry point for understanding a library

## Notes

At the time of this decision, `Expecto.FsCheck 10.2.3` resolves to an older `FsCheck` package line than the newest version visible on NuGet. That is acceptable for now because alignment with the active Expecto integration package is preferred over premature version chasing.
