namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.EventStore

[<RequireQualifiedAccess>]
module ConversationOverlapTests =
    let private writeProjection
        eventStoreRoot
        conversationId
        title
        messageCount
        firstOccurredAt
        lastOccurredAt
        providers
        =
        let projectionsRoot = Path.Combine(eventStoreRoot, "projections", "conversations")
        Directory.CreateDirectory(projectionsRoot) |> ignore

        let path = Path.Combine(projectionsRoot, $"{conversationId}.toml")

        File.WriteAllText(
            path,
            $"""schema_version = 1
projection_kind = "conversation_projection"
conversation_id = "{conversationId}"
title = "{title}"
message_count = {messageCount}
artifact_reference_count = 0
revision_count = 0
first_occurred_at = "{firstOccurredAt}"
last_occurred_at = "{lastOccurredAt}"
providers = ["{String.concat "\", \"" providers}"]
provider_conversation_ids = ["{conversationId}-native"]
import_ids = ["import-{conversationId}"]
"""
        )

    let tests =
        testList
            "conversation overlap"
            [ testCase "report finds conservative overlap candidates from projections" (fun () ->
                  TestHelpers.withTempDirectory "nexus-overlap-report" (fun tempRoot ->
                      let eventStoreRoot = Path.Combine(tempRoot, "event-store")

                      writeProjection
                          eventStoreRoot
                          "codex-1"
                          "Setup Codex for Access"
                          12
                          "2026-03-24T10:00:00Z"
                          "2026-03-24T11:00:00Z"
                          [ "codex" ]

                      writeProjection
                          eventStoreRoot
                          "chatgpt-1"
                          "Setup Codex for Access"
                          12
                          "2026-03-24T10:30:00Z"
                          "2026-03-24T11:30:00Z"
                          [ "chatgpt" ]

                      writeProjection
                          eventStoreRoot
                          "chatgpt-2"
                          "Retaining Wall Material Advice"
                          9
                          "2026-03-20T10:30:00Z"
                          "2026-03-20T11:30:00Z"
                          [ "chatgpt" ]

                      let report = ConversationOverlap.buildReport eventStoreRoot "codex" "chatgpt" 20

                      Expect.equal report.LeftConversationCount 1 "Expected one Codex conversation in scope."
                      Expect.equal report.RightConversationCount 2 "Expected two ChatGPT conversations in scope."
                      Expect.equal report.CandidateCount 1 "Expected one conservative overlap candidate."

                      let candidate = report.Candidates |> List.exactlyOne

                      Expect.equal candidate.LeftConversationId "codex-1" "Expected the Codex conversation in the candidate."
                      Expect.equal candidate.RightConversationId "chatgpt-1" "Expected the matching ChatGPT conversation in the candidate."
                      Expect.stringContains (String.concat ", " candidate.Signals) "exact_title_slug" "Expected the exact-title signal."
                      Expect.stringContains (String.concat ", " candidate.Signals) "time_window_overlap" "Expected the time-overlap signal.") )

              testCase "CLI report prints overlap candidates and guidance" (fun () ->
                  TestHelpers.withTempDirectory "nexus-overlap-cli" (fun tempRoot ->
                      let eventStoreRoot = Path.Combine(tempRoot, "event-store")

                      writeProjection
                          eventStoreRoot
                          "codex-1"
                          "Setup Codex for Access"
                          12
                          "2026-03-24T10:00:00Z"
                          "2026-03-24T11:00:00Z"
                          [ "codex" ]

                      writeProjection
                          eventStoreRoot
                          "chatgpt-1"
                          "Setup Codex for Access"
                          12
                          "2026-03-24T10:30:00Z"
                          "2026-03-24T11:30:00Z"
                          [ "chatgpt" ]

                      let result =
                          TestHelpers.runCli
                              [ "report-conversation-overlap-candidates"
                                "--event-store-root"
                                eventStoreRoot
                                "--left-provider"
                                "codex"
                                "--right-provider"
                                "chatgpt" ]

                      Expect.equal result.ExitCode 0 "Expected the overlap report to succeed."
                      Expect.equal result.StandardError "" "Did not expect stderr from the overlap report."
                      Expect.stringContains result.StandardOutput "Conversation overlap candidates." "Expected the report header."
                      Expect.stringContains result.StandardOutput "Left provider: codex" "Expected the left-provider line."
                      Expect.stringContains result.StandardOutput "Right provider: chatgpt" "Expected the right-provider line."
                      Expect.stringContains result.StandardOutput "exact_title_slug" "Expected the exact-title signal in the report."
                      Expect.stringContains result.StandardOutput "These are heuristic candidates only." "Expected the reconciliation caution." )) ]
