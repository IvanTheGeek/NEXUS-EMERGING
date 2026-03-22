namespace Nexus.Domain

type SourceAcquisitionKind =
    | ExportZip
    | ManualArtifactAdd
    | ApiCapture
    | BrowserCapture
    | OtherAcquisition of string

type ImportWindowKind =
    | Full
    | Rolling of string
    | Incremental of string
    | ManualWindow of string

type ContentHash =
    { Algorithm: string
      Value: string }

type RawObjectKind =
    | ProviderExportZip
    | ExtractedSnapshot
    | AttachmentPayload
    | AudioPayload
    | ManualArtifact
    | OtherRawObject of string

type RawObjectRef =
    { RawObjectId: string option
      Kind: RawObjectKind
      RelativePath: string
      ArchivedAt: ImportedAt option
      SourceDescription: string option }

type ProviderRef =
    { Provider: ProviderKind
      ObjectKind: ProviderObjectKind
      NativeId: string option
      ConversationNativeId: string option
      MessageNativeId: string option
      ArtifactNativeId: string option }

type FactProvenance =
    { ImportId: ImportId option
      SourceAcquisition: SourceAcquisitionKind
      ProviderRefs: ProviderRef list
      RawObjects: RawObjectRef list
      SupportingEventIds: CanonicalEventId list }
