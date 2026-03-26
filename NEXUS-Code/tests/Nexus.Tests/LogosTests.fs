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
                  let sensitivitySlugs = report.Sensitivities |> List.map (fun item -> item.Slug)

                  Expect.isTrue (sourceSystemSlugs |> List.contains "forum") "Expected forum to be an explicit LOGOS source system."
                  Expect.isTrue (sourceSystemSlugs |> List.contains "email") "Expected email to be an explicit LOGOS source system."
                  Expect.isTrue (sourceSystemSlugs |> List.contains "issue-tracker") "Expected issue-tracker to be an explicit LOGOS source system."
                  Expect.isTrue (sourceSystemSlugs |> List.contains "app-feedback-surface") "Expected app-feedback-surface to be an explicit LOGOS source system."
                  Expect.isTrue (sensitivitySlugs |> List.contains "internal-restricted") "Expected the restricted-default sensitivity to be allowlisted."
                  Expect.isTrue (sensitivitySlugs |> List.contains "public") "Expected public sensitivity to remain explicitly modeled.")

              testCase "LOGOS intake note seed writes a durable markdown note" (fun () ->
                  TestHelpers.withTempDirectory "nexus-logos-note" (fun tempRoot ->
                      let docsRoot = Path.Combine(tempRoot, "docs")
                      let policy =
                          LogosHandlingPolicy.create
                              KnownSensitivities.customerConfidential
                              KnownSharingScopes.caseTeam
                              KnownSanitizationStatuses.redacted
                              KnownRetentionClasses.caseBound

                      let result =
                          LogosIntakeNotes.create
                              { DocsRoot = docsRoot
                                Slug = "support-thread-123"
                                Title = "Support Thread 123"
                                SourceSystemId = KnownSourceSystems.forum
                                IntakeChannelId = CoreIntakeChannels.forumThread
                                SignalKindId = CoreSignalKinds.supportQuestion
                                Policy = policy
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
                          Expect.stringContains text "sensitivity = \"customer-confidential\"" "Expected the note to carry the explicit sensitivity."
                          Expect.stringContains text "sharing_scope = \"case-team\"" "Expected the note to carry the explicit sharing scope."
                          Expect.stringContains text "sanitization_status = \"redacted\"" "Expected the note to carry the explicit sanitization status."
                          Expect.stringContains text "retention_class = \"case-bound\"" "Expected the note to carry the explicit retention class."
                          Expect.stringContains text "Customer cannot complete setup after the latest update." "Expected the seed summary."
                          Expect.stringContains text "https://community.example.com/t/123" "Expected the source URI locator."))

              testCase "CLI report-logos-catalog prints the explicit allowlist" (fun () ->
                  let result = TestHelpers.runCli [ "report-logos-catalog" ]

                  Expect.equal result.ExitCode 0 "Expected the LOGOS catalog report to succeed."
                  Expect.equal result.StandardError "" "Did not expect stderr from report-logos-catalog."
                  Expect.stringContains result.StandardOutput "LOGOS catalog." "Expected the report header."
                  Expect.stringContains result.StandardOutput "forum" "Expected forum in the source-system list."
                  Expect.stringContains result.StandardOutput "email-thread" "Expected email-thread in the intake-channel list."
                  Expect.stringContains result.StandardOutput "support-question" "Expected support-question in the signal-kind list."
                  Expect.stringContains result.StandardOutput "internal-restricted" "Expected sensitivity values in the policy catalog."
                  Expect.stringContains result.StandardOutput "owner-only" "Expected sharing-scope values in the policy catalog.")

              testCase "CLI create-logos-intake-note accepts explicit handling policy metadata" (fun () ->
                  TestHelpers.withTempDirectory "nexus-logos-note-cli" (fun tempRoot ->
                      let docsRoot = Path.Combine(tempRoot, "docs")

                      let cliResult =
                          TestHelpers.runCli
                              [ "create-logos-intake-note"
                                "--docs-root"
                                docsRoot
                                "--slug"
                                "support-thread-123"
                                "--title"
                                "Support Thread 123"
                                "--source-system"
                                "forum"
                                "--intake-channel"
                                "forum-thread"
                                "--signal-kind"
                                "support-question"
                                "--sensitivity"
                                "customer-confidential"
                                "--sharing-scope"
                                "case-team"
                                "--sanitization-status"
                                "redacted"
                                "--retention-class"
                                "case-bound"
                                "--source-uri"
                                "https://community.example.com/t/123" ]

                      let outputPath = Path.Combine(docsRoot, "logos-intake", "support-thread-123.md")
                      let text = File.ReadAllText(outputPath)

                      Expect.equal cliResult.ExitCode 0 "Expected CLI LOGOS intake note creation to succeed."
                      Expect.equal cliResult.StandardError "" "Did not expect stderr from the CLI LOGOS intake note creation."
                      Expect.isTrue (File.Exists(outputPath)) "Expected the CLI-created LOGOS intake note file to exist."
                      Expect.stringContains cliResult.StandardOutput "Sensitivity: customer-confidential" "Expected the CLI summary to print the sensitivity."
                      Expect.stringContains cliResult.StandardOutput "Sharing scope: case-team" "Expected the CLI summary to print the sharing scope."
                      Expect.stringContains text "sanitization_status = \"redacted\"" "Expected the CLI-created note to persist the sanitization status."
                      Expect.stringContains text "retention_class = \"case-bound\"" "Expected the CLI-created note to persist the retention class."))

              testCase "LOGOS sanitized note derives from a restricted intake note without copying raw locators" (fun () ->
                  TestHelpers.withTempDirectory "nexus-logos-sanitized-note" (fun tempRoot ->
                      let docsRoot = Path.Combine(tempRoot, "docs")

                      let sourcePolicy =
                          LogosHandlingPolicy.create
                              KnownSensitivities.customerConfidential
                              KnownSharingScopes.caseTeam
                              KnownSanitizationStatuses.raw
                              KnownRetentionClasses.caseBound

                      let sourceResult =
                          LogosIntakeNotes.create
                              { DocsRoot = docsRoot
                                Slug = "cheddarbooks-debug-case-42"
                                Title = "CheddarBooks Debug Case 42"
                                SourceSystemId = KnownSourceSystems.issueTracker
                                IntakeChannelId = CoreIntakeChannels.bugReport
                                SignalKindId = CoreSignalKinds.bugReport
                                Policy = sourcePolicy
                                Locators = [ LogosLocator.sourceUri "https://support.example.com/cases/42" ]
                                CapturedAt = None
                                Summary = Some "Customer shared a support case with sensitive financial details."
                                Tags = [ "support"; "customer" ] }

                      match sourceResult with
                      | Error error ->
                          failtestf "Expected source LOGOS intake note creation to succeed. %s" error
                      | Ok _ ->
                          let result =
                              LogosSanitizedNotes.create
                                  { DocsRoot = docsRoot
                                    SourceSlug = "cheddarbooks-debug-case-42"
                                    Slug = "cheddarbooks-case-42-anonymized"
                                    Title = "CheddarBooks Case 42 (Anonymized)"
                                    SanitizationStatusId = KnownSanitizationStatuses.anonymized
                                    SensitivityId = Some KnownSensitivities.internalRestricted
                                    SharingScopeId = Some KnownSharingScopes.projectTeam
                                    RetentionClassId = Some KnownRetentionClasses.durable
                                    Summary = Some "Sensitive customer-identifying details removed while preserving the debugging pattern."
                                    Tags = [ "anonymized"; "case-study" ] }

                          match result with
                          | Error error ->
                              failtestf "Expected sanitized LOGOS note creation to succeed. %s" error
                          | Ok note ->
                              let text = File.ReadAllText(note.OutputPath)

                              Expect.isTrue (File.Exists(note.OutputPath)) "Expected the sanitized LOGOS note file to exist."
                              Expect.equal note.NormalizedSlug "cheddarbooks-case-42-anonymized" "Expected the derived slug to stay stable."
                              Expect.stringContains text "note_kind = \"logos_intake_sanitized\"" "Expected the derived note kind."
                              Expect.stringContains text "derived_from = \"logos-intake/cheddarbooks-debug-case-42.md\"" "Expected the derived note to retain a source pointer."
                              Expect.stringContains text "source_system = \"issue-tracker\"" "Expected the source system classification to be preserved."
                              Expect.stringContains text "source_sanitization_status = \"raw\"" "Expected the source policy to remain visible."
                              Expect.stringContains text "sanitization_status = \"anonymized\"" "Expected the derived sanitization status."
                              Expect.stringContains text "sharing_scope = \"project-team\"" "Expected the derived sharing scope."
                              Expect.stringContains text "retention_class = \"durable\"" "Expected the derived retention class."
                              Expect.stringContains text "Sensitive customer-identifying details removed while preserving the debugging pattern." "Expected the derived summary."
                              Expect.isFalse (text.Contains("https://support.example.com/cases/42")) "Expected raw locators to stay out of the derived note."
                              Expect.isFalse (text.Contains("## Locators")) "Expected the derived note to avoid copying the source locator section."))

              testCase "LOGOS sanitized note rejects approved-for-sharing without an explicit sharing scope" (fun () ->
                  TestHelpers.withTempDirectory "nexus-logos-sanitized-note-policy" (fun tempRoot ->
                      let docsRoot = Path.Combine(tempRoot, "docs")

                      let sourcePolicy =
                          LogosHandlingPolicy.create
                              KnownSensitivities.personalPrivate
                              KnownSharingScopes.ownerOnly
                              KnownSanitizationStatuses.raw
                              KnownRetentionClasses.caseBound

                      let sourceResult =
                          LogosIntakeNotes.create
                              { DocsRoot = docsRoot
                                Slug = "personal-chat-1"
                                Title = "Personal Chat 1"
                                SourceSystemId = KnownSourceSystems.chatgpt
                                IntakeChannelId = CoreIntakeChannels.aiConversation
                                SignalKindId = CoreSignalKinds.conversation
                                Policy = sourcePolicy
                                Locators = [ LogosLocator.nativeThreadId "thread-1" ]
                                CapturedAt = None
                                Summary = None
                                Tags = [] }

                      match sourceResult with
                      | Error error ->
                          failtestf "Expected source LOGOS intake note creation to succeed. %s" error
                      | Ok _ ->
                          let result =
                              LogosSanitizedNotes.create
                                  { DocsRoot = docsRoot
                                    SourceSlug = "personal-chat-1"
                                    Slug = "personal-chat-1-shareable"
                                    Title = "Personal Chat 1 (Shareable)"
                                    SanitizationStatusId = KnownSanitizationStatuses.approvedForSharing
                                    SensitivityId = Some KnownSensitivities.publicData
                                    SharingScopeId = None
                                    RetentionClassId = None
                                    Summary = None
                                    Tags = [] }

                          match result with
                          | Ok _ ->
                              failtest "Expected approved-for-sharing without an explicit sharing scope to be rejected."
                          | Error error ->
                              Expect.stringContains error "approved-for-sharing requires an explicit --sharing-scope." "Expected the explicit sharing-scope requirement."))

              testCase "CLI create-logos-sanitized-note creates a derived note" (fun () ->
                  TestHelpers.withTempDirectory "nexus-logos-sanitized-note-cli" (fun tempRoot ->
                      let docsRoot = Path.Combine(tempRoot, "docs")

                      let sourcePolicy =
                          LogosHandlingPolicy.create
                              KnownSensitivities.customerConfidential
                              KnownSharingScopes.caseTeam
                              KnownSanitizationStatuses.raw
                              KnownRetentionClasses.caseBound

                      let sourceResult =
                          LogosIntakeNotes.create
                              { DocsRoot = docsRoot
                                Slug = "support-thread-123"
                                Title = "Support Thread 123"
                                SourceSystemId = KnownSourceSystems.forum
                                IntakeChannelId = CoreIntakeChannels.forumThread
                                SignalKindId = CoreSignalKinds.supportQuestion
                                Policy = sourcePolicy
                                Locators = [ LogosLocator.sourceUri "https://community.example.com/t/123" ]
                                CapturedAt = None
                                Summary = Some "Customer cannot complete setup after the latest update."
                                Tags = [ "support" ] }

                      match sourceResult with
                      | Error error ->
                          failtestf "Expected source LOGOS intake note creation to succeed. %s" error
                      | Ok _ ->
                          let cliResult =
                              TestHelpers.runCli
                                  [ "create-logos-sanitized-note"
                                    "--docs-root"
                                    docsRoot
                                    "--source-slug"
                                    "support-thread-123"
                                    "--slug"
                                    "support-thread-123-redacted"
                                    "--title"
                                    "Support Thread 123 (Redacted)"
                                    "--sanitization-status"
                                    "redacted"
                                    "--sharing-scope"
                                    "project-team"
                                    "--summary"
                                    "Redacted support summary for wider internal sharing."
                                    "--tag"
                                    "redacted" ]

                          let outputPath = Path.Combine(docsRoot, "logos-intake-derived", "support-thread-123-redacted.md")
                          let text = File.ReadAllText(outputPath)

                          Expect.equal cliResult.ExitCode 0 "Expected CLI sanitized LOGOS note creation to succeed."
                          Expect.equal cliResult.StandardError "" "Did not expect stderr from the CLI sanitized LOGOS note creation."
                          Expect.isTrue (File.Exists(outputPath)) "Expected the CLI-created sanitized LOGOS note file to exist."
                          Expect.stringContains cliResult.StandardOutput "Sanitized LOGOS note created." "Expected the CLI result header."
                          Expect.stringContains cliResult.StandardOutput "Source note path:" "Expected the CLI summary to print the source note path."
                          Expect.stringContains cliResult.StandardOutput "Sanitization status: redacted" "Expected the CLI summary to print the sanitization status."
                          Expect.stringContains text "note_kind = \"logos_intake_sanitized\"" "Expected the derived note kind."
                          Expect.stringContains text "sharing_scope = \"project-team\"" "Expected the derived sharing scope."
                          Expect.isFalse (text.Contains("https://community.example.com/t/123")) "Expected raw locators to stay out of the CLI-created derived note."))

              testCase "LOGOS handling report surfaces raw, confidential, and approved notes" (fun () ->
                  TestHelpers.withTempDirectory "nexus-logos-handling-report" (fun tempRoot ->
                      let docsRoot = Path.Combine(tempRoot, "docs")

                      let customerRawPolicy =
                          LogosHandlingPolicy.create
                              KnownSensitivities.customerConfidential
                              KnownSharingScopes.caseTeam
                              KnownSanitizationStatuses.raw
                              KnownRetentionClasses.caseBound

                      let personalRawPolicy =
                          LogosHandlingPolicy.create
                              KnownSensitivities.personalPrivate
                              KnownSharingScopes.ownerOnly
                              KnownSanitizationStatuses.raw
                              KnownRetentionClasses.caseBound

                      let customerResult =
                          LogosIntakeNotes.create
                              { DocsRoot = docsRoot
                                Slug = "cheddarbooks-debug-case-42"
                                Title = "CheddarBooks Debug Case 42"
                                SourceSystemId = KnownSourceSystems.issueTracker
                                IntakeChannelId = CoreIntakeChannels.bugReport
                                SignalKindId = CoreSignalKinds.bugReport
                                Policy = customerRawPolicy
                                Locators = [ LogosLocator.sourceUri "https://support.example.com/cases/42" ]
                                CapturedAt = None
                                Summary = None
                                Tags = [] }

                      let personalResult =
                          LogosIntakeNotes.create
                              { DocsRoot = docsRoot
                                Slug = "personal-chat-1"
                                Title = "Personal Chat 1"
                                SourceSystemId = KnownSourceSystems.chatgpt
                                IntakeChannelId = CoreIntakeChannels.aiConversation
                                SignalKindId = CoreSignalKinds.conversation
                                Policy = personalRawPolicy
                                Locators = [ LogosLocator.nativeThreadId "thread-1" ]
                                CapturedAt = None
                                Summary = None
                                Tags = [] }

                      match customerResult, personalResult with
                      | Ok _, Ok _ ->
                          let approvedResult =
                              LogosSanitizedNotes.create
                                  { DocsRoot = docsRoot
                                    SourceSlug = "cheddarbooks-debug-case-42"
                                    Slug = "cheddarbooks-case-42-shareable"
                                    Title = "CheddarBooks Case 42 (Shareable)"
                                    SanitizationStatusId = KnownSanitizationStatuses.approvedForSharing
                                    SensitivityId = Some KnownSensitivities.publicData
                                    SharingScopeId = Some KnownSharingScopes.publicAudience
                                    RetentionClassId = Some KnownRetentionClasses.durable
                                    Summary = Some "Anonymized debugging pattern approved for public sharing."
                                    Tags = [ "shareable" ] }

                          match approvedResult with
                          | Error error ->
                              failtestf "Expected approved-for-sharing derived note creation to succeed. %s" error
                          | Ok _ ->
                              match LogosHandlingReports.build docsRoot with
                              | Error error ->
                                  failtestf "Expected LOGOS handling report to succeed. %s" error
                              | Ok report ->
                                  Expect.equal report.Notes.Length 3 "Expected two source notes plus one derived note."
                                  Expect.equal report.RawNotes.Length 2 "Expected both source notes to remain raw."
                                  Expect.equal report.PersonalPrivateNotes.Length 1 "Expected one personal-private note."
                                  Expect.equal report.CustomerConfidentialNotes.Length 1 "Expected one customer-confidential note."
                                  Expect.equal report.ApprovedForSharingNotes.Length 1 "Expected one approved-for-sharing derivative."
                                  Expect.equal (report.ApprovedForSharingNotes |> List.head |> fun note -> note.Slug) "cheddarbooks-case-42-shareable" "Expected the approved derived note slug."
                                  Expect.isTrue (report.Sensitivities |> List.exists (fun item -> item.Slug = "public" && item.Count = 1)) "Expected the public derived note to be counted."
                                  Expect.isTrue (report.SanitizationStatuses |> List.exists (fun item -> item.Slug = "approved-for-sharing" && item.Count = 1)) "Expected the approved-for-sharing status to be counted."
                      | Error error, _
                      | _, Error error ->
                          failtestf "Expected source LOGOS intake note creation to succeed. %s" error))

              testCase "CLI report-logos-handling prints flagged handling sections" (fun () ->
                  TestHelpers.withTempDirectory "nexus-logos-handling-cli" (fun tempRoot ->
                      let docsRoot = Path.Combine(tempRoot, "docs")

                      let sourcePolicy =
                          LogosHandlingPolicy.create
                              KnownSensitivities.customerConfidential
                              KnownSharingScopes.caseTeam
                              KnownSanitizationStatuses.raw
                              KnownRetentionClasses.caseBound

                      let sourceResult =
                          LogosIntakeNotes.create
                              { DocsRoot = docsRoot
                                Slug = "support-thread-123"
                                Title = "Support Thread 123"
                                SourceSystemId = KnownSourceSystems.forum
                                IntakeChannelId = CoreIntakeChannels.forumThread
                                SignalKindId = CoreSignalKinds.supportQuestion
                                Policy = sourcePolicy
                                Locators = [ LogosLocator.sourceUri "https://community.example.com/t/123" ]
                                CapturedAt = None
                                Summary = None
                                Tags = [] }

                      match sourceResult with
                      | Error error ->
                          failtestf "Expected source LOGOS intake note creation to succeed. %s" error
                      | Ok _ ->
                          let derivedResult =
                              LogosSanitizedNotes.create
                                  { DocsRoot = docsRoot
                                    SourceSlug = "support-thread-123"
                                    Slug = "support-thread-123-shareable"
                                    Title = "Support Thread 123 (Shareable)"
                                    SanitizationStatusId = KnownSanitizationStatuses.approvedForSharing
                                    SensitivityId = Some KnownSensitivities.publicData
                                    SharingScopeId = Some KnownSharingScopes.projectTeam
                                    RetentionClassId = Some KnownRetentionClasses.durable
                                    Summary = None
                                    Tags = [] }

                          match derivedResult with
                          | Error error ->
                              failtestf "Expected derived LOGOS sanitized note creation to succeed. %s" error
                          | Ok _ ->
                              let result =
                                  TestHelpers.runCli
                                      [ "report-logos-handling"
                                        "--docs-root"
                                        docsRoot
                                        "--limit"
                                        "5" ]

                              Expect.equal result.ExitCode 0 "Expected report-logos-handling to succeed."
                              Expect.equal result.StandardError "" "Did not expect stderr from report-logos-handling."
                              Expect.stringContains result.StandardOutput "LOGOS handling report." "Expected the report header."
                              Expect.stringContains result.StandardOutput "Still raw:" "Expected the raw-note section."
                              Expect.stringContains result.StandardOutput "Customer-confidential:" "Expected the confidential-note section."
                              Expect.stringContains result.StandardOutput "Approved for sharing:" "Expected the shareable-note section."
                              Expect.stringContains result.StandardOutput "support-thread-123.md" "Expected the source note path in the report."
                              Expect.stringContains result.StandardOutput "support-thread-123-shareable" "Expected the derived shareable note slug in the report.")) ]
