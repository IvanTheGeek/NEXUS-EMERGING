# 0006 Storage Roles By Bounded Context

## Status

Accepted

## Context

NEXUS is beginning to need a more practical secondary working layer over canonical history.

At the same time, future NEXUS work is likely to include:

- graph working indexes
- analytical exploration
- semantic retrieval and RAG
- later, possibly graph-native traversal or graph algorithms

These are related, but they are not the same job.

The risk is choosing one storage technology and then forcing it to carry meanings and workloads that belong to different bounded contexts.

NEXUS already has a strong separation:

- raw/object storage is not canonical truth
- canonical append-only history is not the graph working layer
- projections and derived graph work are not source truth

The same separation should apply to storage choices.

## Decision

Do not choose one universal database technology for all NEXUS concerns.

Prefer storage technologies by bounded context and job:

### Canonical History

Keep canonical append-only history in the current transparent Git-backed TOML event store.

This remains:

- durable source truth for the canonical history context
- rebuild source for projections and graph derivation
- independent of any later analytical or retrieval database

### Graph Working Layer

If NEXUS introduces a persisted local working index for graph slices and incremental materialization, prefer SQLite first.

Why:

- embedded and local
- simple operational footprint
- good for indexed lookups, counts, joins, and operator reports
- suitable as a rebuildable cache/materialization

SQLite is not a replacement for the canonical event store.
It is only a practical working index for the derived graph layer.
The repository may keep that index path stable for tooling and reports while leaving the generated SQLite files machine-local and rebuildable rather than treating them as durable source-controlled truth.

### Analytical Exploration

Prefer DuckDB later for analytical exploration when NEXUS needs large scans, aggregations, experimental query work, or analysis-oriented derived datasets.

Why:

- strong fit for analytical workloads
- useful for exploratory queries over large derived tables
- likely a better fit than SQLite for later LOGOS-style exploration and pattern discovery

DuckDB is not the first choice for the graph working layer merely because it is interesting analytically.
Its natural role is a later analytics context, not the first local graph-materialization cache by default.

### Semantic Retrieval And RAG

Treat vector databases as a separate retrieval context.

Use a vector database only when NEXUS needs embedding-based similarity search such as:

- semantic retrieval over notes, conversations, or documents
- retrieval-augmented prompting
- approximate nearest-neighbor search over embedded content

A vector database is not a substitute for:

- canonical history
- structured analytical queries
- graph derivation
- graph traversal

### Graph-Native Traversal

Defer graph databases unless NEXUS reaches a point where graph-native traversal, path queries, or graph algorithms are a dominant working need.

A graph database may become the right tool later if:

- multi-hop traversal becomes central
- graph-native query language and ergonomics matter more than rebuild simplicity
- graph algorithms become a core workflow rather than an occasional export or derived analysis

Until that need is concrete, NEXUS should prefer simpler, more transparent layers.

## Comparison Notes

### DuckDB vs Vector DB

DuckDB is primarily for structured analytical work:

- tables
- scans
- aggregations
- joins
- exploratory analysis

A vector database is primarily for similarity retrieval:

- embedding indexes
- nearest-neighbor search
- semantic recall

These solve different problems.
One does not replace the other.

### DuckDB vs Graph DB

DuckDB is strong for analytical exploration over derived graph-shaped data stored in tables.

A graph database is stronger when the main job is:

- traversal
- neighborhood expansion
- path finding
- graph-native query ergonomics

DuckDB can help analyze graph-derived data.
That is not the same as being the best home for graph-native operational queries.

### SQLite vs DuckDB

SQLite is the better first candidate for a small local working index with incremental updates and simple operator-facing queries.

DuckDB is the better candidate for later analytical exploration over larger derived datasets.

They are adjacent, not interchangeable.

## Consequences

- NEXUS should not collapse working index, analytics, retrieval, and graph traversal into one storage decision
- the current canonical event store remains the stable source layer
- if a persisted graph working index is introduced next, SQLite is the preferred first step
- the generated SQLite working index should remain rebuildable cache/materialization, not a required committed artifact
- DuckDB remains a likely later choice for analytics and discovery work
- vector storage remains a later LOGOS/retrieval concern, not a replacement for structured history
- graph databases remain deferred until graph-native workloads become clearly primary

## Notes

This decision keeps the NEXUS storage story aligned with bounded contexts instead of technology enthusiasm.

It also preserves the existing principle that source truth, derivation, interpretation, and retrieval should not be collapsed into one layer merely because a single tool can store all of them.
