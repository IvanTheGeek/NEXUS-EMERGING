# CLI Commands

This is the quick reference for the NEXUS CLI.

Use it when you want to remember:

- what commands exist
- how to ask the CLI for command-specific help
- which command fits which workflow
- where to go for a more detailed runbook

## Built-In Help

Global help:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- --help
```

Command-specific help:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- help import-provider-export
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- import-provider-export --help
```

The CLI supports both:

- `help <command>`
- `<command> --help`

## Commands

`write-sample-event-store`

- Writes a small sample canonical history bundle.
- Use it to smoke-test event writing, layout, and projection rebuilds without touching real imports.
- Details: `docs/how-to/write-sample-event-store.md`

`import-provider-export`

- Archives a ChatGPT or Claude export zip.
- Parses provider records and appends canonical observed history into `NEXUS-EventStore/`.
- Details: `docs/how-to/import-provider-export.md`

`import-codex-sessions`

- Imports preserved Codex session snapshots from `NEXUS-Objects/`.
- Use this after exporting Codex sessions into the object layer.
- Details: `docs/how-to/import-codex-sessions.md`

`capture-artifact-payload`

- Manually hydrates an artifact payload that was referenced earlier.
- Archives the file into `NEXUS-Objects/` and appends `ArtifactPayloadCaptured` when the payload is new.
- Details: `docs/how-to/capture-artifact-payload.md`

`rebuild-conversation-projections`

- Rebuilds conversation read models from canonical conversation events.
- Use it after imports or reparses when you want the latest conversation summaries.
- Details: `docs/how-to/rebuild-conversation-projections.md`

`rebuild-artifact-projections`

- Rebuilds artifact read models from canonical artifact events.
- Use it before reporting unresolved artifacts if projection files are stale.
- Details: `docs/how-to/rebuild-artifact-projections.md`

`rebuild-graph-assertions`

- Rebuilds the first thin graph-assertion layer from canonical history.
- Use it when you want a derived graph substrate over the canonical event store.
- Details: `docs/how-to/rebuild-graph-assertions.md`

`export-graphviz-dot`

- Exports the derived graph assertions as a Graphviz DOT file.
- Use it when you want an external visual lens over the graph to spot structure, clusters, and relationships.
- Details: `docs/how-to/export-graphviz-dot.md`

`report-unresolved-artifacts`

- Summarizes artifact references whose payloads are still unresolved.
- Use it to find good candidates for manual artifact recovery.
- Details: `docs/how-to/report-unresolved-artifacts.md`

## Common Workflow Sequences

Provider import:

1. Run `import-provider-export`.
2. Run `rebuild-conversation-projections`.
3. Run `rebuild-artifact-projections`.
4. Run `rebuild-graph-assertions` if you want to refresh the thin graph layer.
5. Run `export-graphviz-dot` if you want an external graph view.
6. Run `report-unresolved-artifacts` if you want to identify missing payloads.

Codex session import:

1. Run `dotnet fsi NEXUS-Code/scripts/export_codex_sessions.fsx`.
2. Run `import-codex-sessions`.
3. Run `rebuild-conversation-projections`.

Manual artifact hydration:

1. Identify a target artifact with `report-unresolved-artifacts`.
2. Run `capture-artifact-payload`.
3. Run `rebuild-artifact-projections`.

## Defaults

Unless overridden, the CLI uses repository-local defaults:

- event store root: `NEXUS-EventStore/`
- objects root: `NEXUS-Objects/`
- Codex snapshot root: `NEXUS-Objects/providers/codex/latest/`

## Related Guides

- `docs/how-to/export-codex-sessions.md`
- `docs/how-to/import-provider-export.md`
- `docs/how-to/import-codex-sessions.md`
- `docs/how-to/capture-artifact-payload.md`
- `docs/how-to/rebuild-conversation-projections.md`
- `docs/how-to/rebuild-artifact-projections.md`
- `docs/how-to/rebuild-graph-assertions.md`
- `docs/how-to/export-graphviz-dot.md`
- `docs/how-to/report-unresolved-artifacts.md`
- `docs/how-to/run-tests.md`
