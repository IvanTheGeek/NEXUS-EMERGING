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
                  Expect.equal (LogosSignal.summary signal) (Some "idea seed") "Expected summaries to trim surrounding whitespace.")

              testCase "Known providers map into LOGOS classifications" (fun () ->
                  let classification = ProviderLogosClassification.tryFind "chatgpt"

                  Expect.isSome classification "Expected ChatGPT to map into the LOGOS source model."

                  let value = classification |> Option.get

                  Expect.equal (SourceSystemId.value value.SourceSystemId) "chatgpt" "Expected the LOGOS source system to stay stable."
                  Expect.equal (IntakeChannelId.value value.IntakeChannelId) "ai-conversation" "Expected AI conversation to be the intake channel."
                  Expect.equal (SignalKindId.value value.PrimarySignalKind) "conversation" "Expected conversation to be the primary signal kind."
                  Expect.equal (value.RelatedSignalKinds |> List.map SignalKindId.value) [ "message" ] "Expected message to stay as the related signal kind.") ]
