namespace Nexus.Domain

type MessageRole =
    | Human
    | Assistant
    | System
    | Tool
    | OtherRole of string

type MessageSegmentKind =
    | PlainText
    | Markdown
    | Quote
    | Code
    | Reasoning
    | ToolUse
    | ToolResult
    | Multimodal
    | UnknownSegment of string

type MessageSegment =
    { Kind: MessageSegmentKind
      Text: string }

type CanonicalEventEnvelope =
    { EventId: CanonicalEventId
      ConversationId: ConversationId option
      MessageId: MessageId option
      ArtifactId: ArtifactId option
      TurnId: TurnId option
      DomainId: DomainId option
      BoundedContextId: BoundedContextId option
      OccurredAt: OccurredAt option
      ObservedAt: ObservedAt
      ImportedAt: ImportedAt option
      SourceAcquisition: SourceAcquisitionKind
      NormalizationVersion: NormalizationVersion option
      ContentHash: ContentHash option
      ImportId: ImportId option
      ProviderRefs: ProviderRef list
      RawObjects: RawObjectRef list }

type ProviderArtifactReceived =
    { ArtifactId: ArtifactId
      Provider: ProviderKind
      FileName: string
      Window: ImportWindowKind option
      ByteCount: int64 option }

type RawSnapshotExtracted =
    { ArtifactId: ArtifactId
      ExtractedEntries: int option
      Notes: string option }

type ProviderConversationObserved =
    { ConversationId: ConversationId
      ProviderConversation: ProviderRef
      Title: string option
      IsArchived: bool option
      MessageCountHint: int option }

type ProviderMessageObserved =
    { MessageId: MessageId
      ConversationId: ConversationId
      ProviderMessage: ProviderRef
      Role: MessageRole
      Segments: MessageSegment list
      ModelName: string option
      SequenceHint: int option }

type ProviderMessageRevisionObserved =
    { MessageId: MessageId
      PriorContentHash: ContentHash option
      RevisedContentHash: ContentHash
      RevisionReason: string option }

type ArtifactReferenceDisposition =
    | PayloadIncluded
    | PayloadMissing
    | PayloadUnknown

type ArtifactReferenced =
    { ArtifactId: ArtifactId
      ConversationId: ConversationId option
      MessageId: MessageId option
      FileName: string option
      MediaType: string option
      Disposition: ArtifactReferenceDisposition
      ProviderArtifact: ProviderRef option }

type ArtifactPayloadCaptured =
    { ArtifactId: ArtifactId
      CapturedObject: RawObjectRef
      MediaType: string option
      ByteCount: int64 option
      CaptureNotes: string option }

type ImportCounts =
    { ConversationsSeen: int
      MessagesSeen: int
      ArtifactsReferenced: int
      NewEventsAppended: int
      DuplicatesSkipped: int
      RevisionsObserved: int
      ReparseObservationsAppended: int }

type ImportCompleted =
    { ImportId: ImportId
      Window: ImportWindowKind option
      Counts: ImportCounts
      Notes: string option }

type ImportManifest =
    { ImportId: ImportId
      Provider: ProviderKind
      SourceAcquisition: SourceAcquisitionKind
      NormalizationVersion: NormalizationVersion option
      Window: ImportWindowKind option
      ImportedAt: ImportedAt
      RootArtifact: RawObjectRef
      Counts: ImportCounts
      NewCanonicalEventIds: CanonicalEventId list
      Notes: string list }

type CanonicalEventBody =
    | ProviderArtifactReceived of ProviderArtifactReceived
    | RawSnapshotExtracted of RawSnapshotExtracted
    | ProviderConversationObserved of ProviderConversationObserved
    | ProviderMessageObserved of ProviderMessageObserved
    | ProviderMessageRevisionObserved of ProviderMessageRevisionObserved
    | ArtifactReferenced of ArtifactReferenced
    | ArtifactPayloadCaptured of ArtifactPayloadCaptured
    | ImportCompleted of ImportCompleted

type CanonicalEvent =
    { Envelope: CanonicalEventEnvelope
      Body: CanonicalEventBody }
