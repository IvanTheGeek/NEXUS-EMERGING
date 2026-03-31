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

After the first baseline capture, the Codex checkpoint export now works incrementally:

- `providers/codex/latest/` is maintained as the full current source snapshot
- each commit-checkpoint archive still writes its own manifest under `providers/codex/archive/...`
- those archive snapshots always include `session_index.jsonl`
- unchanged transcript files are omitted from the archive snapshot
- only changed transcript files are imported from the checkpoint archive
- the shared checkpoint workflow now takes an internal file gate before writing shared object-store and event-store roots

That keeps commit checkpoints faster without changing canonical truth boundaries.

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
- The first capture is usually the heaviest one because it establishes the baseline Codex snapshot.
- When several repos share the same NEXUS object and event-store roots, checkpoint capture is serialized internally so overlapping post-commit hooks do not trample the shared `providers/codex/latest/` surface.
- This is the first layer of commit-to-chat traceability; richer exact-message linking can build on top of it later.
- If you want this to happen automatically after every commit, install the managed hook with [`install-codex-commit-checkpoint-hook`](install-codex-commit-checkpoint-hook.md).
