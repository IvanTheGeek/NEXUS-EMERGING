# FnTools Foundation

FnTools is an orthogonal tool and branding line.

It is not under Cheddar.

FnTools is where the more technical, developer-facing, operator-facing, and reusable tooling work belongs.

## Scope

FnTools is concerned with things like:

- reusable API libraries
- MCP servers and tooling
- protocol and integration helpers
- networking and OpenWrt-oriented tools
- other technical tools that are valuable to developers and operators even when they are not end-user finance apps

## Current Surfaced Directions

From the imported discussion history so far, the current known directions include:

- `FnAPI.Penpot`
  a reusable Penpot API library
- `FnMCP.Penpot`
  a higher-level MCP/tooling line over that library
- Event Modeling tool work, likely using Penpot as an early integration seam rather than the final product boundary
- broader MCP and technical tooling work
- networking-focused tools with interest in OpenWrt and related operator concerns

## Relationship To Cheddar

FnTools is not a sub-brand of Cheddar.

The relationship is:

- both can use NEXUS foundations
- both can use FnHCI/FnUI where that makes sense
- both can share engineering conventions
- but they are distinct product/tool lines with different audiences and goals

## Relationship To NEXUS

NEXUS remains the foundation workspace.

FnTools is one concrete line of work built on those foundations, just as Cheddar and CheddarBooks are.

## Branch Guidance

When the line is active, keep it visible as its own workstream.

Typical branches include:

- `fntools-foundation`
- `penpot-integration`
- `event-modeling-tool-foundation`
- `openwrt-tooling`

## Related

- [`README.md`](README.md)
- [`docs/repository-concern-lines.md`](repository-concern-lines.md)
- [`docs/event-modeling-tool-foundation.md`](event-modeling-tool-foundation.md)
- [`docs/penpot-access-and-structure.md`](penpot-access-and-structure.md)
