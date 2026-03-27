# LaundryLog Foundation

LaundryLog is a real application domain, not only a test fixture.

It is also currently the best small proving ground for the platform work happening in NEXUS.

## Role

LaundryLog has two roles at once:

1. a real app you intend to use and share
2. a proving ground that helps shape NEXUS, FnHCI, and FnUI foundations

Those two roles should stay connected, but they should not be confused.

## Why LaundryLog Needs Its Own Area

LaundryLog now needs its own application-domain home because:

- it is a concrete product direction
- it has its own usage goals
- it has its own UI and workflow requirements
- it has its own convergence and sync concerns
- it should not remain only a side note under FnUI platform docs

## Current Understanding

From the imported discussion history so far, LaundryLog is currently understood as:

- a small laundry expense tracking application
- strongly mobile-friendly in use
- a good candidate for browser-first plus PWA packaging
- likely to benefit from optional cloud-backed capabilities
- a domain where offline use and later convergence matter

## Relationship To FnUI

LaundryLog should continue to act as the first proving ground for:

- small-screen view composition
- fast entry workflows
- PWA-capable delivery
- offline-first or offline-tolerant interaction
- later convergence across nodes

But LaundryLog should not define FnUI completely.

FnUI remains platform-level.
LaundryLog remains one concrete application domain using that platform.

## Relationship To Git Branching

LaundryLog is now substantial enough to justify its own branch line when active work begins.

Current branch guidance:

- keep FnUI platform evolution on `fnui-foundation`
- branch LaundryLog application work on `laundrylog-foundation`
- when LaundryLog depends on active, unmerged FnUI work, branch it from `fnui-foundation`
- periodically merge the active FnUI line into LaundryLog while both remain in motion

This keeps the history understandable:

- platform work remains platform work
- application work remains application work
- shared evolution is still visible through real merges

## Near-Term LaundryLog Requirements Pressure

LaundryLog currently pressures the platform in these ways:

- phone-friendly interaction
- few but important screens
- low-friction data entry
- optional hosted services
- offline use
- later convergence or sync
- likely PWA packaging

## Next LaundryLog Docs

The first concrete LaundryLog docs now live in:

- [`laundrylog/introduction.md`](laundrylog/introduction.md)
- [`laundrylog/requirements.md`](laundrylog/requirements.md)
- [`laundrylog/product.md`](laundrylog/product.md)
- [`laundrylog/domain-model.md`](laundrylog/domain-model.md)
- [`laundrylog/privacy-and-ownership.md`](laundrylog/privacy-and-ownership.md)
- [`laundrylog/delivery.md`](laundrylog/delivery.md)
- [`laundrylog/convergence.md`](laundrylog/convergence.md)
- [`laundrylog/data-sync-boundaries.md`](laundrylog/data-sync-boundaries.md)
- [`laundrylog/view-contracts.md`](laundrylog/view-contracts.md)
- [`laundrylog/screens.md`](laundrylog/screens.md)
- [`laundrylog/workflows.md`](laundrylog/workflows.md)

Current next code step after that:

- `Nexus.LaundryLog`
- `Nexus.LaundryLog.UI`
