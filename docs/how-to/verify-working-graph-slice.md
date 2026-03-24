# Verify Working Graph Slice

This command verifies one graph working import slice back to canonical events and preserved raw objects.

Use it when you want stronger confidence than the fast working-index path alone provides.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  verify-working-graph-slice \
  --import-id <uuid>
```

## Example

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  verify-working-graph-slice \
  --import-id 019d174e-e953-7e8b-b506-5f1475399fc7
```

## What It Verifies

For the selected working slice, the command checks:

1. the working slice exists under `graph/working/imports/<import-id>/`
2. supporting canonical event IDs referenced by the working assertions still exist
3. preserved raw object paths referenced by the working assertions still exist
4. working-slice assertion provenance still points at the expected import

This follows the intended NEXUS verification chain:

- working slice
- canonical events
- raw object refs

## Exit Codes

- `0` when the verification report is clean
- `2` when the slice exists but canonical or raw verification fails
- `1` for argument or command errors

## Options

- `--event-store-root <path>`
  Override the event-store root. Defaults to `NEXUS-EventStore/`.

- `--objects-root <path>`
  Override the objects root. Defaults to `NEXUS-Objects/`.

- `--import-id <uuid>`
  Required. Selects the graph working slice to verify.

## Notes

- This is the first practical verification path over a derived working layer.
- It does not treat the SQLite working index as source truth.
- It is intended for confidence-sensitive flows where provenance matters more than raw speed.
