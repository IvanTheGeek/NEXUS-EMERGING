namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module GraphWorkingIndexTests =
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
        eventStoreRoot

    let tests =
        testList
            "graph working index"
            [ testCase "Graph working SQLite index summarizes one imported slice" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-index" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let importResult = ImportWorkflow.run request

                      let report =
                          GraphWorkingIndex.tryBuildImportSliceReport eventStoreRoot importResult.ImportId 10
                          |> Option.defaultWith (fun () -> failwith "Expected a graph working SQLite report for the imported slice.")

                      Expect.equal report.IndexRelativePath "graph/working/index/graph-working.sqlite" "Expected the stable working-index path."
                      Expect.equal report.ImportId importResult.ImportId "Expected the report to target the imported slice."
                      Expect.equal report.Provider (Some "claude") "Expected provider enrichment from the import manifest."
                      Expect.equal report.Window (Some "full") "Expected window enrichment from the import manifest."
                      Expect.equal report.CanonicalEventCount 7 "Expected the canonical event count from the fixture import."
                      Expect.equal report.GraphAssertionCount 46 "Expected the graph assertion count from the fixture import."
                      Expect.equal report.DistinctSubjectCount 8 "Expected the fixture working slice subject count."
                      Expect.equal report.NodeRefAssertionCount 22 "Expected the fixture working slice node-ref count."
                      Expect.equal report.LiteralAssertionCount 24 "Expected the fixture working slice literal count."
                      Expect.isGreaterThan report.PredicateCounts.Length 0 "Expected predicate counts in the slice summary."
                      Expect.equal report.PredicateCounts.Head.Predicate "has_node_kind" "Expected the most common fixture predicate to be has_node_kind."
                      Expect.equal report.PredicateCounts.Head.Count 8 "Expected the top predicate count for the fixture slice."))

              testCase "CLI report-working-graph-slice shows SQLite working-index details" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-index-cli" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let importResult = ImportWorkflow.run request

                      let cliResult =
                          TestHelpers.runCli
                              [ "report-working-graph-slice"
                                "--event-store-root"
                                eventStoreRoot
                                "--import-id"
                                (ImportId.format importResult.ImportId)
                                "--limit"
                                "5" ]

                      Expect.equal cliResult.ExitCode 0 "Expected the working-index slice report command to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the working-index slice report command."
                      Expect.stringContains cliResult.StandardOutput "Graph working slice report." "Expected the slice report header."
                      Expect.stringContains cliResult.StandardOutput "Index: graph/working/index/graph-working.sqlite" "Expected the SQLite index path in the report."
                      Expect.stringContains cliResult.StandardOutput "Provider: claude" "Expected provider enrichment in the report."
                      Expect.stringContains cliResult.StandardOutput "Graph assertions: 46" "Expected the graph assertion count in the report."
                      Expect.stringContains cliResult.StandardOutput "has_node_kind: 8" "Expected predicate counts in the report.")) ]
