# CheddarBooks LaundryLog FnUI Proving Ground

CheddarBooks LaundryLog is the first strong concrete proving ground for the `FnUI` line.

It is not the whole reason FnUI exists, but it is an excellent first application for shaping the requirements.

## Why CheddarBooks LaundryLog Fits

The recorded discussions already point to LaundryLog as a very good first proving ground inside the CheddarBooks domain:

- [`019d174e-ea99-76b0-8ed6-1b124c5fe938.toml`](../NEXUS-EventStore/projections/conversations/019d174e-ea99-76b0-8ed6-1b124c5fe938.toml)
- [`019d174f-2c81-71f4-9610-205e18960e01.toml`](../NEXUS-EventStore/projections/conversations/019d174f-2c81-71f4-9610-205e18960e01.toml)
- [`019d174e-e9e9-7732-8fb0-053fb558797f.toml`](../NEXUS-EventStore/projections/conversations/019d174e-e9e9-7732-8fb0-053fb558797f.toml)
- [`019d174f-2cc5-7148-bae0-e699e5da62b3.toml`](../NEXUS-EventStore/projections/conversations/019d174f-2cc5-7148-bae0-e699e5da62b3.toml)

The current signal from that history is:

- LaundryLog is small in domain scope
- it has relatively few screens
- it is mobile-friendly and event-heavy
- it is already the learning and dogfooding app
- it raises real offline and convergence concerns early

## Current Understanding

From the current recorded discussions, LaundryLog appears to be:

- a mobile-friendly laundry expense tracker
- aimed especially at drivers and similar real-world mobile use
- suitable for individual, family, or household use
- likely to benefit from optional hosted services rather than requiring them

## Why It Helps FnUI

CheddarBooks LaundryLog is a good proving ground because it pressures the right parts of the UI line:

- mobile-first where that truly makes sense
- fast entry
- few screens
- multiple visual states on one screen
- event-heavy workflows
- offline pressure
- later sync or convergence pressure

It is small enough to stay understandable while still being real enough to expose architectural mistakes.

## Required LaundryLog Pressure On FnUI

LaundryLog, as a CheddarBooks tool app, should pressure the FnUI stack in at least these ways:

1. browser-first phone experience
2. PWA-capable packaging
3. room for optional native host exploration later
4. offline entry support
5. later convergence across devices or nodes
6. small-screen-friendly view composition
7. low-friction data entry

## Convergence Direction

The current thinking fits well with:

- local-first entry
- optional cloud-backed capabilities
- later convergence across nodes

The exact convergence mechanism is still open, but the requirement pressure is already visible:

- a phone may be offline
- a desktop may also be offline
- the same logical activity may later need convergence

That means CheddarBooks LaundryLog is a good forcing function for FnUI, not just a demo.

## Relationship To FnHCI.UI.Blazor

CheddarBooks LaundryLog should influence the requirements by pressure-testing:

- the web/PWA path
- the host boundary
- the offline model
- the state/view seams

It should not prematurely force the whole FnUI line to become LaundryLog-specific.

## Current Token Pressure

LaundryLog is now also the first concrete proving ground for the token-model direction:

- [FnHCI.UI Token Model](fnhci-ui-token-model.md)
- [LaundryLog FnHCI.UI Token Vocabulary](fnhci-ui-laundrylog-token-vocabulary.md)

The current Penpot file is already strong enough to pressure:

- foundation color naming
- semantic button and input tokens
- typography tiers
- mobile-first sizing and spacing
- the first explicit breakpoint axis value
