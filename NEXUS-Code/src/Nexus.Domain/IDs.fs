namespace Nexus.Domain

open System

[<RequireQualifiedAccess>]
module Uuid7 =
    let create () = Guid.CreateVersion7()

[<Struct>]
type ImportId = ImportId of Guid

module ImportId =
    let create () = ImportId (Uuid7.create ())
    let value (ImportId value) = value
    let parse (value: string) = value |> Guid.Parse |> ImportId
    let format (ImportId value) = value.ToString()

[<Struct>]
type CanonicalEventId = CanonicalEventId of Guid

module CanonicalEventId =
    let create () = CanonicalEventId (Uuid7.create ())
    let value (CanonicalEventId value) = value
    let parse (value: string) = value |> Guid.Parse |> CanonicalEventId
    let format (CanonicalEventId value) = value.ToString()

[<Struct>]
type ConversationId = ConversationId of Guid

module ConversationId =
    let create () = ConversationId (Uuid7.create ())
    let value (ConversationId value) = value
    let parse (value: string) = value |> Guid.Parse |> ConversationId
    let format (ConversationId value) = value.ToString()

[<Struct>]
type MessageId = MessageId of Guid

module MessageId =
    let create () = MessageId (Uuid7.create ())
    let value (MessageId value) = value
    let parse (value: string) = value |> Guid.Parse |> MessageId
    let format (MessageId value) = value.ToString()

[<Struct>]
type ArtifactId = ArtifactId of Guid

module ArtifactId =
    let create () = ArtifactId (Uuid7.create ())
    let value (ArtifactId value) = value
    let parse (value: string) = value |> Guid.Parse |> ArtifactId
    let format (ArtifactId value) = value.ToString()

[<Struct>]
type TurnId = TurnId of Guid

module TurnId =
    let create () = TurnId (Uuid7.create ())
    let value (TurnId value) = value
    let parse (value: string) = value |> Guid.Parse |> TurnId
    let format (TurnId value) = value.ToString()

[<Struct>]
type DomainId = DomainId of string

module DomainId =
    let create (value: string) = DomainId value
    let value (DomainId value) = value

[<Struct>]
type BoundedContextId = BoundedContextId of string

module BoundedContextId =
    let create (value: string) = BoundedContextId value
    let value (BoundedContextId value) = value

[<Struct>]
type LensId = LensId of string

module LensId =
    let create (value: string) = LensId value
    let value (LensId value) = value

[<Struct>]
type NodeId = NodeId of Guid

module NodeId =
    let create () = NodeId (Uuid7.create ())
    let value (NodeId value) = value
    let parse (value: string) = value |> Guid.Parse |> NodeId
    let format (NodeId value) = value.ToString()

[<Struct>]
type EdgeId = EdgeId of Guid

module EdgeId =
    let create () = EdgeId (Uuid7.create ())
    let value (EdgeId value) = value
    let parse (value: string) = value |> Guid.Parse |> EdgeId
    let format (EdgeId value) = value.ToString()

[<Struct>]
type FactId = FactId of Guid

module FactId =
    let create () = FactId (Uuid7.create ())
    let value (FactId value) = value
    let parse (value: string) = value |> Guid.Parse |> FactId
    let format (FactId value) = value.ToString()

type ProviderKind =
    | ChatGpt
    | Claude
    | Codex
    | OtherProvider of string

type ProviderObjectKind =
    | ExportArtifact
    | ConversationObject
    | MessageObject
    | ArtifactObject
    | ProjectObject
    | MemoryObject
    | UserObject
    | OtherProviderObject of string
