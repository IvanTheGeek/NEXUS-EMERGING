# Penpot MCP And Codex Retrospective

This note is a deeper research and opinion surface for the Penpot work done through Codex, local MCP, backend API, and exported `.penpot` snapshots.

It is intentionally more detailed than a public forum post.

Its job is to:

- preserve why Penpot was attractive for this line of work
- distinguish what actually worked from what only seemed promising
- keep exact evidence references available for later investigation
- support shorter public Penpot posts with a stronger traceable source

This is not a single upstream bug report.

It is a retrospective over one serious integration attempt.

## Current Publication Strategy

The recommended publication stack for this material is:

1. a focused GitHub issue for one concrete reproducible bug
2. a shorter Penpot community forum post for the broader narrative
3. this deeper research note as the full evidence and interpretation trail

Current public Penpot trace that should be included in that chain:

- [API and MCP issues](https://community.penpot.app/t/api-and-mcp-issues/9962)
- [Penpot MCP Server showcase + ask for help](https://community.penpot.app/t/penpot-mcp-server-showcase-ask-for-help/10040)
- [Push the Penpot MCP to its Limits. Join the Beta test!](https://community.penpot.app/t/push-the-penpot-mcp-to-its-limits-join-the-beta-test/10363)

Additional already-posted public links from this specific workstream should be added here once collected:

- existing GitHub issue for one MCP aspect: `TODO add URL`
- existing Penpot forum post for one MCP aspect: `TODO add URL`

## Why Penpot Was Attractive

Penpot was not approached here as "just another design tool".

The attraction was that it looked like a plausible bridge between:

- Event Modeling and path work
- reusable component and token work
- a live visual projection surface
- AI-assisted inspection and manipulation
- later generated application surfaces

The motivating idea was not shallow design export.

It was closer to:

- model -> projection -> artifact
- with Penpot as one of the live projection surfaces

### Motivation Trace

Useful historical references:

- bootstrap the event model from the design surface:
  - [019d174f-2c90-79b5-beb8-99077a7d6fe0.toml#L625](/home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore/projections/conversations/019d174f-2c90-79b5-beb8-99077a7d6fe0.toml#L625)
- use Penpot for the event model until equivalent in-app functionality existed:
  - [019d174f-2c90-79b5-beb8-99077a7d6fe0.toml#L1040](/home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore/projections/conversations/019d174f-2c90-79b5-beb8-99077a7d6fe0.toml#L1040)
- define a graph/model to Penpot interface:
  - [019d174f-2cce-7c36-a16b-4057b71a0212.toml#L22](/home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore/projections/conversations/019d174f-2cce-7c36-a16b-4057b71a0212.toml#L22)
- prove design export into an app pipeline:
  - [019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml#L148](/home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore/projections/conversations/019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml#L148)
- represent a screen state as a board built from reusable components:
  - [019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml#L159](/home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore/projections/conversations/019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml#L159)
- recreate an already-designed HTML screen in Penpot:
  - [019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml#L618](/home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore/projections/conversations/019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml#L618)

## What Worked

The Penpot attempt was not a total failure.

Several things did work well enough to matter:

- live backend API access
- authenticated `.penpot` export through backend `export-binfile`
- live file/page/board inspection
- board-level visual export on the plugin path
- Penpot as design evidence and projection material
- component-oriented experimentation

Good durable summaries:

- [penpot-live-backend-and-export.md#L1](/home/ivan/NEXUS/NEXUS-EMERGING-main/docs/penpot-live-backend-and-export.md#L1)
- [penpot-projection.md#L1](/home/ivan/NEXUS/CheddarBooks/docs/laundrylog/event-modeling/penpot-projection.md#L1)

## What Failed Or Became Too Fragile

The main problem was not one single bug.

It was cumulative workflow fragility.

### 1. MCP Transport Fragility

The strongest hard log-backed failure was the MCP transport conflict.

Codex hit:

- `Already connected to a transport. Call close() before connecting to a new transport...`

Best exact references:

- [rollout-2026-03-28T02-27-32-019d3320-5d98-7771-9b93-48c49ffc8715.jsonl#L27073](/home/ivan/.codex/sessions/2026/03/28/rollout-2026-03-28T02-27-32-019d3320-5d98-7771-9b93-48c49ffc8715.jsonl#L27073)
- [rollout-2026-03-28T02-27-32-019d3320-5d98-7771-9b93-48c49ffc8715.jsonl#L27077](/home/ivan/.codex/sessions/2026/03/28/rollout-2026-03-28T02-27-32-019d3320-5d98-7771-9b93-48c49ffc8715.jsonl#L27077)

Durable interpretation:

- [penpot-live-backend-and-export.md#L100](/home/ivan/NEXUS/NEXUS-EMERGING-main/docs/penpot-live-backend-and-export.md#L100)

### 2. Plugin/API Surface Divergence

The backend API and MCP/plugin were both useful, but they were not interchangeable.

That meant:

- the backend was often the reliable live/export surface
- the plugin path was the stronger live editing surface
- real work had to juggle both rather than trust one unified integration seam

Durable notes:

- [penpot-surface-comparison.md#L1](/home/ivan/NEXUS/NEXUS-EMERGING-main/docs/penpot-surface-comparison.md#L1)
- [penpot-live-backend-and-export.md#L1](/home/ivan/NEXUS/NEXUS-EMERGING-main/docs/penpot-live-backend-and-export.md#L1)

### 3. Instance Overrides Stored But Not Rendered

This was one of the deeper workflow breakers.

The higher-on-the-ladder reusable seam was correct:

- use `LibraryComponent.instance()`

But in the current lab:

- instance text overrides were present in live shape data
- the live canvas still painted the master/default text
- export also painted the master/default text

Best durable references:

- [penpot-surface-comparison.md#L228](/home/ivan/NEXUS/NEXUS-EMERGING-main/docs/penpot-surface-comparison.md#L228)
- [penpot-projection.md#L261](/home/ivan/NEXUS/CheddarBooks/docs/laundrylog/event-modeling/penpot-projection.md#L261)

### 4. Active-Page / Top-Level Placement Quirks

Another major source of brittleness was that top-level board creation and reparenting followed the active page.

That meant automation had to respect Penpot page focus in a way that felt more operational than semantic.

Best durable reference:

- [penpot-surface-comparison.md#L411](/home/ivan/NEXUS/NEXUS-EMERGING-main/docs/penpot-surface-comparison.md#L411)

Historical trace:

- [019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml#L1398](/home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore/projections/conversations/019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml#L1398)

### 5. Token And Render Uncertainty

Token work and text/render behavior both required explicit skepticism.

Current local durable cautions:

- `shape.applyToken(...)` threw a Penpot-side `check error`
- some text/render paths required resizing or overlays
- whole-page export was not a safe default

Best references:

- [penpot-projection.md#L400](/home/ivan/NEXUS/CheddarBooks/docs/laundrylog/event-modeling/penpot-projection.md#L400)
- [penpot-surface-comparison.md#L200](/home/ivan/NEXUS/NEXUS-EMERGING-main/docs/penpot-surface-comparison.md#L200)
- [penpot-live-backend-and-export.md#L140](/home/ivan/NEXUS/NEXUS-EMERGING-main/docs/penpot-live-backend-and-export.md#L140)

## Why Penpot Was Abandoned For Now

The decision was not:

- "Penpot is useless"

The decision was:

- Penpot was useful research
- Penpot remained good projection and evidence material
- but Penpot was not deterministic enough to be the primary working engine for this phase

The best concise local summary of that shift is:

- [html-path-renderer-proving-ground.md#L1](/home/ivan/NEXUS/CheddarBooks/docs/laundrylog/html-path-renderer-proving-ground.md#L1)

Short reason:

> Penpot was abandoned for now because the workflow stopped being deterministic enough for the phase of work being done. MCP transport was fragile, plugin/API behavior diverged, reusable instance overrides were not trustworthy on canvas/export, and automation kept requiring Penpot-specific workarounds. At that point Penpot was producing more integration friction than modeling leverage.

## What Replaced It For Now

The active work shifted to deterministic renderers:

- typed F#
- self-contained HTML/CSS
- reviewable file-based artifacts

That replacement direction lives here:

- [html-path-renderer-proving-ground.md#L1](/home/ivan/NEXUS/CheddarBooks/docs/laundrylog/html-path-renderer-proving-ground.md#L1)
- [html-screen-renderer-proving-ground.md#L1](/home/ivan/NEXUS/CheddarBooks/docs/laundrylog/html-screen-renderer-proving-ground.md#L1)
- [html-screen-path-renderer-proving-ground.md#L1](/home/ivan/NEXUS/CheddarBooks/docs/laundrylog/html-screen-path-renderer-proving-ground.md#L1)

## Suggested Public Post Split

### GitHub Issue

Keep it narrow.

Use it for:

- the exact MCP transport/connection failure

Reference:

- [rollout-2026-03-28T02-27-32-019d3320-5d98-7771-9b93-48c49ffc8715.jsonl#L27073](/home/ivan/.codex/sessions/2026/03/28/rollout-2026-03-28T02-27-32-019d3320-5d98-7771-9b93-48c49ffc8715.jsonl#L27073)

### Forum Post

Keep it broader.

Use it for:

- motivation
- what worked
- what failed
- why Penpot was paused
- link to this research note
- link to the narrow GitHub issue

## Things Penpot Team Could Investigate

These are the most actionable questions from this work:

1. Is the current MCP transport/session model intentionally single-client in the way the observed failure suggests?
2. Should plugin-connected + Codex-connected workflows be expected to coexist cleanly?
3. Are connected component instance text overrides expected to render immediately on canvas and export in the `LibraryComponent.instance()` path?
4. Are active-page semantics for top-level creation/reparenting intentional or an implementation quirk?
5. What is the intended trustworthy token-application contract for plugin-side automation?

## Working Rule For Future Notes

When a shorter public Penpot post is created from this work:

- keep the public text concise
- link here for the full trail
- keep exact logs and stronger interpretation here rather than dumping them into the public post

