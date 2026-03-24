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
        eventStoreRoot

    let tests =
        testList
            "normalized import snapshots"
            [ testCase "snapshot comparison tracks normalized provider snapshot changes across imports" (fun () ->
                  TestHelpers.withTempDirectory "nexus-import-snapshots" (fun tempRoot ->
                      let baseRequest, eventStoreRoot = buildImportRequest tempRoot "claude"
                      let currentRequest, _ = buildImportRequest tempRoot "claude-follow-on"

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
                      let baseRequest, eventStoreRoot = buildImportRequest tempRoot "claude"
                      let currentRequest, _ = buildImportRequest tempRoot "claude-follow-on"

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
                      Expect.stringContains result.StandardOutput "messages=2 -> 3" "Expected the shared conversation message delta." )) ]
