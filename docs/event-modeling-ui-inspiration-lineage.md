# Event Modeling UI Inspiration Lineage

This note records the current inspiration chain for the slice-based Event Modeling UI direction.

The goal is practical:

- keep useful historical references easy to recover
- separate inspiration from current source of truth
- preserve the Bolero-to-FnHCI/FnUI transition line
- keep future humans and AI from rediscovering the same context only through chat

## Current Working Rule

These inspiration surfaces are useful, but they are not the current semantic source of truth.

Keep the distinction clear:

- inspiration can shape look, feel, layout, and comparison
- durable repo notes carry the current intended meaning
- Penpot is the current projection surface for the visual board
- later spec and FORGE work should derive from durable meaning, not from historical mockups alone

## Current Inspiration Surfaces

### Slice-Based Visualization Page

- [Event Model - Slice-Based Visualization 2](https://concepts.em.ivanthegeek.com/Event%20Model%20-%20Slice-Based%20Visualization%202.html)

What is useful there:

- clean slice-card presentation
- calm horizontal reading flow
- visible distinction between command/event/view/screen areas
- page feel that reads more like a designed product than a rough diagram

What is not currently authoritative there:

- the vertical ordering
- the precise slice semantics
- the exact terminology

### Machine-Local MHTML Inspiration Artifact

There is also a machine-local scratch artifact:

- `Event-Modeling-Experiment-localhost.mhtml`

What is useful there:

- legend treatment
- section grouping
- test-panel presentation
- a more application-like visual feel

What is not currently authoritative there:

- ordering
- slice rules
- any implied source-of-truth claim

### EM-1 Historical Experiment Repo

- [IvanTheGeek/EM-1](https://github.com/IvanTheGeek/EM-1)

This repo is useful as a historical experiment line rather than a current foundation.

Important artifacts there include:

- `MyApp/src/MyApp.Client/wwwroot/Slice.html`
- `MyApp/src/MyApp.Client/wwwroot/main.html`
- `exploration/nexus-slice-renderer.html`
- `MyApp/src/MyApp.Server/data/model/.../*.toml`
- `MyApp/src/NEXUS/Core.fs`

What is useful there:

- earlier slice-oriented HTML presentation experiments
- TOML-shaped slice specs
- the idea of a graph-oriented substrate under higher-level lenses
- historical hosting-mode experiments across WASM, server, and hybrid shapes

One still-live hosted reference from that line is:

- [EM-1 Slice working file](https://em1.ivanthegeek.com/wwwroot/Slice.html)

That page is useful as a direct presentation reference for the card feel and section rhythm, even though it is not the current semantic source of truth.

What should not be carried forward uncritically:

- Bolero as the long-term answer
- the exact visual ordering of earlier slice pages
- the assumption that those TOML files are now the authoritative shape

### Recovered Slice HTML Milestones

The strongest surviving recovery path for the EM-1 HTML look-and-feel work is the git history on `main`.

Earlier imported conversation history recorded these now-missing remote branches:

- `experiment`
- `TOML`
- `claude/eager-nash`
- `claude/sweet-goodall`

But the current GitHub remote only exposes `main`, so the practical recovery surface is the commit lineage that remains there.

Useful milestones:

- `d47c513`
  base command-slice HTML concept
- `f229656`
  GWT row added and layout refined
- `e6f1b00`
  view-slice section added
- `f63e1af`
  read-model row added and badge grouping refined

These commits are useful because they show the visual evolution from a single command-slice card toward the calmer slice-card language that later inspired the current Penpot direction.

## Bolero To FnHCI/FnUI Source Chain

The current FnHCI/FnUI direction did not appear in isolation.

The recorded source chain includes:

- [`019d174f-2cd6-772c-97db-8fdcb16a0050.toml`](../NEXUS-EventStore/projections/conversations/019d174f-2cd6-772c-97db-8fdcb16a0050.toml)
  Bolero seam analysis, including the “build on Blazor as the substrate” line
- [`019d174f-2ce1-7496-a7f3-2e5cae80727e.toml`](../NEXUS-EventStore/projections/conversations/019d174f-2ce1-7496-a7f3-2e5cae80727e.toml)
  FnHCI / FnUI naming and the projection-model split
- [`019d174f-2cc3-773c-9d90-ae4306d996d5.toml`](../NEXUS-EventStore/projections/conversations/019d174f-2cc3-773c-9d90-ae4306d996d5.toml)
  slice graph, PATH, projection, agent/view semantics, and the split into two slices
- [`019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml`](../NEXUS-EventStore/projections/conversations/019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml)
  LaundryLog Penpot/Bolero proof-of-concept direction

The important transition is:

- EM-1 and Bolero work were useful exploration
- the seam at Blazor remained valuable
- the durable direction moved toward `FnHCI` / `FnUI` rather than repairing or adopting Bolero as-is

## What To Retain

Retain these ideas:

- slice-based visual storytelling
- visually calm, product-like Event Modeling pages
- explicit difference between `CommandSlice` and `ViewSlice`
- TOML-shaped thinking for human-readable specs
- graph-backed substrate exploration as a future direction

## What To Discard Or Keep Only As Historical Context

Do not let these become accidental authority:

- incorrect vertical ordering from inspiration pages
- slice structures that contradict the current `CommandSlice` / `ViewSlice` understanding
- old Bolero coupling as if it were still the target
- derived static pages becoming authoritative over current repo memory

## Current Direction

The current practical direction is:

- durable meaning in repo docs
- Penpot as the live projection surface
- reusable interaction abstraction under `FnHCI` / `FnUI`
- later graph-backed spec surfaces
- later FORGE transforms from spec to code to running artifact

That means these inspiration surfaces are valuable inputs, but they are not the final model.
