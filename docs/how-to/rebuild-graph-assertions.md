# Rebuild Graph Assertions

This command rebuilds the first thin graph layer from canonical history.

It does not change canonical event history. It rewrites the derived graph assertion layer under `NEXUS-EventStore/graph/assertions/`.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-graph-assertions
```

Optional override:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-graph-assertions \
  --event-store-root /some/other/event-store-root
```

## What It Produces

The rebuild writes one file per derived fact:

```text
NEXUS-EventStore/graph/assertions/<fact-id>.toml
```

The first pass currently derives assertions such as:

- node kind facts for conversations, messages, artifacts, imports, domains, and bounded contexts
- semantic role facts such as `imprint` on current message and artifact nodes
- message-to-conversation relationships
- message-to-artifact relationships
- subject-to-import relationships
- subject-to-domain relationships
- subject-to-bounded-context relationships
- small literal attributes such as title, role, sequence hint, file name, media type, and reference disposition

## Notes

- Graph assertions are rebuildable derived structure, not source truth.
- The rebuild deletes and rewrites the existing graph assertion folder.
- The CLI now emits progress while scanning canonical events, deriving assertions, and writing graph files.
- Node and fact IDs in this layer are deterministic so rebuilds remain stable.
- This is intentionally a thin graph pass. It is meant to create a usable substrate without locking the final NEXUS ontology too early.
- This layer is also intended to support future graph visualization consumers such as Graphviz and later FnHCI views.
