namespace Nexus.Importers

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Text
open Nexus.Domain
open Nexus.EventStore

[<RequireQualifiedAccess>]
module CodexImportWorkflow =
    let private currentNormalizationVersion = NormalizationNaming.codexSessionsCurrent

    type private Intake =
        { ImportedAt: ImportedAt
          RootArtifact: RawObjectRef
          SnapshotRawObjects: RawObjectRef list
          SourceFileName: string
          SourceByteCount: int64
          SnapshotName: string option
          Conversations: ParsedConversation list
          Notes: string list }

    let private observedAtFromImported (ImportedAt value) = ObservedAt value
    let private occurredAt value = OccurredAt value

    let private sha256ForText (value: string) =
        let bytes = Encoding.UTF8.GetBytes(value)
        SHA256.HashData(bytes) |> Convert.ToHexString |> fun hash -> hash.ToLowerInvariant()

    let private contentHashForSignature signature =
        { Algorithm = "sha256"
          Value = sha256ForText signature }

    let private providerArtifactRef fileName =
        { Provider = Codex
          ObjectKind = ExportArtifact
          NativeId = Some fileName
          ConversationNativeId = None
          MessageNativeId = None
          ArtifactNativeId = None }

    let private conversationProviderRef conversationNativeId =
        { Provider = Codex
          ObjectKind = ConversationObject
          NativeId = Some conversationNativeId
          ConversationNativeId = Some conversationNativeId
          MessageNativeId = None
          ArtifactNativeId = None }

    let private messageProviderRef conversationNativeId messageNativeId =
        { Provider = Codex
          ObjectKind = MessageObject
          NativeId = Some messageNativeId
          ConversationNativeId = Some conversationNativeId
          MessageNativeId = Some messageNativeId
          ArtifactNativeId = None }

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
          SourceAcquisition = LocalSessionExport
          NormalizationVersion = Some currentNormalizationVersion
          ContentHash = None
          ImportId = Some importId
          ProviderRefs = []
          RawObjects = intake.SnapshotRawObjects }

    let private envelopeRawObjectsForConversation intake (conversation: ParsedConversation) =
        intake.SnapshotRawObjects @ conversation.RawObjects

    let private importCounts conversationsSeen messagesSeen artifactsReferenced newEvents duplicates revisions reparses : ImportCounts =
        { ConversationsSeen = conversationsSeen
          MessagesSeen = messagesSeen
          ArtifactsReferenced = artifactsReferenced
          NewEventsAppended = newEvents
          DuplicatesSkipped = duplicates
          RevisionsObserved = revisions
          ReparseObservationsAppended = reparses }

    let private appendEvent (events: ResizeArray<CanonicalEvent>) (event: CanonicalEvent) =
        events.Add(event)

    let private tryGetOrAddConversationId index conversationNativeId =
        let providerKey = ProviderKey.conversation Codex conversationNativeId

        match index.ConversationsByProviderKey.TryGetValue(providerKey) with
        | true, existingConversationId -> existingConversationId, true
        | false, _ ->
            let conversationId = ConversationId.create ()
            index.ConversationsByProviderKey[providerKey] <- conversationId
            conversationId, false

    let private tryGetMessageState index conversationNativeId messageNativeId =
        let providerKey = ProviderKey.message Codex conversationNativeId messageNativeId

        match index.MessagesByProviderKey.TryGetValue(providerKey) with
        | true, state -> Some(providerKey, state)
        | false, _ -> None

    let private setMessageState index providerKey state =
        index.MessagesByProviderKey[providerKey] <- state

    /// <summary>
    /// Imports a preserved Codex local-session snapshot into canonical history.
    /// </summary>
    /// <param name="request">The snapshot location and destination roots for the import run.</param>
    /// <returns>A summary of appended events, manifest location, and observed counts.</returns>
    /// <remarks>
    /// Uses the Codex-specific normalization version and local-session acquisition kind.
    /// Full workflow notes: docs/how-to/import-codex-sessions.md
    /// </remarks>
    let run (request: CodexSessionImportRequest) =
        let objectsRoot = Path.GetFullPath(request.ObjectsRoot)
        let snapshotRoot = Path.GetFullPath(request.SnapshotRoot)
        let eventStoreRoot = Path.GetFullPath(request.EventStoreRoot)

        Directory.CreateDirectory(eventStoreRoot) |> ignore

        let parsedSnapshot = CodexSessions.parse objectsRoot snapshotRoot
        let importId = ImportId.create ()
        let rootArtifactId = ArtifactId.create ()
        let importedAt = ImportedAt DateTimeOffset.UtcNow

        let intake =
            { ImportedAt = importedAt
              RootArtifact = parsedSnapshot.RootArtifact
              SnapshotRawObjects = [ parsedSnapshot.RootArtifact ]
              SourceFileName = parsedSnapshot.SourceFileName
              SourceByteCount = parsedSnapshot.SourceByteCount
              SnapshotName = parsedSnapshot.SnapshotName
              Conversations = parsedSnapshot.Conversations
              Notes = parsedSnapshot.Notes }

        let rootArtifactProviderRef = providerArtifactRef intake.SourceFileName
        let index = EventStoreIndex.load eventStoreRoot
        let events = ResizeArray<CanonicalEvent>()
        let mutable conversationsSeen = 0
        let mutable messagesSeen = 0
        let mutable duplicatesSkipped = 0
        let mutable revisionsObserved = 0
        let mutable reparseObservationsAppended = 0

        appendEvent
            events
            { Envelope =
                { baseEnvelope intake importId (CanonicalEventId.create ()) with
                    ArtifactId = Some rootArtifactId
                    ProviderRefs = [ rootArtifactProviderRef ] }
              Body =
                ProviderArtifactReceived
                    { ArtifactId = rootArtifactId
                      Provider = Codex
                      FileName = intake.SourceFileName
                      Window = None
                      ByteCount = Some intake.SourceByteCount } }

        for conversation in intake.Conversations do
            conversationsSeen <- conversationsSeen + 1

            let conversationId, conversationAlreadyKnown =
                tryGetOrAddConversationId index conversation.ProviderConversationId

            let conversationRef = conversationProviderRef conversation.ProviderConversationId
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

            for message in conversation.Messages do
                messagesSeen <- messagesSeen + 1

                let messageRef = messageProviderRef conversation.ProviderConversationId message.ProviderMessageId
                let messageContentHash = contentHashForSignature message.ContentSignature

                match tryGetMessageState index conversation.ProviderConversationId message.ProviderMessageId with
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
                                      RevisionReason = Some "Codex session message was observed again with different canonical content under the same normalization version." } }

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
                    let providerKey = ProviderKey.message Codex conversation.ProviderConversationId message.ProviderMessageId
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

        let counts =
            importCounts
                conversationsSeen
                messagesSeen
                0
                (events.Count + 1)
                duplicatesSkipped
                revisionsObserved
                reparseObservationsAppended

        appendEvent
            events
            { Envelope =
                { baseEnvelope intake importId (CanonicalEventId.create ()) with
                    ArtifactId = Some rootArtifactId
                    ProviderRefs = [ rootArtifactProviderRef ] }
              Body =
                ImportCompleted
                    { ImportId = importId
                      Window = None
                      Counts = counts
                      Notes =
                        Some
                            (match intake.SnapshotName with
                             | Some snapshotName -> $"Imported Codex local session export snapshot {snapshotName}."
                             | None -> "Imported Codex local session export snapshot.") } }

        let eventList = events |> Seq.toList
        let logosMetadata = ProviderLogosImportMetadata.tryBuild Codex
        let manifest =
            { ImportId = importId
              Provider = Codex
              SourceAcquisition = LocalSessionExport
              NormalizationVersion = Some currentNormalizationVersion
              Window = None
              ImportedAt = importedAt
              RootArtifact = intake.RootArtifact
              LogosMetadata = logosMetadata
              Counts = counts
              NewCanonicalEventIds = eventList |> List.map (fun event -> event.Envelope.EventId)
              Notes = intake.Notes }

        let eventPaths = CanonicalStore.writeCanonicalEvents eventStoreRoot eventList
        let manifestPath = CanonicalStore.writeImportManifest eventStoreRoot manifest
        let workingGraph =
            GraphMaterialization.materializeImportBatchWithStatus ignore eventStoreRoot importId eventList
        let workingGraphCatalogRelativePath =
            GraphWorkingCatalog.upsertImportBatch eventStoreRoot workingGraph
        let workingGraphIndexRelativePath =
            GraphWorkingIndex.refreshImportBatch eventStoreRoot workingGraph

        { ImportId = importId
          SnapshotRoot = snapshotRoot
          RootArtifactRelativePath = intake.RootArtifact.RelativePath
          EventPaths = eventPaths
          ManifestRelativePath = manifestPath
          WorkingGraphManifestRelativePath = Some workingGraph.ManifestRelativePath
          WorkingGraphCatalogRelativePath = Some workingGraphCatalogRelativePath
          WorkingGraphIndexRelativePath = Some workingGraphIndexRelativePath
          WorkingGraphAssertionCount = Some workingGraph.GraphAssertionCount
          ConversationSummaries =
            intake.Conversations
            |> List.map (fun conversation ->
                { ProviderConversationId = conversation.ProviderConversationId
                  Title = conversation.Title
                  MessageCountHint = conversation.MessageCountHint })
          Counts = counts }
