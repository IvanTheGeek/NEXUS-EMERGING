# Bootstrap CheddarBooks Repo

This runbook stages a standalone `CheddarBooks` repo from the current `NEXUS-EMERGING` workspace.

Use it when you want a real repo root to start the extraction without hand-copying files.

## What It Does

The bootstrap script:

- copies the current `CheddarBooks.LaundryLog*` code projects into a new repo-shaped root
- copies the `CheddarBooks.Tests` project
- copies the current LaundryLog docs
- renames the copied project files from the older `Nexus.CheddarBooks*` path names to `CheddarBooks.*`
- rewrites project references so the staged repo is self-contained except for a bootstrap dependency on a local `FnTools` repo root
- creates:
  - `CheddarBooks.slnx`
  - `README.md`
  - `.gitignore`
  - `bootstrap-source.toml`

## Command

```bash
./NEXUS-Code/scripts/bootstrap_cheddarbooks_repo.sh \
  --destination-root /tmp/CheddarBooks \
  --fntools-root /home/ivan/NEXUS/FnTools
```

Useful options:

```bash
./NEXUS-Code/scripts/bootstrap_cheddarbooks_repo.sh \
  --destination-root /tmp/CheddarBooks \
  --fntools-root /home/ivan/NEXUS/FnTools \
  --source-commit 1ee17e03 \
  --force
```

Dry run:

```bash
./NEXUS-Code/scripts/bootstrap_cheddarbooks_repo.sh \
  --destination-root /tmp/CheddarBooks \
  --fntools-root /home/ivan/NEXUS/FnTools \
  --dry-run
```

## After Bootstrap

From the staged repo root:

```bash
dotnet build /tmp/CheddarBooks/CheddarBooks.slnx
dotnet run --no-build --project /tmp/CheddarBooks/tests/CheddarBooks.Tests/CheddarBooks.Tests.fsproj
```

## Notes

- the current source project paths in `NEXUS-EMERGING` still use older `Nexus.CheddarBooks*` folder names
- the staged repo renames those copied project files to the intended `CheddarBooks.*` names
- this first bootstrap uses a local `FnTools` repo root directly instead of a package feed
- later we can switch the UI dependency to packaged `FnTools.FnHCI.UI`
