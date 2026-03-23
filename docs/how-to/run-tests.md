# Run Tests

This guide explains the current automated test scaffold for the F# codebase.

## Command

Run the Expecto test project from the repository root:

```bash
dotnet run --project NEXUS-Code/tests/Nexus.Tests/Nexus.Tests.fsproj
```

## What It Covers Right Now

- provider adapter parsing for small ChatGPT and Claude fixtures
- duplicate-import behavior for provider exports
- manual artifact hydration duplicate detection
- Codex session import behavior
- conversation and artifact projection rebuild checks

## Notes

- the fixtures are intentionally tiny and curated
- these are regression tests for importer behavior, not full end-to-end coverage of your real exports
- this is the first layer of automated checks; property tests and snapshot-style output tests can be added next
