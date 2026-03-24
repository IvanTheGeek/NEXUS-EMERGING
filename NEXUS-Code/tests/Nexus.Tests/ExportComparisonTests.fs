namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.Domain
open Nexus.Importers

[<RequireQualifiedAccess>]
module ExportComparisonTests =
    let private createZipFromProviderFixture tempRoot fixtureFolderName =
        let zipPath = Path.Combine(tempRoot, $"{fixtureFolderName}.zip")
        TestHelpers.createZipFromFixture $"provider-export/{fixtureFolderName}" zipPath
        zipPath

    let tests =
        testList
            "provider export comparison"
            [ testCase "Claude raw export comparison reports added and changed provider-native conversations" (fun () ->
                  TestHelpers.withTempDirectory "nexus-export-comparison" (fun tempRoot ->
                      let baseZipPath = createZipFromProviderFixture tempRoot "claude"
                      let currentZipPath = createZipFromProviderFixture tempRoot "claude-follow-on"

                      let report = ExportComparison.compare Claude baseZipPath currentZipPath 10

                      Expect.equal report.Provider Claude "Expected the Claude provider comparison."
                      Expect.isFalse report.ZipArtifactsIdentical "Expected different raw zip content for the follow-on fixture."
                      Expect.equal report.BaseConversationCount 1 "Expected one base conversation."
                      Expect.equal report.CurrentConversationCount 2 "Expected two current conversations."
                      Expect.equal report.BaseMessageCount 2 "Expected two base messages."
                      Expect.equal report.CurrentMessageCount 4 "Expected four current messages."
                      Expect.equal report.BaseArtifactReferenceCount 1 "Expected one base artifact reference."
                      Expect.equal report.CurrentArtifactReferenceCount 1 "Expected one current artifact reference."
                      Expect.equal report.AddedConversationCount 1 "Expected one newly added provider-native conversation."
                      Expect.equal report.RemovedConversationCount 0 "Expected no removed provider-native conversations."
                      Expect.equal report.ChangedConversationCount 1 "Expected the shared provider-native conversation to be marked changed."
                      Expect.equal report.UnchangedConversationCount 0 "Expected no unchanged shared conversations."
                      Expect.equal report.AddedConversations.Head.ProviderConversationId "claude-conv-2" "Expected the follow-on provider-native conversation ID."
                      Expect.equal report.ChangedConversations.Head.ProviderConversationId "claude-conv-1" "Expected the shared provider-native conversation ID."
                      Expect.equal report.ChangedConversations.Head.BaseMessageCount 2 "Expected two base messages for the shared conversation."
                      Expect.equal report.ChangedConversations.Head.CurrentMessageCount 3 "Expected three current messages for the shared conversation."
                      Expect.equal report.ChangedConversations.Head.AddedMessageCount 1 "Expected one added provider-native message in the shared conversation."
                      Expect.equal report.ChangedConversations.Head.RemovedMessageCount 0 "Expected no removed provider-native messages in the shared conversation."))

              testCase "CLI compare-provider-exports prints source-layer zip deltas" (fun () ->
                  TestHelpers.withTempDirectory "nexus-export-comparison-cli" (fun tempRoot ->
                      let baseZipPath = createZipFromProviderFixture tempRoot "claude"
                      let currentZipPath = createZipFromProviderFixture tempRoot "claude-follow-on"

                      let result =
                          TestHelpers.runCli
                              [ "compare-provider-exports"
                                "--provider"
                                "claude"
                                "--base-zip"
                                baseZipPath
                                "--current-zip"
                                currentZipPath
                                "--limit"
                                "10" ]

                      Expect.equal result.ExitCode 0 "Expected the raw export comparison command to succeed."
                      Expect.equal result.StandardError "" "Did not expect stderr from the raw export comparison command."
                      Expect.stringContains result.StandardOutput "Provider export comparison." "Expected the comparison header."
                      Expect.stringContains result.StandardOutput "Zip artifacts identical: false" "Expected non-identical zip output."
                      Expect.stringContains result.StandardOutput "Added conversations: 1" "Expected the added conversation count."
                      Expect.stringContains result.StandardOutput "Changed conversations: 1" "Expected the changed conversation count."
                      Expect.stringContains result.StandardOutput "Claude Follow-on Conversation" "Expected the added conversation label."
                      Expect.stringContains result.StandardOutput "messages=2 -> 3 (added=1 removed=0)" "Expected the shared conversation message delta.")) ]
