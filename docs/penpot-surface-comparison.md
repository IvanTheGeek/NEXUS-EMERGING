# Penpot Surface Comparison

This note is the current comparison surface for Penpot work in NEXUS.

Use it when deciding whether a task should use:

- backend API
- MCP/plugin
- exported `.penpot` file

This is a derived working note, not the final source of truth.

The goal is practical:

- show what is currently available where
- make gaps visible
- keep future AI and human work from rediscovering the same surface differences repeatedly

## Update Rule

When a collaborator proves a capability, limitation, or incompatibility on one Penpot surface:

- update this note
- say whether it is verified, inferred, or still open
- do not silently let chat become the only place that comparison knowledge exists

## Current Comparison

| Concern | Backend API | MCP/plugin | Exported `.penpot` |
| --- | --- | --- | --- |
| Current live file identity | Verified | Verified | Snapshot only |
| Current live page identity | Verified | Verified | Snapshot only |
| Current selection | Not available | Verified | Not available |
| Current page / UI context | Not available | Verified | Not available |
| Page and object structure | Verified through file/page methods | Verified through live plugin code | Verified from archive contents |
| Full file snapshot export | Verified via `export-binfile` | Not primary surface | Native artifact |
| Board-level visual export | Not yet verified on backend path | Verified via `export_shape` | Not applicable |
| Whole-page visual export | Not yet verified on backend path | Not yet stable in current setup | Not applicable |
| Live mutation surface exists | Verified by API docs | Verified by plugin API/tool model | Not a live surface |
| Live mutation shown in open Penpot app | Inferred, not yet proven in recorded test | Inferred, not yet proven in recorded test | Not applicable |
| Plugin-context operations | Not available | Verified surface exists | Not available |
| Portability / offline inspection | Low | Low | High |

## Current Verified Backend API Findings

Verified useful methods include:

- `get-file`
- `get-page`
- `get-file-fragment`
- `get-project-files`
- `search-files`
- `export-binfile`
- `import-binfile`

Verified mutation-oriented methods are exposed in the docs:

- `update-file`
- `update-file-library-sync-status`
- `update-file-snapshot`

The current backend export behavior is:

1. call `export-binfile`
2. consume the SSE stream
3. wait for the final asset URI
4. download the resulting `.penpot` archive

## Current Verified MCP/Plugin Findings

Verified in the current local setup:

- the plugin can connect to the MCP server
- Codex can reach the live file through the MCP/plugin path
- live file/page enumeration works
- live board enumeration works
- board-level `export_shape` works
- whole-page `export_shape` is currently unreliable and should not be the default export attempt
- component-oriented live board editing works well through the plugin surface
- duplicating a component base, evolving the duplicate, and instantiating it back into the board is a practical live workflow
- for duplicated slice-card shells, directly editing inherited text nodes did not reliably show up in exported visuals
- the current reliable workaround is to keep the component shell and overlay fresh local text nodes for per-card variation
- inspect-style API surfaces are available through `generateStyle` and `generateMarkup`, and they are useful comparison surfaces when the visual export path is suspicious

Verified current live file example:

- `LaundryLog`
- pages:
  - `Components`
  - `Screens`
  - `PATHS`
  - `Event Model`

Current local operational note:

- the working local path currently uses `@penpot/mcp@2.15.0-rc.1.116`
- Codex uses `mcp-remote` against the Penpot `/sse` endpoint

That is a machine-local operational finding and should be re-verified in other environments.

## Current Text Mutation Lab Findings

The current `TextMutationLab.V1` in the live `LaundryLog` Penpot file established a sharper result set.

Verified outcomes:

- fresh non-component text exports correctly in both `png` and `svg`
- direct text edits on a component-derived slice instance were visible in live shape data but did not appear in exported visuals
- detaching that instance did not by itself make inherited text edits export correctly
- renaming the edited inherited text node did not make the export honor the changed text
- hiding the inherited text node and overlaying a fresh local text node did export correctly

So the current problem appears narrower than \"Penpot text is broken\".

The stronger current reading is:

- fresh local text is fine
- inherited text inside this component-derived slice shell is the unstable case for export

## Current Token Caveat

The current slice-title texts in this experiment had no token bindings.

That means the current overlay workaround preserved resolved visual style, but it did not prove token-preserving behavior.

So the current practical warning is:

- overlay text is acceptable as a current experiment workaround
- it should not automatically be treated as the final pattern for token-governed component systems
- for real reusable UI/component work, stay as high as possible on the ladder of:
  - component instance
  - variant/stateful component
  - detached instance
  - fully local overlay or one-off text

## Current Exported File Findings

The exported `.penpot` file is currently a ZIP archive containing readable JSON structure and related assets.

That makes it useful for:

- checkpointing
- offline inspection
- durable structural comparison
- sharing a stable snapshot

It is not the live backend state.

## Current Open Questions

- Can whole-page visual export be made reliable through the MCP/plugin path in the current stack?
- Which live mutation operations are easier through backend API versus plugin context?
- Do backend `update-file` mutations appear immediately in an open Penpot UI without manual refresh?
- Which operations are available only in plugin context and not via backend API?

## Current Mutation Comparison

The current comparison is no longer purely inferred.

### MCP / Plugin Path

Verified practical fit for:

- creating new slice-card structures
- duplicating component bases
- evolving those duplicates into new visual bases
- instantiating those bases back into the Event Model board

This is currently the stronger surface for component-driven board work.

### Backend API Path

Verified practical fit for:

- live file and page inspection
- export and checkpoint generation
- lower-level mutation surface exposure through `update-file`

But the current mutation seam is stricter and lower-level.

A first live `update-file` mutation attempt against a LaundryLog `ViewSlice` failed because the payload must satisfy Penpot's internal shape schema very precisely. In particular, enum-like stroke values were not accepted when passed as naive JSON strings.

So the current practical rule is:

- use MCP/plugin for live component-driven visual editing
- use backend API for inspection, export, and lower-level patch work once the exact schema contract is known

## Current LaundryLog Slice Proof

The current live LaundryLog file now includes first-pass recovered slice-language proofs built through the MCP/plugin path:

- `CommandSlice.Base.V2`
- `ViewSlice.Base.V2`

And visible PATH 1 instances derived from those bases:

- `PATH1: CommandSlice - Set Location (V2 Instance)`
- `PATH1: ViewSlice - Ready (V2 Instance)`

It now also includes a first readable `PATHS` page row projection:

- `PATH1.Row.V1`

Current practical characteristics of that row:

- one horizontal path row
- a short path description above the row
- detached slice-card shells derived from the V2 bases
- per-card content rendered through fresh overlay text nodes
- screen slots still acting as screenshot-like placeholders rather than full embedded screenshots

That proof used a recovered EM-1 slice-card language characterized by:

- narrow calm slice cards
- strong outer border and tinted header band
- small system pill and uppercase slice-type marker
- title plus short subtitle
- semantic row containers for `Screen`, `Command`, `Event`, and `View`
- stronger colored title bars with lighter structured data panels below

## Working Rule

For current Penpot work:

- treat backend API as the primary live state and export surface
- treat MCP/plugin as the primary current-file and live Penpot-context surface
- treat exported `.penpot` files as explicit checkpoints
- do not default to whole-page `export_shape`; prefer board-level or shape-level export until page export is re-verified
- if duplicated component text does not render reliably, prefer overlay text on the detached shell instead of assuming inherited text edits are authoritative
- keep this comparison note current as new findings emerge
