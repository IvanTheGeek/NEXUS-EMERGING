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

`compare-provider-exports`

- Compares two raw ChatGPT, Claude, or Grok export zips before canonical import.
- Use it when you want a source-layer view of added, removed, and changed provider-native conversations or messages.
- It also reports whether the two zip artifacts are byte-identical.
- Details: `docs/how-to/compare-provider-exports.md`

`compare-import-snapshots`

- Compares two normalized import snapshots after provider import.
- Use it when you want full-export or rolling-window snapshot semantics inside the NEXUS pipeline, without confusing additive dedupe with snapshot membership.
- It is keyed by provider-native conversation identity and is derived from the parsed provider payload before canonical dedupe.
- If older imports are missing snapshot files, run `rebuild-import-snapshots`.
- Details: `docs/how-to/compare-import-snapshots.md`

`report-provider-import-history`

- Reports one provider's normalized import snapshots in chronological order.
- Use it when you want a timeline view of export/import history plus adjacent snapshot deltas.
- When the preserved raw artifacts are still available, it also reports raw SHA-256 and whether each artifact matches the previous snapshot's artifact.
- This is a snapshot-history report, not an additive working-slice report.
- Details: `docs/how-to/report-provider-import-history.md`

`report-current-ingestion`

- Reports the latest known import state across providers.
- Use it when you want one operational view of what is currently ingested without checking each provider separately.
- It reads the newest import manifest per provider, adds normalized snapshot totals when available, and reports raw root-artifact SHA-256 when the preserved file still exists.
- It also shows the current LOGOS source/channel/signal classification for known providers.
- Details: `docs/how-to/report-current-ingestion.md`

`report-logos-catalog`

- Reports the explicit allowlisted LOGOS source systems, intake channels, and signal kinds.
- Use it when you want to see the current concrete LOGOS intake vocabulary before classifying or seeding a source.
- Details: `docs/how-to/report-logos-catalog.md`

`report-conversation-overlap-candidates`

- Reports conservative conversation-level overlap candidates between two providers' projection sets.
- Use it when you want to spot possible cross-source overlap, such as local Codex capture vs later provider export, without collapsing anything automatically.
- It is based on explainable signals like normalized title similarity, time overlap, and message-count closeness.
- This is a candidate report only, not reconciliation.
- Details: `docs/how-to/report-conversation-overlap-candidates.md`

`rebuild-import-snapshots`

- Rebuilds normalized import snapshots for older provider-export imports from preserved raw artifacts.
- This rewrites derived snapshot files only. It does not append canonical events.
- Use `--import-id <uuid>` for one import or `--all` for an explicit full backfill pass.
- Details: `docs/how-to/rebuild-import-snapshots.md`

`import-provider-export`

- Archives a ChatGPT, Claude, or Grok export zip.
- Parses provider records and appends canonical observed history into `NEXUS-EventStore/`.
- Also writes a normalized import snapshot under `snapshots/imports/<import-id>/`.
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

`create-logos-intake-note`

- Creates a durable LOGOS intake seed note from explicit source, channel, signal, and locator metadata.
- Use it for forum/email/bug-report/app-feedback items before a full ingestion path exists for that source type.
- Details: `docs/how-to/create-logos-intake-note.md`

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
- It also supports `--working-node-id` for one node's immediate neighborhood inside a fresh working import slice.
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

`report-working-import-conversations`

- Summarizes the conversation nodes present in one import-local graph working slice.
- Use it when you want to understand a fresh import batch in conversation terms before drilling into graph details.
- Details: `docs/how-to/report-working-import-conversations.md`

`compare-working-import-conversations`

- Compares the conversation contributions present in two import-local graph working slices.
- Use it when you want a fast batch-to-batch view of added, removed, and changed conversation contributions in the working layer.
- This is intentionally about batch-local derived contributions, not full provider snapshot truth.
- Details: `docs/how-to/compare-working-import-conversations.md`

`find-working-graph-nodes`

- Finds candidate nodes from the SQLite working index by title/slug text plus explicit role and batch filters.
- Use it when you need node IDs before inspecting a local neighborhood.
- Details: `docs/how-to/find-working-graph-nodes.md`

`report-working-graph-slice`

- Summarizes one graph working import slice from the SQLite working index.
- Use it when you want a quick structural view of a specific fresh import batch.
- Details: `docs/how-to/report-working-graph-slice.md`

`report-working-graph-neighborhood`

- Shows the local neighborhood of one node inside a single graph working import slice.
- Use it after `find-working-graph-nodes` when you want the nearby literals and node-to-node connections.
- Details: `docs/how-to/report-working-graph-neighborhood.md`

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

1. Run `compare-provider-exports` if you want to understand raw export-window deltas before import.
2. Run `import-provider-export`.
3. Run `report-provider-import-history --provider <chatgpt|claude|grok|codex>` if you want a chronological snapshot timeline for one provider.
   Add `--objects-root <path>` when the preserved raw artifacts are not under the repository-default object store and you want raw SHA-256 evidence in the report.
4. Run `report-current-ingestion` if you want one cross-provider status view of what the store currently contains.
5. Run `compare-import-snapshots --base-import-id <uuid> --current-import-id <uuid>` if you want normalized snapshot semantics for one specific import pair after import.
6. Run `rebuild-conversation-projections`.
7. Run `report-conversation-overlap-candidates --left-provider codex --right-provider chatgpt` if you want a first explicit cross-source overlap candidate check.
8. Run `rebuild-artifact-projections`.
9. Run `rebuild-graph-assertions` if you want to refresh the thin graph layer.
10. Run `export-graphviz-dot` if you want an external graph view.
11. Run `render-graphviz-dot` if you want SVG or PNG output from the DOT file.
12. Run `report-unresolved-artifacts` if you want to identify missing payloads.
13. Run `report-working-graph-imports` if you want a quick view of the current graph working slices.
14. Run `report-working-import-conversations --import-id <uuid>` if you want a conversation-centric view of one fresh import batch.
15. Run `compare-working-import-conversations --base-import-id <uuid> --current-import-id <uuid>` if you want a batch-to-batch comparison of conversation contributions in the working layer.
16. Run `find-working-graph-nodes` if you want to discover candidate node IDs from the SQLite working index.
17. Run `report-working-graph-slice --import-id <uuid>` if you want the SQLite-backed summary for one import batch.
18. Run `report-working-graph-neighborhood --import-id <uuid> --node-id <node-id>` if you want the local structure around one indexed node.
19. Run `rebuild-working-graph-index` if the SQLite working index needs to be recreated from existing working slices.
20. Run `verify-working-graph-slice --import-id <uuid>` if you want to validate that the slice still traces back cleanly to canonical and raw layers.
21. Run `export-graphviz-dot --working-import-id <uuid> --verification traceable` if you want a graph export that refuses to render when that traceability chain is broken.

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

LOGOS intake seeding:

1. Run `report-logos-catalog`.
2. Run `create-logos-intake-note`.
3. Refine the note under `docs/logos-intake/`.

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
- `docs/how-to/compare-import-snapshots.md`
- `docs/how-to/compare-provider-exports.md`
- `docs/how-to/import-provider-export.md`
- `docs/how-to/import-codex-sessions.md`
- `docs/how-to/capture-artifact-payload.md`
- `docs/how-to/render-graphviz-dot.md`
- `docs/how-to/rebuild-conversation-projections.md`
- `docs/how-to/create-concept-note.md`
- `docs/how-to/create-logos-intake-note.md`
- `docs/how-to/rebuild-artifact-projections.md`
- `docs/how-to/rebuild-graph-assertions.md`
- `docs/how-to/rebuild-working-graph-index.md`
- `docs/how-to/export-graphviz-dot.md`
- `docs/how-to/report-unresolved-artifacts.md`
- `docs/how-to/report-current-ingestion.md`
- `docs/how-to/report-logos-catalog.md`
- `docs/how-to/report-working-graph-imports.md`
- `docs/how-to/report-working-import-conversations.md`
- `docs/how-to/compare-working-import-conversations.md`
- `docs/how-to/find-working-graph-nodes.md`
- `docs/how-to/report-working-graph-slice.md`
- `docs/how-to/report-working-graph-neighborhood.md`
- `docs/how-to/run-tests.md`
- `docs/how-to/verify-working-graph-slice.md`
