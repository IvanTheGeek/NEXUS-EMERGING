namespace Nexus.Tests

open System
open System.IO
open Expecto
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module GraphWorkingIndexTests =
    let private buildClaudeImportRequest (fixtureRelativePath: string) (tempRoot: string) =
        let objectsRoot = Path.Combine(tempRoot, "objects")
        let eventStoreRoot = Path.Combine(tempRoot, "event-store")
        let zipPath = Path.Combine(tempRoot, $"{fixtureRelativePath.Replace('/', '-')}.zip")

        TestHelpers.createZipFromFixture fixtureRelativePath zipPath

        { Provider = Claude
          SourceZipPath = zipPath
          Window = Some Full
          ObjectsRoot = objectsRoot
          EventStoreRoot = eventStoreRoot },
        eventStoreRoot

    let private buildClaudeFixtureImportRequest tempRoot =
        buildClaudeImportRequest "provider-export/claude" tempRoot

    let private buildClaudeFollowOnImportRequest tempRoot =
        buildClaudeImportRequest "provider-export/claude-follow-on" tempRoot

    let tests =
        testList
            "graph working index"
            [ testCase "Graph working SQLite index summarizes one imported slice" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-index" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
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

              testCase "Graph working SQLite index can summarize conversations inside one import slice" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-conversations" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let importResult = ImportWorkflow.run request

                      let report =
                          GraphWorkingIndex.tryBuildImportConversationReport eventStoreRoot importResult.ImportId 10
                          |> Option.defaultWith (fun () -> failwith "Expected a working-import conversation report for the imported slice.")

                      Expect.equal report.ImportId importResult.ImportId "Expected the report to target the imported slice."
                      Expect.equal report.Provider (Some "claude") "Expected provider enrichment in the conversation report."
                      Expect.equal report.Window (Some "full") "Expected window enrichment in the conversation report."
                      Expect.equal report.ConversationCount 1 "Expected one conversation node in the fixture slice."
                      Expect.equal report.Items.Length 1 "Expected one conversation summary row."
                      Expect.equal report.Items.Head.Title (Some "Claude Fixture Conversation") "Expected the fixture conversation title."
                      Expect.equal report.Items.Head.MessageCount 2 "Expected two fixture messages in the conversation."
                      Expect.equal report.Items.Head.ArtifactCount 1 "Expected one referenced artifact in the conversation."))

              testCase "Graph working SQLite index can find nodes by title text and provider filter" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-index-find" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let _ = ImportWorkflow.run request

                      let matches =
                          GraphWorkingIndex.findNodes
                              eventStoreRoot
                              None
                              (Some "claude")
                              (Some "fixture")
                              None
                              None
                              10

                      Expect.equal matches.Length 1 "Expected one fixture conversation node match."
                      Expect.equal matches.Head.Provider (Some "claude") "Expected provider enrichment on the node match."
                      Expect.equal matches.Head.Title (Some "Claude Fixture Conversation") "Expected the fixture conversation title."
                      Expect.equal matches.Head.NodeKind (Some "conversation_node") "Expected the conversation node kind."
                      Expect.contains matches.Head.MatchReasons "title_or_slug" "Expected a title/slug match reason."))

              testCase "Graph working SQLite index can report a node neighborhood inside one import slice" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-index-neighborhood" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let importResult = ImportWorkflow.run request

                      let nodeMatch =
                          GraphWorkingIndex.findNodes
                              eventStoreRoot
                              (Some importResult.ImportId)
                              (Some "claude")
                              (Some "fixture")
                              None
                              None
                              10
                          |> List.head

                      let report =
                          GraphWorkingIndex.tryBuildNeighborhoodReport eventStoreRoot importResult.ImportId nodeMatch.NodeId 10
                          |> Option.defaultWith (fun () -> failwith "Expected a neighborhood report for the fixture conversation node.")

                      Expect.equal report.Title (Some "Claude Fixture Conversation") "Expected the neighborhood to target the fixture conversation."
                      Expect.isGreaterThan report.TotalLiteralAssertionCount 0 "Expected literal assertions for the node."
                      Expect.isGreaterThan report.TotalOutgoingConnectionCount 0 "Expected outgoing graph connections for the node."
                      Expect.isGreaterThan report.TotalIncomingConnectionCount 0 "Expected incoming graph connections for the node."
                      Expect.isTrue
                          (report.OutgoingConnections |> List.exists (fun item -> item.Predicate = "located_in_domain"))
                          "Expected the conversation to connect to its domain."
                      Expect.isTrue
                          (report.IncomingConnections |> List.exists (fun item -> item.Predicate = "belongs_to_conversation"))
                          "Expected messages to point into the conversation neighborhood."))

              testCase "CLI report-working-graph-batch shows SQLite working-index details" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-index-cli" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let importResult = ImportWorkflow.run request

                      let cliResult =
                          TestHelpers.runCli
                              [ "report-working-graph-batch"
                                "--event-store-root"
                                eventStoreRoot
                                "--import-id"
                                (ImportId.format importResult.ImportId)
                                "--limit"
                                "5" ]

                      Expect.equal cliResult.ExitCode 0 "Expected the working-index batch report command to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the working-index batch report command."
                      Expect.stringContains cliResult.StandardOutput "Graph working batch report." "Expected the batch report header."
                      Expect.stringContains cliResult.StandardOutput "Index: graph/working/index/graph-working.sqlite" "Expected the SQLite index path in the report."
                      Expect.stringContains cliResult.StandardOutput "Provider: claude" "Expected provider enrichment in the report."
                      Expect.stringContains cliResult.StandardOutput "Graph assertions: 46" "Expected the graph assertion count in the report."
                      Expect.stringContains cliResult.StandardOutput "has_node_kind: 8" "Expected predicate counts in the report."))

              testCase "CLI report-working-import-conversations shows conversation summaries" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-conversations-cli" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let importResult = ImportWorkflow.run request

                      let cliResult =
                          TestHelpers.runCli
                              [ "report-working-import-conversations"
                                "--event-store-root"
                                eventStoreRoot
                                "--import-id"
                                (ImportId.format importResult.ImportId)
                                "--limit"
                                "5" ]

                      Expect.equal cliResult.ExitCode 0 "Expected the working-import conversation report command to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the working-import conversation report command."
                      Expect.stringContains cliResult.StandardOutput "Working import conversations report." "Expected the conversation report header."
                      Expect.stringContains cliResult.StandardOutput "Conversations in batch: 1" "Expected the conversation count in the report."
                      Expect.stringContains cliResult.StandardOutput "Claude Fixture Conversation" "Expected the fixture conversation title in the report."
                      Expect.stringContains cliResult.StandardOutput "messages=2 artifacts=1" "Expected the message and artifact counts in the report."))

              testCase "Graph working SQLite index can compare conversation contributions between two imports" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-conversation-compare" (fun tempRoot ->
                      let baseRequest, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let baseResult = ImportWorkflow.run baseRequest

                      let followOnRequest, _ = buildClaudeFollowOnImportRequest tempRoot
                      let currentResult = ImportWorkflow.run followOnRequest

                      let report =
                          GraphWorkingIndex.tryBuildImportConversationComparisonReport
                              eventStoreRoot
                              baseResult.ImportId
                              currentResult.ImportId
                              10
                          |> Option.defaultWith (fun () -> failwith "Expected a working-import conversation comparison report.")

                      Expect.equal report.BaseImportId baseResult.ImportId "Expected the base import ID in the comparison report."
                      Expect.equal report.CurrentImportId currentResult.ImportId "Expected the current import ID in the comparison report."
                      Expect.equal report.AddedConversationCount 1 "Expected one newly contributed conversation in the follow-on slice."
                      Expect.equal report.RemovedConversationCount 0 "Expected no removed conversations in this fixture progression."
                      Expect.equal report.ChangedConversationCount 1 "Expected the existing fixture conversation contribution to differ."
                      Expect.equal report.UnchangedConversationCount 0 "Expected no unchanged shared conversations in the fixture progression."
                      Expect.equal report.AddedConversations.Length 1 "Expected one detailed added conversation row."
                      Expect.equal report.RemovedConversations.Length 0 "Expected no removed conversation rows."
                      Expect.equal report.ChangedConversations.Length 1 "Expected one changed conversation row."
                      Expect.equal report.AddedConversations.Head.Title (Some "Claude Follow-on Conversation") "Expected the follow-on conversation title."
                      Expect.equal report.AddedConversations.Head.MessageCount 1 "Expected the new follow-on conversation contribution count."
                      Expect.equal report.ChangedConversations.Head.BaseTitle (Some "Claude Fixture Conversation") "Expected the shared fixture conversation label."
                      Expect.equal report.ChangedConversations.Head.BaseMessageCount 2 "Expected the base conversation contribution to include two messages."
                      Expect.equal report.ChangedConversations.Head.CurrentMessageCount 1 "Expected the follow-on slice to contribute one new message."
                      Expect.equal report.ChangedConversations.Head.BaseArtifactCount 1 "Expected one artifact in the base slice contribution."
                      Expect.equal report.ChangedConversations.Head.CurrentArtifactCount 0 "Expected no new artifact references in the follow-on slice contribution."))

              testCase "CLI find-working-graph-nodes shows indexed matches" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-index-find-cli" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let _ = ImportWorkflow.run request

                      let cliResult =
                          TestHelpers.runCli
                              [ "find-working-graph-nodes"
                                "--event-store-root"
                                eventStoreRoot
                                "--provider"
                                "claude"
                                "--match"
                                "fixture" ]

                      Expect.equal cliResult.ExitCode 0 "Expected node search to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the node search."
                      Expect.stringContains cliResult.StandardOutput "Graph working node search." "Expected the search header."
                      Expect.stringContains cliResult.StandardOutput "Matches: 1" "Expected one fixture node match."
                      Expect.stringContains cliResult.StandardOutput "Claude Fixture Conversation" "Expected the fixture conversation title in the search output."
                      Expect.stringContains cliResult.StandardOutput "matched_on=title_or_slug" "Expected the match reason in the search output."))

              testCase "CLI compare-working-import-conversations shows added and changed batch-local contributions" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-conversation-compare-cli" (fun tempRoot ->
                      let baseRequest, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let baseResult = ImportWorkflow.run baseRequest

                      let followOnRequest, _ = buildClaudeFollowOnImportRequest tempRoot
                      let currentResult = ImportWorkflow.run followOnRequest

                      let cliResult =
                          TestHelpers.runCli
                              [ "compare-working-import-conversations"
                                "--event-store-root"
                                eventStoreRoot
                                "--base-import-id"
                                (ImportId.format baseResult.ImportId)
                                "--current-import-id"
                                (ImportId.format currentResult.ImportId)
                                "--limit"
                                "10" ]

                      Expect.equal cliResult.ExitCode 0 "Expected the conversation comparison command to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the conversation comparison command."
                      Expect.stringContains cliResult.StandardOutput "Working import conversation comparison report." "Expected the comparison header."
                      Expect.stringContains cliResult.StandardOutput "Added conversations: 1" "Expected the added count."
                      Expect.stringContains cliResult.StandardOutput "Changed conversations: 1" "Expected the changed count."
                      Expect.stringContains cliResult.StandardOutput "Claude Follow-on Conversation" "Expected the added conversation label."
                      Expect.stringContains cliResult.StandardOutput "messages=2 -> 1" "Expected the changed message contribution counts."
                      Expect.stringContains cliResult.StandardOutput "artifacts=1 -> 0" "Expected the changed artifact contribution counts."))

              testCase "CLI report-working-graph-neighborhood shows local graph structure" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-index-neighborhood-cli" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let importResult = ImportWorkflow.run request

                      let nodeMatch =
                          GraphWorkingIndex.findNodes
                              eventStoreRoot
                              (Some importResult.ImportId)
                              (Some "claude")
                              (Some "fixture")
                              None
                              None
                              10
                          |> List.head

                      let cliResult =
                          TestHelpers.runCli
                              [ "report-working-graph-neighborhood"
                                "--event-store-root"
                                eventStoreRoot
                                "--import-id"
                                (ImportId.format importResult.ImportId)
                                "--node-id"
                                nodeMatch.NodeId
                                "--limit"
                                "10" ]

                      Expect.equal cliResult.ExitCode 0 "Expected the neighborhood report to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the neighborhood report."
                      Expect.stringContains cliResult.StandardOutput "Graph working neighborhood report." "Expected the neighborhood header."
                      Expect.stringContains cliResult.StandardOutput "Title: Claude Fixture Conversation" "Expected the node title in the neighborhood output."
                      Expect.stringContains cliResult.StandardOutput "Outgoing:" "Expected outgoing neighborhood rows."
                      Expect.stringContains cliResult.StandardOutput "Incoming:" "Expected incoming neighborhood rows."
                      Expect.stringContains cliResult.StandardOutput "belongs_to_conversation" "Expected relationship detail in the neighborhood output."))

              testCase "CLI rebuild-working-graph-index recreates the SQLite index from working batches" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-index-rebuild" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let importResult = ImportWorkflow.run request

                      let indexPath =
                          importResult.WorkingGraphIndexRelativePath
                          |> Option.map (fun relativePath -> Path.Combine(eventStoreRoot, relativePath))
                          |> Option.defaultWith (fun () -> failwith "Missing working graph SQLite index path.")

                      File.Delete(indexPath)
                      Expect.isFalse (File.Exists(indexPath)) "Expected the SQLite graph working index to be deleted before rebuild."

                      let cliResult =
                          TestHelpers.runCli
                              [ "rebuild-working-graph-index"
                                "--event-store-root"
                                eventStoreRoot ]

                      Expect.equal cliResult.ExitCode 0 "Expected the working-index rebuild command to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the working-index rebuild command."
                      Expect.isTrue (File.Exists(indexPath)) "Expected the SQLite graph working index to be recreated."
                      Expect.stringContains cliResult.StandardOutput "Graph working SQLite index rebuilt." "Expected the rebuild header."
                      Expect.stringContains cliResult.StandardOutput "Working batches indexed: 1" "Expected the indexed batch count."
                      Expect.stringContains cliResult.StandardOutput "Graph assertions indexed: 46" "Expected the indexed assertion count."

                      let report =
                          GraphWorkingIndex.tryBuildImportSliceReport eventStoreRoot importResult.ImportId 5
                          |> Option.defaultWith (fun () -> failwith "Expected the rebuilt SQLite index to answer slice queries.")

                      Expect.equal report.GraphAssertionCount 46 "Expected the rebuilt SQLite index to restore the slice."))

              testCase "CLI verify-working-graph-batch succeeds for a clean fixture import" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-verify-clean" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let objectsRoot = Path.Combine(tempRoot, "objects")
                      let importResult = ImportWorkflow.run request

                      let cliResult =
                          TestHelpers.runCli
                              [ "verify-working-graph-batch"
                                "--event-store-root"
                                eventStoreRoot
                                "--objects-root"
                                objectsRoot
                                "--import-id"
                                (ImportId.format importResult.ImportId) ]

                      Expect.equal cliResult.ExitCode 0 "Expected graph working batch verification to succeed for the clean fixture import."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the clean verification command."
                      Expect.stringContains cliResult.StandardOutput "Graph working batch verification." "Expected the verification header."
                      Expect.stringContains cliResult.StandardOutput "Missing canonical event refs: 0" "Expected no missing canonical events."
                      Expect.stringContains cliResult.StandardOutput "Missing raw object refs: 0" "Expected no missing raw objects."))

              testCase "CLI verify-working-graph-batch fails when a canonical event is missing" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-working-verify-broken" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeFixtureImportRequest tempRoot
                      let objectsRoot = Path.Combine(tempRoot, "objects")
                      let importResult = ImportWorkflow.run request

                      let eventPath =
                          importResult.EventPaths
                          |> List.find (fun path -> path.Contains("__provider-message-observed.toml", StringComparison.Ordinal))
                          |> fun relativePath -> Path.Combine(eventStoreRoot, relativePath)

                      File.Delete(eventPath)

                      let cliResult =
                          TestHelpers.runCli
                              [ "verify-working-graph-batch"
                                "--event-store-root"
                                eventStoreRoot
                                "--objects-root"
                                objectsRoot
                                "--import-id"
                                (ImportId.format importResult.ImportId) ]

                      Expect.equal cliResult.ExitCode 2 "Expected verification failure when a canonical event file is missing."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the broken verification command."
                      Expect.stringContains cliResult.StandardOutput "Missing canonical event refs:" "Expected the missing canonical event summary."
                      Expect.stringContains cliResult.StandardOutput "referenced by" "Expected missing-canonical detail output.")) ]
