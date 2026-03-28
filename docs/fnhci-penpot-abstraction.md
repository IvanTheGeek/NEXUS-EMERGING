# FnHCI And Penpot Abstraction Boundary

This note captures the current NEXUS direction for how Penpot relates to `FnHCI`, `FnUI`, and later cross-platform UI generation.

The short version is:

- business behavior is not modeled in terms of buttons
- Penpot components are not the canonical UI abstraction either
- `FnHCI` should own the reusable interaction primitives at the right semantic level
- Penpot, Blazor, HTML, Android, iOS, and similar targets should adapt or project those primitives rather than owning them

## Why This Matters

The current design pressure is clear:

- we want a modeled "button" concept that is still meaningful across platforms
- we do not want HTML tags or Android widget classes to be the abstraction owner
- we do not want Penpot component structure to become the only source of truth
- we do want design surfaces like Penpot to stay useful and mappable

The imported discussion history already points in this direction:

- [`019d174f-2ce1-7496-a7f3-2e5cae80727e.toml`](../NEXUS-EventStore/projections/conversations/019d174f-2ce1-7496-a7f3-2e5cae80727e.toml)
  says `FnUI` becomes a platform-neutral projection model and names lines such as `FnUI.Blazor`, `FnUI.HTML`, and `FnUI.android`
- [`019d174f-2cd6-772c-97db-8fdcb16a0050.toml`](../NEXUS-EventStore/projections/conversations/019d174f-2cd6-772c-97db-8fdcb16a0050.toml)
  says Penpot belongs in the target projection layer too
- [`019d174e-eaa9-7b82-9c3f-c55499fe9fd6.toml`](../NEXUS-EventStore/projections/conversations/019d174e-eaa9-7b82-9c3f-c55499fe9fd6.toml)
  reinforces that business behavior does not know about buttons

## Layering

The current intended layering is:

1. business/domain behavior
   commands, events, policies, and invariants
2. `FnHCI` interaction primitive layer
   reusable semantic controls and view composition
3. design and authoring projections
   Penpot files, components, variants, tokens, and board conventions
4. runtime adapters
   Blazor, HTML, Android, iOS, and later others

This means:

- domain behavior should not depend on `Button`
- a platform widget such as `<button>` or `android.widget.Button` should not be the canonical concept either
- Penpot's `Button.Counter` or other component names should map to a primitive, not define the primitive

## The "Correct Level" For A Button

The current working hypothesis is that a `Button` is a valid cross-platform UI primitive if it is modeled at the interaction level instead of the runtime-widget level.

That means the primitive should capture things like:

- semantic role
  what the button means in the interaction
- content
  label, icon, or both
- state
  enabled, disabled, busy, selected, or similar
- emphasis or affordance
  primary, secondary, destructive, quiet, counter-style, and similar visual intent
- activation behavior
  which interaction command it invokes
- accessibility-facing text
  name, description, hint, or similar metadata

It should avoid hard-coding things like:

- HTML element names
- CSS class names
- Android or iOS control types
- Penpot shape ids
- Penpot variant ids
- renderer-specific event APIs

So the "button" concept we want is not:

- a DOM button
- a Material button
- a Penpot frame

It is a reusable `FnHCI` interaction primitive that those targets can render or project.

## Penpot's Role

Penpot remains important, but its role is different from the canonical abstraction.

Penpot is valuable as:

- a visual authoring surface
- a design-system and token surface
- a collaboration and review canvas
- a projection target and projection source
- a bridge while the dedicated Event Modeling and UI tooling is still emerging

Penpot should therefore be treated as:

- an inspectable artifact surface
- a design projection surface
- an adapter boundary

not as:

- the owner of domain behavior
- the owner of the cross-platform interaction primitive model

## Current LaundryLog Pressure

The current `LaundryLog.penpot` file already gives a good example of this split.

Observed Penpot structures include component-like shapes such as:

- `Button.Counter`

That is useful evidence, but `Button.Counter` should likely map to a deeper `FnHCI` primitive shape such as:

- `Button`
  with a `counter` or `increment/decrement` affordance variant

The reusable primitive would then project into:

- a Penpot component/variant arrangement
- a Blazor render tree
- an HTML element structure
- an Android native control
- an iOS native control

without changing the underlying interaction meaning.

## What This Suggests For FnTools

The first durable reusable code line should likely separate:

- `FnTools.FnHCI`
  semantic interaction primitives and shared cross-platform meanings
- `FnTools.FnHCI.UI`
  view structure, layout, composition, and state
- `FnAPI.Penpot`
  Penpot artifact and backend access
- `FnMCP.Penpot`
  higher-level live Penpot interaction helpers

That lets Penpot integration become strong without making Penpot the abstraction owner.

## First Deterministic Surfaces To Aim For

This should gradually become deterministic tooling such as:

- a normalized Penpot component extractor
- a Penpot-to-`FnHCI` mapping surface
- a `FnHCI` primitive catalog
- runtime adapters from `FnHCI` primitives into specific host/render targets

The key goal is:

- the mapping rules become reviewable and toolable
- they are not trapped inside one AI model or one Penpot GUI session

## Near-Term Next Question

The next concrete modeling step is probably:

- define the first small primitive catalog for `FnHCI.UI`

Likely first candidates include:

- `Button`
- `TextInput`
- `Label`
- `List`
- `Section`

with `Button` as the clearest first pressure point because it is already visible in both the recorded discussions and the current Penpot artifact.

## Related

- [FnHCI Namespace Map](fnhci-namespace-map.md)
- [FnUI Foundation](fnui-foundation.md)
- [FnHCI.UI.Blazor Requirements](fnhci-ui-blazor-requirements.md)
- [Penpot Working Loop For Event Modeling And FnHCI](penpot-working-loop.md)
- [Penpot Access And Structure](penpot-access-and-structure.md)
- [Event Modeling Tool Foundation](event-modeling-tool-foundation.md)
