namespace Nexus.Tests

open Expecto
open System.IO
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module GraphAssertionTests =
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

    let private tryFindAssertion predicate subjectNodeId objectValue assertionDocuments =
        assertionDocuments
        |> List.tryFind (fun (_, document) ->
            TomlDocument.tryScalar "predicate" document = Some predicate
            && TomlDocument.tryScalar "subject_node_id" document = Some subjectNodeId
            && TomlDocument.tryTableValue "object" "value" document = objectValue)

    let private tryFindNodeRefAssertion predicate subjectNodeId objectNodeId assertionDocuments =
        assertionDocuments
        |> List.tryFind (fun (_, document) ->
            TomlDocument.tryScalar "predicate" document = Some predicate
            && TomlDocument.tryScalar "subject_node_id" document = Some subjectNodeId
            && TomlDocument.tryTableValue "object" "kind" document = Some "node_ref"
            && TomlDocument.tryTableValue "object" "node_id" document = Some objectNodeId)

    let tests =
        testList
            "graph assertions"
            [ testCase "Graph assertions rebuild deterministically from canonical history" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-assertions" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let _ = ImportWorkflow.run request

                      let firstPaths = GraphAssertions.rebuild eventStoreRoot
                      let secondPaths = GraphAssertions.rebuild eventStoreRoot

                      Expect.isGreaterThan firstPaths.Length 0 "Expected graph assertions to be written."
                      Expect.equal secondPaths firstPaths "Expected graph assertion rebuilds to be deterministic."

                      let conversationDocuments =
                          TestHelpers.tomlDocumentsUnder eventStoreRoot (Path.Combine("events", "conversations"))

                      let artifactDocuments =
                          TestHelpers.tomlDocumentsUnder eventStoreRoot (Path.Combine("events", "artifacts"))

                      let assertionDocuments =
                          TestHelpers.tomlDocumentsUnder eventStoreRoot (Path.Combine("graph", "assertions"))

                      let assistantMessageDocument =
                          conversationDocuments
                          |> List.map snd
                          |> List.find (fun document ->
                              TomlDocument.tryScalar "event_kind" document = Some "provider_message_observed"
                              && TomlDocument.tryTableValue "body" "role" document = Some "assistant")

                      let messageId =
                          assistantMessageDocument
                          |> TomlDocument.tryScalar "message_id"
                          |> Option.defaultWith (fun () -> failwith "Expected assistant message_id.")

                      let conversationId =
                          assistantMessageDocument
                          |> TomlDocument.tryScalar "conversation_id"
                          |> Option.defaultWith (fun () -> failwith "Expected assistant conversation_id.")

                      let artifactDocument =
                          artifactDocuments
                          |> List.map snd
                          |> List.find (fun document -> TomlDocument.tryScalar "event_kind" document = Some "artifact_referenced")

                      let artifactId =
                          artifactDocument
                          |> TomlDocument.tryScalar "artifact_id"
                          |> Option.defaultWith (fun () -> failwith "Expected artifact_id.")

                      let importCompletedDocument =
                          TestHelpers.tomlDocumentsUnder eventStoreRoot (Path.Combine("events", "imports"))
                          |> List.map snd
                          |> List.find (fun document -> TomlDocument.tryScalar "event_kind" document = Some "import_completed")

                      let importId =
                          importCompletedDocument
                          |> TomlDocument.tryScalar "import_id"
                          |> Option.defaultWith (fun () -> failwith "Expected import_id.")

                      Expect.isSome
                          (tryFindNodeRefAssertion "belongs_to_conversation" messageId conversationId assertionDocuments)
                          "Expected a message-to-conversation assertion."

                      Expect.isSome
                          (tryFindAssertion "has_semantic_role" messageId (Some "imprint") assertionDocuments)
                          "Expected the message node to be annotated with the imprint role."

                      Expect.isSome
                          (tryFindAssertion "has_role" messageId (Some "assistant") assertionDocuments)
                          "Expected an assistant role assertion."

                      Expect.isSome
                          (tryFindNodeRefAssertion "references_artifact" messageId artifactId assertionDocuments)
                          "Expected a message-to-artifact assertion."

                      Expect.isSome
                          (tryFindAssertion "has_semantic_role" artifactId (Some "imprint") assertionDocuments)
                          "Expected the artifact node to be annotated with the imprint role."

                      Expect.isSome
                          (assertionDocuments
                           |> List.tryFind (fun (_, document) ->
                               TomlDocument.tryScalar "predicate" document = Some "has_slug"
                               && TomlDocument.tryTableValue "object" "value" document = Some "ingestion"))
                          "Expected the ingestion domain slug assertion."

                      Expect.isNone
                          (tryFindNodeRefAssertion "observed_during_import" importId importId assertionDocuments)
                          "Did not expect an import node to point to itself through observed_during_import."))

              testCase "Graph assertion rebuild emits progress status" (fun () ->
                  TestHelpers.withTempDirectory "nexus-graph-assertions-status" (fun tempRoot ->
                      let request, eventStoreRoot = buildClaudeImportRequest tempRoot
                      let _ = ImportWorkflow.run request
                      let messages = ResizeArray<string>()

                      let assertionPaths =
                          GraphAssertions.rebuildWithStatus messages.Add eventStoreRoot

                      Expect.isGreaterThan assertionPaths.Length 0 "Expected graph assertion files to be written."
                      Expect.isGreaterThan messages.Count 0 "Expected rebuild status messages."
                      Expect.isTrue
                          (messages |> Seq.exists (fun message -> message.Contains("Scanning", System.StringComparison.Ordinal)))
                          "Expected a scanning status message."
                      Expect.isTrue
                          (messages |> Seq.exists (fun message -> message.Contains("Derivation complete", System.StringComparison.Ordinal)))
                          "Expected a derivation-complete status message."
                      Expect.isTrue
                          (messages |> Seq.exists (fun message -> message.Contains("Writing", System.StringComparison.Ordinal)))
                          "Expected a writing status message."
                      Expect.isTrue
                          (messages |> Seq.exists (fun message -> message.Contains("Graph assertion rebuild completed", System.StringComparison.Ordinal)))
                          "Expected a completion status message."))

              testCase "CLI refuses heavyweight full rebuild without --yes" (fun () ->
                  TestHelpers.withTempDirectory "nexus-heavyweight-rebuild-guard" (fun tempRoot ->
                      let eventStoreRoot = Path.Combine(tempRoot, "event-store")
                      let eventsRoot = Path.Combine(eventStoreRoot, "events", "synthetic")
                      Directory.CreateDirectory(eventsRoot) |> ignore

                      for index in 1 .. 1000 do
                          let path = Path.Combine(eventsRoot, sprintf "synthetic-%04i.toml" index)
                          File.WriteAllText(path, "")

                      let result =
                          TestHelpers.runCli
                              [ "rebuild-graph-assertions"
                                "--event-store-root"
                                eventStoreRoot ]

                      Expect.equal result.ExitCode 2 "Expected heavyweight rebuild refusal without explicit approval."
                      Expect.stringContains result.StandardError "Heavyweight graph rebuild refused without explicit approval." "Expected the refusal message."
                      Expect.stringContains result.StandardError "Re-run with --yes to proceed." "Expected clear remediation guidance.")) ]
