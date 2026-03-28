# 0024: Build Our Own Event Modeling Tool And Use Penpot Transitionally

## Status

Accepted

## Context

NEXUS is now working more directly in Event Modeling language and workflows.

Practical use of currently available external Event Modeling tools has proven too painful for the way this work actually needs to proceed. The tools are not fitting the slice-by-slice, path-driven, reviewable workflow we want to use across NEXUS, FnTools, and downstream apps such as CheddarBooks.

At the same time, Penpot remains attractive as a near-term design and automation surface because:

- it can provide a visual canvas that is already useful for modeling and collaboration
- a local instance can expose direct API access
- Penpot plugin and MCP access may provide a workable bridge for automation and experimentation

## Decision

NEXUS should build its own Event Modeling tool rather than treating an external Event Modeling product as the long-term home for that workflow.

Penpot should be treated as a transitional and interoperable surface, not the final Event Modeling system.

The intended shape is:

- NEXUS records the doctrine, modeling language, and durable rules
- FORGE pushes repeated Event Modeling work toward more deterministic, reviewable surfaces
- FnTools hosts the reusable tooling and integrations that support this direction
- local Penpot can be used as a near-term canvas and integration seam through API and plugin/MCP access

## Consequences

- Event Modeling work should keep being written into durable repo docs even when visual tooling is used
- NEXUS should not depend on one external Event Modeling product for its long-term modeling workflow
- Penpot integration work belongs to the reusable tooling line, especially `FnAPI.Penpot` and `FnMCP.Penpot`
- the Event Modeling tool we build should aim for deterministic, reviewable, compiler-like behavior where the rules are explicit enough
- future automation should prefer inspectable transforms over opaque prompt-only steps

## Notes

This decision does not reject Penpot. It clarifies its role.

Penpot is useful as an immediate canvas and integration surface. The Event Modeling tool itself is still a NEXUS/FORGE/FnTools responsibility.
