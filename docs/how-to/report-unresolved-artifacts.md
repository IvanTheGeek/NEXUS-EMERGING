# Report Unresolved Artifacts

This command summarizes artifact projections that still do not have a captured payload.

Use it to find the next candidates for manual artifact hydration without digging through raw event files.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-unresolved-artifacts
```

Optional filters:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-unresolved-artifacts \
  --provider chatgpt \
  --limit 10
```

## What It Shows

- total artifact streams in the projection layer
- how many already have captured payloads
- how many are still unresolved
- unresolved counts by provider
- a short detailed list with:
  - `artifact_id`
  - provider
  - file name when known
  - provider artifact ID when known
  - canonical conversation/message IDs
  - last observed timestamp

## Notes

- This command reads `NEXUS-EventStore/projections/artifacts/`
- Rebuild artifact projections first if canonical artifact history changed recently
- The detailed list uses internal `artifact_id`, which is the easiest target for `capture-artifact-payload`
