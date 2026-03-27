# Install Codex Commit Checkpoint Hook

This guide explains how to install a managed Git `post-commit` hook that automatically captures a Codex commit checkpoint after each commit.

Use this when you want commit-to-chat traceability to happen by default instead of relying on memory.

## What This Does

`install-codex-commit-checkpoint-hook` writes or refreshes a managed NEXUS block inside:

- `.git/hooks/post-commit`

That managed block:

1. resolves the repo root for the current commit
2. runs `capture-codex-commit-checkpoint`
3. writes checkpoint output and failures to:
   - `.git/nexus-hooks/codex-commit-checkpoint.log`

Important behavior:

- existing hook content is preserved when possible
- only the NEXUS-managed block is replaced on re-install
- checkpoint capture failures do not block the commit itself

## Command

Install the hook for the current repo:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- install-codex-commit-checkpoint-hook
```

Install it for a sibling repo such as `FnTools`:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  install-codex-commit-checkpoint-hook \
  --repo-root /home/ivan/NEXUS/FnTools
```

Install it for `CheddarBooks` while keeping the shared NEXUS object and event-store roots:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  install-codex-commit-checkpoint-hook \
  --repo-root /home/ivan/NEXUS/CheddarBooks \
  --objects-root /home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-Objects \
  --event-store-root /home/ivan/NEXUS/NEXUS-EMERGING/NEXUS-EventStore
```

## After Installation

Make a commit in the target repo as usual.

After the commit completes, the hook should capture a checkpoint automatically.

To look it up later:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  report-codex-commit-checkpoint \
  --repo-root /home/ivan/NEXUS/FnTools \
  --commit <sha>
```

## Notes

- This is the recommended next step after enabling commit-linked checkpoints.
- The hook is intentionally post-commit, not pre-commit, because it needs a stable committed `HEAD`.
- The installer is idempotent for the managed NEXUS block.
- If `dotnet` is missing when the hook runs, the hook prints a warning and skips capture.
