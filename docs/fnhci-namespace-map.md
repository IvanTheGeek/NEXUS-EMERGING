# FnHCI Namespace Map

This note maps the broader `FnHCI` concern to the narrower `FnUI` system line.

The governing decision is:

- [`docs/decisions/0015-fnhci-owns-the-top-interaction-namespace.md`](decisions/0015-fnhci-owns-the-top-interaction-namespace.md)
- [`docs/fnhci-ui-blazor-requirements.md`](fnhci-ui-blazor-requirements.md)

## Core Distinction

- `FnHCI`
  the broader human-computer interaction concern line
- `FnUI`
  the concrete visual/UI-specific system line inside that broader concern

## Intended Layering

1. concern line
   `FnHCI`
2. top internal namespace
   `FnTools.FnHCI`
3. visual subsystem namespace
   `FnTools.FnHCI.UI`
4. Blazor-specific host/runtime seam
   `FnTools.FnHCI.UI.Blazor`
5. outward-facing package or product line
   `FnUI`, `FnUI.Blazor`, or similar

## Why This Split Helps

- `FnHCI` can keep the wider meaning for CLI, API, accessibility, and other interaction modes
- the Bolero-replacement system can stay clearly visual/UI-specific
- internal code namespaces can remain structurally honest
- package naming can remain clear and memorable

## Current Reading

Based on the current discussions, the likely interpretation is:

- `FnHCI`
  the umbrella interaction namespace and concern
- `FnUI`
  the branded/public line for the visual system that replaces Bolero while sitting on top of Blazor

## Evidence From Recorded Conversation

The strongest current reference point is:

- [`019d174f-2ce1-7496-a7f3-2e5cae80727e.toml`](../NEXUS-EventStore/projections/conversations/019d174f-2ce1-7496-a7f3-2e5cae80727e.toml)

That conversation currently captures three especially relevant steps:

- `FnHCI` at the top with `FnUI`, `FnCLI`, `FnAPI`, and `FnA11y` under it
- concrete visual naming like `FnUI.Blazor`, `FnUI.HTML`, and `FnUI.android`
- the explicit statement that `FnUI` might be used as a lens over `FnHCI.UI.Blazor` as the NuGet and "marketing" term

That is the clearest current signal that:

- `FnHCI` should own the broader namespace
- the Blazor-based replacement layer belongs under that namespace
- `FnUI` can remain the outward-facing visual system line

## First Likely Code Shapes

If this direction holds, early code and project shapes may look more like:

- `FnTools.FnHCI`
  interaction primitives and shared abstractions
- `FnTools.FnHCI.UI`
  visual view/state/composition model
- `FnTools.FnHCI.UI.Blazor`
  Blazor host/runtime adapter

with package and outward naming such as:

- `FnUI`
- `FnUI.Blazor`

## Open Questions

- Should the public package line use `FnUI` alone, or should `FnHCI` appear in some package names too?
- Which primitives belong in `FnTools.FnHCI` versus `FnTools.FnHCI.UI`?
- How much of the eventual NEXUS GUI belongs to the reusable `FnUI` system versus app-specific composition?

## Current Scaffold Note

The current code namespace inside this repo now uses `FnTools.FnHCI.*`.

Project and filesystem paths may still temporarily use the older `Nexus.FnHCI` project-path names while the extraction into a dedicated `FnTools` repo is being prepared.
