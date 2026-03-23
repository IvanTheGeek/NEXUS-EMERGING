# F# Documentation Convention

This document defines the documentation convention for F# code in NEXUS.

## Purpose

NEXUS wants documentation to be useful in three places at once:

- IntelliSense and quick source navigation
- conceptual repo docs for humans and AI collaborators
- tests as executable examples of behavior

Because F# xmldoc is attached to source symbols via `///` comments, and F# does not currently support the richer external-XML inclusion workflow that some C#-oriented ecosystems rely on, the practical pattern is:

- concise xmldoc in source
- expanded Markdown docs in the repo
- behavioral examples in tests

## Rule Of Thumb

- source xmldoc explains API intent
- Markdown docs explain concepts, rationale, and workflows
- tests demonstrate behavior

Short version:

- TOC docs teach meaning
- examples teach usage
- source teaches truth
- XML docs teach exact API details

## Minimum Xmldoc Standard

For public F# code, the minimum expectation is:

- every public type has a `/// <summary>`
- every public function/member has a `/// <summary>`
- add `/// <param>` for non-obvious parameters
- add `/// <typeparam>` for generic type parameters when relevant
- add `/// <returns>` when the result meaning is not obvious from the signature
- add `/// <remarks>` only when there is a key invariant, caution, semantic note, or stable link to fuller docs

The source comment should stay short enough to help in IntelliSense without drowning the code.

## Recommended Pattern

Use layers:

1. In source
   one-sentence purpose, plus one key invariant/caution if needed
2. In Markdown
   full explanation, rationale, examples, diagrams, related concepts
3. In tests
   proof of behavior and concrete usage

## Preferred Xmldoc Shape

```fsharp
/// <summary>
/// One-sentence API-facing purpose.
/// </summary>
/// <remarks>
/// Key invariant or caution.
/// Full notes: docs/path/to/file.md
/// </remarks>
val someFunction : Input -> Output
```

For a more detailed function:

```fsharp
/// <summary>
/// Creates a canonical import manifest from an observed import run.
/// </summary>
/// <param name="counts">Observed and appended counts for the import.</param>
/// <returns>The canonical manifest written to the event store.</returns>
/// <remarks>
/// Preserves append-only import history.
/// Full notes: docs/nexus-ingestion-architecture.md
/// </remarks>
val buildImportManifest : ImportCounts -> ImportManifest
```

## What Belongs In Markdown

Use repo docs for:

- domain meaning
- workflow descriptions
- event-flow explanations
- invariants and tradeoffs
- architecture rationale
- examples longer than a quick snippet
- diagrams and broader narrative

A good conceptual doc shape is:

```markdown
# Concept Name

## Purpose

## Inputs

## Rules / Invariants

## Output

## Event Flow

## Examples

## Related Symbols
```

## What To Avoid

- do not repeat the type signature in prose
- do not write large essay-like xmldoc blocks on every function
- do not rely on XML docs alone for domain explanation
- do not link to unstable doc paths
- do not omit in-source summaries just because a Markdown doc exists

## Guidance For AI-Friendly Libraries

To make a library easier for AI agents to use correctly:

- keep a docs spine in the repo
- document public entry points with concise xmldoc
- document config types and defaults clearly
- document semantic differences between sibling APIs
- add stable links from xmldoc into the fuller docs when needed
- keep examples and tests readable enough to act as working guidance

The goal is not “more docs everywhere.” The goal is the right amount of documentation in the right layer.
