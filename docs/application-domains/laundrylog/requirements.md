# LaundryLog Requirements

This file is now the summary and entry point for LaundryLog requirements.

The detailed requirement notes are split into smaller focused docs so they can evolve more like a test suite or architecture spine instead of becoming one large catch-all page.

Primary source threads still include:

- [`019d174f-2ca0-75d3-9497-c66c25acbc78.toml`](../../../NEXUS-EventStore/projections/conversations/019d174f-2ca0-75d3-9497-c66c25acbc78.toml)
- [`019d174e-e9e9-7732-8fb0-053fb558797f.toml`](../../../NEXUS-EventStore/projections/conversations/019d174e-e9e9-7732-8fb0-053fb558797f.toml)
- [`019d174e-ea99-76b0-8ed6-1b124c5fe938.toml`](../../../NEXUS-EventStore/projections/conversations/019d174e-ea99-76b0-8ed6-1b124c5fe938.toml)
- [`019d174f-2cc5-7148-bae0-e699e5da62b3.toml`](../../../NEXUS-EventStore/projections/conversations/019d174f-2cc5-7148-bae0-e699e5da62b3.toml)

## Requirement Spine

Start with these focused requirement areas:

- [`product.md`](product.md)
- [`domain-model.md`](domain-model.md)
- [`privacy-and-ownership.md`](privacy-and-ownership.md)
- [`delivery.md`](delivery.md)
- [`convergence.md`](convergence.md)
- [`screens.md`](screens.md)
- [`workflows.md`](workflows.md)

## Summary

LaundryLog is currently understood as:

- a small real app, not only a platform proving ground
- a browser-first, mobile-friendly expense logger
- a strong candidate for PWA delivery
- a domain where offline use and later convergence matter early
- a product where low-friction entry matters more than broad feature breadth

## Current Core Requirement Themes

- quick expense capture on a phone
- enough context for later recordkeeping or tax use
- user ownership and privacy by default
- simple repetitive-entry workflow during one laundry outing
- room for optional hosted services without making them mandatory

## Still Open

These remain open decisions across the requirement set:

- exact sync or convergence mechanism
- exact payment-method detail level
- exact reporting/export surface
- exact hosted-service boundary for community location data
