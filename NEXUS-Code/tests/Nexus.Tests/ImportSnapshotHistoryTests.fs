namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module ImportSnapshotHistoryTests =
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

    let tests =
        testList
            "normalized import snapshot history"
            [ testCase "history report orders snapshots chronologically and computes adjacent deltas" (fun () ->
                  TestHelpers.withTempDirectory "nexus-import-snapshot-history" (fun tempRoot ->
                      let baseRequest, _, eventStoreRoot = buildImportRequest tempRoot "claude"
                      let currentRequest, _, _ = buildImportRequest tempRoot "claude-follow-on"

                      let baseImport = ImportWorkflow.run baseRequest
                      let currentImport = ImportWorkflow.run currentRequest

                      let report =
                          ImportSnapshots.tryBuildHistoryReport eventStoreRoot "claude" 20
                          |> Option.defaultWith (fun () -> failwith "Expected normalized import snapshot history data.")

                      Expect.equal report.Provider "claude" "Expected the provider slug."
                      Expect.equal report.AvailableSnapshotCount 2 "Expected both snapshot rows to be available."
                      Expect.equal report.ReportedEntryCount 2 "Expected both snapshot rows to be reported."
                      Expect.equal report.Entries.Length 2 "Expected two history rows."
                      Expect.equal report.Entries[0].ImportId baseImport.ImportId "Expected the older import first."
                      Expect.equal report.Entries[1].ImportId currentImport.ImportId "Expected the newer import second."
                      Expect.isNone report.Entries[0].DeltaFromPrevious "Expected no delta for the first snapshot."

                      let delta =
                          report.Entries[1].DeltaFromPrevious
                          |> Option.defaultWith (fun () -> failwith "Expected an adjacent delta for the second snapshot.")

                      Expect.equal delta.PreviousImportId baseImport.ImportId "Expected the second row to compare back to the first import."
                      Expect.equal delta.AddedConversationCount 1 "Expected one added conversation in the history delta."
                      Expect.equal delta.RemovedConversationCount 0 "Expected no removed conversations in the history delta."
                      Expect.equal delta.ChangedConversationCount 1 "Expected one changed shared conversation in the history delta."
                      Expect.equal delta.UnchangedConversationCount 0 "Expected no unchanged shared conversations in the fixture delta." ))

              testCase "CLI report-provider-import-history prints chronological snapshot history" (fun () ->
                  TestHelpers.withTempDirectory "nexus-import-snapshot-history-cli" (fun tempRoot ->
                      let baseRequest, objectsRoot, eventStoreRoot = buildImportRequest tempRoot "claude"
                      let currentRequest, _, _ = buildImportRequest tempRoot "claude-follow-on"

                      let _ = ImportWorkflow.run baseRequest
                      let _ = ImportWorkflow.run currentRequest

                      let result =
                          TestHelpers.runCli
                              [ "report-provider-import-history"
                                "--event-store-root"
                                eventStoreRoot
                                "--objects-root"
                                objectsRoot
                                "--provider"
                                "claude"
                                "--limit"
                                "10" ]

                      Expect.equal result.ExitCode 0 "Expected the provider import history command to succeed."
                      Expect.equal result.StandardError "" "Did not expect stderr from the provider import history command."
                      Expect.stringContains result.StandardOutput "Normalized import snapshot history." "Expected the history header."
                      Expect.stringContains result.StandardOutput "Provider: claude" "Expected the provider line."
                      Expect.stringContains result.StandardOutput "Available snapshots: 2" "Expected the snapshot count."
                      Expect.stringContains result.StandardOutput "delta_from_previous=none (first snapshot for provider)" "Expected the first-row delta note."
                      Expect.stringContains result.StandardOutput "added=1 removed=0 changed=1 unchanged=0" "Expected the adjacent delta counts."
                      Expect.stringContains result.StandardOutput "source_artifact_relative_path=" "Expected the source artifact path in the history output."
                      Expect.stringContains result.StandardOutput "source_artifact_sha256=" "Expected the raw artifact hash in the history output."
                      Expect.stringContains result.StandardOutput "source_artifact_matches_previous=false" "Expected the second fixture artifact to differ from the first." )) ]
