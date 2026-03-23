# Export Graphviz DOT

Use this command when you want an external graph view over the derived NEXUS graph assertions.

This is useful for:

- spotting patterns and clusters outside current NEXUS views
- comparing what the graph reveals against what NEXUS currently emphasizes
- generating Graphviz outputs that can help guide later FnHCI and internal visualization work

## Command

Default output path:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot
```

Custom output path:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --output /tmp/nexus-graph.dot
```

Custom event-store root:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --event-store-root /tmp/nexus-event-store
```

## Default Location

Unless you override it, the DOT file is written to:

`NEXUS-EventStore/graph/exports/nexus-graph.dot`

## Recommended Sequence

If the derived graph may be stale:

1. Run `rebuild-graph-assertions`
2. Run `export-graphviz-dot`

## Rendering

If Graphviz is installed, you can render the DOT file into something visual, for example:

```bash
dot -Tsvg NEXUS-EventStore/graph/exports/nexus-graph.dot -o /tmp/nexus-graph.svg
```

Or:

```bash
dot -Tpng NEXUS-EventStore/graph/exports/nexus-graph.dot -o /tmp/nexus-graph.png
```

## Notes

- This export is derived from `graph/assertions/`, not from canonical history directly.
- The DOT file is an external lens, not the source of truth.
- It is meant to help surface structure and relationships that may not yet be obvious from inside NEXUS itself.
