namespace Nexus.Logos

open System

/// <summary>
/// Describes how a known provider currently maps into the LOGOS source model.
/// </summary>
/// <remarks>
/// This is a classification bridge from existing ingestion providers into LOGOS source semantics. It does not perform overlap reconciliation.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
type ProviderLogosClassification =
    { SourceSystemId: SourceSystemId
      IntakeChannelId: IntakeChannelId
      PrimarySignalKind: SignalKindId
      RelatedSignalKinds: SignalKindId list
      DefaultHandlingPolicy: LogosHandlingPolicy
      EntryPool: string }

/// <summary>
/// Maps currently known providers into LOGOS source/channel/signal classifications.
/// </summary>
[<RequireQualifiedAccess>]
module ProviderLogosClassification =
    let private classification sourceSystem intakeChannel primarySignal relatedSignals =
        { SourceSystemId = sourceSystem
          IntakeChannelId = intakeChannel
          PrimarySignalKind = primarySignal
          RelatedSignalKinds = relatedSignals
          DefaultHandlingPolicy = LogosHandlingPolicy.restrictedDefault
          EntryPool = "raw" }

    let private knownMappings =
        [ "chatgpt",
          classification
              KnownSourceSystems.chatgpt
              CoreIntakeChannels.aiConversation
              CoreSignalKinds.conversation
              [ CoreSignalKinds.message ]
          "claude",
          classification
              KnownSourceSystems.claude
              CoreIntakeChannels.aiConversation
              CoreSignalKinds.conversation
              [ CoreSignalKinds.message ]
          "grok",
          classification
              KnownSourceSystems.grok
              CoreIntakeChannels.aiConversation
              CoreSignalKinds.conversation
              [ CoreSignalKinds.message ]
          "codex",
          classification
              KnownSourceSystems.codex
              CoreIntakeChannels.aiConversation
              CoreSignalKinds.conversation
              [ CoreSignalKinds.message ] ]
        |> Map.ofList

    /// <summary>
    /// Looks up the LOGOS classification for a known provider slug.
    /// </summary>
    let tryFind (providerSlug: string) =
        let normalized = providerSlug.Trim().ToLowerInvariant()

        if String.IsNullOrWhiteSpace(normalized) then
            None
        else
            Map.tryFind normalized knownMappings
