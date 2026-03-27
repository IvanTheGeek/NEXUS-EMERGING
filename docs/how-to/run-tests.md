# Run Tests

This guide explains the current automated test scaffold for the F# codebase.

## Full Suite

Run the full split suite from the repository root:

```bash
./NEXUS-Code/tests/run-all-tests.sh
```

This runs:

- `Nexus.Foundation.Tests`
- `FnTools.Tests`
- `CheddarBooks.Tests`

## Individual Projects

Run one project directly when you only need one concern line:

```bash
dotnet run --project NEXUS-Code/tests/Nexus.Foundation.Tests/Nexus.Foundation.Tests.fsproj
dotnet run --project NEXUS-Code/tests/FnTools.Tests/FnTools.Tests.fsproj
dotnet run --project NEXUS-Code/tests/CheddarBooks.Tests/CheddarBooks.Tests.fsproj
```

For faster repeated runs after a successful build:

```bash
dotnet run --no-build --project NEXUS-Code/tests/Nexus.Foundation.Tests/Nexus.Foundation.Tests.fsproj
dotnet run --no-build --project NEXUS-Code/tests/FnTools.Tests/FnTools.Tests.fsproj
dotnet run --no-build --project NEXUS-Code/tests/CheddarBooks.Tests/CheddarBooks.Tests.fsproj
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

The foundation test project now includes Verify-based snapshot tests under:

`/home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-Code/tests/Nexus.Foundation.Tests/snapshots`

Committed `.verified.toml` files are the approved expected output.

When a serializer change is intentional, rerun the tests and inspect the generated `.received.*` files. If the new output is correct, promote that content into the matching `.verified.*` file and rerun the suite.

## Notes

- the fixtures are intentionally tiny and curated
- these are regression tests for importer behavior, not full end-to-end coverage of your real exports
- the property tests focus on stable importer invariants rather than raw parser exhaustiveness
- the snapshot tests are aimed at wire-format stability for generated artifacts
- the split now mirrors the emerging repo boundaries:
  - `Nexus.Foundation.Tests`
  - `FnTools.Tests`
  - `CheddarBooks.Tests`
- property tests can still be added later for stronger invariant coverage
- Expecto runs tests in parallel by default in this project
- use `--sequenced` when debugging order-sensitive or potentially interfering tests
- use `--parallel-workers <n>` if you want to tune worker count for the current machine
- the biggest time difference in day-to-day runs is often `--no-build`, not test parallelism alone
- avoid parallel `dotnet run` or `dotnet build` invocations against the same project path unless at most one of them is building
- when you want parallel follow-up runs, do one successful build first and then prefer `dotnet run --no-build ...`
- if you truly need concurrent builds, isolate them by project or worktree so they are not competing for the same `bin/` and `obj/` files
