# Repository Instructions

This repository is the foundation workspace for NEXUS.

It is shared operating memory for humans and AI agents.

Human collaborators are the ultimate decision makers in NEXUS; AI agents exist to help execute, clarify, and suggest better strategies, tools, and implementations in service of human direction.

Read in this order before substantial work:

1. [`README.md`](README.md)
2. [`docs/agent-readme.md`](docs/agent-readme.md)
3. [`docs/current-focus.md`](docs/current-focus.md)
4. [`docs/cortex-repo-memory-protocol.md`](docs/cortex-repo-memory-protocol.md)
5. [`docs/fsharp-usage-learning-and-guidance.md`](docs/fsharp-usage-learning-and-guidance.md) when the work is primarily F#
6. the relevant docs, code, and tests

Working rules:

- do not rely on chat memory alone for project understanding
- do not guess from memory, old chat, screenshots, or stale assumptions when the current repo state can be inspected directly; verify the actual docs, code, tests, artifacts, and branch/worktree shape before acting
- keep scratch, durable docs, canonical history, and derived views distinct
- record durable learnings in the repo when they should matter later
- for now, keep `main` as the default active branch and merge accepted work back into `main` promptly instead of leaving side branches alive longer than needed
- use additional linked Git worktrees only as temporary operating surfaces for active side branches, merge/convergence work, or isolated parallel work, then remove them once that need ends
- after a branch is merged, remove its extra worktree if one exists and delete the local branch; delete the remote branch too when the shared remote no longer needs that ref
- when code, renderer, command behavior, or visible behavior changes, add or update tests by default
- if a relevant test is not added or updated, say why explicitly
- for UI, HTML, CSS, renderer, and screen work, inspect the actual local source and current artifacts before changing behavior
- when a repeat F# seam, preference, or verification rule becomes clear, record it durably so later agents do not have to rediscover it
- AI agents are welcome to proactively suggest better-fit tools or languages when the current concern calls for them; make the tradeoff visible and keep the broader F#-centered direction clear
- the human has the final decision; once that decision is made, use it, while still allowing respectful future prompting when a materially better option becomes apparent

Primary references:

- [`docs/agent-readme.md`](docs/agent-readme.md)
- [`docs/current-focus.md`](docs/current-focus.md)
- [`docs/cortex-repo-memory-protocol.md`](docs/cortex-repo-memory-protocol.md)
- [`docs/fsharp-usage-learning-and-guidance.md`](docs/fsharp-usage-learning-and-guidance.md)
- [`docs/index.md`](docs/index.md)
