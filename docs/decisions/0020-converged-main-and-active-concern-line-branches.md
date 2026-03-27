# 0020. Converged Main And Active Concern-Line Branches

## Status

Accepted

## Context

NEXUS is being built across several active concern lines at once.

Examples already visible in this repository include:

- NEXUS doctrine and ontology
- ingestion and canonical-history work
- LOGOS intake and handling
- reusable `FnTools` library work
- `CheddarBooks` application work

That means a simple short-lived branch model is not enough on its own.

At the same time, the repository still needs one converged baseline so humans and AI agents do not drift into competing local truths.

## Decision

Use `main` as the current converged shared truth.

Also allow multiple active long-lived concern-line branches when they represent genuinely different lines of work that are still evolving.

Working rules:

- `main` is the current accepted and converged baseline for new work unless there is a clear reason to branch from an active concern-line branch instead
- active long-lived branches are allowed when they represent real ongoing lines such as `fntools-foundation` or `cheddarbooks-foundation`
- active long-lived branches may merge into `main` multiple times across their lifespan
- merge accepted checkpoints from those branches into `main` with `--no-ff`
- periodically merge `main` back into active long-lived branches when shared foundations, decisions, or reusable code have moved
- use milestone or release tags on meaningful `main` commits when a converged checkpoint should stay easy to recover
- prefer explicit convergence over letting branches drift for too long
- avoid direct commits on `main` for normal implementation follow-up work; use a branch unless the change is truly tiny and administrative

## Branch Lifecycle Rule

Not every merged branch is finished forever.

There are two normal branch lifecycles here:

1. short-lived focused branch
   - created for one coherent piece of work
   - merged into `main`
   - deleted after merge when the workstream is complete enough that the next step should start fresh from `main`

2. long-lived concern-line branch
   - created for an ongoing line such as `fntools-foundation`
   - may merge checkpoints into `main` multiple times
   - may also take periodic merges from `main`
   - remains alive until that concern line no longer needs an independent cadence

## When To Delete A Branch

Delete a branch when all of the following are true:

- its useful history is already preserved by merge commits on `main`
- the branch no longer needs to evolve separately from `main`
- the next expected work in that area can sensibly branch from `main`
- there is no active reason to keep its own roadmap, review thread, or convergence cadence

Keep a branch alive when any of the following are true:

- the concern line is still evolving independently
- more checkpoints are expected to merge into `main`
- the branch is serving as the active integration line for related sub-branches
- deleting it would just lead to immediately recreating the same branch again

## Collaboration Rule

This repository is expected to be worked on by multiple humans and multiple AI systems over time.

That means collaborators should not rely on chat memory alone to understand the current truth.

Before doing significant work, collaborators should orient themselves from the repo:

1. read [`README.md`](../../README.md)
2. read [`repository-concern-lines.md`](../repository-concern-lines.md)
3. read [`glossary.md`](../glossary.md)
4. read the decision records and how-to docs for the relevant concern line
5. inspect the active branch and its relationship to `main`
6. then inspect source and tests

## Consequences

Positive:

- `main` remains a reliable baseline for new work
- long-lived foundation lines can continue without forcing premature closure
- merge history shows real convergence instead of hidden rebases or overwritten branch shape
- future humans, Claude, Codex, and later CORTEX can recover expectations from the repo itself
- branch deletion becomes an explicit lifecycle choice instead of guesswork

Tradeoffs:

- branch maintenance becomes deliberate work
- active branches need periodic synchronization
- docs and decisions must stay current enough to support repo-based onboarding

## Related

- [`0008-branch-topology-by-workstream.md`](0008-branch-topology-by-workstream.md)
- [`0017-docs-and-tests-ship-with-work.md`](0017-docs-and-tests-ship-with-work.md)
- [`../repository-concern-lines.md`](../repository-concern-lines.md)
- [`../collaboration-protocol.md`](../collaboration-protocol.md)
