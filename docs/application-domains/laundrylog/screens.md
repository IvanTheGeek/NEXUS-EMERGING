# LaundryLog Screens

These notes describe the first concrete LaundryLog screen surfaces and screen states currently visible in the imported design and workflow discussions.

Primary source threads include:

- [`019d174e-e9e9-7732-8fb0-053fb558797f.toml`](../../../NEXUS-EventStore/projections/conversations/019d174e-e9e9-7732-8fb0-053fb558797f.toml)
- [`019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml`](../../../NEXUS-EventStore/projections/conversations/019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml)

## Screen Model

Current direction is to treat each meaningful visual state as a full screen or board, built from reusable components.

That means screen work should stay aligned with:

- path states
- user decision milestones
- reusable component composition

## Current Named Screens

### Screen.NewSession

Purpose:

- begin a laundry outing by establishing location context

Current expected elements:

- `Input.Location`
- `Button.GPS`
- `Button.SetLocation`

Important state behavior:

- `Set Location` hidden when there is no draft location
- `Set Location` visible when a location has been entered
- GPS is future-facing and should not block the first manual path

### Screen.EntryForm

Purpose:

- capture one expense entry inside the current location/session context

Current likely elements:

- location context display
- machine type selection
- quantity control
- unit price entry
- payment method selection
- session total bar
- log or save entry action

## Core Visual Sections

### Location Section

Current direction:

- location is the first or top-most information
- session context depends on location
- location should remain obvious once set

### Machine Type Section

Current supported choices:

- washer
- dryer
- supplies

Current UX direction:

- the choices are visually obvious enough that a heavy section header may not be necessary
- the buttons themselves can carry the meaning

### Quantity Section

Current direction:

- `+/-` controls for fast changes
- direct tap on the numeric value for keypad entry
- controls sized for touch use

### Unit Price Section

Current direction:

- direct numeric entry
- optional adjustment helpers
- auto-calculated line total from quantity × unit price

### Payment Section

Current direction:

- keep the initial surface simple
- reveal more detail only when needed
- card-specific options should appear only after card is selected

### Session Total Surface

Current direction:

- running session total should remain visible and easy to scan
- it should not feel secondary to the line-entry controls

## Screen State Progression

Current screen-state thinking includes at least:

1. New session, no location yet
2. Location entered, ready to set
3. Entry form active under chosen location
4. Repeated entry within the same ongoing outing

## Future Likely Screens

The current conversations suggest these are likely next surfaces, even if not fully defined yet:

- session review
- entry history
- reporting/export surfaces
- settings/preferences

## Screen Requirements Summary

LaundryLog screens should be:

- mobile-friendly
- compact
- touch-usable
- state-explicit
- component-based
- aligned to real workflow milestones rather than arbitrary CRUD pages
