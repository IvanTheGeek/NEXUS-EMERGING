# Penpot Working Loop For Event Modeling And FnHCI

This note captures the current NEXUS working hypothesis for how Penpot can be used productively without becoming the final source of truth.

The short version is:

- yes, Penpot can help us build screens
- yes, Penpot can help us lay out paths and click through them with prototypes
- yes, Penpot can likely serve as a useful visual canvas for Event Modeling diagrams
- no, Penpot alone should not become the lasting semantic source of truth

The deeper source of truth should still live in structured NEXUS, FORGE, and `FnHCI` surfaces.

## Why This Matters

There is an obvious catch-22 here:

- we want an Event Modeling and UI tool of our own
- we do not have that tool yet
- Penpot is good enough to help us move now
- but if we let Penpot own the meaning, we risk getting trapped inside the bridge tool

So the answer is not "Penpot or our own tool."

The answer is:

- use Penpot as a working canvas and bridge surface now
- keep the deeper language and semantics durable outside Penpot
- gradually turn the repeated useful parts into deterministic `FnTools` and FORGE surfaces

## Penpot's Useful Roles Right Now

Penpot can already serve several legitimate roles.

### Screen Authoring Surface

Penpot can hold:

- screen boards
- component libraries
- tokens and styling
- layout experiments
- variants and visual states

This is the most obvious current use.

### Prototype Surface

Penpot can also help us:

- wire board-to-board navigation
- click through a path screen by screen
- test whether the sequence feels right for a human
- compare the modeled path to the actual visual flow

This is especially useful for `PATH` work where a human needs to feel the flow, not just read about it.

### Event Modeling Canvas

Penpot should also be considered a candidate visual canvas for Event Modeling diagrams themselves.

That may include:

- `PATH` boards
- `CommandSlice` and `ViewSlice` layouts
- event chains
- supporting annotations
- multi-fidelity views of the same path

This does not mean Penpot is the final Event Modeling tool.

It means Penpot can hold visual working artifacts while NEXUS is still forming the dedicated tool.

### FnHCI Pressure Surface

Penpot can pressure the `FnHCI` design directly by making us answer:

- which parts are semantic primitives
- which parts are layout
- which parts are visual treatment
- which parts are runtime-specific
- which parts are only design-time convenience

That makes Penpot useful for finding the right abstraction level.

## The Important Boundary

The visual Penpot artifact should not be the only source of truth.

The current intended structure is:

1. business/domain truth
   commands, events, policies, views, and `PATH` semantics
2. `FnHCI` truth
   reusable interaction primitives and view composition semantics
3. Penpot visual working model
   boards, prototypes, components, variants, tokens, annotations
4. runtime implementations
   `FnUI`, Blazor, HTML, Android, iOS, and similar targets

That means:

- Penpot helps express and test the model
- Penpot does not own the model's deepest meaning
- prototypes help validate paths
- prototypes do not replace the path definition itself

## The Working Loop

The current dogfooding loop should probably be:

1. define or refine `PATH`, `CommandSlice`, `Event`, `ViewSlice`, and `View` in durable repo memory
2. represent the relevant screens and flow in Penpot
3. wire Penpot prototype links so the path can be clicked through
4. compare the Penpot path to the semantic path and reconcile mismatches
5. extract or define the reusable `FnHCI` primitives implied by the Penpot components
6. implement the `FnUI` runtime projection over those primitives
7. compare the resulting runtime output back to the Penpot design and the semantic path

This turns Penpot into a working collaborator surface instead of a hidden design island.

## What Penpot Should Help Us Learn

If we use it well, Penpot should help us discover:

- what the real stable screen states are
- what the real `PATH` transitions are
- which components are truly reusable
- which component variations are semantic versus merely stylistic
- which design ideas should become `FnHCI` primitives
- where the Event Modeling tool eventually needs better purpose-built behavior than Penpot can provide

## Suggested Penpot Areas

The current practical Penpot organization likely wants pages such as:

- `Components`
- `Screens`
- `PATHS`
- `EventModels`

And likely naming conventions such as:

- `Screen.NewSession`
- `Screen.EntryForm`
- `Path1.1-NewSession`
- `Path1.3-EntryForm`
- `Button.Option`
- `Button.Counter`

The exact conventions can evolve, but the principle should remain:

- names should help us map visual artifacts back to durable modeled concepts

## How This Connects To FnHCI

The goal is not to derive HTML or Blazor directly from arbitrary Penpot shapes.

The better direction is:

- Penpot components pressure the primitive design
- `FnHCI` owns the primitive model
- `FnUI` and other runtimes render those primitives
- Penpot remains a design/projection surface over the same deeper concepts

So if Penpot contains a `Button.Counter` component, that should help us discover:

- a likely `Button` primitive
- a likely counter-style or increment/decrement affordance
- a likely set of state and content concerns

without making the Penpot component itself the deepest truth.

## What Should Become Deterministic Later

Over time, this should become more toolable and less conversational.

Likely future deterministic surfaces include:

- Penpot board and component inspection helpers
- Penpot prototype extraction helpers
- Penpot-to-`PATH` or Penpot-to-`FnHCI` mapping helpers
- `FnHCI` primitive catalogs
- `FnUI` projection and comparison workflows
- eventually a dedicated Event Modeling tool that carries these concepts more natively than Penpot

That is exactly the kind of progression FORGE is meant to encourage.

## Current Rule Of Thumb

Use Penpot for:

- visualizing
- prototyping
- componentizing
- discussing
- pressuring the abstractions

Do not rely on Penpot alone for:

- semantic source of truth
- final path meaning
- business event definition
- long-term deterministic compiler behavior

## Related

- [FnHCI And Penpot Abstraction Boundary](fnhci-penpot-abstraction.md)
- [Event Modeling Tool Foundation](event-modeling-tool-foundation.md)
- [Penpot Access And Structure](penpot-access-and-structure.md)
- [FORGE Foundation](forge-foundation.md)
