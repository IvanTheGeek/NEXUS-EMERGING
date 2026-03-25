namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.Logos

[<RequireQualifiedAccess>]
module LogosTests =
    let tests =
        testList
            "logos"
            [ testCase "Source system identifiers require allowlisted slugs" (fun () ->
                  Expect.equal (SourceSystemId.value KnownSourceSystems.chatgpt) "chatgpt" "Expected the known source-system slug to stay stable."

                  Expect.throws
                      (fun () -> SourceSystemId.create "ChatGPT" |> ignore)
                      "Expected uppercase characters to be rejected by the allowlist.")

              testCase "LOGOS source references require at least one locator" (fun () ->
                  Expect.throws
                      (fun () ->
                          LogosSourceRef.create KnownSourceSystems.codex CoreIntakeChannels.aiConversation []
                          |> ignore)
                      "Expected LOGOS source references without locators to be rejected.")

              testCase "LOGOS signals normalize blank optional text to None" (fun () ->
                  let source =
                      LogosSourceRef.create
                          KnownSourceSystems.codex
                          CoreIntakeChannels.aiConversation
                          [ LogosLocator.nativeThreadId "rollout-123" ]

                  let signal =
                      LogosSignal.create
                          CoreSignalKinds.conversation
                          source
                          None
                          (Some "   ")
                          (Some "  idea seed  ")

                  Expect.isNone (LogosSignal.title signal) "Expected blank titles to normalize away."
                  Expect.equal (LogosSignal.summary signal) (Some "idea seed") "Expected summaries to trim surrounding whitespace.")

              testCase "Known providers map into LOGOS classifications" (fun () ->
                  let classification = ProviderLogosClassification.tryFind "chatgpt"

                  Expect.isSome classification "Expected ChatGPT to map into the LOGOS source model."

                  let value = classification |> Option.get

                  Expect.equal (SourceSystemId.value value.SourceSystemId) "chatgpt" "Expected the LOGOS source system to stay stable."
                  Expect.equal (IntakeChannelId.value value.IntakeChannelId) "ai-conversation" "Expected AI conversation to be the intake channel."
                  Expect.equal (SignalKindId.value value.PrimarySignalKind) "conversation" "Expected conversation to be the primary signal kind."
                  Expect.equal (value.RelatedSignalKinds |> List.map SignalKindId.value) [ "message" ] "Expected message to stay as the related signal kind.")

              testCase "LOGOS catalog includes concrete non-chat source systems" (fun () ->
                  let report = LogosCatalog.build ()
                  let sourceSystemSlugs = report.SourceSystems |> List.map (fun item -> item.Slug)

                  Expect.isTrue (sourceSystemSlugs |> List.contains "forum") "Expected forum to be an explicit LOGOS source system."
                  Expect.isTrue (sourceSystemSlugs |> List.contains "email") "Expected email to be an explicit LOGOS source system."
                  Expect.isTrue (sourceSystemSlugs |> List.contains "issue-tracker") "Expected issue-tracker to be an explicit LOGOS source system."
                  Expect.isTrue (sourceSystemSlugs |> List.contains "app-feedback-surface") "Expected app-feedback-surface to be an explicit LOGOS source system.")

              testCase "LOGOS intake note seed writes a durable markdown note" (fun () ->
                  TestHelpers.withTempDirectory "nexus-logos-note" (fun tempRoot ->
                      let docsRoot = Path.Combine(tempRoot, "docs")

                      let result =
                          LogosIntakeNotes.create
                              { DocsRoot = docsRoot
                                Slug = "support-thread-123"
                                Title = "Support Thread 123"
                                SourceSystemId = KnownSourceSystems.forum
                                IntakeChannelId = CoreIntakeChannels.forumThread
                                SignalKindId = CoreSignalKinds.supportQuestion
                                Locators = [ LogosLocator.sourceUri "https://community.example.com/t/123" ]
                                CapturedAt = None
                                Summary = Some "Customer cannot complete setup after the latest update."
                                Tags = [ "support"; "seed" ] }

                      match result with
                      | Error error ->
                          failtestf "Expected LOGOS intake note creation to succeed. %s" error
                      | Ok note ->
                          let text = File.ReadAllText(note.OutputPath)

                          Expect.isTrue (File.Exists(note.OutputPath)) "Expected the LOGOS intake note file to exist."
                          Expect.equal note.NormalizedSlug "support-thread-123" "Expected the slug to stay stable."
                          Expect.stringContains text "note_kind = \"logos_intake_seed\"" "Expected the LOGOS intake note kind."
                          Expect.stringContains text "source_system = \"forum\"" "Expected the forum source system."
                          Expect.stringContains text "intake_channel = \"forum-thread\"" "Expected the forum-thread intake channel."
                          Expect.stringContains text "signal_kind = \"support-question\"" "Expected the support-question signal kind."
                          Expect.stringContains text "Customer cannot complete setup after the latest update." "Expected the seed summary."
                          Expect.stringContains text "https://community.example.com/t/123" "Expected the source URI locator."))

              testCase "CLI report-logos-catalog prints the explicit allowlist" (fun () ->
                  let result = TestHelpers.runCli [ "report-logos-catalog" ]

                  Expect.equal result.ExitCode 0 "Expected the LOGOS catalog report to succeed."
                  Expect.equal result.StandardError "" "Did not expect stderr from report-logos-catalog."
                  Expect.stringContains result.StandardOutput "LOGOS catalog." "Expected the report header."
                  Expect.stringContains result.StandardOutput "forum" "Expected forum in the source-system list."
                  Expect.stringContains result.StandardOutput "email-thread" "Expected email-thread in the intake-channel list."
                  Expect.stringContains result.StandardOutput "support-question" "Expected support-question in the signal-kind list.") ]
