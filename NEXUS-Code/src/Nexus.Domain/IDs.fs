namespace Nexus.Domain

open System

/// <summary>
/// Generates UUIDv7 identifiers for canonical NEXUS IDs.
/// </summary>
[<RequireQualifiedAccess>]
module Uuid7 =
    /// <summary>
    /// Creates a new UUIDv7 value.
    /// </summary>
    let create () = Guid.CreateVersion7()

[<Struct>]
/// <summary>
/// Identifies a single canonical import run.
/// </summary>
type ImportId = ImportId of Guid

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.ImportId" />.
/// </summary>
module ImportId =
    /// <summary>
    /// Creates a new canonical import identifier.
    /// </summary>
    let create () = ImportId (Uuid7.create ())
    /// <summary>
    /// Extracts the underlying GUID value.
    /// </summary>
    let value (ImportId value) = value
    /// <summary>
    /// Parses a persisted import identifier.
    /// </summary>
    let parse (value: string) = value |> Guid.Parse |> ImportId
    /// <summary>
    /// Formats an import identifier for storage and file naming.
    /// </summary>
    let format (ImportId value) = value.ToString()

[<Struct>]
/// <summary>
/// Identifies a canonical event in append-only history.
/// </summary>
type CanonicalEventId = CanonicalEventId of Guid

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.CanonicalEventId" />.
/// </summary>
module CanonicalEventId =
    /// <summary>
    /// Creates a new canonical event identifier.
    /// </summary>
    let create () = CanonicalEventId (Uuid7.create ())
    /// <summary>
    /// Extracts the underlying GUID value.
    /// </summary>
    let value (CanonicalEventId value) = value
    /// <summary>
    /// Parses a persisted canonical event identifier.
    /// </summary>
    let parse (value: string) = value |> Guid.Parse |> CanonicalEventId
    /// <summary>
    /// Formats a canonical event identifier for storage and file naming.
    /// </summary>
    let format (CanonicalEventId value) = value.ToString()

[<Struct>]
/// <summary>
/// Identifies a canonical conversation stream.
/// </summary>
type ConversationId = ConversationId of Guid

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.ConversationId" />.
/// </summary>
module ConversationId =
    /// <summary>
    /// Creates a new canonical conversation identifier.
    /// </summary>
    let create () = ConversationId (Uuid7.create ())
    /// <summary>
    /// Extracts the underlying GUID value.
    /// </summary>
    let value (ConversationId value) = value
    /// <summary>
    /// Parses a persisted conversation identifier.
    /// </summary>
    let parse (value: string) = value |> Guid.Parse |> ConversationId
    /// <summary>
    /// Formats a conversation identifier for storage and file naming.
    /// </summary>
    let format (ConversationId value) = value.ToString()

[<Struct>]
/// <summary>
/// Identifies a canonical message within conversation history.
/// </summary>
type MessageId = MessageId of Guid

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.MessageId" />.
/// </summary>
module MessageId =
    /// <summary>
    /// Creates a new canonical message identifier.
    /// </summary>
    let create () = MessageId (Uuid7.create ())
    /// <summary>
    /// Extracts the underlying GUID value.
    /// </summary>
    let value (MessageId value) = value
    /// <summary>
    /// Parses a persisted message identifier.
    /// </summary>
    let parse (value: string) = value |> Guid.Parse |> MessageId
    /// <summary>
    /// Formats a message identifier for storage and file naming.
    /// </summary>
    let format (MessageId value) = value.ToString()

[<Struct>]
/// <summary>
/// Identifies an artifact reference or payload in canonical history.
/// </summary>
type ArtifactId = ArtifactId of Guid

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.ArtifactId" />.
/// </summary>
module ArtifactId =
    /// <summary>
    /// Creates a new canonical artifact identifier.
    /// </summary>
    let create () = ArtifactId (Uuid7.create ())
    /// <summary>
    /// Extracts the underlying GUID value.
    /// </summary>
    let value (ArtifactId value) = value
    /// <summary>
    /// Parses a persisted artifact identifier.
    /// </summary>
    let parse (value: string) = value |> Guid.Parse |> ArtifactId
    /// <summary>
    /// Formats an artifact identifier for storage and file naming.
    /// </summary>
    let format (ArtifactId value) = value.ToString()

[<Struct>]
/// <summary>
/// Identifies an optional canonical turn grouping.
/// </summary>
type TurnId = TurnId of Guid

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.TurnId" />.
/// </summary>
module TurnId =
    /// <summary>
    /// Creates a new canonical turn identifier.
    /// </summary>
    let create () = TurnId (Uuid7.create ())
    /// <summary>
    /// Extracts the underlying GUID value.
    /// </summary>
    let value (TurnId value) = value
    /// <summary>
    /// Parses a persisted turn identifier.
    /// </summary>
    let parse (value: string) = value |> Guid.Parse |> TurnId
    /// <summary>
    /// Formats a turn identifier for storage and file naming.
    /// </summary>
    let format (TurnId value) = value.ToString()

[<Struct>]
/// <summary>
/// Identifies a broad domain partition in the NEXUS graph.
/// </summary>
type DomainId = DomainId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.DomainId" />.
/// </summary>
module DomainId =
    /// <summary>
    /// Creates a domain identifier from a stable string value.
    /// </summary>
    let create (value: string) = DomainId value
    /// <summary>
    /// Extracts the underlying domain identifier value.
    /// </summary>
    let value (DomainId value) = value

[<Struct>]
/// <summary>
/// Identifies a bounded context within a domain.
/// </summary>
type BoundedContextId = BoundedContextId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.BoundedContextId" />.
/// </summary>
module BoundedContextId =
    /// <summary>
    /// Creates a bounded-context identifier from a stable string value.
    /// </summary>
    let create (value: string) = BoundedContextId value
    /// <summary>
    /// Extracts the underlying bounded-context identifier value.
    /// </summary>
    let value (BoundedContextId value) = value

[<Struct>]
/// <summary>
/// Identifies a lens over the shared graph substrate.
/// </summary>
type LensId = LensId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.LensId" />.
/// </summary>
module LensId =
    /// <summary>
    /// Creates a lens identifier from a stable string value.
    /// </summary>
    let create (value: string) = LensId value
    /// <summary>
    /// Extracts the underlying lens identifier value.
    /// </summary>
    let value (LensId value) = value

[<Struct>]
/// <summary>
/// Identifies a node in the thin graph layer.
/// </summary>
type NodeId = NodeId of Guid

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.NodeId" />.
/// </summary>
module NodeId =
    /// <summary>
    /// Creates a new graph node identifier.
    /// </summary>
    let create () = NodeId (Uuid7.create ())
    /// <summary>
    /// Extracts the underlying GUID value.
    /// </summary>
    let value (NodeId value) = value
    /// <summary>
    /// Parses a persisted node identifier.
    /// </summary>
    let parse (value: string) = value |> Guid.Parse |> NodeId
    /// <summary>
    /// Formats a node identifier for storage and file naming.
    /// </summary>
    let format (NodeId value) = value.ToString()

[<Struct>]
/// <summary>
/// Identifies an edge in the thin graph layer.
/// </summary>
type EdgeId = EdgeId of Guid

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.EdgeId" />.
/// </summary>
module EdgeId =
    /// <summary>
    /// Creates a new graph edge identifier.
    /// </summary>
    let create () = EdgeId (Uuid7.create ())
    /// <summary>
    /// Extracts the underlying GUID value.
    /// </summary>
    let value (EdgeId value) = value
    /// <summary>
    /// Parses a persisted edge identifier.
    /// </summary>
    let parse (value: string) = value |> Guid.Parse |> EdgeId
    /// <summary>
    /// Formats an edge identifier for storage and file naming.
    /// </summary>
    let format (EdgeId value) = value.ToString()

[<Struct>]
/// <summary>
/// Identifies a graph-level fact or assertion.
/// </summary>
type FactId = FactId of Guid

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.FactId" />.
/// </summary>
module FactId =
    /// <summary>
    /// Creates a new fact identifier.
    /// </summary>
    let create () = FactId (Uuid7.create ())
    /// <summary>
    /// Extracts the underlying GUID value.
    /// </summary>
    let value (FactId value) = value
    /// <summary>
    /// Parses a persisted fact identifier.
    /// </summary>
    let parse (value: string) = value |> Guid.Parse |> FactId
    /// <summary>
    /// Formats a fact identifier for storage and file naming.
    /// </summary>
    let format (FactId value) = value.ToString()

/// <summary>
/// Identifies the source provider from which observed history was acquired.
/// </summary>
type ProviderKind =
    | ChatGpt
    | Claude
    | Grok
    | Codex
    | OtherProvider of string

/// <summary>
/// Identifies the kind of provider object a provider reference points to.
/// </summary>
type ProviderObjectKind =
    | ExportArtifact
    | ConversationObject
    | MessageObject
    | ArtifactObject
    | ProjectObject
    | MemoryObject
    | UserObject
    | OtherProviderObject of string
