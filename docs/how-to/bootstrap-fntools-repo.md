# Bootstrap FnTools Repo

This runbook stages a standalone `FnTools` repo from the current `NEXUS-EMERGING` workspace.

Use it when you want a real repo root to start the extraction without hand-copying files.

## What It Does

The bootstrap script:

- copies the current `FnTools.FnHCI*` code projects into a new repo-shaped root
- copies the `FnTools.Tests` project
- copies the currently owned `FnTools` and `FnUI` docs
- renames the copied project files from the older `Nexus.FnHCI*` path names to `FnTools.FnHCI*`
- rewrites project references so the staged repo is self-contained
- creates:
  - `FnTools.slnx`
  - `README.md`
  - `.gitignore`
  - `bootstrap-source.toml`

## Command

```bash
./NEXUS-Code/scripts/bootstrap_fntools_repo.sh \
  --destination-root /tmp/FnTools
```

Useful options:

```bash
./NEXUS-Code/scripts/bootstrap_fntools_repo.sh \
  --destination-root /tmp/FnTools \
  --source-commit bb87d037 \
  --force
```

Dry run:

```bash
./NEXUS-Code/scripts/bootstrap_fntools_repo.sh \
  --destination-root /tmp/FnTools \
  --dry-run
```

## After Bootstrap

From the staged repo root:

```bash
dotnet build /tmp/FnTools/FnTools.slnx
dotnet run --project /tmp/FnTools/tests/FnTools.Tests/FnTools.Tests.fsproj
```

## Notes

- the current source project paths in `NEXUS-EMERGING` still use `Nexus.FnHCI*` folder names
- the staged repo renames those copied project files to the intended `FnTools.FnHCI*` names
- the bootstrap is a staging workflow, not yet a full remote-repo creation workflow
