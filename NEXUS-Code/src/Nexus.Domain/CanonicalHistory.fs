namespace Nexus.Domain

/// <summary>
/// The canonical roles used for normalized messages.
/// </summary>
type MessageRole =
    | Human
    | Assistant
    | System
    | Tool
    | OtherRole of string

/// <summary>
/// The normalized segment kinds used inside message content.
/// </summary>
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

/// <summary>
/// A normalized unit of message content.
/// </summary>
type MessageSegment =
    { Kind: MessageSegmentKind
      Text: string }

/// <summary>
/// Shared metadata carried by every canonical event.
/// </summary>
/// <remarks>
/// Full canonical-history notes: docs/nexus-ingestion-architecture.md
/// </remarks>
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

/// <summary>
/// Records receipt of a provider-supplied root artifact such as an export zip or session snapshot.
/// </summary>
type ProviderArtifactReceived =
    { ArtifactId: ArtifactId
      Provider: ProviderKind
      FileName: string
      Window: ImportWindowKind option
      ByteCount: int64 option }

/// <summary>
/// Records extraction of a raw snapshot from a provider artifact.
/// </summary>
type RawSnapshotExtracted =
    { ArtifactId: ArtifactId
      ExtractedEntries: int option
      Notes: string option }

/// <summary>
/// Records the first canonical observation of a provider conversation.
/// </summary>
type ProviderConversationObserved =
    { ConversationId: ConversationId
      ProviderConversation: ProviderRef
      Title: string option
      IsArchived: bool option
      MessageCountHint: int option }

/// <summary>
/// Records a canonical observation of a provider message under a specific normalization version.
/// </summary>
type ProviderMessageObserved =
    { MessageId: MessageId
      ConversationId: ConversationId
      ProviderMessage: ProviderRef
      Role: MessageRole
      Segments: MessageSegment list
      ModelName: string option
      SequenceHint: int option }

/// <summary>
/// Records that a provider message was re-observed with changed canonical content under the same normalization rules.
/// </summary>
type ProviderMessageRevisionObserved =
    { MessageId: MessageId
      PriorContentHash: ContentHash option
      RevisedContentHash: ContentHash
      RevisionReason: string option }

/// <summary>
/// Describes whether an artifact payload was present in the source acquisition.
/// </summary>
type ArtifactReferenceDisposition =
    | PayloadIncluded
    | PayloadMissing
    | PayloadUnknown

/// <summary>
/// Records that a message or conversation referenced an artifact.
/// </summary>
type ArtifactReferenced =
    { ArtifactId: ArtifactId
      ConversationId: ConversationId option
      MessageId: MessageId option
      FileName: string option
      MediaType: string option
      Disposition: ArtifactReferenceDisposition
      ProviderArtifact: ProviderRef option }

/// <summary>
/// Records that a payload was later captured for an already-known artifact reference.
/// </summary>
type ArtifactPayloadCaptured =
    { ArtifactId: ArtifactId
      CapturedObject: RawObjectRef
      MediaType: string option
      ByteCount: int64 option
      CaptureNotes: string option }

/// <summary>
/// Summarizes observed, appended, duplicate, revision, and reparse counts for an import run.
/// </summary>
type ImportCounts =
    { ConversationsSeen: int
      MessagesSeen: int
      ArtifactsReferenced: int
      NewEventsAppended: int
      DuplicatesSkipped: int
      RevisionsObserved: int
      ReparseObservationsAppended: int }

/// <summary>
/// Records completion of an import run and its final counts.
/// </summary>
type ImportCompleted =
    { ImportId: ImportId
      Window: ImportWindowKind option
      Counts: ImportCounts
      Notes: string option }

/// <summary>
/// The persisted summary record written once for each import run.
/// </summary>
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

/// <summary>
/// The append-only event families currently used by canonical observed history.
/// </summary>
type CanonicalEventBody =
    | ProviderArtifactReceived of ProviderArtifactReceived
    | RawSnapshotExtracted of RawSnapshotExtracted
    | ProviderConversationObserved of ProviderConversationObserved
    | ProviderMessageObserved of ProviderMessageObserved
    | ProviderMessageRevisionObserved of ProviderMessageRevisionObserved
    | ArtifactReferenced of ArtifactReferenced
    | ArtifactPayloadCaptured of ArtifactPayloadCaptured
    | ImportCompleted of ImportCompleted

/// <summary>
/// A single append-only canonical event.
/// </summary>
type CanonicalEvent =
    { Envelope: CanonicalEventEnvelope
      Body: CanonicalEventBody }
