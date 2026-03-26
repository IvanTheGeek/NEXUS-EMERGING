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

    let private buildGrokImportRequest tempRoot =
        let objectsRoot = Path.Combine(tempRoot, "objects")
        let eventStoreRoot = Path.Combine(tempRoot, "event-store")
        let zipPath = Path.Combine(tempRoot, "grok-fixture.zip")

        TestHelpers.createZipFromFixture "provider-export/grok" zipPath

        { Provider = Grok
          SourceZipPath = zipPath
          Window = Some(Rolling "30d")
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
                      Expect.isSome firstImport.ImportSnapshotManifestRelativePath "Expected the import to write a normalized import snapshot manifest."
                      Expect.isSome firstImport.ImportSnapshotConversationsRelativePath "Expected the import to write a normalized import snapshot conversation file."
                      Expect.isSome firstImport.WorkingGraphManifestRelativePath "Expected the import to materialize a graph working batch."
                      Expect.isSome firstImport.WorkingGraphCatalogRelativePath "Expected the import to update the graph working catalog."
                      Expect.isSome firstImport.WorkingGraphIndexRelativePath "Expected the import to refresh the SQLite graph working index."
                      Expect.equal firstImport.WorkingGraphAssertionCount (Some 46) "Expected the graph working batch assertion count for the fixture import."

                      let importSnapshotManifestPath =
                          firstImport.ImportSnapshotManifestRelativePath
                          |> Option.map (fun relativePath -> Path.Combine(eventStoreRoot, relativePath))
                          |> Option.defaultWith (fun () -> failwith "Missing import snapshot manifest path.")

                      let importSnapshotManifest = TestHelpers.readToml importSnapshotManifestPath
                      Expect.equal (TomlDocument.tryScalar "snapshot_kind" importSnapshotManifest) (Some "normalized_import_snapshot") "Expected the normalized import snapshot manifest kind."
                      Expect.equal (TomlDocument.tryTableValue "counts" "conversations_seen" importSnapshotManifest) (Some "1") "Expected one conversation in the normalized import snapshot."
                      Expect.equal (TomlDocument.tryTableValue "logos" "source_system" importSnapshotManifest) (Some "claude") "Expected the snapshot manifest to persist the LOGOS source system."
                      Expect.equal (TomlDocument.tryTableValue "logos.handling_policy" "sanitization_status" importSnapshotManifest) (Some "raw") "Expected the snapshot manifest to persist the LOGOS handling policy."
                      Expect.equal (TomlDocument.tryTableValue "logos" "entry_pool" importSnapshotManifest) (Some "raw") "Expected the snapshot manifest to persist the LOGOS entry pool."

                      let importManifestPath = Path.Combine(eventStoreRoot, firstImport.ManifestRelativePath)
                      let importManifest = TestHelpers.readToml importManifestPath
                      Expect.equal (TomlDocument.tryTableValue "logos" "source_system" importManifest) (Some "claude") "Expected the import manifest to persist the LOGOS source system."
                      Expect.equal (TomlDocument.tryTableValue "logos" "intake_channel" importManifest) (Some "ai-conversation") "Expected the import manifest to persist the LOGOS intake channel."
                      Expect.equal (TomlDocument.tryTableValue "logos.handling_policy" "sharing_scope" importManifest) (Some "owner-only") "Expected the import manifest to persist the LOGOS sharing scope."

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

              testCase "Grok provider export import uses the provider-specific payload file" (fun () ->
                  TestHelpers.withTempDirectory "nexus-grok-import" (fun tempRoot ->
                      let request, _, _ = buildGrokImportRequest tempRoot

                      let importResult = ImportWorkflow.run request

                      Expect.equal importResult.Provider Grok "Expected the Grok provider import."
                      Expect.equal importResult.Counts.ConversationsSeen 1 "Expected one Grok conversation."
                      Expect.equal importResult.Counts.MessagesSeen 2 "Expected two Grok messages."
                      Expect.equal importResult.Counts.ArtifactsReferenced 1 "Expected one Grok artifact reference."
                      Expect.equal importResult.Counts.NewEventsAppended 7 "Expected the Grok first import event set."
                      Expect.isSome importResult.ImportSnapshotManifestRelativePath "Expected the Grok import to write a normalized import snapshot manifest."
                      Expect.equal
                          (importResult.ExtractedPayloadRelativePath |> Option.map Path.GetFileName)
                          (Some "prod-grok-backend.json")
                          "Expected the Grok import to preserve the provider-specific payload path."))

              testCase "Provider export imports archive raw zips into distinct directories even within the same second" (fun () ->
                  TestHelpers.withTempDirectory "nexus-claude-import-archive-distinct" (fun tempRoot ->
                      let request, objectsRoot, _ = buildClaudeImportRequest tempRoot

                      let firstImport = ImportWorkflow.run request
                      let secondImport = ImportWorkflow.run request

                      Expect.notEqual firstImport.ArchivedZipRelativePath secondImport.ArchivedZipRelativePath "Expected distinct archived zip paths across successive imports."

                      let firstArchivedZip = Path.Combine(objectsRoot, firstImport.ArchivedZipRelativePath)
                      let secondArchivedZip = Path.Combine(objectsRoot, secondImport.ArchivedZipRelativePath)

                      Expect.isTrue (File.Exists(firstArchivedZip)) "Expected the first archived zip to remain preserved."
                      Expect.isTrue (File.Exists(secondArchivedZip)) "Expected the second archived zip to remain preserved."
                      Expect.notEqual (Path.GetDirectoryName(firstArchivedZip)) (Path.GetDirectoryName(secondArchivedZip)) "Expected distinct archive directories for successive imports." ))

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
                      Expect.isSome firstImport.WorkingGraphManifestRelativePath "Expected Codex import to materialize a graph working batch."
                      Expect.isSome firstImport.WorkingGraphCatalogRelativePath "Expected Codex import to update the graph working catalog."
                      Expect.isSome firstImport.WorkingGraphIndexRelativePath "Expected Codex import to refresh the SQLite graph working index."
                      Expect.equal firstImport.WorkingGraphAssertionCount (Some 36) "Expected the Codex graph working batch assertion count for the fixture import."

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
                      Expect.equal (TomlDocument.tryScalar "message_count" projection) (Some "2") "Expected the Codex projection message count."))

              testCase "Codex import skips null-padding lines in transcript JSONL" (fun () ->
                  TestHelpers.withTempDirectory "nexus-codex-import-null-padding" (fun tempRoot ->
                      let objectsRoot = Path.Combine(tempRoot, "objects")
                      let eventStoreRoot = Path.Combine(tempRoot, "event-store")
                      let snapshotRoot = Path.Combine(objectsRoot, "providers", "codex", "latest")

                      TestHelpers.copyFixtureDirectory "codex/latest" snapshotRoot

                      let transcriptPath =
                          Path.Combine(snapshotRoot, "sessions", "2026", "03", "21", "rollout-codex-session-1.jsonl")

                      File.AppendAllText(transcriptPath, "\n" + String('\000', 48) + "\n")

                      let request =
                          { SnapshotRoot = snapshotRoot
                            ObjectsRoot = objectsRoot
                            EventStoreRoot = eventStoreRoot }

                      let importResult = CodexImportWorkflow.run request

                      Expect.equal importResult.Counts.ConversationsSeen 1 "Expected the Codex conversation to remain importable with null-padding lines."
                      Expect.equal importResult.Counts.MessagesSeen 2 "Expected null-padding lines to be ignored rather than counted as messages."
                      Expect.equal importResult.Counts.NewEventsAppended 5 "Expected the canonical event count to remain unchanged when null-padding lines are skipped.")) ]
