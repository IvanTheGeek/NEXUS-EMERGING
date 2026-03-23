namespace Nexus.Domain

open System

/// <summary>
/// Names the parser/canonicalizer shape that produced a canonical observation.
/// </summary>
type NormalizationVersion = private NormalizationVersion of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Domain.NormalizationVersion" />.
/// </summary>
[<RequireQualifiedAccess>]
module NormalizationVersion =
    /// <summary>
    /// Creates a normalization version from a non-blank stable label.
    /// </summary>
    let create (value: string) =
        let normalized = value.Trim()

        if String.IsNullOrWhiteSpace(normalized) then
            invalidArg "value" "Normalization version cannot be blank."

        NormalizationVersion normalized

    /// <summary>
    /// Parses a persisted normalization version.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying normalization label.
    /// </summary>
    let value (NormalizationVersion value) = value

/// <summary>
/// Identifies how a source record entered NEXUS observed history.
/// </summary>
type SourceAcquisitionKind =
    | ExportZip
    | LocalSessionExport
    | ManualArtifactAdd
    | ApiCapture
    | BrowserCapture
    | OtherAcquisition of string

/// <summary>
/// Identifies the capture window semantics of an import source.
/// </summary>
type ImportWindowKind =
    | Full
    | Rolling of string
    | Incremental of string
    | ManualWindow of string

/// <summary>
/// A stable content hash used for dedupe, revision detection, and idempotence.
/// </summary>
type ContentHash =
    { Algorithm: string
      Value: string }

/// <summary>
/// Identifies the category of raw object preserved in the object layer.
/// </summary>
type RawObjectKind =
    | ProviderExportZip
    | ExtractedSnapshot
    | SessionIndex
    | SessionTranscript
    | AttachmentPayload
    | AudioPayload
    | ManualArtifact
    | OtherRawObject of string

/// <summary>
/// Points from canonical history back to a preserved raw object.
/// </summary>
type RawObjectRef =
    { RawObjectId: string option
      Kind: RawObjectKind
      RelativePath: string
      ArchivedAt: ImportedAt option
      SourceDescription: string option }

/// <summary>
/// Preserves provider-native identities alongside NEXUS canonical identifiers.
/// </summary>
type ProviderRef =
    { Provider: ProviderKind
      ObjectKind: ProviderObjectKind
      NativeId: string option
      ConversationNativeId: string option
      MessageNativeId: string option
      ArtifactNativeId: string option }

/// <summary>
/// Records the provenance supporting a graph-level fact or assertion.
/// </summary>
type FactProvenance =
    { ImportId: ImportId option
      SourceAcquisition: SourceAcquisitionKind
      ProviderRefs: ProviderRef list
      RawObjects: RawObjectRef list
      SupportingEventIds: CanonicalEventId list }
