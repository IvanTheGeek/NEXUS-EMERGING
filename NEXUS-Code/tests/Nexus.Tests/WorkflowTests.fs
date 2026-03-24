namespace Nexus.Tests

open Expecto
open System
open System.IO
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module WorkflowTests =
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

    let tests =
        testList
            "import workflows"
            [ testCase "Provider export import dedupes on second Claude import" (fun () ->
                  TestHelpers.withTempDirectory "nexus-claude-import" (fun tempRoot ->
                      let request, _, eventStoreRoot = buildClaudeImportRequest tempRoot

                      let firstImport = ImportWorkflow.run request
                      Expect.equal firstImport.Counts.ConversationsSeen 1 "Expected one Claude conversation."
                      Expect.equal firstImport.Counts.MessagesSeen 2 "Expected two Claude messages."
                      Expect.equal firstImport.Counts.ArtifactsReferenced 1 "Expected one Claude artifact reference."
                      Expect.equal firstImport.Counts.NewEventsAppended 7 "Expected the full first import event set."
                      Expect.isSome firstImport.WorkingGraphManifestRelativePath "Expected the import to materialize a graph working slice."
                      Expect.isSome firstImport.WorkingGraphCatalogRelativePath "Expected the import to update the graph working catalog."
                      Expect.isSome firstImport.WorkingGraphIndexRelativePath "Expected the import to refresh the SQLite graph working index."
                      Expect.equal firstImport.WorkingGraphAssertionCount (Some 46) "Expected the graph working slice assertion count for the fixture import."

                      let workingManifestPath =
                          firstImport.WorkingGraphManifestRelativePath
                          |> Option.map (fun relativePath -> Path.Combine(eventStoreRoot, relativePath))
                          |> Option.defaultWith (fun () -> failwith "Missing working graph manifest path.")

                      let workingManifest = TestHelpers.readToml workingManifestPath
                      Expect.equal (TomlDocument.tryScalar "mode" workingManifest) (Some "incremental_import_batch") "Expected the incremental graph working manifest."
                      Expect.equal (TomlDocument.tryScalar "graph_assertions_written" workingManifest) (Some "46") "Expected the working graph assertion count in the manifest."

                      let catalogPath =
                          firstImport.WorkingGraphCatalogRelativePath
                          |> Option.map (fun relativePath -> Path.Combine(eventStoreRoot, relativePath))
                          |> Option.defaultWith (fun () -> failwith "Missing working graph catalog path.")

                      let catalog = TestHelpers.readToml catalogPath
                      Expect.equal (TomlDocument.tryScalar "catalog_version" catalog) (Some "graph-working-import-catalog-v1") "Expected the graph working catalog version."
                      Expect.equal (TomlDocument.tryScalar "entry_count" catalog) (Some "1") "Expected a single graph working catalog entry."

                      let indexPath =
                          firstImport.WorkingGraphIndexRelativePath
                          |> Option.map (fun relativePath -> Path.Combine(eventStoreRoot, relativePath))
                          |> Option.defaultWith (fun () -> failwith "Missing working graph SQLite index path.")

                      Expect.isTrue (File.Exists(indexPath)) "Expected the SQLite graph working index file to exist."

                      let secondImport = ImportWorkflow.run request
                      Expect.equal secondImport.Counts.NewEventsAppended 3 "Expected only import-stream events on duplicate import."
                      Expect.equal secondImport.Counts.DuplicatesSkipped 4 "Expected duplicate conversation, messages, and artifact reference to be skipped."
                      Expect.equal secondImport.Counts.RevisionsObserved 0 "Did not expect revisions from an unchanged duplicate import."
                      Expect.equal secondImport.Counts.ReparseObservationsAppended 0 "Did not expect reparses from an unchanged duplicate import."))

              testCase "Provider export import emits phase and completion status" (fun () ->
                  TestHelpers.withTempDirectory "nexus-claude-import-status" (fun tempRoot ->
                      let request, _, _ = buildClaudeImportRequest tempRoot
                      let messages = ResizeArray<string>()

                      let result = ImportWorkflow.runWithStatus messages.Add request

                      Expect.equal result.Counts.ConversationsSeen 1 "Expected the status-bearing import to still complete normally."
                      Expect.isGreaterThan messages.Count 0 "Expected status messages to be emitted."

                      let combined = String.concat "\n" messages
                      Expect.stringContains combined "Preparing provider import for claude" "Expected the initial import-preparation status."
                      Expect.stringContains combined "Archiving raw export zip into NEXUS-Objects." "Expected the raw archive phase."
                      Expect.stringContains combined "Parsing provider payload from conversations.json." "Expected the parser phase."
                      Expect.stringContains combined "Loading event-store index for dedupe and revision checks." "Expected the dedupe-index phase."
                      Expect.stringContains combined "Processing 1 conversations into canonical history." "Expected the canonical processing phase."
                      Expect.stringContains combined "Writing 7 canonical events to the event store." "Expected the write phase."
                      Expect.stringContains combined "Provider import completed in" "Expected the completion status."))

              testCase "Manual artifact capture hydrates once and skips duplicate content" (fun () ->
                  TestHelpers.withTempDirectory "nexus-artifact-capture" (fun tempRoot ->
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
                            Notes = Some "Fixture hydration" }

                      let firstCapture = ManualArtifactWorkflow.run captureRequest
                      Expect.isFalse firstCapture.DuplicateSkipped "Expected the first artifact hydration to append a capture event."
                      Expect.isSome firstCapture.EventPath "Expected the capture event path to be returned."

                      let secondCapture = ManualArtifactWorkflow.run captureRequest
                      Expect.isTrue secondCapture.DuplicateSkipped "Expected duplicate artifact content to be skipped on second capture."

                      let projectionPaths = ArtifactProjections.rebuild eventStoreRoot
                      Expect.equal projectionPaths.Length 1 "Expected one artifact projection."

                      let projectionPath = Path.Combine(eventStoreRoot, projectionPaths.Head)
                      let projection = TestHelpers.readToml projectionPath

                      Expect.equal (TomlDocument.tryScalar "payload_captured" projection) (Some "true") "Expected the artifact projection to show a captured payload."
                      Expect.equal (TomlDocument.tryScalar "capture_count" projection) (Some "1") "Expected exactly one captured payload in the projection."))

              testCase "Codex import writes canonical history and conversation projection" (fun () ->
                  TestHelpers.withTempDirectory "nexus-codex-import" (fun tempRoot ->
                      let objectsRoot = Path.Combine(tempRoot, "objects")
                      let eventStoreRoot = Path.Combine(tempRoot, "event-store")
                      let snapshotRoot = Path.Combine(objectsRoot, "providers", "codex", "latest")

                      TestHelpers.copyFixtureDirectory "codex/latest" snapshotRoot

                      let request =
                          { SnapshotRoot = snapshotRoot
                            ObjectsRoot = objectsRoot
                            EventStoreRoot = eventStoreRoot }

                      let firstImport = CodexImportWorkflow.run request
                      Expect.equal firstImport.Counts.ConversationsSeen 1 "Expected one Codex conversation."
                      Expect.equal firstImport.Counts.MessagesSeen 2 "Expected two Codex fixture messages."
                      Expect.equal firstImport.Counts.NewEventsAppended 5 "Expected artifact, conversation, two messages, and import completion events."
                      Expect.isSome firstImport.WorkingGraphManifestRelativePath "Expected Codex import to materialize a graph working slice."
                      Expect.isSome firstImport.WorkingGraphCatalogRelativePath "Expected Codex import to update the graph working catalog."
                      Expect.isSome firstImport.WorkingGraphIndexRelativePath "Expected Codex import to refresh the SQLite graph working index."
                      Expect.equal firstImport.WorkingGraphAssertionCount (Some 36) "Expected the Codex graph working slice assertion count for the fixture import."

                      let secondImport = CodexImportWorkflow.run request
                      Expect.equal secondImport.Counts.NewEventsAppended 2 "Expected only import-stream events on duplicate Codex import."
                      Expect.equal secondImport.Counts.DuplicatesSkipped 3 "Expected the duplicate conversation and messages to be skipped."

                      let catalogPath =
                          firstImport.WorkingGraphCatalogRelativePath
                          |> Option.map (fun relativePath -> Path.Combine(eventStoreRoot, relativePath))
                          |> Option.defaultWith (fun () -> failwith "Missing Codex working graph catalog path.")

                      let catalog = TestHelpers.readToml catalogPath
                      Expect.equal (TomlDocument.tryScalar "entry_count" catalog) (Some "2") "Expected both Codex import batches to appear in the graph working catalog."

                      let projectionPaths = ConversationProjections.rebuild eventStoreRoot
                      Expect.equal projectionPaths.Length 1 "Expected one conversation projection."

                      let projectionPath = Path.Combine(eventStoreRoot, projectionPaths.Head)
                      let projection = TestHelpers.readToml projectionPath

                      Expect.equal (TomlDocument.tryScalar "title" projection) (Some "Codex Fixture Session") "Expected the Codex projection title."
                      Expect.equal (TomlDocument.tryScalar "message_count" projection) (Some "2") "Expected the Codex projection message count.")) ]
