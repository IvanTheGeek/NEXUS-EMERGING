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
- Also materializes a batch-local graph working slice under `graph/working/imports/<import-id>/`.
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

`create-concept-note`

- Creates a curated concept-note seed from one or more canonical conversation projections.
- Use it to promote recurring ideas from chat history into durable repo memory with provenance.
- Details: `docs/how-to/create-concept-note.md`

`rebuild-artifact-projections`

- Rebuilds artifact read models from canonical artifact events.
- Use it before reporting unresolved artifacts if projection files are stale.
- Details: `docs/how-to/rebuild-artifact-projections.md`

`rebuild-graph-assertions`

- Rebuilds the first thin graph-assertion layer from canonical history.
- Use it when you want a derived graph substrate over the canonical event store.
- Large stores now require explicit `--yes` approval because full rebuild is treated as a heavyweight operation.
- Writes a rebuild manifest under `graph/rebuilds/` so timings and counts are durable.
- Details: `docs/how-to/rebuild-graph-assertions.md`

`export-graphviz-dot`

- Exports the derived graph assertions as a Graphviz DOT file.
- Use it when you want an external visual lens over the graph to spot structure, clusters, and relationships.
- It now supports provider, provider-conversation, and import slices so you do not have to render the whole graph every time.
- It can also traceably verify a `--working-import-id` slice back to canonical events and raw object refs before writing the DOT file.
- Details: `docs/how-to/export-graphviz-dot.md`

`render-graphviz-dot`

- Renders an existing DOT file into SVG or PNG using an explicitly allowlisted Graphviz engine.
- Use it after `export-graphviz-dot` when you want a directly viewable file.
- Details: `docs/how-to/render-graphviz-dot.md`

`report-unresolved-artifacts`

- Summarizes artifact references whose payloads are still unresolved.
- Use it to find good candidates for manual artifact recovery.
- Details: `docs/how-to/report-unresolved-artifacts.md`

`report-working-graph-imports`

- Summarizes the graph working-layer import slices from the lightweight catalog.
- Use it when you want a fast view of the current secondary graph working layer.
- Details: `docs/how-to/report-working-graph-imports.md`

`report-working-graph-slice`

- Summarizes one graph working import slice from the SQLite working index.
- Use it when you want a quick structural view of a specific fresh import batch.
- Details: `docs/how-to/report-working-graph-slice.md`

`verify-working-graph-slice`

- Verifies one graph working import slice back to canonical events and preserved raw objects.
- Use it when a slice matters enough to trade speed for stronger provenance validation.
- Details: `docs/how-to/verify-working-graph-slice.md`

`rebuild-working-graph-index`

- Rebuilds the SQLite graph working index from the existing graph working import slices.
- Use it when the local working index is missing, stale, or intentionally reset.
- Details: `docs/how-to/rebuild-working-graph-index.md`

## Common Workflow Sequences

Provider import:

1. Run `import-provider-export`.
2. Run `rebuild-conversation-projections`.
3. Run `rebuild-artifact-projections`.
4. Run `rebuild-graph-assertions` if you want to refresh the thin graph layer.
5. Run `export-graphviz-dot` if you want an external graph view.
6. Run `render-graphviz-dot` if you want SVG or PNG output from the DOT file.
7. Run `report-unresolved-artifacts` if you want to identify missing payloads.
8. Run `report-working-graph-imports` if you want a quick view of the current graph working slices.
9. Run `report-working-graph-slice --import-id <uuid>` if you want the SQLite-backed summary for one import batch.
10. Run `rebuild-working-graph-index` if the SQLite working index needs to be recreated from existing working slices.
11. Run `verify-working-graph-slice --import-id <uuid>` if you want to validate that the slice still traces back cleanly to canonical and raw layers.
12. Run `export-graphviz-dot --working-import-id <uuid> --verification traceable` if you want a graph export that refuses to render when that traceability chain is broken.

Codex session import:

1. Run `dotnet fsi NEXUS-Code/scripts/export_codex_sessions.fsx`.
2. Run `import-codex-sessions`.
3. Run `rebuild-conversation-projections`.
4. Run `report-working-graph-slice --import-id <uuid>` if you want the local working-index summary for that batch.

Concept harvest:

1. Run `rebuild-conversation-projections` if needed.
2. Run `create-concept-note` with one or more canonical `conversation_id` values.
3. Edit the seed note under `docs/concepts/`.
4. Use `export-graphviz-dot --conversation-id <uuid>` when you want the local graph neighborhood alongside the note.

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
- `docs/how-to/render-graphviz-dot.md`
- `docs/how-to/rebuild-conversation-projections.md`
- `docs/how-to/create-concept-note.md`
- `docs/how-to/rebuild-artifact-projections.md`
- `docs/how-to/rebuild-graph-assertions.md`
- `docs/how-to/rebuild-working-graph-index.md`
- `docs/how-to/export-graphviz-dot.md`
- `docs/how-to/report-unresolved-artifacts.md`
- `docs/how-to/report-working-graph-imports.md`
- `docs/how-to/report-working-graph-slice.md`
- `docs/how-to/run-tests.md`
- `docs/how-to/verify-working-graph-slice.md`
