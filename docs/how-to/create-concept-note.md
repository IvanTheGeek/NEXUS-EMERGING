# Create Concept Notes

Use `create-concept-note` when you want to turn one or more imported conversations into a durable concept seed in the repo docs.

This is the first curation layer between:

- imported conversation history
- derived graph views
- durable project memory for humans and AI collaborators

It does not change canonical history.

It creates a Markdown note under `docs/concepts/` with:

- a small TOML front matter block
- a summary scaffold
- working-note prompts
- provenance back to canonical conversation projections
- short excerpts from source messages
- a ready-to-run Graphviz slice command for each source conversation

## Basic Use

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- create-concept-note --slug fnhci --title FnHCI --conversation-id 019d174e-e960-7507-8aa6-06ee0064e499
```

## Multiple Conversations

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- create-concept-note --slug graph-lenses --title "Graph Lenses" --conversation-id <uuid> --conversation-id <uuid>
```

## Optional Domain and Tag Hints

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- create-concept-note --slug fnhci --title FnHCI --conversation-id <uuid> --domain interaction-design --tag seed
```

## Options

- `--slug <slug>`
  Required. File-safe note slug. It will be normalized to lowercase kebab-case.
- `--title <title>`
  Required. Human-readable concept title.
- `--conversation-id <uuid>`
  Required. Repeat to harvest more than one canonical conversation into the seed note.
- `--domain <slug>`
  Optional domain hint recorded in the note front matter.
- `--tag <slug>`
  Optional tag recorded in the note front matter.
- `--docs-root <path>`
  Optional docs root override. Defaults to the repo `docs/` folder.
- `--event-store-root <path>`
  Optional event-store root override. Defaults to `NEXUS-EventStore/`.

## Notes

- This command expects the source conversations to already exist in `NEXUS-EventStore/projections/conversations/`.
- Run `rebuild-conversation-projections` first if projections are missing or stale.
- The command refuses to overwrite an existing concept note.
- The note is intentionally a seed. Edit it afterward to tighten the summary, connect related concepts, and merge insights from other sources.
