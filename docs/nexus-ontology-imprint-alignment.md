# NEXUS Ontology Alignment v0

This note aligns the current `nexus-emerging` implementation with the newer ontology insight around causality, imprint, and interpretation.

It does not replace the current ingestion architecture.

It clarifies where this ontology fits relative to it.

## Core Alignment

The repository already separates:

- structure
- meaning
- derivation
- write-side history
- read-side projections

That means the new insight is mostly additive, not disruptive.

What changes is the clarity of one central role:

- `Imprint`

## Core Insight

The ontology should recognize two distinct relationships:

- causality produces imprints
- interpretation derives meaning from imprints

This is not a single required lifecycle.

It is explicitly not:

```text
Intent -> Event -> Fact -> View
```

That kind of forced chain collapses concerns that NEXUS is trying to keep separate.

## Imprint

Working definition:

> An Imprint is the persistent structural result of causality, serving as a source for interpretation.

Implications:

- an imprint exists as structure
- an imprint is produced by causality
- an imprint is consumed by interpretation
- an imprint is domain-neutral
- an imprint is not itself identical to `Event`, `Fact`, or `Projection`

## How This Fits the Existing Repo

### 1. Structure Layer

This still aligns with:

- `Node` as the structural primitive

The current implementation does not yet expose a single final `Node` abstraction everywhere, but the graph direction already assumes that structure is separate from meaning.

### 2. Meaning Layer

This is where the new insight lands.

`Imprint` should not become a new structural primitive.

It should be modeled as a role or meaning classification applied to structure.

That means:

- `Imprint` belongs in the semantic layer
- `Event` and `Fact` can be interpreted meanings of an imprint inside different bounded contexts
- `Record` can be another interpreted meaning in software-oriented contexts

### 3. Projection

The repository already treats projection as derived and rebuildable.

That is aligned.

The ontology update strengthens that:

- projection is not fundamental structure
- projection is interpretation or derivation output
- projection must not become source truth

### 4. Write and Read Separation

This was already a good direction in the repo.

The ontology update sharpens it by adding:

- causality and interpretation are also distinct

So the separation becomes:

- write-side observed history is not read-side projection
- causality is not interpretation
- source structure is not the same thing as derived meaning

## Important Bounded-Context Warning

The word `Event` is currently overloaded.

In the current ingestion implementation:

- `CanonicalEvent` means an append-only observed-history record

In the newer ontology:

- `Event` may mean an imprint interpreted in a time-based domain

These are not automatically the same concept.

That is acceptable, but it must be documented and kept context-specific.

## Recommendation for Code

Do not immediately hard-code the whole ontology into one giant discriminated union.

Instead:

- introduce a small semantic kernel
- keep it focused on stable naming primitives
- let richer ontology meaning accumulate through docs and later use cases

## Recommended Kernel Scope

The first `Nexus.Kernel` project should stay small and contain only:

- `RoleId`
- `RelationKindId`
- a few stable core role constants such as `imprint`
- a few stable relation constants describing causality, interpretation, and projection derivation

It should not yet contain:

- importer concerns
- provider concerns
- projection implementations
- the full NEXUS graph ontology
- a forced `Event | Fact | Record | Projection` hierarchy

## Current Recommendation

Yes, it is time to create a small internal kernel library inside this repository.

No, it is not yet time to treat that library as a final external contract for all future NEXUS systems.

The right move is:

1. document the ontology alignment
2. create a minimal kernel project
3. validate it against more than one bounded context before freezing more concepts
