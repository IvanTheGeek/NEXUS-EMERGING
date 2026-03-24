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

Custom output root:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --provider claude --output-root /tmp/nexus-graph-exports
```

Provider slice:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --provider claude
```

Canonical conversation slice:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --conversation-id 019d174e-e960-7507-8aa6-06ee0064e499
```

Provider conversation slice:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --provider chatgpt --provider-conversation-id <provider-conversation-id>
```

Import slice:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --import-id <import-id>
```

Working import slice:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --working-import-id <import-id>
```

Custom event-store root:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --event-store-root /tmp/nexus-event-store
```

## Default Location

Unless you override it, the DOT file is written to:

`NEXUS-EventStore/graph/exports/nexus-graph.dot`

When you use filters, the default file name becomes filter-aware, for example:

- `nexus-graph__provider-claude.dot`
- `nexus-graph__canonical-conversation-019d174e-e960-7507-8aa6-06ee0064e499.dot`
- `nexus-graph__provider-chatgpt__conversation-abc123.dot`
- `nexus-graph__import-019d....dot`

Working import slices default to:

- `graph/working/exports/nexus-working-graph__import-019d....dot`

If you use `--output-root`, NEXUS keeps the generated file name but writes it under the directory you specify.

## Recommended Sequence

If the derived graph may be stale:

1. Run `rebuild-graph-assertions`
2. Run `export-graphviz-dot`

If you just finished an import and want the batch-local working slice instead:

1. Run `import-provider-export`
2. Run `report-working-graph-imports` if you want to confirm the available working slices
3. Run `export-graphviz-dot --working-import-id <import-id>`

For large stores, prefer slices first:

1. `--provider` for a provider-wide view
2. `--conversation-id` for one canonical conversation and its immediate neighborhood
3. `--provider-conversation-id` for one provider-native conversation
4. `--import-id` for one import batch
5. `--working-import-id` for one fresh graph working slice without a full durable-graph rebuild

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
- `--working-import-id` is the exception: it reads `graph/working/imports/<import-id>/assertions/` directly from the secondary working layer.
- Use either `--output` or `--output-root`, not both.
- Filters are applied from graph assertion provenance, which makes provider, conversation, and import slices practical without replaying the canonical event layer.
- `--conversation-id` uses the canonical conversation ID from a conversation projection and keeps only that conversation plus its immediate graph neighborhood.
- `--working-import-id` cannot be combined with the durable-graph filter options because it already selects one explicit working slice.
- The DOT file is an external lens, not the source of truth.
- It is meant to help surface structure and relationships that may not yet be obvious from inside NEXUS itself.
