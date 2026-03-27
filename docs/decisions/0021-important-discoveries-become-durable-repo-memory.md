# 0021. Important Discoveries Become Durable Repo Memory

## Status

Accepted

## Context

NEXUS work is exploratory, but the important outcomes of that exploration cannot stay trapped in chat history or personal memory.

This repository is expected to be worked on over time by:

- multiple humans
- Codex
- Claude
- later CORTEX and other AI systems

If important learnings are left only in chat, then future work becomes inconsistent, duplicated, or dependent on whoever happens to remember the prior conversation.

## Decision

When a discovery, correction, rule, boundary, naming decision, or architectural learning is important enough to guide future work, it must be recorded in durable repo memory.

Durable repo memory means one or more of:

- a decision record
- a glossary entry
- a concern-line or architecture doc
- a concept note
- a requirements or workflow doc
- tests when the learning should be enforced behaviorally

Important learnings should also be referenced from the places where they matter, rather than expecting collaborators to rediscover them from chat or memory.

## Working Rule

Collaborators should prefer this sequence:

1. discover or clarify something important
2. record it in the repo at the right level
3. reference that durable record from related work
4. then continue building on top of it

Do not rely on:

- chat memory alone
- implicit team folklore
- one AI assistant remembering a prior conversation
- one human remembering why a rule exists

## Consequences

Positive:

- future humans and AI systems can onboard from the repo instead of reconstructing chat history
- cross-branch and cross-repo work stays more consistent
- governance becomes inspectable and teachable
- important discoveries survive model changes, handoffs, and time

Tradeoffs:

- collaborators must spend some effort writing and linking the durable record
- some discoveries will require deciding which doc layer they belong in
- the repo memory must stay curated enough to remain usable

## Related

- [`0017-docs-and-tests-ship-with-work.md`](0017-docs-and-tests-ship-with-work.md)
- [`0020-converged-main-and-active-concern-line-branches.md`](0020-converged-main-and-active-concern-line-branches.md)
- [`../collaboration-protocol.md`](../collaboration-protocol.md)
- [`../glossary.md`](../glossary.md)
