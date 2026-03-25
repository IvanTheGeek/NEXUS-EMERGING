namespace Nexus.Importers

open System
open System.Collections.Generic
open System.Diagnostics
open System.Globalization
open System.IO
open System.IO.Compression
open System.Security.Cryptography
open System.Text
open Nexus.Domain
open Nexus.EventStore

[<RequireQualifiedAccess>]
module ImportWorkflow =
    /// <summary>
    /// Emits human-readable provider import progress.
    /// </summary>
    type StatusReporter = string -> unit

    let private currentNormalizationVersion = NormalizationNaming.current

    type private RawIntake =
        { ImportedAt: ImportedAt
          SourceFileName: string
          SourceByteCount: int64
          ArchiveDirectoryRelativePath: string
          ArchivedZipAbsolutePath: string
          ArchivedZipRelativePath: string
          LatestZipRelativePath: string
          ProviderPayloadAbsolutePath: string option
          ProviderPayloadRelativePath: string option
          ExtractedEntries: int
          ExtractedFileNames: Set<string>
          RootArtifact: RawObjectRef
          ProviderPayloadSnapshot: RawObjectRef option }

    let private importedAtValue (ImportedAt value) = value
    let private observedAtFromImported (ImportedAt value) = ObservedAt value
    let private occurredAt value = OccurredAt value

    let private sha256ForText (value: string) =
        let bytes = Encoding.UTF8.GetBytes(value)
        SHA256.HashData(bytes) |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

    let private contentHashForSignature signature =
        { Algorithm = "sha256"
          Value = sha256ForText signature }

    let private importWindowLabel =
        function
        | Some window -> ImportWindowNaming.value window
        | None -> "full"

    let private formatElapsed (elapsed: TimeSpan) =
        elapsed.ToString(@"hh\:mm\:ss")

    let private emitStatus (status: StatusReporter) message =
        status message

    let private timestampFolderName (timestamp: DateTimeOffset) =
        timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ss-fffffffZ", CultureInfo.InvariantCulture)

    let private ensureEmptyDirectory path =
        if Directory.Exists(path) then
            Directory.Delete(path, true)

        Directory.CreateDirectory(path) |> ignore

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
                extractedNames <- extractedNames.Add(entry.FullName.Trim().ToLowerInvariant())

        extractedEntries, extractedNames

    let rec private copyDirectory sourceDirectory destinationDirectory =
        Directory.CreateDirectory(destinationDirectory) |> ignore

        for filePath in Directory.EnumerateFiles(sourceDirectory) do
            let targetPath = Path.Combine(destinationDirectory, Path.GetFileName(filePath))
            File.Copy(filePath, targetPath, true)

        for childDirectory in Directory.EnumerateDirectories(sourceDirectory) do
            let childName = Path.GetFileName(childDirectory)
            copyDirectory childDirectory (Path.Combine(destinationDirectory, childName))

    let private archiveRawImport (request: ImportRequest) =
        let sourceZipPath = Path.GetFullPath(request.SourceZipPath)

        if not (File.Exists(sourceZipPath)) then
            invalidArg "request.SourceZipPath" $"Source zip not found: {sourceZipPath}"

        let importedAt = ImportedAt DateTimeOffset.UtcNow
        let importedAtInstant = importedAtValue importedAt
        let providerSlug = ProviderNaming.slug request.Provider
        let latestBaseName = ImportWindowNaming.latestBaseName request.Window
        let sourceFileName = Path.GetFileName(sourceZipPath)
        let sourceByteCount = FileInfo(sourceZipPath).Length

        let archiveDirectoryName = $"{timestampFolderName importedAtInstant}-{latestBaseName}"

        let archiveDirectoryRelativePath =
            Path.Combine("providers", providerSlug, "archive", archiveDirectoryName).Replace('\\', '/')

        let archiveDirectoryAbsolutePath = Path.Combine(request.ObjectsRoot, archiveDirectoryRelativePath)
        Directory.CreateDirectory(archiveDirectoryAbsolutePath) |> ignore

        let archivedZipAbsolutePath = Path.Combine(archiveDirectoryAbsolutePath, sourceFileName)
        File.Copy(sourceZipPath, archivedZipAbsolutePath, true)

        let extractedArchiveAbsolutePath = Path.Combine(archiveDirectoryAbsolutePath, "extracted")
        let extractedEntries, extractedNames = safeExtractToDirectory archivedZipAbsolutePath extractedArchiveAbsolutePath

        let latestDirectoryAbsolutePath = Path.Combine(request.ObjectsRoot, "providers", providerSlug, "latest")
        Directory.CreateDirectory(latestDirectoryAbsolutePath) |> ignore

        let latestZipRelativePath =
            Path.Combine("providers", providerSlug, "latest", $"{latestBaseName}.zip").Replace('\\', '/')

        let latestZipAbsolutePath = Path.Combine(request.ObjectsRoot, latestZipRelativePath)
        File.Copy(archivedZipAbsolutePath, latestZipAbsolutePath, true)

        let latestExtractedAbsolutePath = Path.Combine(latestDirectoryAbsolutePath, latestBaseName)
        ensureEmptyDirectory latestExtractedAbsolutePath
        copyDirectory extractedArchiveAbsolutePath latestExtractedAbsolutePath

        let archivedZipRelativePath =
            Path.Combine(archiveDirectoryRelativePath, sourceFileName).Replace('\\', '/')

        let providerPayloadLocation =
            ProviderAdapters.tryLocatePayload request.Provider extractedArchiveAbsolutePath

        let providerPayloadRelativePath =
            providerPayloadLocation
            |> Option.map (fun location ->
                Path.Combine(archiveDirectoryRelativePath, "extracted", location.RelativePath).Replace('\\', '/'))

        let rootArtifact =
            { RawObjectId = None
              Kind = ProviderExportZip
              RelativePath = archivedZipRelativePath
              ArchivedAt = Some importedAt
              SourceDescription = Some $"Archived {providerSlug} export zip" }

        let providerPayloadSnapshot =
            providerPayloadLocation
            |> Option.map (fun location ->
                let relativePath =
                    Path.Combine(archiveDirectoryRelativePath, "extracted", location.RelativePath).Replace('\\', '/')

                { RawObjectId = None
                  Kind = ExtractedSnapshot
                  RelativePath = relativePath
                  ArchivedAt = Some importedAt
                  SourceDescription = Some location.Description })

        { ImportedAt = importedAt
          SourceFileName = sourceFileName
          SourceByteCount = sourceByteCount
          ArchiveDirectoryRelativePath = archiveDirectoryRelativePath
          ArchivedZipAbsolutePath = archivedZipAbsolutePath
          ArchivedZipRelativePath = archivedZipRelativePath
          LatestZipRelativePath = latestZipRelativePath
          ProviderPayloadAbsolutePath = providerPayloadLocation |> Option.map (fun value -> value.AbsolutePath)
          ProviderPayloadRelativePath = providerPayloadRelativePath
          ExtractedEntries = extractedEntries
          ExtractedFileNames = extractedNames
          RootArtifact = rootArtifact
          ProviderPayloadSnapshot = providerPayloadSnapshot }

    let private conversationProviderRef provider conversationNativeId =
        { Provider = provider
          ObjectKind = ConversationObject
          NativeId = Some conversationNativeId
          ConversationNativeId = Some conversationNativeId
          MessageNativeId = None
          ArtifactNativeId = None }

    let private messageProviderRef provider conversationNativeId messageNativeId =
        { Provider = provider
          ObjectKind = MessageObject
          NativeId = Some messageNativeId
          ConversationNativeId = Some conversationNativeId
          MessageNativeId = Some messageNativeId
          ArtifactNativeId = None }

    let private artifactProviderRef provider conversationNativeId messageNativeId artifactNativeId =
        { Provider = provider
          ObjectKind = ArtifactObject
          NativeId = Some artifactNativeId
          ConversationNativeId = Some conversationNativeId
          MessageNativeId = Some messageNativeId
          ArtifactNativeId = Some artifactNativeId }

    let private providerExportRef provider sourceFileName =
        { Provider = provider
          ObjectKind = ExportArtifact
          NativeId = Some sourceFileName
          ConversationNativeId = None
          MessageNativeId = None
          ArtifactNativeId = None }

    let private rawObjectsForEnvelope intake =
        [ Some intake.RootArtifact
          intake.ProviderPayloadSnapshot ]
        |> List.choose id

    let private envelopeRawObjectsForConversation intake (conversation: ParsedConversation) =
        (rawObjectsForEnvelope intake) @ conversation.RawObjects

    let private baseEnvelope intake importId eventId =
        { EventId = eventId
          ConversationId = None
          MessageId = None
          ArtifactId = None
          TurnId = None
          DomainId = Some (DomainId.create "ingestion")
          BoundedContextId = Some (BoundedContextId.create "canonical-history")
          OccurredAt = None
          ObservedAt = observedAtFromImported intake.ImportedAt
          ImportedAt = Some intake.ImportedAt
          SourceAcquisition = ExportZip
          NormalizationVersion = Some currentNormalizationVersion
          ContentHash = None
          ImportId = Some importId
          ProviderRefs = []
          RawObjects = rawObjectsForEnvelope intake }

    let private appendEvent (events: ResizeArray<CanonicalEvent>) (event: CanonicalEvent) =
        events.Add(event)

    let private tryGetOrAddConversationId index provider conversationNativeId =
        let providerKey = ProviderKey.conversation provider conversationNativeId

        match index.ConversationsByProviderKey.TryGetValue(providerKey) with
        | true, existingConversationId -> existingConversationId, true
        | false, _ ->
            let conversationId = ConversationId.create ()
            index.ConversationsByProviderKey[providerKey] <- conversationId
            conversationId, false

    let private tryGetMessageState index provider conversationNativeId messageNativeId =
        let providerKey = ProviderKey.message provider conversationNativeId messageNativeId

        match index.MessagesByProviderKey.TryGetValue(providerKey) with
        | true, state -> Some(providerKey, state)
        | false, _ -> None

    let private setMessageState index providerKey state =
        index.MessagesByProviderKey[providerKey] <- state

    let private tryGetOrAddArtifactId index provider conversationNativeId messageNativeId artifactNativeId fileName =
        let providerKey =
            artifactNativeId |> Option.map (ProviderKey.artifact provider conversationNativeId messageNativeId)

        let fallbackKey =
            fileName |> Option.map (ProviderKey.artifactFallback provider conversationNativeId messageNativeId)

        match providerKey |> Option.bind (fun key -> match index.ArtifactsByProviderKey.TryGetValue(key) with | true, artifactId -> Some(key, artifactId) | _ -> None) with
        | Some (_, artifactId) -> artifactId, true
        | None ->
            match fallbackKey |> Option.bind (fun key -> match index.ArtifactsByFallbackKey.TryGetValue(key) with | true, artifactId -> Some(key, artifactId) | _ -> None) with
            | Some (_, artifactId) -> artifactId, true
            | None ->
                let artifactId = ArtifactId.create ()

                providerKey |> Option.iter (fun key -> index.ArtifactsByProviderKey[key] <- artifactId)
                fallbackKey |> Option.iter (fun key -> index.ArtifactsByFallbackKey[key] <- artifactId)
                artifactId, false

    let private importCounts conversationsSeen messagesSeen artifactsReferenced newEvents duplicates revisions reparses =
        { ConversationsSeen = conversationsSeen
          MessagesSeen = messagesSeen
          ArtifactsReferenced = artifactsReferenced
          NewEventsAppended = newEvents
          DuplicatesSkipped = duplicates
          RevisionsObserved = revisions
          ReparseObservationsAppended = reparses }

    /// <summary>
    /// Imports a provider export zip into the object layer and canonical event store, emitting phase and progress messages.
    /// </summary>
    /// <param name="status">Receives human-readable workflow progress messages.</param>
    /// <param name="request">Locations and provider metadata for the import run.</param>
    /// <returns>A summary of archived raw artifacts, appended events, and import counts.</returns>
    /// <remarks>
    /// Preserves raw input, appends canonical history, and distinguishes duplicates from reparses.
    /// Full workflow notes: docs/how-to/import-provider-export.md
    /// </remarks>
    let runWithStatus (status: StatusReporter) (request: ImportRequest) =
        let stopwatch = Stopwatch.StartNew()
        let objectsRoot = Path.GetFullPath(request.ObjectsRoot)
        let eventStoreRoot = Path.GetFullPath(request.EventStoreRoot)
        let providerSlug = ProviderNaming.slug request.Provider
        let windowLabel = importWindowLabel request.Window

        emitStatus status $"Preparing provider import for {providerSlug} ({windowLabel})."
        Directory.CreateDirectory(objectsRoot) |> ignore
        Directory.CreateDirectory(eventStoreRoot) |> ignore

        emitStatus status "Archiving raw export zip into NEXUS-Objects."
        let intake = archiveRawImport { request with ObjectsRoot = objectsRoot }
        emitStatus status $"Raw archive complete: {intake.SourceFileName} copied and {intake.ExtractedEntries} extracted entries staged."

        let providerPayloadPath =
            match intake.ProviderPayloadAbsolutePath with
            | Some value -> value
            | None -> failwith $"The provider export did not contain the expected {providerSlug} payload file."

        let providerPayloadLabel =
            intake.ProviderPayloadRelativePath
            |> Option.map Path.GetFileName
            |> Option.defaultValue "provider payload"

        emitStatus status $"Parsing provider payload from {providerPayloadLabel}."
        let parsedImport =
            ProviderAdapters.parse
                request.Provider
                request.Window
                intake.SourceFileName
                intake.SourceByteCount
                intake.ExtractedEntries
                intake.ExtractedFileNames
                providerPayloadPath

        let parsedConversationCount = parsedImport.Conversations.Length
        let parsedMessageCount = parsedImport.Conversations |> List.sumBy (fun conversation -> conversation.Messages.Length)
        let parsedArtifactCount =
            parsedImport.Conversations
            |> List.sumBy (fun conversation ->
                conversation.Messages
                |> List.sumBy (fun message -> message.ArtifactReferences.Length))

        emitStatus
            status
            $"Provider payload parsed: {parsedConversationCount} conversations, {parsedMessageCount} messages, {parsedArtifactCount} artifact references."

        let importId = ImportId.create ()
        let importArtifactId = ArtifactId.create ()
        let exportProviderRef = providerExportRef request.Provider intake.SourceFileName
        emitStatus status "Loading event-store index for dedupe and revision checks."
        let index = EventStoreIndex.load eventStoreRoot
        let events = ResizeArray<CanonicalEvent>()
        let snapshotConversations = ResizeArray<NormalizedImportSnapshotConversation>()
        let mutable conversationsSeen = 0
        let mutable messagesSeen = 0
        let mutable artifactsReferenced = 0
        let mutable duplicatesSkipped = 0
        let mutable revisionsObserved = 0
        let mutable reparseObservationsAppended = 0
        let progressInterval =
            match parsedConversationCount with
            | count when count <= 10 -> 1
            | count when count <= 50 -> 5
            | count when count <= 200 -> 10
            | _ -> 25

        appendEvent
            events
            { Envelope =
                { baseEnvelope intake importId (CanonicalEventId.create ()) with
                    ArtifactId = Some importArtifactId
                    ProviderRefs = [ exportProviderRef ] }
              Body =
                ProviderArtifactReceived
                    { ArtifactId = importArtifactId
                      Provider = request.Provider
                      FileName = intake.SourceFileName
                      Window = request.Window
                      ByteCount = Some intake.SourceByteCount } }

        appendEvent
            events
            { Envelope =
                { baseEnvelope intake importId (CanonicalEventId.create ()) with
                    ArtifactId = Some importArtifactId
                    ProviderRefs = [ exportProviderRef ] }
              Body =
                RawSnapshotExtracted
                    { ArtifactId = importArtifactId
                      ExtractedEntries = Some intake.ExtractedEntries
                      Notes = Some "Provider export extracted into working raw snapshot." } }

        emitStatus status $"Processing {parsedConversationCount} conversations into canonical history."

        for conversationIndex, conversation in parsedImport.Conversations |> List.indexed do
            conversationsSeen <- conversationsSeen + 1

            let conversationId, conversationAlreadyKnown =
                tryGetOrAddConversationId index request.Provider conversation.ProviderConversationId

            let conversationRef = conversationProviderRef request.Provider conversation.ProviderConversationId
            let envelopeRawObjects = envelopeRawObjectsForConversation intake conversation

            if conversationAlreadyKnown then
                duplicatesSkipped <- duplicatesSkipped + 1
            else
                appendEvent
                    events
                    { Envelope =
                        { baseEnvelope intake importId (CanonicalEventId.create ()) with
                            ConversationId = Some conversationId
                            OccurredAt = conversation.OccurredAt |> Option.map occurredAt
                            ProviderRefs = [ conversationRef ]
                            RawObjects = envelopeRawObjects }
                      Body =
                        ProviderConversationObserved
                            { ConversationId = conversationId
                              ProviderConversation = conversationRef
                              Title = conversation.Title
                                      IsArchived = conversation.IsArchived
                                      MessageCountHint = conversation.MessageCountHint } }

            snapshotConversations.Add
                { CanonicalConversationId = ConversationId.format conversationId
                  ProviderConversationId = conversation.ProviderConversationId
                  Title = conversation.Title
                  IsArchived = conversation.IsArchived
                  OccurredAt = conversation.OccurredAt
                  MessageCount = conversation.Messages.Length
                  ArtifactReferenceCount =
                    conversation.Messages
                    |> List.sumBy (fun value -> value.ArtifactReferences.Length) }

            for message in conversation.Messages do
                messagesSeen <- messagesSeen + 1

                let messageRef = messageProviderRef request.Provider conversation.ProviderConversationId message.ProviderMessageId
                let messageContentHash = contentHashForSignature message.ContentSignature

                match tryGetMessageState index request.Provider conversation.ProviderConversationId message.ProviderMessageId with
                | Some (providerKey, existingMessage) ->
                    match EventStoreIndex.tryGetObservedContentHash currentNormalizationVersion existingMessage with
                    | Some (Some existingContentHash) when existingContentHash = messageContentHash ->
                        duplicatesSkipped <- duplicatesSkipped + 1
                    | Some existingContentHash ->
                        revisionsObserved <- revisionsObserved + 1

                        appendEvent
                            events
                            { Envelope =
                                { baseEnvelope intake importId (CanonicalEventId.create ()) with
                                    ConversationId = Some conversationId
                                    MessageId = Some existingMessage.MessageId
                                    OccurredAt = message.OccurredAt |> Option.map occurredAt
                                    ContentHash = Some messageContentHash
                                    ProviderRefs = [ conversationRef; messageRef ]
                                    RawObjects = envelopeRawObjects }
                              Body =
                                ProviderMessageRevisionObserved
                                    { MessageId = existingMessage.MessageId
                                      PriorContentHash = existingContentHash
                                      RevisedContentHash = messageContentHash
                                      RevisionReason = Some "Provider message was observed again with different canonical content under the same normalization version." } }

                        EventStoreIndex.setObservedContentHash currentNormalizationVersion (Some messageContentHash) existingMessage
                    | None ->
                        reparseObservationsAppended <- reparseObservationsAppended + 1

                        appendEvent
                            events
                            { Envelope =
                                { baseEnvelope intake importId (CanonicalEventId.create ()) with
                                    ConversationId = Some conversationId
                                    MessageId = Some existingMessage.MessageId
                                    OccurredAt = message.OccurredAt |> Option.map occurredAt
                                    ContentHash = Some messageContentHash
                                    ProviderRefs = [ conversationRef; messageRef ]
                                    RawObjects = envelopeRawObjects }
                              Body =
                                ProviderMessageObserved
                                    { MessageId = existingMessage.MessageId
                                      ConversationId = conversationId
                                      ProviderMessage = messageRef
                                      Role = message.Role
                                      Segments = message.Segments
                                      ModelName = message.ModelName
                                      SequenceHint = message.SequenceHint } }

                        EventStoreIndex.setObservedContentHash currentNormalizationVersion (Some messageContentHash) existingMessage
                | None ->
                    let messageId = MessageId.create ()
                    let providerKey = ProviderKey.message request.Provider conversation.ProviderConversationId message.ProviderMessageId
                    let contentHashesByNormalizationVersion = Dictionary<string, ContentHash option>(StringComparer.Ordinal)
                    contentHashesByNormalizationVersion[NormalizationNaming.value currentNormalizationVersion] <- Some messageContentHash

                    setMessageState
                        index
                        providerKey
                        { MessageId = messageId
                          ConversationId = Some conversationId
                          ContentHashesByNormalizationVersion = contentHashesByNormalizationVersion }

                    appendEvent
                        events
                        { Envelope =
                            { baseEnvelope intake importId (CanonicalEventId.create ()) with
                                ConversationId = Some conversationId
                                MessageId = Some messageId
                                OccurredAt = message.OccurredAt |> Option.map occurredAt
                                ContentHash = Some messageContentHash
                                ProviderRefs = [ conversationRef; messageRef ]
                                RawObjects = envelopeRawObjects }
                          Body =
                            ProviderMessageObserved
                                { MessageId = messageId
                                  ConversationId = conversationId
                                  ProviderMessage = messageRef
                                  Role = message.Role
                                  Segments = message.Segments
                                  ModelName = message.ModelName
                                  SequenceHint = message.SequenceHint } }

                let messageId =
                    match tryGetMessageState index request.Provider conversation.ProviderConversationId message.ProviderMessageId with
                    | Some (_, state) -> state.MessageId
                    | None -> failwith "Message state should exist after observation handling."

                for artifact in message.ArtifactReferences do
                    artifactsReferenced <- artifactsReferenced + 1

                    let artifactId, artifactAlreadyKnown =
                        tryGetOrAddArtifactId
                            index
                            request.Provider
                            conversation.ProviderConversationId
                            message.ProviderMessageId
                            artifact.ProviderArtifactId
                            artifact.FileName

                    if artifactAlreadyKnown then
                        duplicatesSkipped <- duplicatesSkipped + 1
                    else
                        let providerRefs =
                            [ yield conversationRef
                              yield messageRef

                              match artifact.ProviderArtifactId with
                              | Some artifactNativeId ->
                                  yield artifactProviderRef request.Provider conversation.ProviderConversationId message.ProviderMessageId artifactNativeId
                              | None -> () ]

                        appendEvent
                            events
                            { Envelope =
                                { baseEnvelope intake importId (CanonicalEventId.create ()) with
                                    ConversationId = Some conversationId
                                    MessageId = Some messageId
                                    ArtifactId = Some artifactId
                                    ProviderRefs = providerRefs
                                    RawObjects = envelopeRawObjects }
                              Body =
                                ArtifactReferenced
                                    { ArtifactId = artifactId
                                      ConversationId = Some conversationId
                                      MessageId = Some messageId
                                      FileName = artifact.FileName
                                      MediaType = artifact.MediaType
                                      Disposition = artifact.Disposition
                                      ProviderArtifact =
                                        artifact.ProviderArtifactId
                                        |> Option.map (artifactProviderRef request.Provider conversation.ProviderConversationId message.ProviderMessageId) } }

            let processedConversations = conversationIndex + 1

            if processedConversations = parsedConversationCount
               || processedConversations % progressInterval = 0 then
                emitStatus
                    status
                    $"Processed {processedConversations}/{parsedConversationCount} conversations; {messagesSeen} messages, {artifactsReferenced} artifact references, {events.Count} pending events, {duplicatesSkipped} duplicates, {revisionsObserved} revisions, {reparseObservationsAppended} reparses."

        let importCompletedCounts =
            importCounts
                conversationsSeen
                messagesSeen
                artifactsReferenced
                (events.Count + 1)
                duplicatesSkipped
                revisionsObserved
                reparseObservationsAppended

        let importCompletedEvent =
            { Envelope =
                { baseEnvelope intake importId (CanonicalEventId.create ()) with
                    ArtifactId = Some importArtifactId
                    ProviderRefs = [ exportProviderRef ] }
              Body =
                ImportCompleted
                    { ImportId = importId
                      Window = request.Window
                      Counts = importCompletedCounts
                      Notes = Some $"Imported {ProviderNaming.slug request.Provider} provider export." } }

        appendEvent events importCompletedEvent

        let eventList = events |> Seq.toList
        let manifest =
            { ImportId = importId
              Provider = request.Provider
              SourceAcquisition = ExportZip
              NormalizationVersion = Some currentNormalizationVersion
              Window = request.Window
              ImportedAt = intake.ImportedAt
              RootArtifact = intake.RootArtifact
              Counts = importCompletedCounts
              NewCanonicalEventIds = eventList |> List.map (fun event -> event.Envelope.EventId)
              Notes = parsedImport.Notes }

        emitStatus status $"Writing {eventList.Length} canonical events to the event store."
        let eventPaths = CanonicalStore.writeCanonicalEvents eventStoreRoot eventList
        emitStatus status "Writing import manifest."
        let manifestPath = CanonicalStore.writeImportManifest eventStoreRoot manifest
        emitStatus status "Writing normalized import snapshot."
        let importSnapshot =
            { ImportId = importId
              Provider = providerSlug
              Window = request.Window |> Option.map ImportWindowNaming.value
              ImportedAt = importedAtValue intake.ImportedAt
              NormalizationVersion = Some (NormalizationNaming.value currentNormalizationVersion)
              SourceArtifactRelativePath = Some intake.ArchivedZipRelativePath
              Conversations = snapshotConversations |> Seq.toList }

        let importSnapshotResult = ImportSnapshots.write eventStoreRoot importSnapshot
        emitStatus status $"Materializing graph working slice for import {ImportId.format importId}."
        let workingGraph =
            GraphMaterialization.materializeImportBatchWithStatus status eventStoreRoot importId eventList
        let workingGraphCatalogRelativePath =
            GraphWorkingCatalog.upsertImportBatch eventStoreRoot workingGraph
        emitStatus status $"Refreshing SQLite graph working index for import {ImportId.format importId}."
        let workingGraphIndexRelativePath =
            GraphWorkingIndex.refreshImportBatch eventStoreRoot workingGraph
        stopwatch.Stop()
        emitStatus
            status
            $"Provider import completed in {formatElapsed stopwatch.Elapsed}: {importCompletedCounts.NewEventsAppended} new events appended, {importCompletedCounts.DuplicatesSkipped} duplicates skipped, {importCompletedCounts.RevisionsObserved} revisions, {importCompletedCounts.ReparseObservationsAppended} reparses."

        { Provider = request.Provider
          ImportId = importId
          ArchivedZipRelativePath = intake.ArchivedZipRelativePath
          LatestZipRelativePath = intake.LatestZipRelativePath
          ExtractedPayloadRelativePath = intake.ProviderPayloadRelativePath
          EventPaths = eventPaths
          ManifestRelativePath = manifestPath
          ImportSnapshotManifestRelativePath = Some importSnapshotResult.ManifestRelativePath
          ImportSnapshotConversationsRelativePath = Some importSnapshotResult.ConversationsRelativePath
          WorkingGraphManifestRelativePath = Some workingGraph.ManifestRelativePath
          WorkingGraphCatalogRelativePath = Some workingGraphCatalogRelativePath
          WorkingGraphIndexRelativePath = Some workingGraphIndexRelativePath
          WorkingGraphAssertionCount = Some workingGraph.GraphAssertionCount
          Counts = importCompletedCounts }

    /// <summary>
    /// Imports a provider export zip into the object layer and canonical event store.
    /// </summary>
    /// <param name="request">Locations and provider metadata for the import run.</param>
    /// <returns>A summary of archived raw artifacts, appended events, and import counts.</returns>
    /// <remarks>
    /// Use <c>runWithStatus</c> when you want human-readable progress messages during larger imports.
    /// Full workflow notes: docs/how-to/import-provider-export.md
    /// </remarks>
    let run (request: ImportRequest) = runWithStatus ignore request
