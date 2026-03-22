namespace Nexus.Importers

open System
open Nexus.Domain

[<RequireQualifiedAccess>]
module ProviderNaming =
    let slug =
        function
        | ChatGpt -> "chatgpt"
        | Claude -> "claude"
        | OtherProvider value -> value.Trim().ToLowerInvariant()

    let tryParse (value: string) =
        match value.Trim().ToLowerInvariant() with
        | "chatgpt"
        | "chat-gpt"
        | "chat_gpt" -> Some ChatGpt
        | "claude" -> Some Claude
        | _ -> None

[<RequireQualifiedAccess>]
module ImportWindowNaming =
    let value =
        function
        | Full -> "full"
        | Rolling window -> window
        | Incremental window -> window
        | ManualWindow window -> window

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

type ParsedArtifactReference =
    { ProviderArtifactId: string option
      FileName: string option
      MediaType: string option
      Disposition: ArtifactReferenceDisposition }

type ParsedMessage =
    { ProviderMessageId: string
      Role: MessageRole
      Segments: MessageSegment list
      OccurredAt: DateTimeOffset option
      ModelName: string option
      SequenceHint: int option
      ContentSignature: string
      ArtifactReferences: ParsedArtifactReference list }

type ParsedConversation =
    { ProviderConversationId: string
      Title: string option
      IsArchived: bool option
      OccurredAt: DateTimeOffset option
      MessageCountHint: int option
      Messages: ParsedMessage list }

type ParsedImport =
    { Provider: ProviderKind
      Window: ImportWindowKind option
      SourceFileName: string
      SourceByteCount: int64
      ExtractedEntries: int
      Conversations: ParsedConversation list
      Notes: string list }

type ImportRequest =
    { Provider: ProviderKind
      SourceZipPath: string
      Window: ImportWindowKind option
      ObjectsRoot: string
      EventStoreRoot: string }

type ImportResult =
    { Provider: ProviderKind
      ImportId: ImportId
      ArchivedZipRelativePath: string
      LatestZipRelativePath: string
      ExtractedConversationRelativePath: string option
      EventPaths: string list
      ManifestRelativePath: string
      Counts: ImportCounts }

[<RequireQualifiedAccess>]
module ProviderKey =
    let private normalize (value: string) = value.Trim()

    let conversation provider conversationNativeId =
        $"{ProviderNaming.slug provider}|conversation|{normalize conversationNativeId}"

    let message provider conversationNativeId messageNativeId =
        $"{ProviderNaming.slug provider}|message|{normalize conversationNativeId}|{normalize messageNativeId}"

    let artifact provider conversationNativeId messageNativeId artifactNativeId =
        $"{ProviderNaming.slug provider}|artifact|{normalize conversationNativeId}|{normalize messageNativeId}|{normalize artifactNativeId}"

    let artifactFallback provider conversationNativeId messageNativeId fileName =
        $"{ProviderNaming.slug provider}|artifact-fallback|{normalize conversationNativeId}|{normalize messageNativeId}|{normalize fileName}"
