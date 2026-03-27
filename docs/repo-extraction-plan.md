# Repo Extraction Plan

This document is the practical extraction plan for the first repo splits out of `NEXUS-EMERGING`.

It is intentionally concrete.

Use it when we are ready to create:

- `FnTools`
- `CheddarBooks`

## Baseline

Use `main` at merge commit `bb87d037` as the extraction baseline.

That commit is the first converged line that includes:

- the current namespace-and-repo boundary decisions
- FnTools namespace correction in code
- CheddarBooks namespace correction in code
- split test projects by concern line

## Split Order

Split in this order:

1. `FnTools`
2. `CheddarBooks`

Why this order:

- `FnTools` is already self-contained
- `CheddarBooks` needs `FnTools.FnHCI.UI`
- extracting `FnTools` first gives `CheddarBooks` a cleaner downstream dependency target

## Shared Rules

For both repo extractions:

- treat `NEXUS` as the durable pre-split origin history
- start the new repo from a curated bootstrap commit
- record the source baseline commit hash in the new repo README and bootstrap commit message
- move only the docs that the new repo truly owns
- leave cross-line doctrine and concept notes in `NEXUS` with links outward
- require repo-local build and repo-local test success before calling the extraction complete

## Shared Inventory State Already Done

The following preparation work is already complete inside `NEXUS`:

- `FnTools.FnHCI*` namespaces exist in code
- `CheddarBooks.*` namespaces exist in code for LaundryLog
- test projects are physically split into:
  - `Nexus.Foundation.Tests`
  - `FnTools.Tests`
  - `CheddarBooks.Tests`
- root docs already distinguish:
  - `Nexus.*`
  - `FnTools.*`
  - `CheddarBooks.*`

## What Stays In NEXUS

These lines remain in `NEXUS` after the splits:

- ontology and doctrine
- ingestion and canonical history
- LOGOS
- CORTEX, ATLAS, FORGE as they emerge
- concept notes and broad project-memory docs
- cross-line decisions that are about the NEXUS paradigm itself

`NEXUS` remains the umbrella foundation repo.

## Immediate Deliverables Per Extracted Repo

Each new repo should ship with:

- a root README
- its own solution file
- its own test project(s)
- a minimal doc spine
- the current code it owns
- a bootstrap note pointing back to `NEXUS` baseline `bb87d037`

## Repo-Specific Plans

- [`repo-extraction/fntools.md`](repo-extraction/fntools.md)
- [`repo-extraction/cheddarbooks.md`](repo-extraction/cheddarbooks.md)

## Cutover Checklist

Before calling a split complete:

1. the new repo builds from its own root
2. the new repo tests pass from its own root
3. old project references pointing back into `NEXUS-EMERGING` are removed
4. the new repo README explains origin and purpose
5. `NEXUS` docs are updated to point to the new repo
6. active downstream branches are rebased or merged onto the new dependency line intentionally
