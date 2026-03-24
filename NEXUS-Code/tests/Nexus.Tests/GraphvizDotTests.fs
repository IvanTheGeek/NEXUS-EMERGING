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

    let private buildMixedGraphStore tempRoot =
        let objectsRoot = Path.Combine(tempRoot, "objects")
        let eventStoreRoot = Path.Combine(tempRoot, "event-store")

        let claudeZipPath = Path.Combine(tempRoot, "claude-fixture.zip")
        TestHelpers.createZipFromFixture "provider-export/claude" claudeZipPath

        let _ =
            ImportWorkflow.run
                { Provider = Claude
                  SourceZipPath = claudeZipPath
                  Window = Some Full
                  ObjectsRoot = objectsRoot
                  EventStoreRoot = eventStoreRoot }

        let codexSnapshotRoot = Path.Combine(objectsRoot, "providers", "codex", "latest")
        TestHelpers.copyFixtureDirectory "codex/latest" codexSnapshotRoot

        let _ =
            CodexImportWorkflow.run
                { SnapshotRoot = codexSnapshotRoot
                  ObjectsRoot = objectsRoot
                  EventStoreRoot = eventStoreRoot }

        let _ = GraphAssertions.rebuild eventStoreRoot
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
                      Expect.stringContains firstDot "message: assistant" "Expected message role annotations in the node labels."))

              testCase "Provider slice export reduces the graph to a practical subgraph" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graphviz-dot-slice" (fun tempRoot ->
                      let eventStoreRoot = buildMixedGraphStore tempRoot
                      let outputPath = Path.Combine(tempRoot, "codex-slice.dot")

                      let fullResult = GraphvizDot.export eventStoreRoot None

                      let sliceResult =
                          GraphvizDot.exportFiltered
                              eventStoreRoot
                              (Some outputPath)
                              { GraphvizDot.ExportFilter.empty with
                                  Provider = Some "codex" }

                      let sliceDot = File.ReadAllText(sliceResult.OutputPath)

                      Expect.isTrue (File.Exists(sliceResult.OutputPath)) "Expected the provider slice DOT file to exist."
                      Expect.equal sliceResult.ScannedAssertionCount fullResult.ScannedAssertionCount "Expected the slice to scan the same assertion corpus."
                      Expect.isLessThan sliceResult.AssertionCount fullResult.AssertionCount "Expected the provider slice to include fewer assertions than the full graph."
                      Expect.isLessThan sliceResult.NodeCount fullResult.NodeCount "Expected the provider slice to include fewer nodes than the full graph."
                      Expect.stringContains sliceDot "Codex Fixture Session" "Expected the Codex conversation to appear in the provider slice."
                      Expect.isFalse
                          (sliceDot.Contains("Mermaid sequence diagram for chat", System.StringComparison.Ordinal))
                          "Did not expect the Claude conversation title in the Codex slice."))

              testCase "Canonical conversation slice keeps a focused local neighborhood" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graphviz-conversation-slice" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let _ = ImportWorkflow.run request
                      let _ = GraphAssertions.rebuild eventStoreRoot

                      let projectionPaths = ConversationProjections.rebuild eventStoreRoot
                      let projectionPath = Path.Combine(eventStoreRoot, projectionPaths.Head)
                      let projection = TestHelpers.readToml projectionPath

                      let conversationId =
                          projection
                          |> TomlDocument.tryScalar "conversation_id"
                          |> Option.defaultWith (fun () -> failwith "Expected conversation_id in projection.")

                      let conversationTitle =
                          projection
                          |> TomlDocument.tryScalar "title"
                          |> Option.defaultWith (fun () -> failwith "Expected title in projection.")

                      let fullResult = GraphvizDot.export eventStoreRoot None

                      let sliceResult =
                          GraphvizDot.exportFiltered
                              eventStoreRoot
                              None
                              { GraphvizDot.ExportFilter.empty with
                                  ConversationId = Some conversationId }

                      let sliceDot = File.ReadAllText(sliceResult.OutputPath)

                      Expect.isLessThan sliceResult.AssertionCount fullResult.AssertionCount "Expected a conversation slice to include fewer assertions than the full graph."
                      Expect.isLessThan sliceResult.NodeCount fullResult.NodeCount "Expected a conversation slice to include fewer nodes than the full graph."
                      Expect.stringContains sliceDot conversationTitle "Expected the selected conversation title in the slice."
                      Expect.stringContains sliceDot "references_artifact" "Expected local relationship edges around the conversation."
                      Expect.stringContains sliceDot "semantic: imprint" "Expected node metadata for the local neighborhood."))

              testCase "Working import slice export works without a durable graph rebuild" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graphviz-working-import" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let importResult = ImportWorkflow.run request
                      let outputPath = Path.Combine(tempRoot, "working-import.dot")

                      let moduleResult =
                          GraphvizDot.exportWorkingImportBatch
                              eventStoreRoot
                              (ImportId.format importResult.ImportId)
                              (Some outputPath)

                      let dotText = File.ReadAllText(moduleResult.OutputPath)

                      Expect.isTrue (File.Exists(moduleResult.OutputPath)) "Expected the working-slice DOT file to exist."
                      Expect.equal moduleResult.ScannedAssertionCount 46 "Expected the working slice to read only its own assertion batch."
                      Expect.equal moduleResult.AssertionCount 46 "Expected the exported working slice assertion count."
                      Expect.stringContains dotText "Claude Fixture Conversation" "Expected the working import graph to include the imported conversation title."

                      let cliResult =
                          TestHelpers.runCli
                              [ "export-graphviz-dot"
                                "--event-store-root"
                                eventStoreRoot
                                "--working-import-id"
                                (ImportId.format importResult.ImportId)
                                "--output"
                                (Path.Combine(tempRoot, "working-import-cli.dot")) ]

                      Expect.equal cliResult.ExitCode 0 "Expected working-import Graphviz export through the CLI to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the working-import Graphviz export."
                      Expect.stringContains cliResult.StandardOutput "Source: graph working slice" "Expected the CLI to report the working-slice source kind."))

              testCase "DOT export can target an output root while keeping the generated file name" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graphviz-output-root" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let _ = ImportWorkflow.run request
                      let _ = GraphAssertions.rebuild eventStoreRoot
                      let outputRoot = Path.Combine(tempRoot, "exports")

                      let cliResult =
                          TestHelpers.runCli
                              [ "export-graphviz-dot"
                                "--event-store-root"
                                eventStoreRoot
                                "--provider"
                                "claude"
                                "--output-root"
                                outputRoot ]

                      Expect.equal cliResult.ExitCode 0 "Expected DOT export with --output-root to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from DOT export with --output-root."

                      let expectedPath = Path.Combine(outputRoot, "nexus-graph__provider-claude.dot")
                      Expect.isTrue (File.Exists(expectedPath)) "Expected the generated DOT file under the requested output root."
                      Expect.stringContains cliResult.StandardOutput expectedPath "Expected the CLI summary to report the output-root path."))

              testCase "Rendered Graphviz output can be produced from an exported DOT file" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graphviz-render" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let _ = ImportWorkflow.run request
                      let _ = GraphAssertions.rebuild eventStoreRoot

                      let dotResult =
                          GraphvizDot.export
                              eventStoreRoot
                              (Some (Path.Combine(tempRoot, "fixture-graph.dot")))

                      let outputPath = Path.Combine(tempRoot, "fixture-graph.svg")

                      let cliResult =
                          TestHelpers.runCli
                              [ "render-graphviz-dot"
                                "--input"
                                dotResult.OutputPath
                                "--output"
                                outputPath
                                "--engine"
                                "dot"
                                "--format"
                                "svg" ]

                      Expect.equal cliResult.ExitCode 0 "Expected Graphviz render through the CLI to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the Graphviz render command."
                      Expect.isTrue (File.Exists(outputPath)) "Expected the rendered SVG file to exist."

                      let svgText = File.ReadAllText(outputPath)
                      Expect.stringContains cliResult.StandardOutput "Graphviz DOT rendered." "Expected the render summary header."
                      Expect.stringContains cliResult.StandardOutput "Engine: dot" "Expected the render engine in the summary."
                      Expect.stringContains cliResult.StandardOutput "Format: svg" "Expected the render format in the summary."
                      Expect.stringContains svgText "<svg" "Expected SVG output from the render command."))

              testCase "Rendered Graphviz output can target an output root while keeping the generated file name" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graphviz-render-output-root" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let _ = ImportWorkflow.run request
                      let _ = GraphAssertions.rebuild eventStoreRoot

                      let dotResult =
                          GraphvizDot.export
                              eventStoreRoot
                              (Some (Path.Combine(tempRoot, "fixture-graph.dot")))

                      let outputRoot = Path.Combine(tempRoot, "rendered")

                      let cliResult =
                          TestHelpers.runCli
                              [ "render-graphviz-dot"
                                "--input"
                                dotResult.OutputPath
                                "--output-root"
                                outputRoot
                                "--format"
                                "svg" ]

                      let expectedPath = Path.Combine(outputRoot, "fixture-graph.svg")
                      Expect.equal cliResult.ExitCode 0 "Expected render with --output-root to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from render with --output-root."
                      Expect.isTrue (File.Exists(expectedPath)) "Expected the rendered file under the requested output root."
                      Expect.stringContains cliResult.StandardOutput expectedPath "Expected the CLI summary to report the output-root render path.")) ]
