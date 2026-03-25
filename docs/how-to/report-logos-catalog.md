# Report LOGOS Catalog

Use this when you want to inspect the currently allowlisted LOGOS source vocabulary before seeding or modeling intake.

It reports:

- recognized source systems
- recognized intake channels
- recognized signal kinds

This keeps early LOGOS work explicit and deterministic rather than open-ended.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-logos-catalog
```

## What It Is For

Use this before:

- creating a LOGOS intake note
- deciding how to classify a new source
- checking whether a needed source kind is already modeled explicitly

## Notes

- This is an allowlist report, not a dynamic taxonomy browser.
- If a needed source system or signal kind is not listed, that is a modeling decision to add explicitly, not something to improvise ad hoc.
