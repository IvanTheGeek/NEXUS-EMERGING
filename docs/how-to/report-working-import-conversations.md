# Report Working Import Conversations

Use this command when you want to understand one import-local working slice in conversation terms instead of raw graph-node terms.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-working-import-conversations --import-id <import-id>
```

With an explicit limit:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-working-import-conversations --import-id <import-id> --limit 10
```

## What It Shows

- conversation node ID
- title or slug
- message count in that conversation for the selected import slice
- distinct referenced artifact count for that conversation

## Notes

- This report reads the SQLite working index under `graph/working/index/graph-working.sqlite`.
- It is useful for understanding what a fresh provider export contributed before you inspect individual graph neighborhoods.
- Use `find-working-graph-nodes` or `report-working-graph-neighborhood` afterward if you want to go deeper into one conversation node.
