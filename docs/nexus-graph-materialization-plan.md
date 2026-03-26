# NEXUS Graph Materialization Plan

## Purpose

This note captures the next architectural step for the derived graph layer after the first real rebuild benchmarks on March 24, 2026.

The goal is not to replace the current model.

The goal is to make graph work more practical while preserving the existing rules:

- canonical history remains append-only source truth
- graph assertions remain derived
- full rebuild from canonical source remains possible
- scoped views and lenses remain first-class goals

## Benchmark Snapshot

### Full Rebuild

Post-CPU-upgrade benchmark on the real local event store:

- canonical event files scanned: `17,820`
- distinct graph assertions derived: `89,254`
- derivation phase: `01:19:52`
- full rebuild wall-clock: `01:20:31`
- peak resident memory: about `454 MB`

Earlier baseline before the VM CPU increase:

- canonical event files scanned: `17,302`
- distinct graph assertions derived: `84,744`
- full rebuild wall-clock: `01:29:26`

### Additive Import

Benchmark using the March 23, 2026 ChatGPT export against a temp copy of the committed pre-import event store:

- conversations seen: `88`
- messages seen: `5,599`
- artifact references seen: `71`
- new canonical events appended: `515`
- duplicates skipped: `5,246`
- workflow-reported completion: `00:00:03`
- end-to-end CLI wall-clock: `00:00:09.44`

## What The Benchmarks Mean

The important pattern is:

- canonical import is already incremental and relatively cheap
- full graph rebuild is still a full-source operation and is expensive
- RAM is not the bottleneck
- I/O is present but not obviously saturated
- CPU helps, but the current rebuild path does not scale linearly with more cores

This suggests that the next major gains will come from changing the graph materialization flow rather than adding more hardware.

## Working Rule

Full graph rebuild from canonical source is a heavyweight maintenance operation.

It should remain available because:

- it is the correctness fallback
- it proves the graph can be reconstructed from canonical history
- it protects NEXUS from depending on an opaque mutable cache

But it should not be treated as the normal feedback loop for day-to-day work.

## Recommended Layering

The graph side should be treated as three distinct layers:

### 1. Canonical History

This remains the durable source of truth:

- append-only canonical events
- import manifests
- projections derived from canonical history

### 2. Durable Derived Assertion Layer

This is the current `graph/assertions/*.toml` layer:

- rebuildable from canonical history
- durable enough to inspect, diff, and export
- still too expensive to regenerate casually at full scale

This layer should remain because it preserves a transparent derived history format.

### 3. Secondary Graph Working Layer

This is the missing piece.

It should be a local materialized working substrate optimized for:

- interactive graph queries
- visualization preparation
- batch generation
- lens experimentation
- incremental updates

This layer is not source truth.
It is a practical working index/cache/materialization over the durable derived graph.

## Design Direction

### Keep Full Rebuilds Explicit

The `rebuild-graph-assertions` command should remain a deliberate operation.

Likely future UX:

- clear warning/help text that it rewrites the full derived graph layer
- optional explicit confirmation or `--yes` flag for large real stores
- rebuild metrics captured in a small manifest/log

### Prefer Incremental Materialization

Most working updates should happen incrementally from the import path, not through full rebuild.

Practical units of work:

- import-scoped updates
- provider-scoped updates
- conversation-scoped updates
- artifact-scoped updates
- later lens-scoped or domain-scoped updates

### Prefer Scoped Work

Most human workflows do not need the whole graph at once.

The common working unit is more likely to be:

- a conversation cluster
- a concept cluster such as `FnHCI`
- a provider scope
- an import batch
- a domain or bounded-context scope

This aligns with the earlier Graphviz experiments: smaller scoped graphs are more useful than one giant picture.

## Chunking, Distribution, And GPU

### Is The Full Graph Always Needed?

No.

Most actual work should be chunked or sliced.
The full graph is important for correctness checks, complete exports, and periodic rebuild validation, but it should not be the only mode.

### Can Graph Work Be Split Across Machines?

Yes, eventually.

That becomes practical once derivation/materialization is explicit about shard boundaries such as:

- import
- provider
- conversation
- domain

At that point, jobs can be processed independently and merged by deterministic fact IDs.

This is a future direction, not a current requirement.

### Would GPU Help?

Probably not yet.

The current workload is dominated by:

- parsing
- string/key normalization
- dictionary/set work
- many small filesystem operations

That is not the kind of workload where a GPU is likely to be the first meaningful win.

## Secondary Working Layer Options

The secondary working layer should be designed as an implementation detail behind a stable materialization boundary.

Candidate shapes:

- SQLite-backed working graph index
- DuckDB-backed analytic read model
- later specialized graph store if the need becomes real

Current recommendation:

- prefer a simple local embedded store first
- keep the durable TOML layers
- do not force a heavyweight external service too early

See also:

- `docs/decisions/0006-storage-roles-by-bounded-context.md`

## Recommended Next Implementation Steps

1. Add explicit heavyweight-language and confirmation flow around full graph rebuilds.
2. Record rebuild metrics in a small manifest/log so performance changes can be compared over time.
3. Introduce a graph materializer abstraction separate from full TOML assertion rebuild.
4. Implement a first local secondary working layer for incremental graph updates.
5. Update the import path so new canonical events can feed incremental graph materialization without forcing a full rebuild.
6. Keep Graphviz and later FnHCI visualization workflows focused on slices first, full graph second.

## Practical Implication For The Current Repo

For now:

- keep canonical import incremental
- keep full graph rebuild available and truthful
- stop treating full rebuild as the default follow-up after every import
- build the secondary graph working layer next

That gives NEXUS a better practical flow without giving up correctness or rebuildability.
