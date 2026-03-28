# Collaboration Protocol

This document is the practical collaboration contract for humans and AI systems working in this repository.

Use it when:

- starting work on a branch
- handing work between collaborators
- deciding whether to branch from `main` or from an active concern-line branch
- checking what is expected before opening code and making changes

## Core Idea

NEXUS work is repo-centered, not chat-centered.

Chat can help discovery and momentum.

The repository is the durable memory.

That means collaborators should recover expectations from:

- docs
- decisions
- tests
- source
- branch topology

not from memory of prior conversations alone.

Important discoveries should become durable repo memory before collaborators rely on them repeatedly.

## Start-Of-Work Checklist

Before significant work:

1. read [`README.md`](../README.md)
2. read [`repository-concern-lines.md`](repository-concern-lines.md)
3. read [`glossary.md`](glossary.md)
4. read the relevant decision records and runbooks
5. inspect whether the work belongs on:
   - `main`
   - an active concern-line branch
   - a new focused child branch
6. inspect the branch relationship to `main`
7. inspect relevant source and tests

## Tooling Expectations

When a collaborator expects a standard local tool and it is not installed or not available in the current environment:

- say so explicitly
- do not silently substitute a weaker workaround if the missing tool meaningfully affects the workflow
- prefer giving the user the exact tool name and installation need so they can install it if desired
- once the tool is available, prefer the expected tool over ad hoc substitutes

Examples:

- `gh` for GitHub repo and PR workflows
- visualization tools such as `dot` or `sfdp`
- managed Git automation such as the Codex commit-checkpoint post-commit hook when commit-to-chat traceability is part of the workflow
- other build, packaging, or repo-management tools that materially change the normal workflow

When using `dotnet` tooling:

- do not run parallel `dotnet build`, `dotnet run`, or `dotnet test` commands against the same project path when they will write to the same build output
- prefer one build followed by `--no-build` runs when parallel follow-up work is needed
- if true parallelism is needed, isolate outputs or use separate worktrees/projects so the commands are not fighting over the same files

This avoids avoidable MSBuild file-copy contention and misleading build noise.

## Branching Expectations

- `main` is the current converged shared baseline
- long-lived branches are allowed when a concern line is still actively evolving
- short-lived branches are preferred for tightly scoped work
- accepted work merges into `main` with `--no-ff`
- active long-lived branches should periodically take merges from `main`

Examples of long-lived concern-line branches:

- `fntools-foundation`
- `cheddarbooks-foundation`
- `logos-intake-foundation`

## Implementation Expectations

When behavior changes:

- update or add tests
- update docs and runbooks
- update public help and xmldoc when public surfaces change

If a change is docs-only or tests are not applicable, say that explicitly.

See:

- [`decisions/0017-docs-and-tests-ship-with-work.md`](decisions/0017-docs-and-tests-ship-with-work.md)
- [`how-to/run-tests.md`](how-to/run-tests.md)

## Durable Learning Expectations

When an important rule, discovery, correction, or architectural learning emerges:

- record it in the repo at the right level instead of leaving it only in chat
- update an existing durable doc when that is the clearest home
- create a new decision record, concept note, glossary entry, or requirements doc when needed
- add or update tests when the learning should be enforced behaviorally
- reference the durable record from the places where that learning matters

For Penpot-specific work:

- compare the backend API, MCP/plugin, and exported `.penpot` surfaces instead of assuming one surface tells the whole story
- record verified differences and gaps durably so future collaborators do not have to rediscover them
- update the Penpot comparison note when a capability is proven or ruled out

Do not assume future collaborators will recover the learning from chat history alone.

See:

- [`decisions/0021-important-discoveries-become-durable-repo-memory.md`](decisions/0021-important-discoveries-become-durable-repo-memory.md)
- [`penpot-surface-comparison.md`](penpot-surface-comparison.md)

## Decision Expectations

Formalize durable rules when they are:

- expected to guide future work repeatedly
- important to collaboration or safety
- easy to forget if left only in chat

That usually means writing or updating:

- a decision record
- a glossary entry
- a concern-line doc
- a runbook
- durable repo memory for important discoveries and corrections

## Repo Split Expectations

When lines become distinct enough, namespaces and repos should follow the conceptual ownership:

- `Nexus.*` for foundation/system work
- `FnTools.*` for reusable tooling libraries
- `CheddarBooks.*` for concrete product/app work

See:

- [`decisions/0018-namespace-and-repo-boundaries-by-line.md`](decisions/0018-namespace-and-repo-boundaries-by-line.md)
- [`repo-extraction-plan.md`](repo-extraction-plan.md)

## Goal

The goal is that a new human collaborator, Claude, Codex, or later CORTEX can enter this repository, read a small set of docs, and understand:

- what NEXUS is doing
- what branch topology means here
- what quality and documentation expectations exist
- where a new piece of work belongs
