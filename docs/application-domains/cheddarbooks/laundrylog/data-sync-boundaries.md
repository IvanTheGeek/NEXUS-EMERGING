# CheddarBooks LaundryLog Data And Sync Boundaries

This note captures the first important data boundaries for LaundryLog.

## Why This Matters Early

LaundryLog is small, but it immediately pressures:

- offline entry
- later convergence across devices
- user ownership of expense data
- optional shared or hosted enrichment

Those concerns should stay explicit before we guess at one sync mechanism.

## Data Boundary 1: Personal Expense Log

The personal expense log is the primary application data.

It includes:

- laundry location chosen by the user
- expense entries
- quantities
- prices
- payment-method detail
- session totals

Current rule:

- this data belongs to the user first
- it should remain useful locally
- convergence must not require surrendering ownership

## Data Boundary 2: Local Session State

LaundryLog also has short-lived local state such as:

- draft location text
- in-progress entry values
- current session context
- current screen or path state

This state is important to UX, but it is not identical to the durable expense log.

Current rule:

- transient UI state should be allowed to stay local
- only the durable business facts need long-term convergence

## Data Boundary 3: Shared Location Knowledge

LaundryLog may later benefit from optional shared knowledge such as:

- community-known laundromat names
- location hints
- saved common locations
- other hosted enrichment

Current rule:

- shared location knowledge is secondary enrichment
- it should not become a mandatory dependency for personal expense logging

## Data Boundary 4: Hosted Services

Hosted services may later provide:

- sync or convergence help
- shared location catalogs
- reporting or export assistance

Current rule:

- hosted services must remain optional for the first useful product
- personal logging must still make sense without them

## Current Sync Guidance

Current direction remains:

- local-first capture
- later convergence
- Git is still a plausible seam to explore
- Git is not yet a locked product decision

## Current Safety Rule

LaundryLog should avoid collapsing these into one bucket:

- personal durable facts
- transient local UI state
- optional shared knowledge
- hosted-system state

Those may converge differently and should not be forced into the same storage or sync path prematurely.
