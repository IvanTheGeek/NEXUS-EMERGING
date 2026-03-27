# CheddarBooks Repo Extraction

This is the concrete extraction plan for the first `CheddarBooks` repo.

## Goal

Create the first concrete product/app repo for the `CheddarBooks` line, beginning with `LaundryLog` and leaving room for later `PerDiemLog` and broader `CheddarBooks` applications.

## Preconditions

Do not extract `CheddarBooks` until `FnTools` is already extracted and usable as a downstream dependency.

At minimum:

- the `FnTools` repo exists
- `FnTools.FnHCI.UI` is buildable outside `NEXUS`
- a consumption mode is chosen for `FnTools` packages or source dependencies

## Current Owned Inventory In NEXUS

### Code Projects

- `NEXUS-Code/src/Nexus.CheddarBooks.LaundryLog/`
- `NEXUS-Code/src/Nexus.CheddarBooks.LaundryLog.UI/`

The code namespaces now use:

- `CheddarBooks.LaundryLog`
- `CheddarBooks.LaundryLog.UI`

### Tests

- `NEXUS-Code/tests/CheddarBooks.Tests/`

### Docs That Move

- `docs/application-domains/cheddarbooks-foundation.md`
- `docs/application-domains/cheddarbooks/README.md`
- `docs/application-domains/cheddarbooks/laundrylog/README.md`
- `docs/application-domains/cheddarbooks/laundrylog/introduction.md`
- `docs/application-domains/cheddarbooks/laundrylog/requirements.md`
- `docs/application-domains/cheddarbooks/laundrylog/product.md`
- `docs/application-domains/cheddarbooks/laundrylog/domain-model.md`
- `docs/application-domains/cheddarbooks/laundrylog/privacy-and-ownership.md`
- `docs/application-domains/cheddarbooks/laundrylog/delivery.md`
- `docs/application-domains/cheddarbooks/laundrylog/convergence.md`
- `docs/application-domains/cheddarbooks/laundrylog/data-sync-boundaries.md`
- `docs/application-domains/cheddarbooks/laundrylog/view-contracts.md`
- `docs/application-domains/cheddarbooks/laundrylog/screens.md`
- `docs/application-domains/cheddarbooks/laundrylog/workflows.md`

### Docs That Stay In NEXUS

- `docs/application-domains/cheddar/README.md`
  umbrella portfolio and brand context
- `docs/decisions/0018-namespace-and-repo-boundaries-by-line.md`
  this is still the umbrella boundary decision
- cross-line concept and doctrine notes

## Recommended New Repo Root Shape

```text
CheddarBooks/
  README.md
  CheddarBooks.slnx
  src/
    CheddarBooks.LaundryLog/
    CheddarBooks.LaundryLog.UI/
  tests/
    CheddarBooks.Tests/
  docs/
    foundation.md
    laundrylog/
      introduction.md
      requirements.md
      product.md
      domain-model.md
      privacy-and-ownership.md
      delivery.md
      convergence.md
      data-sync-boundaries.md
      view-contracts.md
      screens.md
      workflows.md
```

## Extraction Steps

1. Create the new `CheddarBooks` repo from the `NEXUS` baseline `bb87d037` or later updated baseline after the `FnTools` extraction is stable.
2. Copy the owned projects, tests, and docs into the new repo root.
3. Rename project folders and project files:
   - `Nexus.CheddarBooks.LaundryLog` -> `CheddarBooks.LaundryLog`
   - `Nexus.CheddarBooks.LaundryLog.UI` -> `CheddarBooks.LaundryLog.UI`
4. Replace the in-repo project reference to `Nexus.FnHCI.UI` with the chosen `FnTools.FnHCI.UI` dependency form.
5. Add a bootstrap README section that says this repo was extracted from `NEXUS-EMERGING` and notes the source baseline commit.
6. Build and run `CheddarBooks.Tests` from the new repo root.
7. Tag the first clean extraction baseline in the new repo.

## Immediate Success Criteria

- repo-local `dotnet build` succeeds
- repo-local `CheddarBooks.Tests` succeeds
- no project reference points back into `NEXUS-EMERGING`
- no project reference points to old `Nexus.CheddarBooks.*` names
- the app line is documented as `CheddarBooks`, not as a NEXUS subsystem

## Dependency Direction After Extraction

Expected direction:

- `CheddarBooks.LaundryLog` has no required dependency on `Nexus.*`
- `CheddarBooks.LaundryLog.UI` depends on `FnTools.FnHCI.UI`
- `CheddarBooks` may later depend on other stable reusable libraries, but should not depend on `NEXUS` doctrine repos directly for ordinary app runtime code

## Immediate Follow-Up After Extraction

After the repo exists and builds cleanly:

1. create the first app shell and event-modeling path docs in the new repo
2. move active LaundryLog implementation work there
3. leave NEXUS with outward links instead of continuing to host the app code
