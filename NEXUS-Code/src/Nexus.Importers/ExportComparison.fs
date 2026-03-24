namespace Nexus.Importers

open System
open System.Globalization
open System.IO
open System.IO.Compression
open System.Security.Cryptography
open Nexus.Domain

/// <summary>
/// Summarizes one provider-native conversation discovered directly from a raw export zip.
/// </summary>
type ProviderExportConversationSummary =
    { ProviderConversationId: string
      Title: string option
      MessageCount: int
      ArtifactReferenceCount: int }

/// <summary>
/// Describes how one shared provider-native conversation differs between two raw export zips.
/// </summary>
type ProviderExportConversationDelta =
    { ProviderConversationId: string
      BaseTitle: string option
      CurrentTitle: string option
      BaseMessageCount: int
      CurrentMessageCount: int
      AddedMessageCount: int
      RemovedMessageCount: int
      BaseArtifactReferenceCount: int
      CurrentArtifactReferenceCount: int
      AddedArtifactReferenceCount: int
      RemovedArtifactReferenceCount: int }

/// <summary>
/// Compares two raw provider export zips before canonical import.
/// </summary>
/// <remarks>
/// This is a source-layer comparison over provider-native conversations and messages.
/// It is useful for reasoning about export-window behavior without appending anything to the event store.
/// </remarks>
type ProviderExportComparisonReport =
    { Provider: ProviderKind
      BaseZipPath: string
      CurrentZipPath: string
      BaseZipSha256: string
      CurrentZipSha256: string
      ZipArtifactsIdentical: bool
      BaseConversationCount: int
      CurrentConversationCount: int
      BaseMessageCount: int
      CurrentMessageCount: int
      BaseArtifactReferenceCount: int
      CurrentArtifactReferenceCount: int
      AddedConversationCount: int
      RemovedConversationCount: int
      ChangedConversationCount: int
      UnchangedConversationCount: int
      AddedConversations: ProviderExportConversationSummary list
      RemovedConversations: ProviderExportConversationSummary list
      ChangedConversations: ProviderExportConversationDelta list }

/// <summary>
/// Compares raw provider export zips without importing them into canonical history.
/// </summary>
/// <remarks>
/// Full ingestion notes: docs/nexus-ingestion-architecture.md
/// </remarks>
[<RequireQualifiedAccess>]
module ExportComparison =
    let private sha256ForFile (path: string) =
        use stream = File.OpenRead(path)
        SHA256.HashData(stream)
        |> Convert.ToHexString
        |> fun value -> value.ToLowerInvariant()

    let private ensureZipExists argumentName (path: string) =
        let absolutePath = Path.GetFullPath(path)

        if File.Exists(absolutePath) then
            absolutePath
        else
            invalidArg argumentName $"Zip file not found: {absolutePath}"

    let private safeExtractToDirectory zipPath destinationDirectory =
        Directory.CreateDirectory(destinationDirectory) |> ignore

        let mutable extractedEntries = 0
        let mutable extractedNames = Set.empty
        let destinationRoot = Path.GetFullPath(destinationDirectory)

        use archive = ZipFile.OpenRead(zipPath)

        for entry in archive.Entries do
            let destinationPath = Path.GetFullPath(Path.Combine(destinationDirectory, entry.FullName))

            if not (destinationPath = destinationRoot
                    || destinationPath.StartsWith(destinationRoot + string Path.DirectorySeparatorChar, StringComparison.Ordinal)) then
                failwith $"Zip entry escaped the destination root: {entry.FullName}"

            if String.IsNullOrWhiteSpace(entry.Name) then
                Directory.CreateDirectory(destinationPath) |> ignore
            else
                let directory = Path.GetDirectoryName(destinationPath)

                if not (String.IsNullOrWhiteSpace(directory)) then
                    Directory.CreateDirectory(directory) |> ignore

                entry.ExtractToFile(destinationPath, true)
                extractedEntries <- extractedEntries + 1
                extractedNames <- extractedNames.Add(entry.Name.Trim().ToLowerInvariant())

        extractedEntries, extractedNames

    let private withExtractedConversations provider zipPath action =
        let tempRoot = Path.Combine(Path.GetTempPath(), $"nexus-export-compare-{Guid.NewGuid():N}")
        let extractRoot = Path.Combine(tempRoot, "extracted")

        Directory.CreateDirectory(tempRoot) |> ignore

        try
            let extractedEntries, extractedNames = safeExtractToDirectory zipPath extractRoot
            let conversationsJsonPath = Path.Combine(extractRoot, "conversations.json")

            if not (File.Exists(conversationsJsonPath)) then
                failwith $"The provider export did not contain conversations.json: {zipPath}"

            let parsedImport =
                ProviderAdapters.parse
                    provider
                    None
                    (Path.GetFileName(zipPath))
                    (FileInfo(zipPath).Length)
                    extractedEntries
                    extractedNames
                    conversationsJsonPath

            action parsedImport
        finally
            if Directory.Exists(tempRoot) then
                Directory.Delete(tempRoot, true)

    let private conversationSummaryOfParsed (conversation: ParsedConversation) =
        { ProviderExportConversationSummary.ProviderConversationId = conversation.ProviderConversationId
          Title = conversation.Title
          MessageCount = conversation.Messages.Length
          ArtifactReferenceCount =
            conversation.Messages
            |> List.sumBy (fun message -> message.ArtifactReferences.Length) }

    let private conversationSummaryLabel (value: ProviderExportConversationSummary) =
        value.Title
        |> Option.defaultValue value.ProviderConversationId

    let private conversationDeltaLabel (value: ProviderExportConversationDelta) =
        value.CurrentTitle
        |> Option.orElse value.BaseTitle
        |> Option.defaultValue value.ProviderConversationId

    let private messageIdSet (conversation: ParsedConversation) =
        conversation.Messages
        |> List.map (fun message -> message.ProviderMessageId)
        |> Set.ofList

    let private artifactKeySet (conversation: ParsedConversation) =
        conversation.Messages
        |> List.collect (fun message ->
            message.ArtifactReferences
            |> List.map (fun artifact ->
                let providerArtifactId = artifact.ProviderArtifactId |> Option.defaultValue String.Empty
                let fileName = artifact.FileName |> Option.defaultValue String.Empty
                $"{message.ProviderMessageId}|{providerArtifactId}|{fileName}"))
        |> Set.ofList

    /// <summary>
    /// Compares two raw provider export zips and returns a source-layer delta report.
    /// </summary>
    /// <param name="provider">The provider adapter to use for both zips.</param>
    /// <param name="baseZipPath">The older or reference raw export zip.</param>
    /// <param name="currentZipPath">The newer or comparison raw export zip.</param>
    /// <param name="limit">The maximum number of detailed rows to include in each result bucket.</param>
    /// <returns>A provider-native comparison report over the two raw source artifacts.</returns>
    let compare provider baseZipPath currentZipPath limit =
        let baseZipAbsolutePath = ensureZipExists "baseZipPath" baseZipPath
        let currentZipAbsolutePath = ensureZipExists "currentZipPath" currentZipPath

        let baseZipSha256 = sha256ForFile baseZipAbsolutePath
        let currentZipSha256 = sha256ForFile currentZipAbsolutePath

        withExtractedConversations provider baseZipAbsolutePath (fun baseImport ->
            withExtractedConversations provider currentZipAbsolutePath (fun currentImport ->
                let baseConversations =
                    baseImport.Conversations
                    |> List.map (fun conversation -> conversation.ProviderConversationId, conversation)
                    |> Map.ofList

                let currentConversations =
                    currentImport.Conversations
                    |> List.map (fun conversation -> conversation.ProviderConversationId, conversation)
                    |> Map.ofList

                let baseConversationSummaries =
                    baseImport.Conversations |> List.map conversationSummaryOfParsed

                let currentConversationSummaries =
                    currentImport.Conversations |> List.map conversationSummaryOfParsed

                let addedConversations =
                    currentConversationSummaries
                    |> List.filter (fun conversation -> not (baseConversations.ContainsKey conversation.ProviderConversationId))
                    |> List.sortBy conversationSummaryLabel

                let removedConversations =
                    baseConversationSummaries
                    |> List.filter (fun conversation -> not (currentConversations.ContainsKey conversation.ProviderConversationId))
                    |> List.sortBy conversationSummaryLabel

                let changedConversations =
                    baseImport.Conversations
                    |> List.choose (fun baseConversation ->
                        match currentConversations.TryFind baseConversation.ProviderConversationId with
                        | Some currentConversation ->
                            let baseMessageIds = messageIdSet baseConversation
                            let currentMessageIds = messageIdSet currentConversation
                            let baseArtifactKeys = artifactKeySet baseConversation
                            let currentArtifactKeys = artifactKeySet currentConversation

                            let addedMessageCount = Set.difference currentMessageIds baseMessageIds |> Set.count
                            let removedMessageCount = Set.difference baseMessageIds currentMessageIds |> Set.count
                            let addedArtifactReferenceCount = Set.difference currentArtifactKeys baseArtifactKeys |> Set.count
                            let removedArtifactReferenceCount = Set.difference baseArtifactKeys currentArtifactKeys |> Set.count

                            if baseConversation.Title <> currentConversation.Title
                               || addedMessageCount > 0
                               || removedMessageCount > 0
                               || addedArtifactReferenceCount > 0
                               || removedArtifactReferenceCount > 0 then
                                Some
                                    { ProviderConversationId = baseConversation.ProviderConversationId
                                      BaseTitle = baseConversation.Title
                                      CurrentTitle = currentConversation.Title
                                      BaseMessageCount = baseConversation.Messages.Length
                                      CurrentMessageCount = currentConversation.Messages.Length
                                      AddedMessageCount = addedMessageCount
                                      RemovedMessageCount = removedMessageCount
                                      BaseArtifactReferenceCount = baseConversation.Messages |> List.sumBy (fun message -> message.ArtifactReferences.Length)
                                      CurrentArtifactReferenceCount = currentConversation.Messages |> List.sumBy (fun message -> message.ArtifactReferences.Length)
                                      AddedArtifactReferenceCount = addedArtifactReferenceCount
                                      RemovedArtifactReferenceCount = removedArtifactReferenceCount }
                            else
                                None
                        | None -> None)
                    |> List.sortBy conversationDeltaLabel

                let sharedConversationCount =
                    baseConversations.Keys
                    |> Seq.filter (fun conversationId -> currentConversations.ContainsKey conversationId)
                    |> Seq.length

                { Provider = provider
                  BaseZipPath = baseZipAbsolutePath
                  CurrentZipPath = currentZipAbsolutePath
                  BaseZipSha256 = baseZipSha256
                  CurrentZipSha256 = currentZipSha256
                  ZipArtifactsIdentical = (baseZipSha256 = currentZipSha256)
                  BaseConversationCount = baseImport.Conversations.Length
                  CurrentConversationCount = currentImport.Conversations.Length
                  BaseMessageCount = baseImport.Conversations |> List.sumBy (fun conversation -> conversation.Messages.Length)
                  CurrentMessageCount = currentImport.Conversations |> List.sumBy (fun conversation -> conversation.Messages.Length)
                  BaseArtifactReferenceCount = baseImport.Conversations |> List.sumBy (fun conversation -> conversation.Messages |> List.sumBy (fun message -> message.ArtifactReferences.Length))
                  CurrentArtifactReferenceCount = currentImport.Conversations |> List.sumBy (fun conversation -> conversation.Messages |> List.sumBy (fun message -> message.ArtifactReferences.Length))
                  AddedConversationCount = addedConversations.Length
                  RemovedConversationCount = removedConversations.Length
                  ChangedConversationCount = changedConversations.Length
                  UnchangedConversationCount = sharedConversationCount - changedConversations.Length
                  AddedConversations = addedConversations |> List.truncate limit
                  RemovedConversations = removedConversations |> List.truncate limit
                  ChangedConversations = changedConversations |> List.truncate limit }))
