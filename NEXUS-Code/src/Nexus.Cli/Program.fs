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
        printfn "Options for rebuild-conversation-projections:"
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

    let private rebuildConversationProjections eventStoreRoot =
        let projectionPaths = ConversationProjections.rebuild eventStoreRoot
        printfn "Conversation projections rebuilt."
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
        | Ok (RebuildConversationProjections eventStoreRoot) ->
            rebuildConversationProjections eventStoreRoot
        | Error exitCode ->
            exitCode
