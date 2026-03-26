namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.EventStore
open Nexus.Importers
open Nexus.Domain
open Nexus.Logos

[<RequireQualifiedAccess>]
module CurrentIngestionTests =
    let private buildChatGptImportRequest tempRoot =
        let objectsRoot = Path.Combine(tempRoot, "objects")
        let eventStoreRoot = Path.Combine(tempRoot, "event-store")
        let zipPath = Path.Combine(tempRoot, "chatgpt-fixture.zip")

        TestHelpers.createZipFromFixture "provider-export/chatgpt" zipPath

        { Provider = ChatGpt
          SourceZipPath = zipPath
          Window = Some Full
          ObjectsRoot = objectsRoot
          EventStoreRoot = eventStoreRoot },
        objectsRoot,
        eventStoreRoot

    let private buildCodexImportRequest tempRoot =
        let objectsRoot = Path.Combine(tempRoot, "objects")
        let eventStoreRoot = Path.Combine(tempRoot, "event-store")
        let snapshotRoot = Path.Combine(objectsRoot, "providers", "codex", "latest")

        TestHelpers.copyFixtureDirectory "codex/latest" snapshotRoot

        { SnapshotRoot = snapshotRoot
          ObjectsRoot = objectsRoot
          EventStoreRoot = eventStoreRoot },
        objectsRoot,
        eventStoreRoot

    let tests =
        testList
            "current ingestion"
            [ testCase "report includes the latest import per provider with snapshot awareness" (fun () ->
                  TestHelpers.withTempDirectory "nexus-current-ingestion" (fun tempRoot ->
                      let chatGptRequest, objectsRoot, eventStoreRoot = buildChatGptImportRequest tempRoot
                      let codexRequest, _, _ = buildCodexImportRequest tempRoot

                      let chatGptImport = ImportWorkflow.run chatGptRequest
                      let codexImport = CodexImportWorkflow.run codexRequest

                      let report = CurrentIngestion.buildReport eventStoreRoot objectsRoot

                      Expect.equal report.ImportManifestCount 2 "Expected two import manifests in the fixture store."
                      Expect.equal report.ProviderCount 2 "Expected one current-ingestion row per provider with imports."
                      Expect.equal report.MissingKnownProviders [ "claude"; "grok" ] "Expected the remaining known providers to be reported as missing."

                      let chatGptEntry =
                          report.Entries
                          |> List.find (fun entry -> entry.Provider = "chatgpt")

                      Expect.equal chatGptEntry.ImportId chatGptImport.ImportId "Expected the ChatGPT row to point at the latest ChatGPT import."
                      Expect.isTrue chatGptEntry.SnapshotAvailable "Expected provider-export imports to have normalized snapshots."
                      Expect.equal chatGptEntry.LogosSourceSystem (Some(SourceSystemId.value KnownSourceSystems.chatgpt)) "Expected ChatGPT to map into LOGOS source semantics."
                      Expect.equal chatGptEntry.LogosIntakeChannel (Some(IntakeChannelId.value CoreIntakeChannels.aiConversation)) "Expected AI conversation LOGOS intake classification."
                      Expect.equal chatGptEntry.LogosPrimarySignalKind (Some(SignalKindId.value CoreSignalKinds.conversation)) "Expected conversation to be the primary LOGOS signal kind."
                      Expect.equal chatGptEntry.LogosRelatedSignalKinds [ SignalKindId.value CoreSignalKinds.message ] "Expected message as the related LOGOS signal kind."
                      Expect.equal chatGptEntry.LogosSensitivity (Some "internal-restricted") "Expected provider import sensitivity to be persisted."
                      Expect.equal chatGptEntry.LogosSharingScope (Some "owner-only") "Expected provider import sharing scope to be persisted."
                      Expect.equal chatGptEntry.LogosSanitizationStatus (Some "raw") "Expected provider imports to enter as raw."
                      Expect.equal chatGptEntry.LogosRetentionClass (Some "durable") "Expected provider import retention to be durable."
                      Expect.equal chatGptEntry.LogosEntryPool (Some "raw") "Expected provider imports to enter via the raw pool."
                      Expect.equal chatGptEntry.SnapshotConversationCount (Some 1) "Expected one ChatGPT snapshot conversation."
                      Expect.equal chatGptEntry.SnapshotMessageCount (Some 2) "Expected two ChatGPT snapshot messages."
                      Expect.equal chatGptEntry.SnapshotArtifactReferenceCount (Some 1) "Expected one ChatGPT artifact reference in the fixture snapshot."
                      Expect.equal chatGptEntry.RootArtifactExists (Some true) "Expected the preserved ChatGPT zip to exist."
                      Expect.isSome chatGptEntry.RootArtifactSha256 "Expected the ChatGPT raw artifact hash."

                      let codexEntry =
                          report.Entries
                          |> List.find (fun entry -> entry.Provider = "codex")

                      Expect.equal codexEntry.ImportId codexImport.ImportId "Expected the Codex row to point at the latest Codex import."
                      Expect.isFalse codexEntry.SnapshotAvailable "Expected Codex to report from import manifests without normalized snapshots."
                      Expect.equal codexEntry.LogosSourceSystem (Some(SourceSystemId.value KnownSourceSystems.codex)) "Expected Codex to map into LOGOS source semantics."
                      Expect.equal codexEntry.LogosSensitivity (Some "internal-restricted") "Expected Codex imports to carry the default restricted sensitivity."
                      Expect.equal codexEntry.LogosEntryPool (Some "raw") "Expected Codex imports to enter via the raw pool."
                      Expect.equal codexEntry.Counts.ConversationsSeen 1 "Expected one Codex fixture conversation."
                      Expect.equal codexEntry.Counts.MessagesSeen 2 "Expected two Codex fixture messages."
                      Expect.equal codexEntry.RootArtifactExists (Some true) "Expected the preserved Codex root artifact to exist."
                      Expect.isSome codexEntry.RootArtifactSha256 "Expected the Codex root artifact hash." ))

              testCase "CLI report-current-ingestion prints the cross-provider summary" (fun () ->
                  TestHelpers.withTempDirectory "nexus-current-ingestion-cli" (fun tempRoot ->
                      let chatGptRequest, objectsRoot, eventStoreRoot = buildChatGptImportRequest tempRoot
                      let codexRequest, _, _ = buildCodexImportRequest tempRoot

                      let _ = ImportWorkflow.run chatGptRequest
                      let _ = CodexImportWorkflow.run codexRequest

                      let result =
                          TestHelpers.runCli
                              [ "report-current-ingestion"
                                "--event-store-root"
                                eventStoreRoot
                                "--objects-root"
                                objectsRoot ]

                      Expect.equal result.ExitCode 0 "Expected the current-ingestion report to succeed."
                      Expect.equal result.StandardError "" "Did not expect stderr from report-current-ingestion."
                      Expect.stringContains result.StandardOutput "Current ingestion status." "Expected the report header."
                      Expect.stringContains result.StandardOutput "Providers with imports: 2" "Expected the provider count."
                      Expect.stringContains result.StandardOutput "Missing known providers: claude, grok" "Expected the missing-provider summary."
                      Expect.stringContains result.StandardOutput "1. chatgpt | import_id=" "Expected the ChatGPT row."
                      Expect.stringContains result.StandardOutput "2. codex | import_id=" "Expected the Codex row."
                      Expect.stringContains result.StandardOutput "logos source_system=chatgpt intake_channel=ai-conversation primary_signal=conversation" "Expected the LOGOS classification to appear for ChatGPT."
                      Expect.stringContains result.StandardOutput "logos related_signals=message" "Expected related LOGOS signal kinds to be printed."
                      Expect.stringContains result.StandardOutput "logos handling sensitivity=internal-restricted sharing_scope=owner-only sanitization_status=raw retention_class=durable entry_pool=raw" "Expected the LOGOS handling policy and entry pool to be printed."
                      Expect.stringContains result.StandardOutput "normalized_snapshot_available=true" "Expected snapshot-backed provider guidance."
                      Expect.stringContains result.StandardOutput "normalized_snapshot_available=false" "Expected Codex no-snapshot guidance."
                      Expect.stringContains result.StandardOutput "root_artifact_sha256=" "Expected the raw artifact hash output." )) ]
