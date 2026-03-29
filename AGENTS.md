# Repository Instructions

This repository is the foundation workspace for NEXUS.

It is shared operating memory for humans and AI agents.

Read in this order before substantial work:

1. [`README.md`](README.md)
2. [`docs/agent-readme.md`](docs/agent-readme.md)
3. [`docs/current-focus.md`](docs/current-focus.md)
4. [`docs/cortex-repo-memory-protocol.md`](docs/cortex-repo-memory-protocol.md)
5. [`docs/fsharp-usage-learning-and-guidance.md`](docs/fsharp-usage-learning-and-guidance.md) when the work is primarily F#
6. the relevant docs, code, and tests

Working rules:

- do not rely on chat memory alone for project understanding
- keep scratch, durable docs, canonical history, and derived views distinct
- record durable learnings in the repo when they should matter later
- when code, renderer, command behavior, or visible behavior changes, add or update tests by default
- if a relevant test is not added or updated, say why explicitly
- for UI, HTML, CSS, renderer, and screen work, inspect the actual local source and current artifacts before changing behavior
- when a repeat F# seam, preference, or verification rule becomes clear, record it durably so later agents do not have to rediscover it

Primary references:

- [`docs/agent-readme.md`](docs/agent-readme.md)
- [`docs/current-focus.md`](docs/current-focus.md)
- [`docs/cortex-repo-memory-protocol.md`](docs/cortex-repo-memory-protocol.md)
- [`docs/fsharp-usage-learning-and-guidance.md`](docs/fsharp-usage-learning-and-guidance.md)
- [`docs/index.md`](docs/index.md)
