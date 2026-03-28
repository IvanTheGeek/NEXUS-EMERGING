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

## Current Component State Lab Findings

The current `ComponentStateTokenLab.V1` on the live `LaundryLog` `PATHS` page narrowed the problem further.

Verified outcomes:

- `Button.Option` instance text edits exported correctly
- `Button.Option` detached-instance text edits also exported correctly
- `Input.Text` instance text edits exported correctly
- `Input.Text` detached-instance text edits also exported correctly
- a fresh overlay text node exported correctly once it was given an explicit visible size instead of remaining at a `1x1` default

The stronger current reading is now:

- the export/render problem is not a universal \"component text\" failure
- it appears narrower, and the current leading suspect is the inherited text inside the recovered slice-card shells
- detached instances are still a valid escape hatch when we intentionally want to break inheritance
- but normal reusable component state work should stay higher on the ladder:
  - component instance
  - variant or explicit component state
  - detached instance
  - fresh local overlay or one-off text

The current strongest structural clue came from comparing `generateMarkup` results:

- for the problematic slice shell, `generateMarkup(type = "html")` included both:
  - live edited HTML text nodes
  - stale base-value text inside embedded SVG fragments
- for simpler controls like `Button.Option` and `Input.Text`, the generated HTML did not show that same mixed HTML plus stale-SVG pattern
- `generateMarkup(type = "svg")` for the slice shell followed the stale base-value path, which matches the broken visual export

So the current working explanation is:

- the slice shell is traveling through a mixed markup path
- the stale embedded-SVG side is winning in the final export surface
- this is why live shape data can look correct while exported visuals still show the base component text

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

The current token-application probe added one more caution:

- `shape.applyToken(...)` currently threw a Penpot-side `check error` in this live plugin path
- `token.applyToShapes(...)` completed in a first plain-text probe, but did not produce the expected visible token-binding evidence

So the current working rule is:

- treat token application through the current live plugin path as still under investigation
- do not yet assume that a successful-looking token call means the token contract is durably bound and inspectable
- re-verify token behavior explicitly before we lean on it for component-generation workflows

## Current Upstream Evidence

Penpot community and GitHub history show that nearby component-override problems are real and recurring, even if the exact current slice-shell export issue has not yet been matched one-for-one.

Currently relevant upstream references:

- Penpot Community: [Problem with components override](https://community.penpot.app/t/problem-with-components-override/6067)
  - users reported that once instance text changed, later component style updates did not propagate as expected
- Penpot Community: [Updating component style shouldn't require reseting text](https://community.penpot.app/t/updating-component-style-shouldnt-require-reseting-text/8480)
  - users reported that changed text could block later text-style propagation
  - one participant noted that tokens helped with color propagation but not font changes
  - the thread later reported a fix/improvement in Penpot `2.10`
- Penpot Community: [Components Overrides](https://community.penpot.app/t/components-overrides/8800)
  - users reported that switching a button variant could drop text overrides and revert to the master label
- GitHub: [bug: component "broken" after I switch variant - requires reset overrides #7931](https://github.com/penpot/penpot/issues/7931)
  - open issue describing variant switching that only looks correct after `Reset Overrides`
- GitHub: [feature: Enhance Plugin API with Component Properties #7518](https://github.com/penpot/penpot/issues/7518)
  - open design-to-code request that explicitly calls out missing variant and token detail in the plugin API surface
- GitHub: [bug: exporting changes the font of the content #6658](https://github.com/penpot/penpot/issues/6658)
  - adjacent exporter issue showing that export/render differences are a real upstream concern, especially on self-hosted setups

Current interpretation:

- override and variant behavior are definitely known Penpot problem areas
- exporter mismatches are also a known Penpot problem area
- we have not yet found a published upstream report that exactly matches:
  - \"edited inherited text inside this slice-card shell looks changed in live shape data but exports with the old base text\"
- until that exact match is found, keep our local lab result recorded as a verified repo finding rather than pretending the upstream evidence is exact

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
