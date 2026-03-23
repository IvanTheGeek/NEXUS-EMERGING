namespace Nexus.Domain

open System

/// <summary>
/// The minimal node categories used by the thin NEXUS graph layer.
/// </summary>
type NodeKind =
    | ConversationNode
    | MessageNode
    | ArtifactNode
    | DomainNode
    | BoundedContextNode
    | LensNode
    | FactNode
    | OtherNode of string

/// <summary>
/// The minimal edge categories used by the thin NEXUS graph layer.
/// </summary>
type EdgeKind =
    | BelongsToConversation
    | ReferencesArtifact
    | ObservedDuringImport
    | SupportsFact
    | LocatedInDomain
    | InterpretedWithinContext
    | ViewedThroughLens
    | OtherEdge of string

/// <summary>
/// The literal value kinds that may appear in graph assertions.
/// </summary>
type GraphValue =
    | StringValue of string
    | IntValue of int64
    | DecimalValue of decimal
    | BoolValue of bool
    | TimestampValue of DateTimeOffset

/// <summary>
/// A graph assertion object, either another node or a literal value.
/// </summary>
type GraphTerm =
    | NodeRef of NodeId
    | Literal of GraphValue

/// <summary>
/// A provenance-linked assertion in the thin graph layer.
/// </summary>
/// <remarks>
/// The graph layer is intentionally derived and evolvable over canonical history.
/// Full notes: docs/nexus-core-conceptual-layers.md
/// </remarks>
type GraphAssertion =
    { FactId: FactId
      Subject: NodeId
      Predicate: EdgeKind
      Object: GraphTerm
      DomainId: DomainId option
      BoundedContextId: BoundedContextId option
      LensId: LensId option
      Provenance: FactProvenance }
