# 0025 Official Package Docs First And Local Package Reference Notes

## Status

Accepted

## Context

NEXUS relies on external packages for important behavior such as:

- testing
- snapshot verification
- browser tooling
- serialization
- database access
- provider and API integrations

When those packages are used without first consulting their official docs, it is easy to:

- infer behavior from outputs instead of confirming it
- miss existing conventions and onboarding guidance
- spend time reverse-engineering expected behavior that upstream docs already explain
- repeat the same confusion later because the local repo never records what was learned

This became concrete while working with `Verify`, where the correct upstream guidance existed but was not consulted early enough.

## Decision

When NEXUS adopts or meaningfully uses an external package, the default learning order is:

1. official README or docs entry point first
2. official getting-started, quickstart, wizard, or conventions docs next
3. official examples and tests next
4. source and low-level inspection after that
5. byte-level or binary inspection only as fallback when the stronger sources still leave ambiguity

Do not guess package behavior from generated outputs when official docs can answer the question.

When a package matters to ongoing work in this repo, add or update a local package reference note under:

- [`docs/reference/packages/`](../reference/packages/README.md)

Create or update that note when:

- a new dependency is added
- a new feature area of an existing dependency is used
- a surprising behavior or error is resolved
- a package upgrade changes expectations that matter locally

Local package reference notes should stay practical and should not try to copy upstream docs. They should capture:

- what the package is used for here
- the official docs and links to check first
- the specific features or workflows this repo depends on
- output or file conventions that matter locally
- known gotchas or previously corrected misunderstandings
- the local commands, tests, or runbooks that exercise it

## Consequences

- official package docs become part of the normal verification path, not an optional extra
- local package notes turn one-off troubleshooting into durable repo memory
- AI agents and humans have a clearer starting point before guessing from implementation details
- local notes remain subordinate to upstream docs and should summarize, not fork, the package documentation
