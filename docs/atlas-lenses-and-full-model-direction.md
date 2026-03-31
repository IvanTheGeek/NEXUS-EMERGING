# ATLAS, Lenses, And The Fuller Model

This note records the current NEXUS direction behind ATLAS and why Event Modeling alone is not the whole model.

## The Practical Problem

One reason Event Modeling feels hard to adopt is that different people look at the same board with very different concerns in mind.

Examples:

- a developer is often thinking about implementation seams, runtime behavior, and integration points
- a UI or UX contributor is often thinking about screens, state changes, and component behavior
- a designer is often thinking about layout, tokens, and visual systems
- a customer or product owner is often thinking about task completion, friction, and desired outcome

If one board is expected to satisfy all of those at once, the board starts to feel overloaded and confusing.

## Current NEXUS Reading

Event Modeling still has a real place.

But it should be treated as:

- one bounded context
- one useful set of lenses
- one strong way to reason about business flow, commands, events, views, and paths

It should not be mistaken for the whole model of an app or system.

## The Fuller Model Direction

The broader direction is toward a fuller model that can accumulate multiple concern lines without pretending they are all the same thing.

Examples of concern lines that may all matter to one application:

- business flows
- business rules
- commands, events, views, and paths
- UI composition and component behavior
- information flow
- runtime and execution behavior
- target-device considerations
- design systems and tokens
- sample data and example scenarios
- customer feedback about how a task should work
- imported knowledge from existing repos and systems

These are not all one lens.

They are different views onto one deeper evolving model.

## Where ATLAS Fits

ATLAS is the direction for the environment where those concerns and lenses can be selected deliberately.

That means ATLAS should help with:

- choosing which concerns are visible right now
- suppressing irrelevant concerns for the current task
- letting multiple valid lenses exist over the same underlying model
- keeping those lenses related instead of forcing them into one flattened board

So the goal is not:

- one event model that somehow explains everything

The goal is more like:

- one deeper model
- many explicit lenses and projections
- selectable working environments over that model

## Why This Matters For Specs

If the fuller model becomes explicit enough, then the right spec surfaces can be derived from it.

Examples:

- path and scenario specs
- command/event/view specs
- UI and screen specs
- token and design-system specs
- runtime and integration specs
- test cases driven by sample data and example flows

Those specs should then be reviewable as their own surfaces rather than hidden inside one giant mixed model.

## Why This Matters For FORGE

FORGE should eventually work downstream from sufficiently explicit model and spec surfaces.

That means:

- the fuller model accumulates durable meaning
- selected lenses and projections produce reviewable specs
- FORGE turns those specs into code and later into running artifacts

This is the longer-term path toward rebuilding or generating application lines in F# or another chosen runtime without making any one current framework the permanent center of gravity.

## Current Practical Rule

We are not fully there yet.

For now:

- keep Event Modeling precise inside its own lens
- keep other concerns explicit instead of pretending Event Modeling already covers them
- keep Penpot, markdown, sample data, tokens, and code-facing models as related but distinct surfaces
- let those surfaces pressure the fuller model gradually

## Working Mantra

Event Modeling is one strong lens, not the whole world.

ATLAS is the direction for choosing and relating lenses over a fuller durable model.
