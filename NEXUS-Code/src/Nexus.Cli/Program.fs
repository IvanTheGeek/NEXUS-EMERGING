namespace Nexus.Cli

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

module Program =
    type Command =
        | WriteSampleEventStore of eventStoreRoot: string
        | ImportProviderExport of request: ImportRequest
        | CaptureArtifactPayload of request: ManualArtifactCaptureRequest
        | RebuildArtifactProjections of eventStoreRoot: string
        | RebuildConversationProjections of eventStoreRoot: string

    let private repoRoot =
        Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

    let private defaultEventStoreRoot =
        Path.Combine(repoRoot, "NEXUS-EventStore")

    let private defaultObjectsRoot =
        Path.Combine(repoRoot, "NEXUS-Objects")

    let private sha256ForText (value: string) =
        let bytes = Encoding.UTF8.GetBytes(value)
        let hash = SHA256.HashData(bytes)
        Convert.ToHexString(hash).ToLowerInvariant()

    let private usage () =
        printfn "Usage:"
        printfn "  dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- <command> [options]"
        printfn ""
        printfn "Commands:"
        printfn "  write-sample-event-store    Write a small sample canonical history bundle"
        printfn "  import-provider-export     Archive a provider export zip and write canonical observed history"
        printfn "  capture-artifact-payload   Archive a manually added artifact file and append ArtifactPayloadCaptured"
        printfn "  rebuild-artifact-projections"
        printfn "                              Rebuild artifact projections from canonical artifact events"
        printfn "  rebuild-conversation-projections"
        printfn "                              Rebuild conversation projections from canonical events"
        printfn ""
        printfn "Options for write-sample-event-store:"
        printfn "  --event-store-root <path>   Override the event-store root"
        printfn ""
        printfn "Options for import-provider-export:"
        printfn "  --provider <chatgpt|claude> Provider adapter to use"
        printfn "  --zip <path>                Path to the provider export zip"
        printfn "  --window <kind>             Import window; defaults to full"
        printfn "  --objects-root <path>       Override the objects root"
        printfn "  --event-store-root <path>   Override the event-store root"
        printfn ""
        printfn "Options for capture-artifact-payload:"
        printfn "  --file <path>                        Path to the local artifact payload"
        printfn "  --artifact-id <uuid>                 Existing internal artifact ID to hydrate"
        printfn "  --provider <chatgpt|claude>          Provider for provider-key lookup"
        printfn "  --provider-conversation-id <id>      Provider conversation ID"
        printfn "  --provider-message-id <id>           Provider message ID"
        printfn "  --provider-artifact-id <id>          Provider artifact ID when known"
        printfn "  --file-name <name>                   File-name fallback when provider artifact ID is absent"
        printfn "  --media-type <type>                  Override or supply media type"
        printfn "  --notes <text>                       Optional operator notes"
        printfn "  --objects-root <path>                Override the objects root"
        printfn "  --event-store-root <path>            Override the event-store root"
        printfn ""
        printfn "Options for rebuild-conversation-projections:"
        printfn "  --event-store-root <path>   Override the event-store root"
        printfn ""
        printfn "Options for rebuild-artifact-projections:"
        printfn "  --event-store-root <path>   Override the event-store root"

    let private parseWriteSampleEventStore (args: string list) =
        let rec loop currentRoot remaining =
            match remaining with
            | [] -> Ok (WriteSampleEventStore currentRoot)
            | "--event-store-root" :: value :: rest -> loop value rest
            | option :: _ ->
                eprintfn "Unknown option for write-sample-event-store: %s" option
                usage ()
                Error 1

        loop defaultEventStoreRoot args

    let private parseImportProviderExport (args: string list) =
        let rec loop provider zipPath window objectsRoot eventStoreRoot remaining =
            match remaining with
            | [] ->
                match provider, zipPath with
                | Some providerKind, Some sourceZipPath ->
                    Ok
                        (ImportProviderExport
                            { Provider = providerKind
                              SourceZipPath = sourceZipPath
                              Window = window
                              ObjectsRoot = objectsRoot
                              EventStoreRoot = eventStoreRoot })
                | None, _ ->
                    eprintfn "Missing required option for import-provider-export: --provider"
                    usage ()
                    Error 1
                | _, None ->
                    eprintfn "Missing required option for import-provider-export: --zip"
                    usage ()
                    Error 1
            | "--provider" :: value :: rest ->
                match ProviderNaming.tryParse value with
                | Some providerKind -> loop (Some providerKind) zipPath window objectsRoot eventStoreRoot rest
                | None ->
                    eprintfn "Unsupported provider: %s" value
                    usage ()
                    Error 1
            | "--zip" :: value :: rest ->
                loop provider (Some value) window objectsRoot eventStoreRoot rest
            | "--window" :: value :: rest ->
                match ImportWindowNaming.tryParse value with
                | Some parsedWindow -> loop provider zipPath (Some parsedWindow) objectsRoot eventStoreRoot rest
                | None ->
                    eprintfn "Unsupported window: %s" value
                    usage ()
                    Error 1
            | "--objects-root" :: value :: rest ->
                loop provider zipPath window value eventStoreRoot rest
            | "--event-store-root" :: value :: rest ->
                loop provider zipPath window objectsRoot value rest
            | option :: _ ->
                eprintfn "Unknown option for import-provider-export: %s" option
                usage ()
                Error 1

        loop None None (Some Full) defaultObjectsRoot defaultEventStoreRoot args

    let private parseCaptureArtifactPayload (args: string list) =
        let rec loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath mediaType notes objectsRoot eventStoreRoot remaining =
            match remaining with
            | [] ->
                match sourceFilePath with
                | None ->
                    eprintfn "Missing required option for capture-artifact-payload: --file"
                    usage ()
                    Error 1
                | Some filePath ->
                    let target =
                        match artifactId, provider, conversationNativeId, messageNativeId with
                        | Some internalArtifactId, None, None, None ->
                            Ok (ExistingArtifactId internalArtifactId)
                        | Some _, _, _, _ ->
                            eprintfn "Use either --artifact-id or provider/message lookup options, not both."
                            usage ()
                            Error 1
                        | None, Some providerKind, Some conversationId, Some messageId ->
                            Ok
                                (ProviderArtifactReference(
                                    providerKind,
                                    conversationId,
                                    messageId,
                                    providerArtifactId,
                                    fileName))
                        | None, Some _, _, _ ->
                            eprintfn "Provider lookup requires --provider-conversation-id and --provider-message-id."
                            usage ()
                            Error 1
                        | None, None, _, _ ->
                            eprintfn "Missing target for capture-artifact-payload. Use --artifact-id or provider/message lookup options."
                            usage ()
                            Error 1

                    target
                    |> Result.map (fun resolvedTarget ->
                        CaptureArtifactPayload
                            { Target = resolvedTarget
                              SourceFilePath = filePath
                              ObjectsRoot = objectsRoot
                              EventStoreRoot = eventStoreRoot
                              MediaType = mediaType
                              Notes = notes })
            | "--artifact-id" :: value :: rest ->
                loop
                    (Some (ArtifactId.parse value))
                    provider
                    conversationNativeId
                    messageNativeId
                    providerArtifactId
                    fileName
                    sourceFilePath
                    mediaType
                    notes
                    objectsRoot
                    eventStoreRoot
                    rest
            | "--provider" :: value :: rest ->
                match ProviderNaming.tryParse value with
                | Some providerKind ->
                    loop artifactId (Some providerKind) conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath mediaType notes objectsRoot eventStoreRoot rest
                | None ->
                    eprintfn "Unsupported provider: %s" value
                    usage ()
                    Error 1
            | "--provider-conversation-id" :: value :: rest ->
                loop artifactId provider (Some value) messageNativeId providerArtifactId fileName sourceFilePath mediaType notes objectsRoot eventStoreRoot rest
            | "--provider-message-id" :: value :: rest ->
                loop artifactId provider conversationNativeId (Some value) providerArtifactId fileName sourceFilePath mediaType notes objectsRoot eventStoreRoot rest
            | "--provider-artifact-id" :: value :: rest ->
                loop artifactId provider conversationNativeId messageNativeId (Some value) fileName sourceFilePath mediaType notes objectsRoot eventStoreRoot rest
            | "--file-name" :: value :: rest ->
                loop artifactId provider conversationNativeId messageNativeId providerArtifactId (Some value) sourceFilePath mediaType notes objectsRoot eventStoreRoot rest
            | "--file" :: value :: rest ->
                loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName (Some value) mediaType notes objectsRoot eventStoreRoot rest
            | "--media-type" :: value :: rest ->
                loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath (Some value) notes objectsRoot eventStoreRoot rest
            | "--notes" :: value :: rest ->
                loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath mediaType (Some value) objectsRoot eventStoreRoot rest
            | "--objects-root" :: value :: rest ->
                loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath mediaType notes value eventStoreRoot rest
            | "--event-store-root" :: value :: rest ->
                loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath mediaType notes objectsRoot value rest
            | option :: _ ->
                eprintfn "Unknown option for capture-artifact-payload: %s" option
                usage ()
                Error 1

        loop None None None None None None None None None defaultObjectsRoot defaultEventStoreRoot args

    let private parseRebuildConversationProjections (args: string list) =
        let rec loop eventStoreRoot remaining =
            match remaining with
            | [] -> Ok (RebuildConversationProjections eventStoreRoot)
            | "--event-store-root" :: value :: rest ->
                loop value rest
            | option :: _ ->
                eprintfn "Unknown option for rebuild-conversation-projections: %s" option
                usage ()
                Error 1

        loop defaultEventStoreRoot args

    let private parseRebuildArtifactProjections (args: string list) =
        let rec loop eventStoreRoot remaining =
            match remaining with
            | [] -> Ok (RebuildArtifactProjections eventStoreRoot)
            | "--event-store-root" :: value :: rest ->
                loop value rest
            | option :: _ ->
                eprintfn "Unknown option for rebuild-artifact-projections: %s" option
                usage ()
                Error 1

        loop defaultEventStoreRoot args

    let private parseCommand args =
        match args with
        | [] ->
            usage ()
            Error 1
        | [ "--help" ]
        | [ "-h" ] ->
            usage ()
            Error 0
        | "write-sample-event-store" :: rest ->
            parseWriteSampleEventStore rest
        | "import-provider-export" :: rest ->
            parseImportProviderExport rest
        | "capture-artifact-payload" :: rest ->
            parseCaptureArtifactPayload rest
        | "rebuild-artifact-projections" :: rest ->
            parseRebuildArtifactProjections rest
        | "rebuild-conversation-projections" :: rest ->
            parseRebuildConversationProjections rest
        | command :: _ ->
            eprintfn "Unknown command: %s" command
            usage ()
            Error 1

    let private occurredAt value = OccurredAt value
    let private observedAt value = ObservedAt value
    let private importedAt value = ImportedAt value

    let private contentHashForText value =
        { Algorithm = "sha256"
          Value = sha256ForText value }

    let private buildSampleData () =
        let now = DateTimeOffset.UtcNow
        let importId = ImportId.create ()
        let importArtifactId = ArtifactId.create ()
        let conversationId = ConversationId.create ()
        let firstMessageId = MessageId.create ()
        let secondMessageId = MessageId.create ()
        let artifactId = ArtifactId.create ()

        let importedAtValue = importedAt now
        let observedAtValue = observedAt now

        let rootArtifact =
            { RawObjectId = Some (ArtifactId.format importArtifactId)
              Kind = ProviderExportZip
              RelativePath = "providers/claude/archive/2026-03-22T12-13-53Z/data-2026-03-22-12-13-53-batch-0000.zip"
              ArchivedAt = Some importedAtValue
              SourceDescription = Some "Original Claude export zip" }

        let conversationRef =
            { Provider = Claude
              ObjectKind = ConversationObject
              NativeId = Some "bb96fb46-e42e-4bde-a885-4b59d5290fc0"
              ConversationNativeId = Some "bb96fb46-e42e-4bde-a885-4b59d5290fc0"
              MessageNativeId = None
              ArtifactNativeId = None }

        let firstMessageRef =
            { Provider = Claude
              ObjectKind = MessageObject
              NativeId = Some "019cce65-86c1-7007-bfa7-2847c82547d1"
              ConversationNativeId = conversationRef.ConversationNativeId
              MessageNativeId = Some "019cce65-86c1-7007-bfa7-2847c82547d1"
              ArtifactNativeId = None }

        let secondMessageRef =
            { Provider = Claude
              ObjectKind = MessageObject
              NativeId = Some "019cce65-86c1-7dd4-9b8b-4fd51c2d038e"
              ConversationNativeId = conversationRef.ConversationNativeId
              MessageNativeId = Some "019cce65-86c1-7dd4-9b8b-4fd51c2d038e"
              ArtifactNativeId = None }

        let attachmentRef =
            { Provider = Claude
              ObjectKind = ArtifactObject
              NativeId = Some "6-slices-adam.txt"
              ConversationNativeId = conversationRef.ConversationNativeId
              MessageNativeId = Some "019ccfa2-269f-7274-aa84-b4992c72a09c"
              ArtifactNativeId = Some "6-slices-adam.txt" }

        let baseEnvelope eventId =
            { EventId = eventId
              ConversationId = None
              MessageId = None
              ArtifactId = None
              TurnId = None
              DomainId = Some (DomainId.create "ingestion")
              BoundedContextId = Some (BoundedContextId.create "canonical-history")
              OccurredAt = Some (occurredAt now)
              ObservedAt = observedAtValue
              ImportedAt = Some importedAtValue
              SourceAcquisition = ExportZip
              NormalizationVersion = Some NormalizationNaming.current
              ContentHash = None
              ImportId = Some importId
              ProviderRefs = []
              RawObjects = [ rootArtifact ] }

        let firstMessageText = "can you make a mermaid sequence diagram that renders in this chat?"
        let secondMessageText = "Sure! Here's a simple example using Mermaid syntax."

        let providerArtifactReceivedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ArtifactId = Some importArtifactId }
              Body =
                ProviderArtifactReceived
                    { ArtifactId = importArtifactId
                      Provider = Claude
                      FileName = "data-2026-03-22-12-13-53-batch-0000.zip"
                      Window = Some Full
                      ByteCount = Some 36619935L } }

        let rawSnapshotExtractedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ArtifactId = Some importArtifactId }
              Body =
                RawSnapshotExtracted
                    { ArtifactId = importArtifactId
                      ExtractedEntries = Some 4
                      Notes = Some "Top-level JSON payloads extracted for parsing" } }

        let conversationObservedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ConversationId = Some conversationId
                    ProviderRefs = [ conversationRef ] }
              Body =
                ProviderConversationObserved
                    { ConversationId = conversationId
                      ProviderConversation = conversationRef
                      Title = Some "Mermaid sequence diagram for chat"
                      IsArchived = Some false
                      MessageCountHint = Some 4 } }

        let firstMessageObservedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ConversationId = Some conversationId
                    MessageId = Some firstMessageId
                    ContentHash = Some (contentHashForText firstMessageText)
                    ProviderRefs = [ conversationRef; firstMessageRef ] }
              Body =
                ProviderMessageObserved
                    { MessageId = firstMessageId
                      ConversationId = conversationId
                      ProviderMessage = firstMessageRef
                      Role = Human
                      Segments =
                        [ { Kind = PlainText
                            Text = firstMessageText } ]
                      ModelName = None
                      SequenceHint = Some 1 } }

        let secondMessageObservedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ConversationId = Some conversationId
                    MessageId = Some secondMessageId
                    ContentHash = Some (contentHashForText secondMessageText)
                    ProviderRefs = [ conversationRef; secondMessageRef ] }
              Body =
                ProviderMessageObserved
                    { MessageId = secondMessageId
                      ConversationId = conversationId
                      ProviderMessage = secondMessageRef
                      Role = Assistant
                      Segments =
                        [ { Kind = PlainText
                            Text = secondMessageText } ]
                      ModelName = Some "claude-3-7-sonnet"
                      SequenceHint = Some 2 } }

        let artifactReferencedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ConversationId = Some conversationId
                    MessageId = Some secondMessageId
                    ArtifactId = Some artifactId
                    ProviderRefs = [ conversationRef; secondMessageRef; attachmentRef ] }
              Body =
                ArtifactReferenced
                    { ArtifactId = artifactId
                      ConversationId = Some conversationId
                      MessageId = Some secondMessageId
                      FileName = Some "6-slices-adam.txt"
                      MediaType = Some "text/plain"
                      Disposition = PayloadIncluded
                      ProviderArtifact = Some attachmentRef } }

        let appendedEventCount = 7

        let counts =
            { ConversationsSeen = 1
              MessagesSeen = 2
              ArtifactsReferenced = 1
              NewEventsAppended = appendedEventCount
              DuplicatesSkipped = 0
              RevisionsObserved = 0
              ReparseObservationsAppended = 0 }

        let importCompletedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ArtifactId = Some importArtifactId }
              Body =
                ImportCompleted
                    { ImportId = importId
                      Window = Some Full
                      Counts = counts
                      Notes = Some "Sample event-store smoke test" } }

        let events =
            [ providerArtifactReceivedEvent
              rawSnapshotExtractedEvent
              conversationObservedEvent
              firstMessageObservedEvent
              secondMessageObservedEvent
              artifactReferencedEvent
              importCompletedEvent ]

        let manifest =
            { ImportId = importId
              Provider = Claude
              SourceAcquisition = ExportZip
              NormalizationVersion = Some NormalizationNaming.current
              Window = Some Full
              ImportedAt = importedAtValue
              RootArtifact = rootArtifact
              Counts = counts
              NewCanonicalEventIds = events |> List.map (fun event -> event.Envelope.EventId)
              Notes = [ "Sample event-store smoke test" ] }

        events, manifest

    let private writeSampleEventStore eventStoreRoot =
        let events, manifest = buildSampleData ()
        Directory.CreateDirectory(eventStoreRoot) |> ignore
        let eventPaths = CanonicalStore.writeCanonicalEvents eventStoreRoot events
        let manifestPath = CanonicalStore.writeImportManifest eventStoreRoot manifest

        printfn "Sample canonical history written."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Events written: %d" eventPaths.Length

        for relativePath in eventPaths do
            printfn "    %s" relativePath

        printfn "  Manifest written: %s" manifestPath
        0

    let private importProviderExport request =
        let result = ImportWorkflow.run request

        printfn "Provider export imported."
        printfn "  Provider: %s" (ProviderNaming.slug result.Provider)
        printfn "  Import ID: %s" (ImportId.format result.ImportId)
        printfn "  Archived zip: %s" result.ArchivedZipRelativePath
        printfn "  Latest zip: %s" result.LatestZipRelativePath

        match result.ExtractedConversationRelativePath with
        | Some path -> printfn "  Extracted conversations.json: %s" path
        | None -> ()

        printfn "  Event manifest: %s" result.ManifestRelativePath
        printfn "  Events written: %d" result.EventPaths.Length
        printfn "  Conversations seen: %d" result.Counts.ConversationsSeen
        printfn "  Messages seen: %d" result.Counts.MessagesSeen
        printfn "  Artifact references seen: %d" result.Counts.ArtifactsReferenced
        printfn "  New events appended: %d" result.Counts.NewEventsAppended
        printfn "  Duplicates skipped: %d" result.Counts.DuplicatesSkipped
        printfn "  Revisions observed: %d" result.Counts.RevisionsObserved
        printfn "  Reparse observations appended: %d" result.Counts.ReparseObservationsAppended

        if not result.EventPaths.IsEmpty then
            printfn "  First event files:"

            result.EventPaths
            |> List.truncate 5
            |> List.iter (printfn "    %s")

        0

    let private captureArtifactPayload request =
        let result = ManualArtifactWorkflow.run request

        if result.DuplicateSkipped then
            printfn "Artifact payload already known."
            printfn "  Artifact ID: %s" (ArtifactId.format result.ArtifactId)
            printfn "  Byte count: %d" result.ByteCount
            printfn "  Content hash: %s:%s" result.ContentHash.Algorithm result.ContentHash.Value
        else
            printfn "Artifact payload captured."
            printfn "  Artifact ID: %s" (ArtifactId.format result.ArtifactId)

            match result.Provider with
            | Some provider -> printfn "  Provider: %s" (ProviderNaming.slug provider)
            | None -> ()

            match result.ArchivedRelativePath with
            | Some relativePath -> printfn "  Archived file: %s" relativePath
            | None -> ()

            match result.EventPath with
            | Some relativePath -> printfn "  Event written: %s" relativePath
            | None -> ()

            printfn "  Byte count: %d" result.ByteCount
            printfn "  Content hash: %s:%s" result.ContentHash.Algorithm result.ContentHash.Value

        0

    let private rebuildConversationProjections eventStoreRoot =
        let projectionPaths = ConversationProjections.rebuild eventStoreRoot
        printfn "Conversation projections rebuilt."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Projection files written: %d" projectionPaths.Length

        projectionPaths
        |> List.truncate 5
        |> List.iter (printfn "    %s")

        0

    let private rebuildArtifactProjections eventStoreRoot =
        let projectionPaths = ArtifactProjections.rebuild eventStoreRoot
        printfn "Artifact projections rebuilt."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Projection files written: %d" projectionPaths.Length

        projectionPaths
        |> List.truncate 5
        |> List.iter (printfn "    %s")

        0

    [<EntryPoint>]
    let main argv =
        match parseCommand (argv |> Array.toList) with
        | Ok (WriteSampleEventStore eventStoreRoot) ->
            writeSampleEventStore eventStoreRoot
        | Ok (ImportProviderExport request) ->
            importProviderExport request
        | Ok (CaptureArtifactPayload request) ->
            captureArtifactPayload request
        | Ok (RebuildArtifactProjections eventStoreRoot) ->
            rebuildArtifactProjections eventStoreRoot
        | Ok (RebuildConversationProjections eventStoreRoot) ->
            rebuildConversationProjections eventStoreRoot
        | Error exitCode ->
            exitCode
