# Find Working Graph Nodes

Use this command when you want to discover candidate node IDs from the SQLite graph working index before drilling into a neighborhood or exporting a verified batch.

## Command

Title or slug match:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- find-working-graph-nodes --match fixture
```

Semantic-role scoped search:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- find-working-graph-nodes --semantic-role imprint --provider claude
```

Message-role search inside one import batch:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- find-working-graph-nodes --message-role assistant --import-id <import-id>
```

## Notes

- At least one of `--match`, `--semantic-role`, or `--message-role` is required.
- `--provider` and `--import-id` narrow the search scope but do not replace the need for a real match filter.
- This command reads the SQLite working index under `graph/working/index/graph-working.sqlite`.
- Use `report-working-graph-neighborhood` after this when you want the local neighborhood around a discovered node.
