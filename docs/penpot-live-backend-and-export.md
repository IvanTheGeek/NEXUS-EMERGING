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
  useful live interaction surface, but may have connection constraints
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

However, the current local setup is still hitting a transport limitation in practice.

Current observed effect:

- if the Penpot plugin is already connected
- Codex MCP access may fail with an `Already connected to a transport` error
- restarting the local MCP server does not currently clear that failure reliably

So today:

- backend API is the more reliable automation surface
- MCP is still useful, but should be treated as connection-sensitive until proven otherwise

## Current Working Guidance

For current NEXUS, FnTools, and CheddarBooks work:

- use the live backend as the primary current-state surface
- use exported `.penpot` files as explicit checkpoints
- keep backend credentials in machine-local secret storage
- record important live/export workflow findings durably rather than leaving them only in chat
