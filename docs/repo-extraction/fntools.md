# FnTools Repo Extraction

This is the concrete extraction plan for the first `FnTools` repo.

## Goal

Create a reusable-library repo that owns the current `FnTools.FnHCI*` line and can be consumed by downstream apps such as `CheddarBooks.LaundryLog`.

## Current Owned Inventory In NEXUS

### Code Projects

- `NEXUS-Code/src/Nexus.FnHCI/`
- `NEXUS-Code/src/Nexus.FnHCI.UI/`
- `NEXUS-Code/src/Nexus.FnHCI.UI.Blazor/`

These project paths still carry older `Nexus.FnHCI` names, but the code namespaces now use:

- `FnTools.FnHCI`
- `FnTools.FnHCI.UI`
- `FnTools.FnHCI.UI.Blazor`

### Tests

- `NEXUS-Code/tests/FnTools.Tests/`

### Docs That Move

- [fntools-foundation.md](../fntools-foundation.md)
- [fnui-foundation.md](../fnui-foundation.md)
- [fnhci-namespace-map.md](../fnhci-namespace-map.md)
- [fnhci-ui-blazor-requirements.md](../fnhci-ui-blazor-requirements.md)
- [fnhci-ui-web-requirements.md](../fnhci-ui-web-requirements.md)
- [fnhci-ui-native-host-requirements.md](../fnhci-ui-native-host-requirements.md)
- [fnhci-conversation-reading-surface.md](../fnhci-conversation-reading-surface.md)
- [decisions/0015-fnhci-owns-the-top-interaction-namespace.md](../decisions/0015-fnhci-owns-the-top-interaction-namespace.md)

### Docs That Stay In NEXUS

- [concepts/fnhci.md](../concepts/fnhci.md)
  this is still project-memory and concept history
- [decisions/0018-namespace-and-repo-boundaries-by-line.md](../decisions/0018-namespace-and-repo-boundaries-by-line.md)
  this is a NEXUS boundary/governance decision
- [repository-concern-lines.md](../repository-concern-lines.md)
  this remains the umbrella concern-line map

## Recommended New Repo Root Shape

```text
FnTools/
  README.md
  FnTools.slnx
  src/
    FnTools.FnHCI/
    FnTools.FnHCI.UI/
    FnTools.FnHCI.UI.Blazor/
  tests/
    FnTools.Tests/
  docs/
    foundation.md
    namespace-map.md
    fnui-requirements.md
    fnui-web-requirements.md
    fnui-native-host-requirements.md
    conversation-reading-surface.md
    decisions/
      0001-fnhci-owns-the-top-interaction-namespace.md
```

## Extraction Steps

1. Create the new `FnTools` repo from the `NEXUS` baseline `bb87d037`.
2. Use the bootstrap runbook and script to stage the owned projects, tests, and docs into the new repo root.
3. Rename project folders and project files:
   - `Nexus.FnHCI` -> `FnTools.FnHCI`
   - `Nexus.FnHCI.UI` -> `FnTools.FnHCI.UI`
   - `Nexus.FnHCI.UI.Blazor` -> `FnTools.FnHCI.UI.Blazor`
4. Update assembly names, package IDs, and solution entries to match.
5. Remove any lingering `Nexus.*` naming that is only historical scaffolding.
6. Add a bootstrap README section that says this repo was extracted from `NEXUS-EMERGING` at `bb87d037`.
7. Build and run `FnTools.Tests` from the new repo root.
8. Tag the first clean extraction baseline in the new repo.

## Immediate Success Criteria

- repo-local `dotnet build` succeeds
- repo-local `FnTools.Tests` succeeds
- no project reference points back into `NEXUS-EMERGING`
- namespaces, project names, and folder names all consistently use `FnTools.FnHCI*`
- the repo can be consumed as a library line by downstream repos

## Immediate Follow-Up After Extraction

After the repo exists and builds cleanly:

1. choose the first package naming/versioning convention
2. decide whether initial downstream consumption is:
   - private/local package feed
   - GitHub Packages
   - direct source linkage only during bootstrap
3. update `NEXUS` docs to point to the new repo for active `FnTools` work

## Why FnTools Goes First

`FnTools` is the easiest split because:

- it has no current dependency on `Nexus.*` runtime projects
- its tests are already isolated
- it is the upstream dependency for `CheddarBooks`

## Runbook

- [`../how-to/bootstrap-fntools-repo.md`](../how-to/bootstrap-fntools-repo.md)
