# Decision 0002: No Fast-Forward Merges Into Main

## Status

Accepted

## Decision

Accepted work should be merged into `main` using explicit merge commits with `--no-ff`.

## Why

- feature branches in this repository are durable lines of work, not disposable transport branches
- branch history is part of the project record and should remain inspectable later
- explicit merge commits make it easier to see when a work stream was accepted into `main`
- this fits the intent to preserve experiments, parser iterations, and ontology/modeling branches as meaningful history

## Consequences

- merges into `main` should use `git merge --no-ff <branch>`
- fast-forward-only merges are not the default policy for accepted branch work
- squash merges should be avoided when they would erase useful branch history
- branch names should remain meaningful because they are part of the durable record
