# Report Current Ingestion

Use this when you want one quick view of the latest known import state across providers.

It reads the newest import manifest for each provider and augments it with:

- normalized snapshot totals when they exist
- preserved raw root-artifact SHA-256 when the file is still present

It is useful after imports when you want to answer questions like:

- which providers are currently represented in the store
- what the latest import was for each provider
- whether the latest import has normalized snapshot support
- whether the preserved raw artifact is still available

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-current-ingestion
```

## Optional Roots

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-current-ingestion \
  --event-store-root /path/to/NEXUS-EventStore \
  --objects-root /path/to/NEXUS-Objects
```

## What It Reports

For each provider with at least one import manifest, it prints:

- provider
- latest import ID
- imported-at timestamp
- source acquisition kind
- window label
- normalization version
- root artifact relative path
- root artifact SHA-256 when the preserved file exists
- canonical import counts from the import manifest
- whether a normalized import snapshot is available
- snapshot totals when available

It also reports known providers that are currently missing from the store.

## Important Note

This is an operational status view.

It does not replace:

- `report-provider-import-history` for one provider's chronological timeline
- `compare-import-snapshots` for pairwise snapshot comparison
- canonical event files as the source of truth

Codex currently appears from import manifests without normalized import snapshots, so its latest row will usually show:

- `normalized_snapshot_available=false`

That is expected with the current ingestion model.
