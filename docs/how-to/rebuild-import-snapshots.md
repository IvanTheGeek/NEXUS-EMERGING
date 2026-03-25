# Rebuild Import Snapshots

Use this when older provider-export imports predate normalized import snapshot materialization and `compare-import-snapshots` reports missing snapshot files.

This command:

- reads import manifests from `NEXUS-EventStore/imports/`
- follows preserved raw export paths in `NEXUS-Objects/`
- reparses the preserved export with the current provider-export parser
- rewrites normalized import snapshot files under `NEXUS-EventStore/snapshots/imports/<import-id>/`

It does **not** append canonical events.

## Commands

Rebuild one specific import snapshot:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-import-snapshots \
  --import-id <uuid>
```

Rebuild all import snapshots that can be derived from preserved provider-export imports:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-import-snapshots \
  --all
```

Force overwrite of existing normalized snapshot files:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  rebuild-import-snapshots \
  --import-id <uuid> \
  --force
```

## Options

- `--import-id <uuid>`
  Rebuild one specific import snapshot.
- `--all`
  Rebuild snapshots across all import manifests in the selected event store.
- `--force`
  Overwrite existing normalized snapshot files instead of skipping them.
- `--event-store-root <path>`
  Override the event-store root.
- `--objects-root <path>`
  Override the objects root.

## Notes

- This currently supports preserved `chatgpt` and `claude` provider-export imports.
- Codex local-session imports are not rebuilt through this command.
- Backfilled snapshots are derived from the current provider-export parser rules, not historical parser binaries.
- The import manifest `imported_at` is preserved in the rebuilt normalized snapshot.
- If preserved raw export files or extracted `conversations.json` files are missing, the command reports the import as failed instead of guessing.

## Related

- `compare-import-snapshots`
- `compare-provider-exports`
- `import-provider-export`
