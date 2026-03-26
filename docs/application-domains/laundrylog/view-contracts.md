# LaundryLog View Contracts

This note captures the first code-facing view shape for LaundryLog.

## Purpose

LaundryLog is the first concrete application domain riding on top of the FnHCI and FnUI shell work.

That means the first view contracts should be:

- small
- explicit
- stable enough to test
- close to the currently understood path states

## Current Active Views

The first app shell should expose:

- `new-session`
- `entry-form`

These map to the currently known path states:

- `new-session`
  - `Path1.1-NewSession`
  - `Path1.2-LocationEntered`
- `entry-form`
  - `Path1.3-EntryForm`

## Current Shell Regions

The first LaundryLog shell should stay minimal:

- navigation
- workspace
- status bar

Inspector- and command-heavy regions can come later if the real app starts needing them.

## Current View Intent

### `new-session`

Purpose:

- establish laundry location context
- support manual location-first flow
- keep GPS optional and future-facing

### `entry-form`

Purpose:

- log one expense entry in the current session
- support repeat entry within the same outing
- keep running session total visible

## Code Boundary

The first code boundary for this should live in:

- `Nexus.LaundryLog`
- `Nexus.LaundryLog.UI`

Those domain-specific contracts should depend on the renderer-neutral FnUI shell, not on Blazor implementation details directly.
