# Inspect Penpot Files And Live Surfaces

Use this when you need to understand a Penpot file or local Penpot workspace in a way that can later become deterministic tooling.

## Inspect The File Artifact

Confirm the file type:

```bash
file /path/to/file.penpot
```

List archive contents:

```bash
unzip -l /path/to/file.penpot
```

Read top-level file metadata:

```bash
unzip -p /path/to/file.penpot files/<file-id>.json
```

Read page metadata:

```bash
unzip -p /path/to/file.penpot files/<file-id>/pages/<page-id>.json
```

Read the root object for a page:

```bash
unzip -p /path/to/file.penpot \
  files/<file-id>/pages/<page-id>/00000000-0000-0000-0000-000000000000.json
```

Read a specific shape or component object:

```bash
unzip -p /path/to/file.penpot \
  files/<file-id>/pages/<page-id>/<shape-id>.json
```

## Inspect The Local Penpot Backend

If the local Penpot stack is running, inspect the backend docs:

```bash
curl -L http://localhost:9001/api/main/doc | head
```

Browser URL:

- `http://localhost:9001/api/main/doc`

## Inspect The Local Penpot MCP Surface

If the local Penpot MCP runner is active:

Check the plugin manifest:

```bash
curl http://localhost:4400/manifest.json
```

Check the MCP endpoint:

```bash
curl -i http://localhost:4401/mcp
```

Expected behavior:

- the manifest should return JSON
- the MCP endpoint may return `405 Method Not Allowed` on a simple `GET` or `HEAD`, which still proves the HTTP server is listening

## Inspection Order

Prefer this order:

1. repo docs
2. `.penpot` archive structure
3. backend/API docs
4. MCP/plugin surface
5. GUI confirmation

## Why This Runbook Exists

The purpose is to keep Penpot work:

- inspectable
- reviewable
- teachable to later AI and humans
- easier to turn into F# functions, clients, scripts, and tooling

## Related

- [Penpot Access And Structure](../penpot-access-and-structure.md)
- [Event Modeling Tool Foundation](../event-modeling-tool-foundation.md)
