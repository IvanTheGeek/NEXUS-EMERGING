namespace Nexus.Tests

open Expecto
open System
open System.IO
open Nexus.Domain
open Nexus.EventStore
open VerifyTests
open VerifyExpecto

[<RequireQualifiedAccess>]
module SnapshotTests =
    do VerifierSettings.UseUtf8NoBom()
    do Verifier.UseProjectRelativeDirectory("snapshots")

    let private sourceFilePath =
        Path.Combine(__SOURCE_DIRECTORY__, __SOURCE_FILE__)

    let private fixedOccurredAt =
        DateTimeOffset.Parse("2026-03-23T13:00:00Z")

    let private fixedObservedAt =
        DateTimeOffset.Parse("2026-03-23T13:00:05Z")

    let private fixedImportedAt =
        DateTimeOffset.Parse("2026-03-23T13:00:06Z")

    let private importId =
        ImportId.parse "019d1b00-0000-7000-8000-000000000001"

    let private eventId =
        CanonicalEventId.parse "019d1b00-0000-7000-8000-000000000002"

    let private conversationId =
        ConversationId.parse "019d1b00-0000-7000-8000-000000000003"

    let private messageId =
        MessageId.parse "019d1b00-0000-7000-8000-000000000004"

    let private artifactId =
        ArtifactId.parse "019d1b00-0000-7000-8000-000000000005"

    let private contentHash =
        { Algorithm = "sha256"
          Value = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" }

    let private rootArtifact =
        { RawObjectId = None
          Kind = ProviderExportZip
          RelativePath = "providers/claude/archive/fixture/fixture-export.zip"
          ArchivedAt = Some (ImportedAt fixedImportedAt)
          SourceDescription = Some "Fixture provider export zip" }

    let private conversationRef =
        { Provider = Claude
          ObjectKind = ConversationObject
          NativeId = Some "fixture-conversation"
          ConversationNativeId = Some "fixture-conversation"
          MessageNativeId = None
          ArtifactNativeId = None }

    let private messageRef =
        { Provider = Claude
          ObjectKind = MessageObject
          NativeId = Some "fixture-message"
          ConversationNativeId = Some "fixture-conversation"
          MessageNativeId = Some "fixture-message"
          ArtifactNativeId = None }

    let private sampleEvent =
        { Envelope =
            { EventId = eventId
              ConversationId = Some conversationId
              MessageId = Some messageId
              ArtifactId = None
              TurnId = None
              DomainId = Some (DomainId.create "ingestion")
              BoundedContextId = Some (BoundedContextId.create "canonical-history")
              OccurredAt = Some (OccurredAt fixedOccurredAt)
              ObservedAt = ObservedAt fixedObservedAt
              ImportedAt = Some (ImportedAt fixedImportedAt)
              SourceAcquisition = ExportZip
              NormalizationVersion = Some (NormalizationVersion.create "provider-export-v1")
              ContentHash = Some contentHash
              ImportId = Some importId
              ProviderRefs = [ conversationRef; messageRef ]
              RawObjects = [ rootArtifact ] }
          Body =
            ProviderMessageObserved
                { MessageId = messageId
                  ConversationId = conversationId
                  ProviderMessage = messageRef
                  Role = Assistant
                  Segments =
                    [ { Kind = PlainText
                        Text = "Fixture assistant response." } ]
                  ModelName = Some "claude-3-7-sonnet"
                  SequenceHint = Some 2 } }

    let private sampleManifest =
        { ImportId = importId
          Provider = Claude
          SourceAcquisition = ExportZip
          NormalizationVersion = Some (NormalizationVersion.create "provider-export-v1")
          Window = Some Full
          ImportedAt = ImportedAt fixedImportedAt
          RootArtifact = rootArtifact
          LogosMetadata =
            Some
                { SourceSystem = "claude"
                  IntakeChannel = "ai-conversation"
                  PrimarySignalKind = "conversation"
                  RelatedSignalKinds = [ "message" ]
                  HandlingPolicy =
                    { Sensitivity = "internal-restricted"
                      SharingScope = "owner-only"
                      SanitizationStatus = "raw"
                      RetentionClass = "durable" }
                  EntryPool = "raw" }
          Counts =
            { ConversationsSeen = 1
              MessagesSeen = 2
              ArtifactsReferenced = 1
              NewEventsAppended = 7
              DuplicatesSkipped = 0
              RevisionsObserved = 0
              ReparseObservationsAppended = 0 }
          NewCanonicalEventIds = [ eventId ]
          Notes = [ "Fixture import manifest" ] }

    let private verifyToml (fileNamePrefix: string) (text: string) =
        let settings = VerifySettings()
        settings.UseFileName(fileNamePrefix)

        (Verifier.Verify(
            fileNamePrefix,
            text,
            "toml",
            settings = settings,
            sourceFile = sourceFilePath
        ))
            .ToTask()

    let tests =
        testList
            "snapshots"
            [ testTask "canonical event TOML stays stable" {
                  let serialized = CanonicalStore.serializeCanonicalEvent sampleEvent
                  do! verifyToml "canonical-event" serialized
              }

              testTask "import manifest TOML stays stable" {
                  let serialized = CanonicalStore.serializeImportManifest sampleManifest
                  do! verifyToml "import-manifest" serialized
              } ]
