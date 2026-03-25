namespace Nexus.Tests

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
                  Expect.equal (LogosSignal.summary signal) (Some "idea seed") "Expected summaries to trim surrounding whitespace.") ]
