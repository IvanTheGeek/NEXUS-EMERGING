# Penpot Live Backend And Export

This note records the current practical rule for working with self-hosted Penpot in NEXUS-oriented work.

## Current Rule

When Penpot is running locally, the live backend should be treated as the primary current state.

An exported `.penpot` file should be treated as a snapshot or checkpoint artifact.

That means:

- live edits change backend state
- live edits do not automatically rewrite a previously exported `.penpot` file
- a fresh `.penpot` export is needed when we want a new portable snapshot

## Current Surfaces

The useful surfaces are:

- live backend API
- MCP/plugin connection
- exported `.penpot` archive

The practical split is:

- backend API
  primary live state and automation surface
- MCP/plugin
  live Penpot-context surface for current file, current page, selection, and plugin-driven operations
- exported `.penpot`
  portable artifact for snapshot inspection, sharing, and offline analysis

## Current Local Backend Shape

For the current local Docker-based Penpot stack:

- project/application state lives in Postgres
- assets live in the Penpot objects storage volume

So the exported `.penpot` file is not the live database.

It is a produced export artifact.

## Current API Findings

The local Penpot backend currently exposes useful authenticated API methods including:

- `get-file`
- `get-page`
- `get-file-fragment`
- `get-project-files`
- `search-files`
- `export-binfile`
- `import-binfile`

That is enough to treat the backend as a real automation surface for future `FnAPI.Penpot` / `FnMCP.Penpot` work.

The backend docs also expose mutation-oriented methods including:

- `update-file`
- `update-file-library-sync-status`
- `update-file-snapshot`

So the backend is not only a read/export surface.

## Current Export Behavior

The `export-binfile` method does work, but it is not a direct one-shot ZIP response.

Current observed behavior:

1. call `export-binfile`
2. receive a `text/event-stream` response
3. observe `progress` events while the export is assembled
4. receive a final `end` event containing an asset URI
5. download the resulting asset URI to obtain the `.penpot` ZIP archive

So a proper automated export flow is:

1. authenticated export request
2. consume the SSE stream
3. capture the final asset URI
4. download the asset
5. store it as the export snapshot

## Authentication Rule

The backend requires authentication for these operations.

Use an access token or authenticated session cookie.

Do not store token values in Git repos.

Machine-local secrets should stay outside repos.

## Current MCP Limitation

The official Penpot MCP architecture is:

- AI client connects to the MCP server
- Penpot MCP plugin connects to that same server via WebSocket
- the server relays tool execution into the Penpot Plugin API inside the live design file

That is the intended model.

The originally tried `@penpot/mcp@2.14.0` setup did hit a transport limitation in practice.

Observed effect on that version:

- if the Penpot plugin is already connected
- Codex MCP access may fail with an `Already connected to a transport` error
- restarting the local MCP server does not currently clear that failure reliably

## Current Local MCP Working State

The local MCP path is now working with the following machine-local operational shape:

- `@penpot/mcp@2.15.0-rc.1.116`
- Codex configured with a stdio MCP bridge:
  `npx -y mcp-remote http://localhost:4401/sse --allow-http --transport sse-only --debug`

This is an operational finding for the current machine and current Penpot setup.

It should not be treated as a permanent repo-wide invariant without re-verification.

Current verified MCP/plugin results:

- live file access works
- live page enumeration works
- live board enumeration works
- board-level `export_shape` works

Current verified example:

- file: `LaundryLog`
- pages:
  - `Components`
  - `Screens`
  - `PATHS`
  - `Event Model`

Current known limitation:

- whole-page `export_shape` still hit an HTTP-side error in the current setup

## Current Write-Propagation Expectation

Because the Penpot app and the backend API are both operating on the same live backend state, backend writes should be expected to appear in the live Penpot app.

That expectation is strengthened by the existence of authenticated mutation methods such as `update-file`, which accepts:

- `id`
- `sessionId`
- `revn`
- `vern`
- `changes` or `changesWithMetadata`

However, this specific write-through behavior has not yet been proven end-to-end in the current NEXUS workflow with a disposable live mutation and an observed on-screen update.

So the current rule is:

- inferred: yes, backend writes should surface in the live app
- proven: not yet in a safe disposable test we have recorded durably

## Surface Comparison Rule

When working on Penpot integrations, do not rely on one surface alone.

Actively compare and record the differences between:

- backend API
- MCP/plugin
- exported `.penpot` file

When a capability is discovered on one surface but not the others, update the comparison note rather than leaving that knowledge only in chat.

See:

- [Penpot Surface Comparison](penpot-surface-comparison.md)

## Current Working Guidance

For current NEXUS, FnTools, and CheddarBooks work:

- use the live backend as the primary current-state surface
- use MCP/plugin when current-file context or live Penpot interactions matter
- use exported `.penpot` files as explicit checkpoints
- keep backend credentials in machine-local secret storage
- keep local MCP bridge details in machine-local operational notes, not as secret values in repos
- update the Penpot surface comparison note when new capability or limitation findings emerge
- record important live/export workflow findings durably rather than leaving them only in chat
