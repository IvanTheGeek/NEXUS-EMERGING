# LaundryLog Requirements

These are the first concrete requirements for LaundryLog as an application domain.

They are grounded in the current imported conversation history, especially:

- [`019d174f-2ca0-75d3-9497-c66c25acbc78.toml`](../../../NEXUS-EventStore/projections/conversations/019d174f-2ca0-75d3-9497-c66c25acbc78.toml)
- [`019d174e-e9e9-7732-8fb0-053fb558797f.toml`](../../../NEXUS-EventStore/projections/conversations/019d174e-e9e9-7732-8fb0-053fb558797f.toml)
- [`019d174e-ea99-76b0-8ed6-1b124c5fe938.toml`](../../../NEXUS-EventStore/projections/conversations/019d174e-ea99-76b0-8ed6-1b124c5fe938.toml)
- [`019d174f-2cc5-7148-bae0-e699e5da62b3.toml`](../../../NEXUS-EventStore/projections/conversations/019d174f-2cc5-7148-bae0-e699e5da62b3.toml)

## Product Framing

LaundryLog is a simple app for recording laundry expenses, especially for drivers and others who need to keep track of those costs for IRS deductions.

It is not primarily about "doing laundry" as a domain in itself.
It is about quickly capturing expense facts in a context where receipts are often missing or inconvenient.

## Target Users

Current likely users include:

- truck drivers and professional drivers
- people doing laundry away from home
- individuals or households who want a lightweight expense log

## Product Goals

LaundryLog should:

- make expense entry quick on a phone
- make missing receipts less of a problem
- capture enough detail to support later tax or recordkeeping use
- stay simple and understandable
- preserve user privacy and ownership

## Core Domain Language

The current domain language should prefer:

- command: `LogLaundryExpense`
- event: `LaundryExpenseLogged`

The current understanding is that:

- the expense is the key business fact
- washer, dryer, and supplies are expense types, not separate event kinds
- session context is useful in the UI and workflow, but is not more important than the expense fact itself

## Functional Requirements

LaundryLog must support:

1. setting or confirming a laundry location
2. logging expenses by type:
   - washer
   - dryer
   - supplies
3. entering quantity
4. entering unit price
5. calculating entry total from quantity and unit price
6. capturing payment method
7. showing running session total
8. logging multiple entries during one laundry outing

## UX Requirements

LaundryLog should favor:

- low-friction mobile entry
- touch-friendly controls
- progressive disclosure where it reduces clutter
- defaults that speed repetitive entry
- compact but readable presentation

Specific current UI pressure includes:

- quantity controls that support both quick taps and direct numeric entry
- location first in the flow
- machine type choices that are obvious without heavy labeling
- session total always easy to notice

## Privacy And Ownership Requirements

LaundryLog should align with the wider app principle that users retain control of their data.

That means the app should remain compatible with:

- local-first use
- offline entry
- optional hosted services rather than mandatory cloud dependence

## Delivery Requirements

Current direction:

- browser-first
- mobile-friendly
- likely PWA packaging

Optional native-host exploration may come later, but it should not be required for the first useful product.

## Convergence Requirements

LaundryLog must leave room for later convergence across devices or nodes.

The exact mechanism is not settled yet.

Current direction:

- local-first capture
- later sync or convergence
- Git is a plausible convergence seam to explore, but not yet a locked requirement

## Deferred Or Open Decisions

Not yet settled:

- exact sync/convergence mechanism
- exact payment-method detail level
- exact reporting/export surface
- exact hosted-service boundary for community location data
