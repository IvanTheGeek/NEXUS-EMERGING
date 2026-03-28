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

## Working Rule

For current Penpot work:

- treat backend API as the primary live state and export surface
- treat MCP/plugin as the primary current-file and live Penpot-context surface
- treat exported `.penpot` files as explicit checkpoints
- do not default to whole-page `export_shape`; prefer board-level or shape-level export until page export is re-verified
- keep this comparison note current as new findings emerge
