# Run Tests

This guide explains the current automated test scaffold for the F# codebase.

## Command

Run the Expecto test project from the repository root:

```bash
dotnet run --project NEXUS-Code/tests/Nexus.Tests/Nexus.Tests.fsproj
```

For faster repeated runs after a successful build:

```bash
dotnet run --no-build --project NEXUS-Code/tests/Nexus.Tests/Nexus.Tests.fsproj
```

## What It Covers Right Now

- provider adapter parsing for small ChatGPT and Claude fixtures
- duplicate-import behavior for provider exports
- property-based invariant checks for duplicate imports, reparses, and manual artifact hydration
- manual artifact hydration duplicate detection
- Codex session import behavior
- conversation and artifact projection rebuild checks
- snapshot verification for canonical event TOML and import manifest TOML

## Snapshot Tests

The test project now includes Verify-based snapshot tests under:

`/home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-Code/tests/Nexus.Tests/snapshots`

Committed `.verified.toml` files are the approved expected output.

When a serializer change is intentional, rerun the tests and inspect the generated `.received.*` files. If the new output is correct, promote that content into the matching `.verified.*` file and rerun the suite.

## Notes

- the fixtures are intentionally tiny and curated
- these are regression tests for importer behavior, not full end-to-end coverage of your real exports
- the property tests focus on stable importer invariants rather than raw parser exhaustiveness
- the snapshot tests are aimed at wire-format stability for generated artifacts
- property tests can still be added later for stronger invariant coverage
- Expecto runs tests in parallel by default in this project
- use `--sequenced` when debugging order-sensitive or potentially interfering tests
- use `--parallel-workers <n>` if you want to tune worker count for the current machine
- the biggest time difference in day-to-day runs is often `--no-build`, not test parallelism alone
