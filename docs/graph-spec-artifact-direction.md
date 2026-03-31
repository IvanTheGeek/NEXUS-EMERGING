# Graph, Spec, And Running Artifact Direction

This note records the current NEXUS direction for how application and system source truth may evolve over time.

## Current Short Answer

Yes, that direction is broadly correct.

The likely long-term shape is:

1. deeper semantic substrate
2. derived spec or lens surfaces
3. FORGE-style compilation or generation
4. running artifact

But we are not fully there yet.

## Current Refinement

The important refinement is:

- the graph is a likely deeper semantic substrate
- but not every concern should be forced into one graph prematurely
- durable notes, path definitions, bounded contexts, and explicit lenses still matter
- spec surfaces will likely sit between the graph and the running artifact

So the direction is not simply:

- graph in
- app out

It is more like:

- durable semantics
- graph-backed structural substrate where appropriate
- lens- or domain-specific spec projection
- FORGE transforms
- running artifact

## For Application Lines Like LaundryLog

The likely future shape is:

- business meaning becomes durable and explicit
- graph structure can carry important relationships and state transitions
- selected projections produce reviewable specs
- FORGE turns those specs into code and later into running artifacts

Examples of possible specs:

- UI/view contracts
- path definitions
- event and command structures
- component or token specifications
- deployment or integration specifications

## For NEXUS And FnHCI Lines

The same overall direction should eventually apply more broadly too.

That means:

- NEXUS itself should become more modelable in its own terms
- FnHCI and related interaction lines should also become more modelable
- reusable libraries and app lines should be able to ride the same broader pattern

But this does not mean all of those lines are equally mature today.

## What Is Still Missing

We are not fully there yet because several layers are still emerging:

- stable graph ontology for the relevant domains
- stable spec surfaces
- explicit FORGE compilation boundaries
- deterministic transforms from model to artifact
- enough proven examples across more than one app or tool line

## Current Working Rule

For now:

- keep the durable business meaning explicit
- keep the graph as an important emerging substrate, not a forced total abstraction
- keep specs reviewable
- keep generated or compiled artifacts downstream from those durable meanings

That keeps the system honest while still moving toward the longer-term graph/spec/artifact direction.

For the current clarification that Event Modeling is one lens over a fuller model rather than the whole model, see [ATLAS, Lenses, And The Fuller Model](atlas-lenses-and-full-model-direction.md).
