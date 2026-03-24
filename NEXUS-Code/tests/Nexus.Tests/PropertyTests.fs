namespace Nexus.Tests

open Expecto
open FsCheck
open System
open System.IO
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module PropertyTests =
    let private boundedRepeat rawValue =
        rawValue |> max 1 |> min 4

    let private buildClaudeImportRequest tempRoot =
        let objectsRoot = Path.Combine(tempRoot, "objects")
        let eventStoreRoot = Path.Combine(tempRoot, "event-store")
        let zipPath = Path.Combine(tempRoot, "claude-fixture.zip")

        TestHelpers.createZipFromFixture "provider-export/claude" zipPath

        { Provider = Claude
          SourceZipPath = zipPath
          Window = Some Full
          ObjectsRoot = objectsRoot
          EventStoreRoot = eventStoreRoot },
        objectsRoot,
        eventStoreRoot

    let private countEventFiles eventStoreRoot =
        let eventsRoot = Path.Combine(eventStoreRoot, "events")

        if Directory.Exists(eventsRoot) then
            Directory.EnumerateFiles(eventsRoot, "*.toml", SearchOption.AllDirectories)
            |> Seq.length
        else
            0

    let private downgradeProviderMessageNormalizationVersions eventStoreRoot =
        let eventsRoot = Path.Combine(eventStoreRoot, "events")
        let currentVersion = "normalization_version = \"provider-export-v1\""
        let legacyVersion = "normalization_version = \"provider-export-v0\""

        if not (Directory.Exists(eventsRoot)) then
            0
        else
            let mutable rewrittenFiles = 0

            for path in Directory.EnumerateFiles(eventsRoot, "*.toml", SearchOption.AllDirectories) do
                let contents = File.ReadAllText(path)

                if contents.Contains("event_kind = \"provider_message_observed\"", StringComparison.Ordinal)
                   && contents.Contains(currentVersion, StringComparison.Ordinal) then
                    File.WriteAllText(path, contents.Replace(currentVersion, legacyVersion, StringComparison.Ordinal))
                    rewrittenFiles <- rewrittenFiles + 1

            rewrittenFiles

    let tests =
        testList
            "property invariants"
            [ testProperty "Provider export duplicate imports remain idempotent across repeat counts" <| fun (PositiveInt rawRepeat) ->
                  let repeatCount = boundedRepeat rawRepeat

                  TestHelpers.withTempDirectory "nexus-property-provider-import" (fun tempRoot ->
                      let request, _, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let results = [ for _ in 1..repeatCount -> ImportWorkflow.run request ]
                      let firstImport = results.Head
                      let duplicateImports = results.Tail

                      firstImport.Counts.ConversationsSeen = 1
                      && firstImport.Counts.MessagesSeen = 2
                      && firstImport.Counts.ArtifactsReferenced = 1
                      && firstImport.Counts.NewEventsAppended = 7
                      && (duplicateImports
                          |> List.forall (fun result ->
                              result.Counts.NewEventsAppended = 3
                              && result.Counts.DuplicatesSkipped = 4
                              && result.Counts.RevisionsObserved = 0
                              && result.Counts.ReparseObservationsAppended = 0))
                      && countEventFiles eventStoreRoot = (7 + ((repeatCount - 1) * 3)))

              testProperty "Legacy normalization markers trigger reparses before returning to duplicate skips" <| fun (PositiveInt rawRepeat) ->
                  let settledDuplicateRuns = boundedRepeat rawRepeat

                  TestHelpers.withTempDirectory "nexus-property-reparse" (fun tempRoot ->
                      let request, _, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let _ = ImportWorkflow.run request
                      let downgradedFiles = downgradeProviderMessageNormalizationVersions eventStoreRoot
                      let reparseImport = ImportWorkflow.run request

                      let settledImports =
                          [ for _ in 1..settledDuplicateRuns -> ImportWorkflow.run request ]

                      downgradedFiles = 2
                      && reparseImport.Counts.NewEventsAppended = 5
                      && reparseImport.Counts.DuplicatesSkipped = 2
                      && reparseImport.Counts.RevisionsObserved = 0
                      && reparseImport.Counts.ReparseObservationsAppended = 2
                      && (settledImports
                          |> List.forall (fun result ->
                              result.Counts.NewEventsAppended = 3
                              && result.Counts.DuplicatesSkipped = 4
                              && result.Counts.RevisionsObserved = 0
                              && result.Counts.ReparseObservationsAppended = 0)))

              testProperty "Manual artifact capture stays idempotent across repeated submissions" <| fun (PositiveInt rawRepeat) ->
                  let attemptCount = boundedRepeat rawRepeat

                  TestHelpers.withTempDirectory "nexus-property-artifact-capture" (fun tempRoot ->
                      let request, objectsRoot, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let _ = ImportWorkflow.run request

                      let payloadPath = Path.Combine(tempRoot, "fixture-note.txt")
                      File.WriteAllText(payloadPath, "Recovered fixture artifact payload")

                      let captureRequest =
                          { Target =
                              ProviderArtifactReference(
                                  Claude,
                                  "claude-conv-1",
                                  "claude-msg-2",
                                  Some "claude-artifact-1",
                                  Some "fixture-note.txt")
                            SourceFilePath = payloadPath
                            ObjectsRoot = objectsRoot
                            EventStoreRoot = eventStoreRoot
                            MediaType = Some "text/plain"
                            Notes = Some "Property test hydration" }

                      let captures =
                          [ for _ in 1..attemptCount -> ManualArtifactWorkflow.run captureRequest ]

                      let projectionPaths = ArtifactProjections.rebuild eventStoreRoot
                      let projectionPath = Path.Combine(eventStoreRoot, projectionPaths.Head)
                      let projection = TestHelpers.readToml projectionPath

                      not captures.Head.DuplicateSkipped
                      && (captures.Tail |> List.forall (fun capture -> capture.DuplicateSkipped))
                      && (captures |> List.choose (fun capture -> capture.EventPath) |> List.length) = 1
                      && projectionPaths.Length = 1
                      && TomlDocument.tryScalar "payload_captured" projection = Some "true"
                      && TomlDocument.tryScalar "capture_count" projection = Some "1") ]
