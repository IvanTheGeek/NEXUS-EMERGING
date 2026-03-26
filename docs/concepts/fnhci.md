+++
note_kind = "concept_seed"
title = "FnHCI"
slug = "fnhci"
status = "seed"
created_at = "2026-03-24T10:27:48.3662457+00:00"
updated_at = "2026-03-26T10:26:00Z"
domains = ["interaction-design", "software-development"]
tags = ["seed", "projection-system", "bolero-origin"]
canonical_conversation_ids = ["019d174f-2cc4-781d-8a81-8936b3b29c99", "019d174f-2cd6-772c-97db-8fdcb16a0050", "019d174f-2ce1-7496-a7f3-2e5cae80727e", "019d299b-6609-72fc-92f6-81f3ff776193"]
+++

# FnHCI

## Summary

FnHCI emerged from the Bolero and Blazor investigation line as the broader name for a projection system that goes beyond visual UI alone.

The shift appears to be:

- from `FnUI` as a UI-focused name
- toward `FnHCI` as a wider human-computer interaction model

The reason this matters is scope. `FnHCI` is meant to cover:

- visual UI
- CLI and terminal interaction
- API and contract surfaces
- device interaction
- human workflow
- cross-surface behavior and accessibility concerns

At the current understanding, `FnUI` becomes one lens or sub-area inside `FnHCI`, not the whole thing. The Bolero investigation contributed the substrate insight: build on the rendering/runtime seam without inheriting the framework's ideology as the final model. From there, the concept widened into a platform-neutral projection model over multiple human-facing surfaces.

The newer `FnHCI Concept Overview` conversation reinforces that this is not just a naming tweak. It frames `FnHCI` as a unifying concept for the work already happening in NEXUS: a way to think about human-computer interaction as a projection concern that spans visual interfaces, terminal interaction, APIs, and other operational surfaces without collapsing them into a single UI-only idea.

## Working Notes

- What are the stable core primitives of FnHCI, separate from any one runtime like Blazor or Bolero?
- How should FnUI, FnCLI, FnAPI, and accessibility concerns relate as lenses or subdomains under FnHCI?
- Which parts of FnHCI belong in NEXUS universal graph language versus a software-development-specific lens?

## Source Conversations

### Bolero HTML vs F# DSL
- canonical conversation id: `019d174f-2cc4-781d-8a81-8936b3b29c99`
- message count: `9`
- providers: `chatgpt`
- provider conversation ids: `69b7a6d7-25d0-8332-88c9-e6a59892c07f`
- first occurred at: `2026-03-16T06:45:30.047614Z`
- last occurred at: `2026-03-16T07:57:02.4032912Z`
- projection file: `../../NEXUS-EventStore/projections/conversations/019d174f-2cc4-781d-8a81-8936b3b29c99.toml`
- graph scope command: `dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --conversation-id 019d174f-2cc4-781d-8a81-8936b3b29c99`

#### Excerpts
- `human`: The difference in bolero between using HTML templates and doing F# HTML dsl
- `assistant`: In **entity["software","Bolero","F# web framework"]**, you have two main ways to define UI:  1. **HTML Templates** 2. **F# HTML DSL**  They produce the **sa…

### F# Dependency Management 2026
- canonical conversation id: `019d174f-2cd6-772c-97db-8fdcb16a0050`
- message count: `297`
- providers: `chatgpt`
- provider conversation ids: `69bc6941-c27c-8327-bc79-97ebdeb51501`
- first occurred at: `2026-03-19T21:23:24.3719999Z`
- last occurred at: `2026-03-20T11:07:33.4083769Z`
- projection file: `../../NEXUS-EventStore/projections/conversations/019d174f-2cd6-772c-97db-8fdcb16a0050.toml`
- graph scope command: `dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --conversation-id 019d174f-2cd6-772c-97db-8fdcb16a0050`

#### Excerpts
- `human`: is F# paket still the accepted way to handle dependencies or is there a ms solution now
- `assistant`: search("is F# paket still the accepted way to handle dependencies or is there a ms solution now")

### HTML as Graph Structure
- canonical conversation id: `019d174f-2ce1-7496-a7f3-2e5cae80727e`
- message count: `97`
- providers: `chatgpt`
- provider conversation ids: `69bd356d-61b4-832f-ac05-c291a1e80d99`
- first occurred at: `2026-03-20T11:54:47.6638159Z`
- last occurred at: `2026-03-20T13:25:55.5672249Z`
- projection file: `../../NEXUS-EventStore/projections/conversations/019d174f-2ce1-7496-a7f3-2e5cae80727e.toml`
- graph scope command: `dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --conversation-id 019d174f-2ce1-7496-a7f3-2e5cae80727e`

#### Excerpts
- `human`: HTML with relation to the artifact of text of an HTML file and graph theory is described how.
- `assistant`: HTML, viewed as the text artifact in an `.html` file, can be described in graph terms like this:  An HTML file is **text that encodes a graph-shaped structure*…

### FnHCI Concept Overview
- canonical conversation id: `019d299b-6609-72fc-92f6-81f3ff776193`
- message count: `8`
- providers: `chatgpt`
- provider conversation ids: `69c266ec-63d8-8332-8ce9-969bcfa4b40b`
- first occurred at: `2026-03-24T10:26:54.2279999Z`
- last occurred at: `2026-03-24T10:26:54.7148079Z`
- projection file: `/home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore/projections/conversations/019d299b-6609-72fc-92f6-81f3ff776193.toml`
- graph slice command: `dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --conversation-id 019d299b-6609-72fc-92f6-81f3ff776193`

#### Excerpts
- `human`: fnhci
- `assistant`: You’re circling something very powerful with FnHCI. This could unify a large part of what NEXUS is already building.

## Next Questions

- What should this concept mean structurally?
- Which parts are stable enough to move into durable project memory?
- What additional conversations or artifacts should be harvested into this note?
