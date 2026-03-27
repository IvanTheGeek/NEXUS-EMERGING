# 0019. Phased Repo Extraction For FnTools And CheddarBooks

## Status

Accepted

## Context

The repository now has a converged foundation baseline on `main` at merge commit `bb87d037`.

That baseline includes:

- the concern-line documentation spine
- the namespace boundary decision
- the first `FnTools.FnHCI*` namespace correction pass
- the first `CheddarBooks.*` namespace correction pass
- split test projects for:
  - `Nexus.Foundation.Tests`
  - `FnTools.Tests`
  - `CheddarBooks.Tests`

The lines are now distinct enough that separate repos make sense, but they are not equally ready at the same time.

`FnTools` is currently the easier extraction:

- its code surface is small and self-contained
- its tests are already isolated
- its current projects do not depend on `Nexus.*`

`CheddarBooks` is also becoming separable, but it depends on the reusable interaction line that belongs in `FnTools`.

## Decision

Use `main` at `bb87d037` as the first extraction baseline.

Extract repos in this order:

1. `FnTools`
2. `CheddarBooks`

Use a phased curated-snapshot extraction strategy instead of trying to perfect multi-path history rewriting first.

That means:

- `NEXUS` remains the durable origin history for the pre-split exploration and convergence work
- new repos begin from a curated bootstrap commit that explicitly records the source `NEXUS` commit
- each extracted repo should have its own clean root structure, docs spine, tests, and release cadence

`FnTools` should be extracted first and made packageable before `CheddarBooks` is extracted.

`CheddarBooks` should then consume `FnTools` as a library dependency rather than continuing to treat those projects as in-repo siblings.

## Consequences

Positive:

- extraction can proceed without depending on specialized git-history rewriting tools
- `FnTools` and `CheddarBooks` get cleaner repo roots from day one
- the dependency direction becomes clearer:
  - `CheddarBooks.*` depends on `FnTools.*`
  - `FnTools.*` may depend on stable `Nexus.*` only when that is truly needed
- pre-split exploration history remains preserved in `NEXUS`

Tradeoffs:

- exact per-file history does not move into the new repos automatically
- provenance must be recorded deliberately in the bootstrap commit, README, and tags
- some project-path and file-name cleanup still needs to happen as part of extraction

## Operational Rule

Do not extract `CheddarBooks` until `FnTools` has:

- repo-local build success
- repo-local test success
- stable package identifiers for `FnTools.FnHCI*`
- a chosen immediate consumption mode for downstream repos

## Related

- [`0018-namespace-and-repo-boundaries-by-line.md`](0018-namespace-and-repo-boundaries-by-line.md)
- [`../repo-extraction-plan.md`](../repo-extraction-plan.md)
- [`../repo-extraction/fntools.md`](../repo-extraction/fntools.md)
- [`../repo-extraction/cheddarbooks.md`](../repo-extraction/cheddarbooks.md)
