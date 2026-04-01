# Package References

This area holds short local reference notes for external packages that matter to ongoing NEXUS work.

These notes are not meant to replace upstream documentation.

Use them to record:

- what the package is used for in this repo
- which official docs should be checked first
- the specific local features, commands, and workflows that rely on it
- conventions or gotchas that have already cost us time once

## Default Rule

Before leaning on behavior inferred from output, inspect the official sources first:

1. package README or docs entry point
2. getting-started, quickstart, wizard, or conventions docs
3. examples and tests
4. source and low-level inspection only after that

## When To Add Or Update A Note

Add or update a package note when:

- a new dependency is added
- a new feature area of an existing dependency is used
- a surprising behavior is resolved
- an upgrade changes local expectations

## Current Notes

- [`verify.md`](verify.md)

## Suggested Note Shape

Keep notes short and practical.

Suggested sections:

- purpose in NEXUS
- official docs to check first
- local usage here
- important conventions
- local gotchas and corrected misunderstandings
- related code, tests, and runbooks
