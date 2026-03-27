# CheddarBooks Foundation

CheddarBooks is a division under Cheddar, not only a single app.

It is currently the home for the first concrete tool-app work branching out from the NEXUS and FnUI foundations.

## Role

CheddarBooks currently has two roles at once:

1. a real domain and branding line for practical finance and recordkeeping tools
2. a proving ground that helps shape NEXUS, FnHCI, and FnUI foundations through the first concrete tool apps

Those two roles should stay connected, but they should not be confused.

## Why CheddarBooks Needs Its Own Area

CheddarBooks now needs its own application-domain home because:

- it is a real division under the broader Cheddar line
- it is a concrete product and branding direction
- it can hold more than one concrete tool app or support flow
- it has its own usage goals and user-facing identity
- it should not remain only a side note under FnUI platform docs

## Current Understanding

From the imported discussion history so far, the current first concrete tool app under CheddarBooks is LaundryLog, currently understood as:

- a small laundry expense tracking application
- strongly mobile-friendly in use
- a good candidate for browser-first plus PWA packaging
- likely to benefit from optional cloud-backed capabilities
- a domain where offline use and later convergence matter

The imported history also already points to:

- `PerDiemLog` as the next likely CheddarBooks tool app
- the eventual flagship `CheddarBooks` app itself as the broader bookkeeping and accounting target

## Relationship To FnUI

CheddarBooks, through LaundryLog first, should continue to act as a proving ground for:

- small-screen view composition
- fast entry workflows
- PWA-capable delivery
- offline-first or offline-tolerant interaction
- later convergence across nodes

But the CheddarBooks area should not define FnUI completely.

FnUI remains platform-level.
CheddarBooks remains an application domain using that platform, and LaundryLog remains the first concrete tool app inside it.

## Relationship To Git Branching

CheddarBooks is now substantial enough to justify its own branch line when active work begins.

Current branch guidance:

- keep FnUI platform evolution on `fnui-foundation`
- keep the CheddarBooks domain and shared branding line on `cheddarbooks-foundation`
- branch concrete tool-app work such as LaundryLog from `cheddarbooks-foundation`
- when CheddarBooks depends on active, unmerged FnUI work, branch it from `fnui-foundation`
- periodically merge the active FnUI line into CheddarBooks while both remain in motion

This keeps the history understandable:

- platform work remains platform work
- domain/brand work remains domain/brand work
- individual tool-app work remains individual tool-app work
- shared evolution is still visible through real merges

## Near-Term Pressure

LaundryLog currently pressures the platform and the CheddarBooks domain in these ways:

- phone-friendly interaction
- few but important screens
- low-friction data entry
- optional hosted services
- offline use
- later convergence or sync
- likely PWA packaging

## Current CheddarBooks Docs

The current CheddarBooks docs now live in:

- [`cheddar/README.md`](cheddar/README.md)
- [`cheddarbooks/README.md`](cheddarbooks/README.md)
- [`cheddarbooks/laundrylog/introduction.md`](cheddarbooks/laundrylog/introduction.md)
- [`cheddarbooks/laundrylog/requirements.md`](cheddarbooks/laundrylog/requirements.md)
- [`cheddarbooks/laundrylog/product.md`](cheddarbooks/laundrylog/product.md)
- [`cheddarbooks/laundrylog/domain-model.md`](cheddarbooks/laundrylog/domain-model.md)
- [`cheddarbooks/laundrylog/privacy-and-ownership.md`](cheddarbooks/laundrylog/privacy-and-ownership.md)
- [`cheddarbooks/laundrylog/delivery.md`](cheddarbooks/laundrylog/delivery.md)
- [`cheddarbooks/laundrylog/convergence.md`](cheddarbooks/laundrylog/convergence.md)
- [`cheddarbooks/laundrylog/data-sync-boundaries.md`](cheddarbooks/laundrylog/data-sync-boundaries.md)
- [`cheddarbooks/laundrylog/view-contracts.md`](cheddarbooks/laundrylog/view-contracts.md)
- [`cheddarbooks/laundrylog/screens.md`](cheddarbooks/laundrylog/screens.md)
- [`cheddarbooks/laundrylog/workflows.md`](cheddarbooks/laundrylog/workflows.md)

Current next code step after that:

- `CheddarBooks.LaundryLog`
- `CheddarBooks.LaundryLog.UI`
