namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module ImportSnapshotTests =
    let private buildImportRequest tempRoot fixtureFolderName =
        let objectsRoot = Path.Combine(tempRoot, "objects")
        let eventStoreRoot = Path.Combine(tempRoot, "event-store")
        let zipPath = Path.Combine(tempRoot, $"{fixtureFolderName}.zip")

        TestHelpers.createZipFromFixture $"provider-export/{fixtureFolderName}" zipPath

        { Provider = Claude
          SourceZipPath = zipPath
          Window = Some Full
          ObjectsRoot = objectsRoot
          EventStoreRoot = eventStoreRoot },
        objectsRoot,
        eventStoreRoot

    let private deleteSnapshotArtifacts eventStoreRoot (importResult: ImportResult) =
        let deleteIfExists relativePath =
            let absolutePath = Path.Combine(eventStoreRoot, relativePath)

            if File.Exists(absolutePath) then
                File.Delete(absolutePath)

        importResult.ImportSnapshotManifestRelativePath |> Option.iter deleteIfExists
        importResult.ImportSnapshotConversationsRelativePath |> Option.iter deleteIfExists

    let tests =
        testList
            "normalized import snapshots"
            [ testCase "snapshot comparison tracks normalized provider snapshot changes across imports" (fun () ->
                  TestHelpers.withTempDirectory "nexus-import-snapshots" (fun tempRoot ->
                      let baseRequest, _, eventStoreRoot = buildImportRequest tempRoot "claude"
                      let currentRequest, _, _ = buildImportRequest tempRoot "claude-follow-on"

                      let baseImport = ImportWorkflow.run baseRequest
                      let currentImport = ImportWorkflow.run currentRequest

                      let report =
                          ImportSnapshots.tryBuildComparisonReport eventStoreRoot baseImport.ImportId currentImport.ImportId 10
                          |> Option.defaultWith (fun () -> failwith "Expected normalized import snapshot comparison data.")

                      Expect.equal report.BaseProvider "claude" "Expected the base provider slug."
                      Expect.equal report.CurrentProvider "claude" "Expected the current provider slug."
                      Expect.equal report.AddedConversationCount 1 "Expected one added provider-native conversation."
                      Expect.equal report.RemovedConversationCount 0 "Expected no removed provider-native conversations."
                      Expect.equal report.ChangedConversationCount 1 "Expected one changed shared provider-native conversation."
                      Expect.equal report.UnchangedConversationCount 0 "Expected no unchanged shared conversations in the fixture."
                      Expect.equal report.AddedConversations.Head.ProviderConversationId "claude-conv-2" "Expected the added provider-native conversation ID."
                      Expect.equal report.ChangedConversations.Head.ProviderConversationId "claude-conv-1" "Expected the shared provider-native conversation ID."
                      Expect.equal report.ChangedConversations.Head.BaseMessageCount 2 "Expected two base messages."
                      Expect.equal report.ChangedConversations.Head.CurrentMessageCount 3 "Expected three current messages." ))

              testCase "CLI compare-import-snapshots prints normalized snapshot deltas" (fun () ->
                  TestHelpers.withTempDirectory "nexus-import-snapshots-cli" (fun tempRoot ->
                      let baseRequest, _, eventStoreRoot = buildImportRequest tempRoot "claude"
                      let currentRequest, _, _ = buildImportRequest tempRoot "claude-follow-on"

                      let baseImport = ImportWorkflow.run baseRequest
                      let currentImport = ImportWorkflow.run currentRequest

                      let result =
                          TestHelpers.runCli
                              [ "compare-import-snapshots"
                                "--event-store-root"
                                eventStoreRoot
                                "--base-import-id"
                                (ImportId.format baseImport.ImportId)
                                "--current-import-id"
                                (ImportId.format currentImport.ImportId)
                                "--limit"
                                "10" ]

                      Expect.equal result.ExitCode 0 "Expected the normalized snapshot comparison command to succeed."
                      Expect.equal result.StandardError "" "Did not expect stderr from the normalized snapshot comparison command."
                      Expect.stringContains result.StandardOutput "Normalized import snapshot comparison." "Expected the normalized snapshot comparison header."
                      Expect.stringContains result.StandardOutput "Added conversations: 1" "Expected the added conversation count."
                      Expect.stringContains result.StandardOutput "Changed conversations: 1" "Expected the changed conversation count."
                      Expect.stringContains result.StandardOutput "Claude Follow-on Conversation" "Expected the added conversation label."
                      Expect.stringContains result.StandardOutput "messages=2 -> 3" "Expected the shared conversation message delta." ))

              testCase "snapshot backfill recreates deleted normalized snapshot files from preserved raw exports" (fun () ->
                  TestHelpers.withTempDirectory "nexus-import-snapshot-backfill" (fun tempRoot ->
                      let request, objectsRoot, eventStoreRoot = buildImportRequest tempRoot "claude"
                      let importResult = ImportWorkflow.run request

                      deleteSnapshotArtifacts eventStoreRoot importResult

                      Expect.isNone (ImportSnapshots.tryLoadReport eventStoreRoot importResult.ImportId) "Expected the snapshot report to be missing after deleting the snapshot files."

                      let rebuildResult =
                          ImportSnapshotBackfill.run
                              { EventStoreRoot = eventStoreRoot
                                ObjectsRoot = objectsRoot
                                Scope = SpecificImport importResult.ImportId
                                Force = false }

                      Expect.equal rebuildResult.RebuiltCount 1 "Expected one import snapshot to be rebuilt."
                      Expect.equal rebuildResult.FailedCount 0 "Did not expect the snapshot backfill to fail."

                      let rebuiltReport =
                          ImportSnapshots.tryLoadReport eventStoreRoot importResult.ImportId
                          |> Option.defaultWith (fun () -> failwith "Expected the normalized import snapshot to be recreated.")

                      Expect.equal rebuiltReport.Provider "claude" "Expected the rebuilt snapshot to preserve the provider slug."
                      Expect.equal rebuiltReport.ConversationCount 1 "Expected one conversation in the rebuilt snapshot."
                      Expect.equal rebuiltReport.MessageCount 2 "Expected two messages in the rebuilt snapshot."
                      Expect.equal rebuiltReport.ArtifactReferenceCount 1 "Expected one artifact reference in the rebuilt snapshot."
                      Expect.equal rebuiltReport.NormalizationVersion (Some (NormalizationNaming.value NormalizationNaming.current)) "Expected the rebuilt snapshot to record the current parser normalization version." ))

              testCase "CLI rebuild-import-snapshots restores missing snapshot files so compare-import-snapshots works again" (fun () ->
                  TestHelpers.withTempDirectory "nexus-import-snapshot-backfill-cli" (fun tempRoot ->
                      let baseRequest, objectsRoot, eventStoreRoot = buildImportRequest tempRoot "claude"
                      let currentRequest, _, _ = buildImportRequest tempRoot "claude-follow-on"

                      let baseImport = ImportWorkflow.run baseRequest
                      let currentImport = ImportWorkflow.run currentRequest

                      deleteSnapshotArtifacts eventStoreRoot baseImport
                      deleteSnapshotArtifacts eventStoreRoot currentImport

                      let missingCompareResult =
                          TestHelpers.runCli
                              [ "compare-import-snapshots"
                                "--event-store-root"
                                eventStoreRoot
                                "--base-import-id"
                                (ImportId.format baseImport.ImportId)
                                "--current-import-id"
                                (ImportId.format currentImport.ImportId) ]

                      Expect.equal missingCompareResult.ExitCode 1 "Expected comparison to fail when snapshot files are missing."
                      Expect.stringContains missingCompareResult.StandardError "rebuild-import-snapshots" "Expected the missing-snapshot guidance to mention the rebuild command."

                      let rebuildResult =
                          TestHelpers.runCli
                              [ "rebuild-import-snapshots"
                                "--event-store-root"
                                eventStoreRoot
                                "--objects-root"
                                objectsRoot
                                "--all" ]

                      Expect.equal rebuildResult.ExitCode 0 "Expected snapshot rebuild to succeed."
                      Expect.equal rebuildResult.StandardError "" "Did not expect stderr from snapshot rebuild."
                      Expect.stringContains rebuildResult.StandardOutput "Snapshots rebuilt: 2" "Expected both deleted import snapshots to be rebuilt."

                      let compareResult =
                          TestHelpers.runCli
                              [ "compare-import-snapshots"
                                "--event-store-root"
                                eventStoreRoot
                                "--base-import-id"
                                (ImportId.format baseImport.ImportId)
                                "--current-import-id"
                                (ImportId.format currentImport.ImportId) ]

                      Expect.equal compareResult.ExitCode 0 "Expected snapshot comparison to succeed after rebuilding missing snapshot files."
                      Expect.equal compareResult.StandardError "" "Did not expect stderr from the restored comparison command."
                      Expect.stringContains compareResult.StandardOutput "Normalized import snapshot comparison." "Expected the comparison header after rebuild."
                      Expect.stringContains compareResult.StandardOutput "Added conversations: 1" "Expected the restored comparison to report the added conversation." )) ]
