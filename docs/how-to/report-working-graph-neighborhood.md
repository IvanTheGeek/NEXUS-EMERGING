# Report Working Graph Neighborhood

Use this command when you want the local graph neighborhood of one node inside a single import-local working batch.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-working-graph-neighborhood --import-id <import-id> --node-id <node-id>
```

With an explicit limit:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-working-graph-neighborhood --import-id <import-id> --node-id <node-id> --limit 10
```

## What It Shows

- node summary fields like title, slug, kind, and roles
- literal assertions attached directly to the node
- outgoing node-to-node connections
- incoming node-to-node connections

## Notes

- This command stays inside one working import batch, so you must provide `--import-id`.
- Use `find-working-graph-nodes` first if you need help discovering node IDs.
- This is an indexed working-layer view, not canonical truth.
