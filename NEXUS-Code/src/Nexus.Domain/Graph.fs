namespace Nexus.Domain

open System
open Nexus.Kernel

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
    | HasSemanticRole
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

/// <summary>
/// Applies a semantic role to a node without altering the node's structural identity.
/// </summary>
/// <remarks>
/// This is the bridge between structure and meaning for the emerging ontology kernel.
/// Full notes: docs/nexus-ontology-imprint-alignment.md
/// </remarks>
type SemanticRoleAnnotation =
    { FactId: FactId
      Subject: NodeId
      RoleId: RoleId
      DomainId: DomainId option
      BoundedContextId: BoundedContextId option
      LensId: LensId option
      Provenance: FactProvenance }

/// <summary>
/// Converts semantic role annotations into graph assertions so the derived graph layer can persist them uniformly.
/// </summary>
[<RequireQualifiedAccess>]
module SemanticRoleAnnotation =
    /// <summary>
    /// Converts a semantic role annotation into a graph assertion using the stable <c>has_semantic_role</c> relation.
    /// </summary>
    let toGraphAssertion (annotation: SemanticRoleAnnotation) =
        { FactId = annotation.FactId
          Subject = annotation.Subject
          Predicate = HasSemanticRole
          Object = Literal(StringValue(RoleId.value annotation.RoleId))
          DomainId = annotation.DomainId
          BoundedContextId = annotation.BoundedContextId
          LensId = annotation.LensId
          Provenance = annotation.Provenance }
