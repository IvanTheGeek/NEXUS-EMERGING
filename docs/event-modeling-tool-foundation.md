# Event Modeling Tool Foundation

NEXUS now has a clearer direction for Event Modeling tooling:

- external Event Modeling tools are useful references, but they are not sufficient as the primary working surface
- the long-term answer is to build our own Event Modeling tool
- Penpot is currently the most promising near-term canvas and integration seam

## Why Build Our Own

The working style emerging in NEXUS wants:

- slice-by-slice progression
- path-first modeling
- durable repo memory alongside the visual model
- deterministic, reviewable behavior where possible
- automation surfaces that humans and AI can both inspect and improve

Current external tools have not fit that well enough.

## Near-Term Penpot Role

Local Penpot is valuable because it may give us:

- a usable visual surface while the dedicated tool does not exist yet
- direct API access
- plugin and MCP-oriented experimentation
- a realistic target for early automation and synchronization work

Penpot is therefore a bridge surface, not the final product boundary.

## Where The Work Belongs

- `NEXUS`
  doctrine, language, durable decisions, and architecture memory
- `FORGE`
  the push toward deterministic, compiler-like, reviewable modeling surfaces
- `FnTools`
  reusable integration and tool lines such as `FnAPI.Penpot`, `FnMCP.Penpot`, and later the Event Modeling tool itself or its supporting libraries
- downstream app repos
  proving grounds that pressure the model with real domains

## First Practical Direction

1. Continue modeling in repo docs so the language and slices stay durable.
2. Bring up local Penpot and verify API and plugin/MCP access.
3. Define the first concrete synchronization or automation seams we actually want.
4. Turn repeated useful workflows into deterministic tool surfaces.
5. Evolve toward a dedicated Event Modeling tool that better matches NEXUS working style.

## Related

- [FORGE Foundation](forge-foundation.md)
- [FnTools Foundation](fntools-foundation.md)
- [0022: Functionalize Repeatable Work Into Deterministic Surfaces](decisions/0022-functionalize-repeatable-work-into-deterministic-surfaces.md)
- [0024: Build Our Own Event Modeling Tool And Use Penpot Transitionally](decisions/0024-build-our-own-event-modeling-tool-and-use-penpot-transitionally.md)
