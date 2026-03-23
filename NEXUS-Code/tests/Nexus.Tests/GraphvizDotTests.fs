namespace Nexus.Tests

open Expecto
open System.IO
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module GraphvizDotTests =
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
            "graphviz dot"
            [ testCase "DOT export is deterministic and reflects derived graph assertions" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graphviz-dot" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let _ = ImportWorkflow.run request
                      let _ = GraphAssertions.rebuild eventStoreRoot

                      let firstResult = GraphvizDot.export eventStoreRoot None
                      let firstDot = File.ReadAllText(firstResult.OutputPath)
                      let secondResult = GraphvizDot.export eventStoreRoot None
                      let secondDot = File.ReadAllText(secondResult.OutputPath)

                      Expect.isTrue (File.Exists(firstResult.OutputPath)) "Expected the DOT export file to exist."
                      Expect.equal secondResult firstResult "Expected repeat DOT exports to return the same counts and output path."
                      Expect.equal secondDot firstDot "Expected repeat DOT exports to be deterministic."
                      Expect.isGreaterThan firstResult.AssertionCount 0 "Expected graph assertions to be read."
                      Expect.isGreaterThan firstResult.NodeCount 0 "Expected at least one node in the DOT export."
                      Expect.isGreaterThan firstResult.EdgeCount 0 "Expected at least one edge in the DOT export."
                      Expect.stringContains firstDot "digraph nexus_graph" "Expected a DOT graph header."
                      Expect.stringContains firstDot "belongs_to_conversation" "Expected message-to-conversation edges in the DOT export."
                      Expect.stringContains firstDot "references_artifact" "Expected message-to-artifact edges in the DOT export."
                      Expect.stringContains firstDot "semantic: imprint" "Expected semantic role annotations in the node labels."
                      Expect.stringContains firstDot "message: assistant" "Expected message role annotations in the node labels.")) ]
