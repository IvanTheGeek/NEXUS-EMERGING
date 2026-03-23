# Import Provider Exports

This command takes a provider export zip, archives it into the NEXUS object layer, extracts a working raw snapshot, parses the provider payload, and writes canonical observed-history events plus an import manifest into the event store.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  import-provider-export \
  --provider <chatgpt|claude> \
  --zip <path-to-export.zip> \
  --window full
```

## Examples

Claude:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  import-provider-export \
  --provider claude \
  --zip RawDataExports/data-2026-03-22-12-13-53-batch-0000.zip \
  --window full
```

ChatGPT:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  import-provider-export \
  --provider chatgpt \
  --zip RawDataExports/6dcabe6366235ba54dd09835c0f06f95c4ba2daa260f5ccac73dff295d2f1bda-2026-03-21-00-59-59-9c115c057bfc4f5f9a779548b8846d7d.zip \
  --window full
```

## What It Does

1. Copies the source zip into `NEXUS-Objects/providers/<provider>/archive/...`
2. Updates the stable latest zip and extracted latest snapshot under `NEXUS-Objects/providers/<provider>/latest/`
3. Extracts `conversations.json` and the rest of the zip contents into the raw object layer
4. Parses provider conversations and messages
5. Writes canonical events into `NEXUS-EventStore/events/...`
6. Writes an import manifest into `NEXUS-EventStore/imports/...`

## Current v0 Scope

The importer currently normalizes:

- provider artifact received
- raw snapshot extracted
- provider conversation observed
- provider message observed
- provider message revision observed
- artifact referenced
- import completed

Current provider parsing scope:

- ChatGPT: conversations, messages, attachment references from message metadata
- Claude: conversations, messages, file/attachment references from message payloads

Still deferred:

- full canonical modeling for Claude projects, memories, and users
- richer normalization of non-text provider content
- automatic artifact payload hydration
- live API capture

## Defaults and Overrides

Defaults:

- `--window full`
- objects root: `NEXUS-Objects/`
- event-store root: `NEXUS-EventStore/`

Optional overrides:

```bash
--objects-root /some/other/objects-root
--event-store-root /some/other/event-store-root
```

## Notes

- The raw object layer is ignored by Git in this repo for now.
- The canonical event store is intended to be committed.
- Each import records a `normalization_version` so parser/canonicalizer changes can be tracked explicitly.
- Re-importing the same provider objects under the same normalization version will skip duplicates and emit revision events only when a known provider message is observed with changed canonical content.
- Re-importing the same provider objects under a different normalization version appends new message observations instead of pretending the provider message itself changed.
