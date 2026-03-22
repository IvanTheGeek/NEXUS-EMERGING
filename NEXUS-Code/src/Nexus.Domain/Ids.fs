namespace Nexus.Domain

[<Struct>]
type ImportId = ImportId of string

[<Struct>]
type CanonicalEventId = CanonicalEventId of string

[<Struct>]
type ConversationId = ConversationId of string

[<Struct>]
type MessageId = MessageId of string

[<Struct>]
type ArtifactId = ArtifactId of string

[<Struct>]
type TurnId = TurnId of string

[<Struct>]
type DomainId = DomainId of string

[<Struct>]
type BoundedContextId = BoundedContextId of string

[<Struct>]
type LensId = LensId of string

[<Struct>]
type NodeId = NodeId of string

[<Struct>]
type EdgeId = EdgeId of string

[<Struct>]
type FactId = FactId of string

type ProviderKind =
    | ChatGpt
    | Claude
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
