namespace Nexus.Domain

open System

type NodeKind =
    | ConversationNode
    | MessageNode
    | ArtifactNode
    | DomainNode
    | BoundedContextNode
    | LensNode
    | FactNode
    | OtherNode of string

type EdgeKind =
    | BelongsToConversation
    | ReferencesArtifact
    | ObservedDuringImport
    | SupportsFact
    | LocatedInDomain
    | InterpretedWithinContext
    | ViewedThroughLens
    | OtherEdge of string

type GraphValue =
    | StringValue of string
    | IntValue of int64
    | DecimalValue of decimal
    | BoolValue of bool
    | TimestampValue of DateTimeOffset

type GraphTerm =
    | NodeRef of NodeId
    | Literal of GraphValue

type GraphAssertion =
    { FactId: FactId
      Subject: NodeId
      Predicate: EdgeKind
      Object: GraphTerm
      DomainId: DomainId option
      BoundedContextId: BoundedContextId option
      LensId: LensId option
      Provenance: FactProvenance }
