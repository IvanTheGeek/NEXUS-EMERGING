namespace Nexus.Importers

open System
open Nexus.Domain
open Nexus.Logos

/// <summary>
/// Converts provider identifiers to the stable slugs used in paths, manifests, and CLI-facing values.
/// </summary>
/// <remarks>
/// Full provider-ingestion notes: docs/nexus-ingestion-architecture.md
/// </remarks>
[<RequireQualifiedAccess>]
module ProviderNaming =
    /// <summary>
    /// Returns the canonical slug for a provider kind.
    /// </summary>
    let slug =
        function
        | ChatGpt -> "chatgpt"
        | Claude -> "claude"
        | Grok -> "grok"
        | Codex -> "codex"
        | OtherProvider value -> value.Trim().ToLowerInvariant()

    /// <summary>
    /// Parses a provider slug used in persisted artifacts back into a provider kind.
    /// </summary>
    let tryParse (value: string) =
        match value.Trim().ToLowerInvariant() with
        | "chatgpt"
        | "chat-gpt"
        | "chat_gpt" -> Some ChatGpt
        | "claude" -> Some Claude
        | "grok" -> Some Grok
        | "codex" -> Some Codex
        | _ -> None

/// <summary>
/// Normalizes import window labels used for raw snapshot naming and manifest values.
/// </summary>
[<RequireQualifiedAccess>]
module ImportWindowNaming =
    /// <summary>
    /// Returns the persisted string form of an import window.
    /// </summary>
    let value =
        function
        | Full -> "full"
        | Rolling window -> window
        | Incremental window -> window
        | ManualWindow window -> window

    /// <summary>
    /// Parses a persisted import-window label back into the canonical union value.
    /// </summary>
    let tryParse (value: string) =
        let normalized = value.Trim()

        match normalized.ToLowerInvariant() with
        | "full" -> Some Full
        | _ when normalized.StartsWith("incremental:", StringComparison.OrdinalIgnoreCase) ->
            normalized.Substring("incremental:".Length) |> Incremental |> Some
        | _ when normalized.StartsWith("manual:", StringComparison.OrdinalIgnoreCase) ->
            normalized.Substring("manual:".Length) |> ManualWindow |> Some
        | _ when String.IsNullOrWhiteSpace(normalized) -> None
        | _ -> Some (Rolling normalized)

    /// <summary>
    /// Builds the stable base file name used under provider latest/ paths.
    /// </summary>
    let latestBaseName =
        function
        | None
        | Some Full -> "full-export"
        | Some window ->
            let raw = value window

            raw
            |> Seq.map (fun character ->
                if Char.IsLetterOrDigit(character) then
                    Char.ToLowerInvariant(character)
                else
                    '-')
            |> Seq.toArray
            |> String

/// <summary>
/// Centralizes parser and canonicalization version labels for import workflows.
/// </summary>
/// <remarks>
/// These labels distinguish provider-side revisions from NEXUS-side reparses.
/// Full notes: docs/nexus-ingestion-architecture.md
/// </remarks>
[<RequireQualifiedAccess>]
module NormalizationNaming =
    /// <summary>
    /// The implicit normalization version used for legacy event-store files that predate explicit version stamping.
    /// </summary>
    let legacyDefault = NormalizationVersion.create "provider-export-v0"

    /// <summary>
    /// The active normalization version for provider export imports.
    /// </summary>
    let providerExportsCurrent = NormalizationVersion.create "provider-export-v1"

    /// <summary>
    /// The active normalization version for imported Codex local-session snapshots.
    /// </summary>
    let codexSessionsCurrent = NormalizationVersion.create "codex-sessions-v1"

    /// <summary>
    /// The default normalization version for generic provider export workflows.
    /// </summary>
    let current = providerExportsCurrent

    /// <summary>
    /// Returns the persisted string form of a normalization version.
    /// </summary>
    let value normalizationVersion =
        NormalizationVersion.value normalizationVersion

/// <summary>
/// Bridges known provider imports into persisted LOGOS intake metadata.
/// </summary>
[<RequireQualifiedAccess>]
module ProviderLogosImportMetadata =
    /// <summary>
    /// Builds the persisted LOGOS intake metadata for a known provider kind when available.
    /// </summary>
    let tryBuild provider =
        ProviderNaming.slug provider
        |> ProviderLogosClassification.tryFind
        |> Option.map (fun classification ->
            { SourceSystem = SourceSystemId.value classification.SourceSystemId
              IntakeChannel = IntakeChannelId.value classification.IntakeChannelId
              PrimarySignalKind = SignalKindId.value classification.PrimarySignalKind
              RelatedSignalKinds = classification.RelatedSignalKinds |> List.map SignalKindId.value
              HandlingPolicy =
                { Sensitivity = SensitivityId.value classification.DefaultHandlingPolicy.SensitivityId
                  SharingScope = SharingScopeId.value classification.DefaultHandlingPolicy.SharingScopeId
                  SanitizationStatus = SanitizationStatusId.value classification.DefaultHandlingPolicy.SanitizationStatusId
                  RetentionClass = RetentionClassId.value classification.DefaultHandlingPolicy.RetentionClassId }
              EntryPool = classification.EntryPool })

/// <summary>
/// A normalized artifact reference discovered while parsing provider content.
/// </summary>
type ParsedArtifactReference =
    { ProviderArtifactId: string option
      FileName: string option
      MediaType: string option
      Disposition: ArtifactReferenceDisposition }

/// <summary>
/// A provider message reduced to the canonical parse shape used by import workflows.
/// </summary>
type ParsedMessage =
    { ProviderMessageId: string
      Role: MessageRole
      Segments: MessageSegment list
      OccurredAt: DateTimeOffset option
      ModelName: string option
      SequenceHint: int option
      ContentSignature: string
      ArtifactReferences: ParsedArtifactReference list }

/// <summary>
/// A parsed provider conversation prepared for canonical event writing.
/// </summary>
type ParsedConversation =
    { ProviderConversationId: string
      Title: string option
      IsArchived: bool option
      OccurredAt: DateTimeOffset option
      MessageCountHint: int option
      Messages: ParsedMessage list
      RawObjects: RawObjectRef list }

/// <summary>
/// The provider-specific parse result handed to the shared canonical import workflow.
/// </summary>
type ParsedImport =
    { Provider: ProviderKind
      Window: ImportWindowKind option
      SourceFileName: string
      SourceByteCount: int64
      ExtractedEntries: int
      Conversations: ParsedConversation list
      Notes: string list }

/// <summary>
/// Requests an import of a provider export zip into the object layer and canonical event store.
/// </summary>
/// <remarks>
/// Full workflow notes: docs/how-to/import-provider-export.md
/// </remarks>
type ImportRequest =
    { Provider: ProviderKind
      SourceZipPath: string
      Window: ImportWindowKind option
      ObjectsRoot: string
      EventStoreRoot: string }

/// <summary>
/// Describes the observable outputs of a provider export import run.
/// </summary>
type ImportResult =
    { Provider: ProviderKind
      ImportId: ImportId
      ArchivedZipRelativePath: string
      LatestZipRelativePath: string
      ExtractedPayloadRelativePath: string option
      EventPaths: string list
      ManifestRelativePath: string
      ImportSnapshotManifestRelativePath: string option
      ImportSnapshotConversationsRelativePath: string option
      WorkingGraphManifestRelativePath: string option
      WorkingGraphCatalogRelativePath: string option
      WorkingGraphIndexRelativePath: string option
      WorkingGraphAssertionCount: int option
      Counts: ImportCounts }

/// <summary>
/// Requests import of a preserved Codex local-session snapshot into canonical history.
/// </summary>
/// <remarks>
/// Full workflow notes: docs/how-to/import-codex-sessions.md
/// </remarks>
type CodexSessionImportRequest =
    { SnapshotRoot: string
      ObjectsRoot: string
      EventStoreRoot: string }

/// <summary>
/// Describes the outputs of a Codex session import run.
/// </summary>
type CodexSessionImportResult =
    { ImportId: ImportId
      SnapshotRoot: string
      RootArtifactRelativePath: string
      EventPaths: string list
      ManifestRelativePath: string
      WorkingGraphManifestRelativePath: string option
      WorkingGraphCatalogRelativePath: string option
      WorkingGraphIndexRelativePath: string option
      WorkingGraphAssertionCount: int option
      Counts: ImportCounts }

/// <summary>
/// Identifies which existing artifact reference a manual payload capture should hydrate.
/// </summary>
type ManualArtifactCaptureTarget =
    | ExistingArtifactId of ArtifactId
    | ProviderArtifactReference of
        provider: ProviderKind *
        conversationNativeId: string *
        messageNativeId: string *
        providerArtifactId: string option *
        fileName: string option

/// <summary>
/// Requests manual hydration of an already-known artifact reference.
/// </summary>
/// <remarks>
/// Full workflow notes: docs/how-to/capture-artifact-payload.md
/// </remarks>
type ManualArtifactCaptureRequest =
    { Target: ManualArtifactCaptureTarget
      SourceFilePath: string
      ObjectsRoot: string
      EventStoreRoot: string
      MediaType: string option
      Notes: string option }

/// <summary>
/// Describes the result of attempting a manual artifact payload capture.
/// </summary>
type ManualArtifactCaptureResult =
    { ArtifactId: ArtifactId
      Provider: ProviderKind option
      ArchivedRelativePath: string option
      EventPath: string option
      DuplicateSkipped: bool
      ByteCount: int64
      ContentHash: ContentHash }

/// <summary>
/// Builds stable provider-key strings for dedupe and reconciliation inside the importer layer.
/// </summary>
[<RequireQualifiedAccess>]
module ProviderKey =
    let private normalize (value: string) = value.Trim()

    /// <summary>
    /// Builds the provider key used to identify a conversation across imports.
    /// </summary>
    let conversation provider conversationNativeId =
        $"{ProviderNaming.slug provider}|conversation|{normalize conversationNativeId}"

    /// <summary>
    /// Builds the provider key used to identify a message across imports.
    /// </summary>
    let message provider conversationNativeId messageNativeId =
        $"{ProviderNaming.slug provider}|message|{normalize conversationNativeId}|{normalize messageNativeId}"

    /// <summary>
    /// Builds the provider key used to identify an artifact when the provider exposes a stable artifact identifier.
    /// </summary>
    let artifact provider conversationNativeId messageNativeId artifactNativeId =
        $"{ProviderNaming.slug provider}|artifact|{normalize conversationNativeId}|{normalize messageNativeId}|{normalize artifactNativeId}"

    /// <summary>
    /// Builds the fallback key used when an artifact must be matched by message context and file name.
    /// </summary>
    let artifactFallback provider conversationNativeId messageNativeId fileName =
        $"{ProviderNaming.slug provider}|artifact-fallback|{normalize conversationNativeId}|{normalize messageNativeId}|{normalize fileName}"
