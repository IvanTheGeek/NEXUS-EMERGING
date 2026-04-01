# Verify

## Purpose In NEXUS

`Verify.Expecto` is the snapshot verification package currently used in the NEXUS foundation test suite for generated text artifacts.

Right now we mainly use it for:

- generated TOML snapshots in `Nexus.Foundation.Tests`
- stable output verification where output shape matters as much as logical correctness

## Official Docs To Check First

Start with these upstream sources before inferring behavior from generated files:

1. Verify repo README  
   <https://github.com/VerifyTests/Verify>
2. Getting-started wizard entry  
   <https://github.com/VerifyTests/Verify/blob/main/docs/wiz/readme.md>
3. README conventions section  
   <https://github.com/VerifyTests/Verify?tab=readme-ov-file#conventions>

For the Expecto path specifically, confirm the README and wizard guidance before guessing at local setup or generated-file behavior.

## Local Usage Here

Current package references live in:

- [`NEXUS-Code/tests/Nexus.Foundation.Tests/Nexus.Foundation.Tests.fsproj`](https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/main/NEXUS-Code/tests/Nexus.Foundation.Tests/Nexus.Foundation.Tests.fsproj)

Current snapshot tests live in:

- [`NEXUS-Code/tests/Nexus.Foundation.Tests/SnapshotTests.fs`](https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/main/NEXUS-Code/tests/Nexus.Foundation.Tests/SnapshotTests.fs)

Current runbook:

- [`docs/how-to/run-tests.md`](../../how-to/run-tests.md)

## Important Conventions

One convention that matters locally and should be checked upstream rather than guessed:

- Verify text snapshots are written as UTF-8 with BOM
- text files use LF line endings
- text files do not end with a trailing newline

Those are Verify conventions, not accidental local output quirks.

## Local Gotchas And Corrected Misunderstandings

### Do Not Infer Snapshot Encoding From Surprise Alone

We previously treated the BOM in generated snapshot files as something that needed low-level investigation before confirming the upstream rule.

The better path is:

1. check the Verify README and conventions docs first
2. confirm the expected behavior there
3. only inspect raw bytes if the docs still leave uncertainty

### Use Upstream Docs Before Reconstructing The Workflow

If Verify behavior, setup, or approval flow feels unclear:

- do not start by reverse-engineering the generated files
- check the upstream README and getting-started wizard first

## Related NEXUS Decisions

- [`docs/decisions/0003-testing-stack-and-library-onboarding.md`](../../decisions/0003-testing-stack-and-library-onboarding.md)
- [`docs/decisions/0025-official-package-docs-first-and-local-package-reference-notes.md`](../../decisions/0025-official-package-docs-first-and-local-package-reference-notes.md)
