# Public Forum Post Draft

Use this as the shorter public-facing Penpot community post.

It is intentionally brief and should link outward to:

- the deeper GitHub-hosted retrospective note
- the narrower GitHub issue for one concrete bug

Before posting:

1. make sure the `main` GitHub blob URL is the one you want to reference
2. adjust tone/length as desired for the forum audience
3. add any already-posted public links from this same workstream

## Suggested Title

Why I paused Penpot MCP in my Codex workflow for now

## Draft

I want to share a short retrospective on trying to use Penpot as part of a real AI-assisted workflow with Codex, MCP, local backend access, reusable components, and model-to-projection work.

This is my own research and opinion from one serious integration attempt, not an attempt to speak for Penpot or for every user workflow.

Why I was trying it:

- I wanted Penpot to act as a live visual projection surface for Event Modeling, screen paths, and reusable UI/state work
- I was interested in the bridge between design, modeling, AI manipulation, and later generated application surfaces
- I was hoping for not only `model -> projection -> artifact`, but also `penpotDesign -> ingestion -> model`, where an existing design could feed back into modeling and real app production
- I was specifically trying to see whether Penpot could function as more than a passive design artifact in this workflow

What worked:

- backend access and export were useful
- Penpot was valuable as design evidence and a projection surface
- some live board/component work did work

Why I paused it for now:

- the workflow stopped being deterministic enough for the phase I’m in
- MCP transport/connectivity was fragile in practice
- plugin/API behavior diverged
- reusable instance overrides and some render/export paths were not reliable enough for the active workflow

So this is not a "Penpot is bad" post.

It is more: Penpot was useful research, but it became more integration friction than modeling leverage for this phase, so I moved the active work to deterministic F#/HTML/CSS renderers and kept Penpot as supporting evidence rather than the primary engine.

I wrote up the deeper details here:

- Deep retrospective: <https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/main/docs/research/penpot/codex-mcp-retrospective.md>

And I already filed or linked one narrower bug/aspect post separately, so I’m not trying to rehash all of that here:

- Existing GitHub issue: <https://github.com/penpot/penpot/issues/8826>
- Existing forum plugin note: <https://community.penpot.app/t/what-s-working-and-what-s-missing-in-penpot-plugins/9785/8?u=ivanthegeek>
- Existing forum text-rendering note: <https://community.penpot.app/t/penpot-bug-report-on-text-rendering-overrides/10460?u=ivanthegeek>

I also think this older thread is worth keeping in view as trace:

- [API and MCP issues](https://community.penpot.app/t/api-and-mcp-issues/9962)

If the Penpot team or other users are interested, the deeper note includes:

- exact local Codex log references
- the transport failure trail
- the motivations for trying Penpot this way
- the deeper workflow problems that made me pause it for now

## Suggested GitHub Human-Facing Link

Preferred public URL:

`https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/main/docs/research/penpot/codex-mcp-retrospective.md`

Use the `main` URL for public references when possible.
