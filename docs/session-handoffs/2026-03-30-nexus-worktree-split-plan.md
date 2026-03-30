# Handoff: NEXUS Worktree Split Plan

## Where Things Are

- repo: `NEXUS-EMERGING`
- current state:
  - `11` tracked modified paths
  - `8951` untracked paths
- dominant dirty area:
  - `NEXUS-EventStore/` with `8945` changed or new paths
- secondary dirty area:
  - docs-site bootstrap and docs-structure work under `docs/`, plus `mkdocs.yml` and `requirements-docs.txt`

This note splits the current dirty tree into intentional workstreams so later cleanup and commits can happen deliberately instead of as one mixed bundle.

## Workstream 1: EventStore Import / Projection Batch

### Scope

- `NEXUS-EventStore/events/`:
  about `8702` new paths
- `NEXUS-EventStore/imports/`:
  about `205` new paths
- `NEXUS-EventStore/projections/`:
  about `36` changed or new paths
- `NEXUS-EventStore/snapshots/`:
  new content present
- `NEXUS-EventStore/work-batches/`:
  new content present
- one tracked projection file is also modified:
  - `NEXUS-EventStore/projections/conversations/019d1a55-9f2f-7283-a6ef-fb48cb23bf6f.toml`

### What Completion Looks Like

- confirm this was an intentional ingestion/import run, not accidental local churn
- verify the new `imports/`, `events/`, `projections/`, `snapshots/`, and `work-batches/` belong to the same durable batch
- decide whether the changed tracked projection should ride with this batch
- if the batch is valid, commit it as one explicit EventStore checkpoint
- if the batch is scratch or partial, move it out of the repo worktree instead of leaving it mixed into docs work

### Risks

- this is append-only historical material, so corrections should be additive
- partial staging here is risky unless the batch boundaries are truly understood
- this workstream should stay isolated from docs-site and doctrine commits

## Workstream 2: Docs-Site Bootstrap

### Scope

Untracked docs-site files:

- `mkdocs.yml`
- `requirements-docs.txt`
- `docs/repo-overview.md`
- `docs/how-to/preview-docs-site.md`
- `docs/decisions/README.md`
- `docs/assets/stylesheets/light-matrix.css`

Tracked files already moving in the same direction:

- `.gitignore`
- `docs/index.md`
- `docs/how-to/README.md`
- `docs/research/README.md`
- `docs/cortex-repo-memory-protocol.md`
- `docs/agent-readme.md`

### What Completion Looks Like

- create the local virtualenv described in `docs/how-to/preview-docs-site.md`
- install `requirements-docs.txt`
- run `mkdocs serve` or `mkdocs build`
- confirm the docs site nav works and the new overview pages resolve
- commit the docs-site bootstrap as its own workstream

### Risks

- `docs/agent-readme.md` is shared with a separate doctrine update, so hunk staging may be needed if the commits should stay split
- avoid bundling docs-site bootstrap with EventStore history

## Workstream 3: Branch Naming / Workflow Preference

### Scope

- `README.md`
- `docs/decisions/0008-branch-topology-by-workstream.md`
- `docs/how-to/git-auth-for-codex.md`

### What Completion Looks Like

- confirm the preferred branch naming rule is:
  - describe the workstream itself
  - avoid agent-qualified prefixes unless they add real meaning
- make sure examples and push instructions align with that rule
- commit this as a small doctrine/workflow update

### Risks

- easy to accidentally bury this inside the docs-site bootstrap unless staged intentionally

## Workstream 4: Command / Event / View Presentation Rule

### Scope

The local upstream doctrine edits now in flight are:

- `docs/agent-readme.md`
- `docs/interaction-concern-lines-contexts-and-lenses.md`

The intended rule is:

- do not flatten modeling into a fixed linear `COMMAND -> EVENT -> VIEW` triplet
- present command slices as producing event fact(s)
- present view slices as consuming prior event fact(s)
- allow the consumed event to be earlier than the immediately previous slice
- allow multiple views to consume the same prior event

### What Completion Looks Like

- commit these guidance edits as a small doctrine/AI-instruction update
- keep this separate from EventStore history
- decide whether this should be staged as its own small commit or ride with the docs-site bootstrap if hunk separation becomes too costly

### Risks

- `docs/agent-readme.md` overlaps the docs-site bootstrap edits, so this may require hunk staging if the commit boundary must stay pure

## Suggested Commit Order

Recommended order if all of this is kept:

1. branch naming / workflow preference
2. command/event/view presentation rule
3. docs-site bootstrap
4. EventStore import / projection batch

Why this order:

- the first three are small, understandable doc/doctrine commits
- the EventStore batch is by far the largest and deserves a dedicated checkpoint after the smaller noise is removed

Alternative safe order:

1. docs/doctrine commits first
2. verify the EventStore batch separately
3. commit EventStore batch last as its own historical checkpoint

## Immediate Next Likely Step

- decide whether the EventStore batch is intentional and should be preserved now
- if not handling EventStore first, isolate and commit the small docs/doctrine workstreams before returning to the batch

## Read Next

- `docs/how-to/preview-docs-site.md`
- `docs/agent-readme.md`
- `docs/interaction-concern-lines-contexts-and-lenses.md`
- `docs/decisions/0008-branch-topology-by-workstream.md`
- `NEXUS-EventStore/imports/`
- `NEXUS-EventStore/work-batches/`
