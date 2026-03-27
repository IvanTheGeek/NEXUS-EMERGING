# Capture Codex Commit Checkpoint

This guide explains how to capture the current Codex local-session state, import it into NEXUS, and link it to a Git commit.

Use this when you want a durable path from:

- a commit you are looking at in GitHub
- back to the Codex chat that led to that commit

## What This Does

`capture-codex-commit-checkpoint` performs one explicit checkpoint flow:

1. reads the current Git `HEAD` commit from the target repo
2. exports the current Codex local-session state from `~/.codex`
3. archives that snapshot into `NEXUS-Objects/providers/codex/`
4. imports the archived snapshot into canonical history
5. writes a durable checkpoint manifest under:
   - `NEXUS-EventStore/work-batches/commit-checkpoints/<repo>/<commit>.toml`

That manifest records:

- repo identity
- branch name when present
- remote origin when present
- commit SHA and commit message
- Codex snapshot name and manifest
- Codex import ID and import manifest
- imported conversation hints

## Capture Command

Run this after making a commit:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- capture-codex-commit-checkpoint
```

## Useful Options

Capture against a different repo while still writing into the shared NEXUS object and event-store roots:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  capture-codex-commit-checkpoint \
  --repo-root /home/ivan/NEXUS/FnTools \
  --event-store-root /home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore \
  --objects-root /home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-Objects
```

Override the live Codex source root if needed:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  capture-codex-commit-checkpoint \
  --source-root /home/ivan/.codex
```

If you intentionally need to replace an existing checkpoint for the same commit:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  capture-codex-commit-checkpoint \
  --force
```

## Report Command

To look up the checkpoint later, use:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- report-codex-commit-checkpoint
```

Or use an explicit commit SHA copied from GitHub:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-codex-commit-checkpoint \
  --repo-root /home/ivan/NEXUS/FnTools \
  --commit <sha>
```

The report prints the linked import and the conversation hints captured at checkpoint time.

## Notes

- This is intentionally commit-linked, not prompt-batch inferred.
- One checkpoint manifest is stored per repo and commit SHA.
- The command refuses to overwrite an existing checkpoint unless you pass `--force`.
- This is the first layer of commit-to-chat traceability; richer exact-message linking can build on top of it later.
- If you want this to happen automatically after every commit, install the managed hook with [`install-codex-commit-checkpoint-hook`](install-codex-commit-checkpoint-hook.md).
