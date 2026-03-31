# CheddarBooks LaundryLog Workflows

These notes describe the first concrete LaundryLog workflows emerging from the imported discussion history.

Primary source threads include:

- [`019d174e-e9e9-7732-8fb0-053fb558797f.toml`](https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/d0da2b9d1130c85ede291ede2cf0a7863653fd25/NEXUS-EventStore/projections/conversations/019d174e-e9e9-7732-8fb0-053fb558797f.toml)
- [`019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml`](https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/d0da2b9d1130c85ede291ede2cf0a7863653fd25/NEXUS-EventStore/projections/conversations/019d174e-ea7b-71ca-91ea-5f3ad56b32fd.toml)
- [`019d174e-ea99-76b0-8ed6-1b124c5fe938.toml`](https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/d0da2b9d1130c85ede291ede2cf0a7863653fd25/NEXUS-EventStore/projections/conversations/019d174e-ea99-76b0-8ed6-1b124c5fe938.toml)
- [`019d174f-2cc5-7148-bae0-e699e5da62b3.toml`](https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/d0da2b9d1130c85ede291ede2cf0a7863653fd25/NEXUS-EventStore/projections/conversations/019d174f-2cc5-7148-bae0-e699e5da62b3.toml)

## Workflow Framing

LaundryLog is not trying to model a large business process.

It is trying to support one very practical real-world workflow:

- arrive at a location
- do laundry over time
- log expenses as they happen
- retain enough context for later recordkeeping

## Workflow 1: Start Session With Manual Location

Current first path:

1. open app
2. enter location manually
3. reveal `Set Location` once the location is present
4. confirm location
5. move into the entry form

This is the current happy-path foundation.
GPS lookup is intentionally deferred for the first path.

## Workflow 2: Log One Expense Entry

Current line-entry workflow:

1. choose expense type:
   - washer
   - dryer
   - supplies
2. enter quantity
3. enter unit price
4. choose payment method
5. review calculated line total
6. log the expense

This should work for the most common case with as little friction as possible.

## Workflow 3: Repeat Entry During One Laundry Session

Current understanding is that a laundry session often lasts for multiple hours and includes multiple entries at the same location.

That means the workflow is:

1. set location once
2. log first expense
3. remain in the same location/session context
4. log additional expenses over time
5. watch session total accumulate

This is why batch-style entry is more important than a separate single-entry mode.

## Workflow 4: Batch Entry As The Primary Mode

The current direction is:

- no separate single-entry mode is required
- the batch-oriented flow should also handle the single-entry case

This works because:

- quantity can default to `1`
- the same entry form can be used once or many times
- the user does not need to choose between two mental modes

## Workflow 5: Offline Use

LaundryLog should support real-world offline use.

Examples already considered:

- phone offline at the laundromat or truck stop
- desktop offline elsewhere
- later need to reconcile the same logical activity across devices

This means the workflow model must allow:

- local entry while disconnected
- later convergence
- no requirement for constant server contact just to log an expense

## Workflow 6: Optional Hosted Assistance

The current conversations leave room for optional hosted services, especially around shared location knowledge.

That means:

- local use should stand on its own
- hosted services can enrich the experience
- hosted services should not be required just to keep the app useful

## Current Path Vocabulary

The current path work already suggests a first sequence:

- `Path1.1-NewSession`
- `Path1.2-LocationEntered`
- `Path1.3-EntryForm`

That vocabulary is useful because it links:

- workflow
- screen state
- event-model thinking

## Workflow Requirements Summary

LaundryLog workflows should be:

- mobile-friendly
- low-friction
- location-first
- repeat-entry friendly
- offline-tolerant
- later-convergence aware
- consistent with event-model thinking without forcing unnecessary complexity on the user
