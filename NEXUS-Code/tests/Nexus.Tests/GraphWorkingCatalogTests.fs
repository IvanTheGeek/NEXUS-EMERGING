namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module GraphWorkingCatalogTests =
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
            "graph working catalog"
            [ testCase "Graph working catalog report summarizes import batches" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-catalog" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let importResult = ImportWorkflow.run request

                      let report = GraphWorkingCatalog.buildReport eventStoreRoot 10

                      Expect.equal report.WorkingBatchCount 1 "Expected one graph working batch after the first import."
                      Expect.equal report.TotalCanonicalEvents 7 "Expected the canonical event count from the fixture import."
                      Expect.equal report.TotalGraphAssertions 46 "Expected the graph assertion count from the fixture import."
                      Expect.equal report.ProviderCounts [ "claude", 1 ] "Expected the provider count to flow through from the import manifest."

                      match report.Items with
                      | [ item ] ->
                          Expect.equal item.ImportId importResult.ImportId "Expected the graph working report to track the imported batch."
                          Expect.equal item.Provider (Some "claude") "Expected provider enrichment from the canonical import manifest."
                          Expect.equal item.Window (Some "full") "Expected window enrichment from the canonical import manifest."
                          Expect.equal item.CanonicalEventCount 7 "Expected the canonical event count in the report item."
                          Expect.equal item.GraphAssertionCount 46 "Expected the graph assertion count in the report item."
                      | [] ->
                          failtest "Expected one graph working report item."
                      | _ ->
                          failtest "Expected exactly one graph working report item."))

              testCase "CLI report-working-graph-imports shows the working catalog summary" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-catalog-cli" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let importResult = ImportWorkflow.run request

                      let cliResult =
                          TestHelpers.runCli
                              [ "report-working-graph-imports"
                                "--event-store-root"
                                eventStoreRoot
                                "--limit"
                                "5" ]

                      Expect.equal cliResult.ExitCode 0 "Expected the working-graph report command to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the working-graph report command."
                      Expect.stringContains cliResult.StandardOutput "Graph working imports report." "Expected the report header."
                      Expect.stringContains cliResult.StandardOutput "Working batches: 1" "Expected the working batch count."
                      Expect.stringContains cliResult.StandardOutput "Total canonical events: 7" "Expected the canonical event total."
                      Expect.stringContains cliResult.StandardOutput "Total graph assertions: 46" "Expected the graph assertion total."
                      Expect.stringContains cliResult.StandardOutput (ImportId.format importResult.ImportId) "Expected the import ID in the detailed output."
                      Expect.stringContains cliResult.StandardOutput "provider=claude" "Expected provider enrichment in the detailed output.")) ]
