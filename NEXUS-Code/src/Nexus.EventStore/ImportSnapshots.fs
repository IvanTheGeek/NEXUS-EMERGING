namespace Nexus.EventStore

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open Nexus.Domain

/// <summary>
/// Summarizes one conversation as seen inside a normalized import snapshot.
/// </summary>
type NormalizedImportSnapshotConversation =
    { CanonicalConversationId: string
      ProviderConversationId: string
      Title: string option
      IsArchived: bool option
      OccurredAt: DateTimeOffset option
      MessageCount: int
      ArtifactReferenceCount: int }

/// <summary>
/// Describes the normalized per-import snapshot written from one parsed provider payload.
/// </summary>
type NormalizedImportSnapshot =
    { ImportId: ImportId
      Provider: string
      Window: string option
      ImportedAt: DateTimeOffset
      NormalizationVersion: string option
      SourceArtifactRelativePath: string option
      LogosMetadata: ImportLogosMetadata option
      Conversations: NormalizedImportSnapshotConversation list }

/// <summary>
/// Summarizes the files and totals written for one normalized import snapshot.
/// </summary>
type NormalizedImportSnapshotWriteResult =
    { ManifestRelativePath: string
      ConversationsRelativePath: string
      ConversationCount: int
      MessageCount: int
      ArtifactReferenceCount: int }

/// <summary>
/// Loads one normalized import snapshot back into memory for reporting and comparison.
/// </summary>
type NormalizedImportSnapshotReport =
    { ManifestRelativePath: string
      ConversationsRelativePath: string
      ImportId: ImportId
      Provider: string
      Window: string option
      ImportedAt: DateTimeOffset
      NormalizationVersion: string option
      SourceArtifactRelativePath: string option
      LogosMetadata: ImportLogosMetadata option
      ConversationCount: int
      MessageCount: int
      ArtifactReferenceCount: int
      Conversations: NormalizedImportSnapshotConversation list }

/// <summary>
/// Describes one shared provider conversation whose normalized snapshot values differ between two imports.
/// </summary>
type NormalizedImportSnapshotConversationDelta =
    { ProviderConversationId: string
      BaseCanonicalConversationId: string
      CurrentCanonicalConversationId: string
      BaseTitle: string option
      CurrentTitle: string option
      BaseIsArchived: bool option
      CurrentIsArchived: bool option
      BaseOccurredAt: DateTimeOffset option
      CurrentOccurredAt: DateTimeOffset option
      BaseMessageCount: int
      CurrentMessageCount: int
      BaseArtifactReferenceCount: int
      CurrentArtifactReferenceCount: int }

/// <summary>
/// Compares two normalized import snapshots keyed by provider-native conversation identity.
/// </summary>
/// <remarks>
/// This is a normalized per-import snapshot comparison, not canonical deletion logic.
/// Absence from the current snapshot means absence from that import payload only.
/// </remarks>
type NormalizedImportSnapshotComparisonReport =
    { BaseImportId: ImportId
      CurrentImportId: ImportId
      BaseProvider: string
      CurrentProvider: string
      BaseWindow: string option
      CurrentWindow: string option
      BaseImportedAt: DateTimeOffset
      CurrentImportedAt: DateTimeOffset
      AddedConversationCount: int
      RemovedConversationCount: int
      ChangedConversationCount: int
      UnchangedConversationCount: int
      AddedConversations: NormalizedImportSnapshotConversation list
      RemovedConversations: NormalizedImportSnapshotConversation list
      ChangedConversations: NormalizedImportSnapshotConversationDelta list }

/// <summary>
/// Summarizes how one normalized import snapshot differs from the previous snapshot for the same provider.
/// </summary>
type NormalizedImportSnapshotHistoryDelta =
    { PreviousImportId: ImportId
      PreviousImportedAt: DateTimeOffset
      AddedConversationCount: int
      RemovedConversationCount: int
      ChangedConversationCount: int
      UnchangedConversationCount: int }

/// <summary>
/// Describes one chronological row in a provider's normalized import-snapshot history.
/// </summary>
type NormalizedImportSnapshotHistoryEntry =
    { ImportId: ImportId
      ImportedAt: DateTimeOffset
      Window: string option
      NormalizationVersion: string option
      SourceArtifactRelativePath: string option
      ConversationCount: int
      MessageCount: int
      ArtifactReferenceCount: int
      DeltaFromPrevious: NormalizedImportSnapshotHistoryDelta option }

/// <summary>
/// Summarizes the chronological normalized import-snapshot history for one provider.
/// </summary>
type NormalizedImportSnapshotHistoryReport =
    { Provider: string
      AvailableSnapshotCount: int
      ReportedEntryCount: int
      Entries: NormalizedImportSnapshotHistoryEntry list }

/// <summary>
/// Writes, loads, and compares normalized import snapshots derived from parsed provider payloads.
/// </summary>
[<RequireQualifiedAccess>]
module ImportSnapshots =
    let private snapshotsRootRelativePath =
        Path.Combine("snapshots", "imports").Replace('\\', '/')

    let private importRootRelativePath importId =
        Path.Combine(snapshotsRootRelativePath, ImportId.format importId).Replace('\\', '/')

    let private manifestRelativePath importId =
        Path.Combine(importRootRelativePath importId, "manifest.toml").Replace('\\', '/')

    let private conversationsRelativePath importId =
        Path.Combine(importRootRelativePath importId, "conversations.toml").Replace('\\', '/')

    let private boolText =
        function
        | true -> Some "true"
        | false -> Some "false"

    let private tryParseTimestamp value =
        value
        |> Option.bind (fun (rawValue: string) ->
            match DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private tryParseBool value =
        value
        |> Option.bind (fun (rawValue: string) ->
            match Boolean.TryParse(rawValue) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private conversationLabel (value: NormalizedImportSnapshotConversation) =
        value.Title |> Option.defaultValue value.ProviderConversationId

    let private conversationDeltaLabel (value: NormalizedImportSnapshotConversationDelta) =
        value.CurrentTitle
        |> Option.orElse value.BaseTitle
        |> Option.defaultValue value.ProviderConversationId

    let private reportSortKey (value: NormalizedImportSnapshotReport) =
        value.ImportedAt, ImportId.format value.ImportId

    let private buildComparisonReport baseReport currentReport limit =
        let baseByProviderConversationId =
            baseReport.Conversations
            |> List.map (fun conversation -> conversation.ProviderConversationId, conversation)
            |> Map.ofList

        let currentByProviderConversationId =
            currentReport.Conversations
            |> List.map (fun conversation -> conversation.ProviderConversationId, conversation)
            |> Map.ofList

        let addedConversations =
            currentReport.Conversations
            |> List.filter (fun conversation -> not (baseByProviderConversationId.ContainsKey conversation.ProviderConversationId))
            |> List.sortBy conversationLabel

        let removedConversations =
            baseReport.Conversations
            |> List.filter (fun conversation -> not (currentByProviderConversationId.ContainsKey conversation.ProviderConversationId))
            |> List.sortBy conversationLabel

        let changedConversations =
            baseReport.Conversations
            |> List.choose (fun baseConversation ->
                match currentByProviderConversationId.TryFind baseConversation.ProviderConversationId with
                | Some currentConversation
                    when baseConversation.CanonicalConversationId <> currentConversation.CanonicalConversationId
                         || baseConversation.Title <> currentConversation.Title
                         || baseConversation.IsArchived <> currentConversation.IsArchived
                         || baseConversation.OccurredAt <> currentConversation.OccurredAt
                         || baseConversation.MessageCount <> currentConversation.MessageCount
                         || baseConversation.ArtifactReferenceCount <> currentConversation.ArtifactReferenceCount ->
                    Some
                        { ProviderConversationId = baseConversation.ProviderConversationId
                          BaseCanonicalConversationId = baseConversation.CanonicalConversationId
                          CurrentCanonicalConversationId = currentConversation.CanonicalConversationId
                          BaseTitle = baseConversation.Title
                          CurrentTitle = currentConversation.Title
                          BaseIsArchived = baseConversation.IsArchived
                          CurrentIsArchived = currentConversation.IsArchived
                          BaseOccurredAt = baseConversation.OccurredAt
                          CurrentOccurredAt = currentConversation.OccurredAt
                          BaseMessageCount = baseConversation.MessageCount
                          CurrentMessageCount = currentConversation.MessageCount
                          BaseArtifactReferenceCount = baseConversation.ArtifactReferenceCount
                          CurrentArtifactReferenceCount = currentConversation.ArtifactReferenceCount }
                | Some _
                | None -> None)
            |> List.sortBy conversationDeltaLabel

        let unchangedConversationCount =
            baseReport.Conversations
            |> List.filter (fun baseConversation ->
                match currentByProviderConversationId.TryFind baseConversation.ProviderConversationId with
                | Some currentConversation ->
                    baseConversation.CanonicalConversationId = currentConversation.CanonicalConversationId
                    && baseConversation.Title = currentConversation.Title
                    && baseConversation.IsArchived = currentConversation.IsArchived
                    && baseConversation.OccurredAt = currentConversation.OccurredAt
                    && baseConversation.MessageCount = currentConversation.MessageCount
                    && baseConversation.ArtifactReferenceCount = currentConversation.ArtifactReferenceCount
                | None -> false)
            |> List.length

        { BaseImportId = baseReport.ImportId
          CurrentImportId = currentReport.ImportId
          BaseProvider = baseReport.Provider
          CurrentProvider = currentReport.Provider
          BaseWindow = baseReport.Window
          CurrentWindow = currentReport.Window
          BaseImportedAt = baseReport.ImportedAt
          CurrentImportedAt = currentReport.ImportedAt
          AddedConversationCount = addedConversations.Length
          RemovedConversationCount = removedConversations.Length
          ChangedConversationCount = changedConversations.Length
          UnchangedConversationCount = unchangedConversationCount
          AddedConversations = addedConversations |> List.truncate limit
          RemovedConversations = removedConversations |> List.truncate limit
          ChangedConversations = changedConversations |> List.truncate limit }

    let private renderConversationSnapshot (snapshot: NormalizedImportSnapshot) =
        let builder = create ()
        appendInt builder "schema_version" 1
        appendString builder "snapshot_kind" "normalized_import_snapshot_conversations"
        appendString builder "import_id" (ImportId.format snapshot.ImportId)
        appendString builder "provider" snapshot.Provider
        appendStringOption builder "window_kind" snapshot.Window
        appendTimestamp builder "imported_at" snapshot.ImportedAt
        appendStringOption builder "normalization_version" snapshot.NormalizationVersion
        appendStringOption builder "source_artifact_relative_path" snapshot.SourceArtifactRelativePath
        match snapshot.LogosMetadata with
        | Some value ->
            appendString builder "logos_source_system" value.SourceSystem
            appendString builder "logos_intake_channel" value.IntakeChannel
            appendString builder "logos_primary_signal_kind" value.PrimarySignalKind
            appendStringList builder "logos_related_signal_kinds" value.RelatedSignalKinds
            appendString builder "logos_entry_pool" value.EntryPool
            appendString builder "logos_sensitivity" value.HandlingPolicy.Sensitivity
            appendString builder "logos_sharing_scope" value.HandlingPolicy.SharingScope
            appendString builder "logos_sanitization_status" value.HandlingPolicy.SanitizationStatus
            appendString builder "logos_retention_class" value.HandlingPolicy.RetentionClass
        | None -> ()
        appendInt builder "conversation_count" snapshot.Conversations.Length
        appendInt builder "message_count" (snapshot.Conversations |> List.sumBy (fun value -> value.MessageCount))
        appendInt builder "artifact_reference_count" (snapshot.Conversations |> List.sumBy (fun value -> value.ArtifactReferenceCount))

        snapshot.Conversations
        |> List.sortBy conversationLabel
        |> List.iter (fun conversation ->
            appendBlank builder
            appendArrayTableHeader builder "conversations"
            appendString builder "canonical_conversation_id" conversation.CanonicalConversationId
            appendString builder "provider_conversation_id" conversation.ProviderConversationId
            appendStringOption builder "title" conversation.Title
            appendBoolOption builder "is_archived" conversation.IsArchived
            appendTimestampOption builder "occurred_at" conversation.OccurredAt
            appendInt builder "message_count" conversation.MessageCount
            appendInt builder "artifact_reference_count" conversation.ArtifactReferenceCount)

        render builder

    let private renderManifest (snapshot: NormalizedImportSnapshot) =
        let builder = create ()
        appendInt builder "schema_version" 1
        appendString builder "snapshot_kind" "normalized_import_snapshot"
        appendString builder "import_id" (ImportId.format snapshot.ImportId)
        appendString builder "provider" snapshot.Provider
        appendStringOption builder "window_kind" snapshot.Window
        appendTimestamp builder "imported_at" snapshot.ImportedAt
        appendStringOption builder "normalization_version" snapshot.NormalizationVersion
        appendStringOption builder "source_artifact_relative_path" snapshot.SourceArtifactRelativePath
        appendString builder "conversations_relative_path" (conversationsRelativePath snapshot.ImportId)
        match snapshot.LogosMetadata with
        | Some value ->
            appendBlank builder
            appendTableHeader builder "logos"
            appendString builder "source_system" value.SourceSystem
            appendString builder "intake_channel" value.IntakeChannel
            appendString builder "primary_signal_kind" value.PrimarySignalKind
            appendStringList builder "related_signal_kinds" value.RelatedSignalKinds
            appendString builder "entry_pool" value.EntryPool
            appendBlank builder
            appendTableHeader builder "logos.handling_policy"
            appendString builder "sensitivity" value.HandlingPolicy.Sensitivity
            appendString builder "sharing_scope" value.HandlingPolicy.SharingScope
            appendString builder "sanitization_status" value.HandlingPolicy.SanitizationStatus
            appendString builder "retention_class" value.HandlingPolicy.RetentionClass
        | None -> ()
        appendBlank builder
        appendTableHeader builder "counts"
        appendInt builder "conversations_seen" snapshot.Conversations.Length
        appendInt builder "messages_seen" (snapshot.Conversations |> List.sumBy (fun value -> value.MessageCount))
        appendInt builder "artifacts_referenced" (snapshot.Conversations |> List.sumBy (fun value -> value.ArtifactReferenceCount))
        render builder

    /// <summary>
    /// Writes one normalized import snapshot under snapshots/imports/&lt;import-id&gt;/.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="snapshot">The normalized import snapshot derived from one parsed provider payload.</param>
    /// <returns>The relative paths and totals written for the snapshot.</returns>
    let write eventStoreRoot (snapshot: NormalizedImportSnapshot) =
        let rootPath = Path.Combine(Path.GetFullPath(eventStoreRoot), importRootRelativePath snapshot.ImportId)
        Directory.CreateDirectory(rootPath) |> ignore

        let manifestRelative = manifestRelativePath snapshot.ImportId
        let conversationsRelative = conversationsRelativePath snapshot.ImportId
        let manifestAbsolute = Path.Combine(Path.GetFullPath(eventStoreRoot), manifestRelative)
        let conversationsAbsolute = Path.Combine(Path.GetFullPath(eventStoreRoot), conversationsRelative)

        File.WriteAllText(conversationsAbsolute, renderConversationSnapshot snapshot)
        File.WriteAllText(manifestAbsolute, renderManifest snapshot)

        { ManifestRelativePath = manifestRelative
          ConversationsRelativePath = conversationsRelative
          ConversationCount = snapshot.Conversations.Length
          MessageCount = snapshot.Conversations |> List.sumBy (fun value -> value.MessageCount)
          ArtifactReferenceCount = snapshot.Conversations |> List.sumBy (fun value -> value.ArtifactReferenceCount) }

    let private tryLoadConversationList path =
        if File.Exists(path) then
            let document = File.ReadAllText(path) |> TomlDocument.parse

            document
            |> TomlDocument.tableArray "conversations"
            |> List.map (fun table ->
                let canonicalConversationId =
                    match table.TryGetValue("canonical_conversation_id") with
                    | true, value -> value
                    | false, _ -> failwith $"Missing canonical_conversation_id in {path}"

                let providerConversationId =
                    match table.TryGetValue("provider_conversation_id") with
                    | true, value -> value
                    | false, _ -> failwith $"Missing provider_conversation_id in {path}"

                let title =
                    match table.TryGetValue("title") with
                    | true, value -> Some value
                    | false, _ -> None

                let isArchived =
                    match table.TryGetValue("is_archived") with
                    | true, value -> Some value
                    | false, _ -> None
                    |> tryParseBool

                let occurredAt =
                    match table.TryGetValue("occurred_at") with
                    | true, value -> Some value
                    | false, _ -> None
                    |> tryParseTimestamp

                let messageCount =
                    match table.TryGetValue("message_count") with
                    | true, value -> Int32.Parse(value, CultureInfo.InvariantCulture)
                    | false, _ -> failwith $"Missing message_count in {path}"

                let artifactReferenceCount =
                    match table.TryGetValue("artifact_reference_count") with
                    | true, value -> Int32.Parse(value, CultureInfo.InvariantCulture)
                    | false, _ -> failwith $"Missing artifact_reference_count in {path}"

                { CanonicalConversationId = canonicalConversationId
                  ProviderConversationId = providerConversationId
                  Title = title
                  IsArchived = isArchived
                  OccurredAt = occurredAt
                  MessageCount = messageCount
                  ArtifactReferenceCount = artifactReferenceCount })
            |> Some
        else
            None

    let private tryLoadLogosMetadata document =
        match TomlDocument.tryTableValue "logos" "source_system" document,
              TomlDocument.tryTableValue "logos" "intake_channel" document,
              TomlDocument.tryTableValue "logos" "primary_signal_kind" document,
              TomlDocument.tryTableValue "logos" "entry_pool" document,
              TomlDocument.tryTableValue "logos.handling_policy" "sensitivity" document,
              TomlDocument.tryTableValue "logos.handling_policy" "sharing_scope" document,
              TomlDocument.tryTableValue "logos.handling_policy" "sanitization_status" document,
              TomlDocument.tryTableValue "logos.handling_policy" "retention_class" document with
        | Some sourceSystem,
          Some intakeChannel,
          Some primarySignalKind,
          Some entryPool,
          Some sensitivity,
          Some sharingScope,
          Some sanitizationStatus,
          Some retentionClass ->
            Some
                { SourceSystem = sourceSystem
                  IntakeChannel = intakeChannel
                  PrimarySignalKind = primarySignalKind
                  RelatedSignalKinds = TomlDocument.tryTableStringList "logos" "related_signal_kinds" document |> Option.defaultValue []
                  HandlingPolicy =
                    { Sensitivity = sensitivity
                      SharingScope = sharingScope
                      SanitizationStatus = sanitizationStatus
                      RetentionClass = retentionClass }
                  EntryPool = entryPool }
        | _ -> None

    /// <summary>
    /// Loads one normalized import snapshot report when the persisted snapshot files exist.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="importId">The import whose normalized snapshot should be loaded.</param>
    /// <returns>The normalized snapshot report when the snapshot files exist.</returns>
    let tryLoadReport eventStoreRoot importId =
        let manifestRelative = manifestRelativePath importId
        let manifestAbsolute = Path.Combine(Path.GetFullPath(eventStoreRoot), manifestRelative)

        if File.Exists(manifestAbsolute) then
            let manifestDocument = File.ReadAllText(manifestAbsolute) |> TomlDocument.parse

            let provider =
                TomlDocument.tryScalar "provider" manifestDocument
                |> Option.defaultWith (fun () -> failwith $"Missing provider in {manifestAbsolute}")

            let importedAt =
                TomlDocument.tryScalar "imported_at" manifestDocument
                |> tryParseTimestamp
                |> Option.defaultWith (fun () -> failwith $"Missing or invalid imported_at in {manifestAbsolute}")

            let conversationsRelative =
                TomlDocument.tryScalar "conversations_relative_path" manifestDocument
                |> Option.defaultWith (fun () -> failwith $"Missing conversations_relative_path in {manifestAbsolute}")

            let conversationsAbsolute = Path.Combine(Path.GetFullPath(eventStoreRoot), conversationsRelative)

            match tryLoadConversationList conversationsAbsolute with
            | Some conversations ->
                Some
                    { ManifestRelativePath = manifestRelative
                      ConversationsRelativePath = conversationsRelative
                      ImportId = importId
                      Provider = provider
                      Window = TomlDocument.tryScalar "window_kind" manifestDocument
                      ImportedAt = importedAt
                      NormalizationVersion = TomlDocument.tryScalar "normalization_version" manifestDocument
                      SourceArtifactRelativePath = TomlDocument.tryScalar "source_artifact_relative_path" manifestDocument
                      LogosMetadata = tryLoadLogosMetadata manifestDocument
                      ConversationCount =
                        TomlDocument.tryTableValue "counts" "conversations_seen" manifestDocument
                        |> Option.map (fun value -> Int32.Parse(value, CultureInfo.InvariantCulture))
                        |> Option.defaultValue conversations.Length
                      MessageCount =
                        TomlDocument.tryTableValue "counts" "messages_seen" manifestDocument
                        |> Option.map (fun value -> Int32.Parse(value, CultureInfo.InvariantCulture))
                        |> Option.defaultValue (conversations |> List.sumBy (fun value -> value.MessageCount))
                      ArtifactReferenceCount =
                        TomlDocument.tryTableValue "counts" "artifacts_referenced" manifestDocument
                        |> Option.map (fun value -> Int32.Parse(value, CultureInfo.InvariantCulture))
                        |> Option.defaultValue (conversations |> List.sumBy (fun value -> value.ArtifactReferenceCount))
                      Conversations = conversations }
            | None -> None
        else
            None

    let private tryLoadAllReports eventStoreRoot =
        let snapshotsRoot = Path.Combine(Path.GetFullPath(eventStoreRoot), snapshotsRootRelativePath)

        if Directory.Exists(snapshotsRoot) then
            Directory.EnumerateDirectories(snapshotsRoot)
            |> Seq.choose (fun absolutePath ->
                let directoryName = Path.GetFileName(absolutePath)

                match Guid.TryParse(directoryName) with
                | true, _ -> tryLoadReport eventStoreRoot (ImportId.parse directoryName)
                | false, _ -> None)
            |> Seq.sortBy reportSortKey
            |> Seq.toList
        else
            []

    /// <summary>
    /// Compares two normalized import snapshots keyed by provider-native conversation identity.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="baseImportId">The older or reference import.</param>
    /// <param name="currentImportId">The newer or comparison import.</param>
    /// <param name="limit">The maximum number of detailed rows to include in each result bucket.</param>
    /// <returns>The comparison report when both normalized snapshot files exist.</returns>
    let tryBuildComparisonReport eventStoreRoot baseImportId currentImportId limit =
        match tryLoadReport eventStoreRoot baseImportId, tryLoadReport eventStoreRoot currentImportId with
        | Some baseReport, Some currentReport ->
            Some (buildComparisonReport baseReport currentReport limit)
        | _ -> None

    /// <summary>
    /// Builds the chronological normalized import-snapshot history for one provider.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="provider">The provider slug to report, such as <c>chatgpt</c> or <c>claude</c>.</param>
    /// <param name="limit">The maximum number of newest history rows to include.</param>
    /// <returns>The provider history report when matching normalized snapshots exist.</returns>
    let tryBuildHistoryReport eventStoreRoot provider limit =
        let sortedProviderReports =
            tryLoadAllReports eventStoreRoot
            |> List.filter (fun report -> String.Equals(report.Provider, provider, StringComparison.Ordinal))
            |> List.sortBy reportSortKey

        match sortedProviderReports with
        | [] -> None
        | _ ->
            let historyEntries =
                sortedProviderReports
                |> List.mapi (fun index currentReport ->
                    let deltaFromPrevious =
                        match index with
                        | 0 -> None
                        | _ ->
                            let previousReport = sortedProviderReports[index - 1]
                            let comparison = buildComparisonReport previousReport currentReport 0

                            Some
                                { PreviousImportId = comparison.BaseImportId
                                  PreviousImportedAt = comparison.BaseImportedAt
                                  AddedConversationCount = comparison.AddedConversationCount
                                  RemovedConversationCount = comparison.RemovedConversationCount
                                  ChangedConversationCount = comparison.ChangedConversationCount
                                  UnchangedConversationCount = comparison.UnchangedConversationCount }

                    { ImportId = currentReport.ImportId
                      ImportedAt = currentReport.ImportedAt
                      Window = currentReport.Window
                      NormalizationVersion = currentReport.NormalizationVersion
                      SourceArtifactRelativePath = currentReport.SourceArtifactRelativePath
                      ConversationCount = currentReport.ConversationCount
                      MessageCount = currentReport.MessageCount
                      ArtifactReferenceCount = currentReport.ArtifactReferenceCount
                      DeltaFromPrevious = deltaFromPrevious })

            let reportedEntries =
                let skipCount = max 0 (historyEntries.Length - limit)
                historyEntries |> List.skip skipCount

            Some
                { Provider = provider
                  AvailableSnapshotCount = historyEntries.Length
                  ReportedEntryCount = reportedEntries.Length
                  Entries = reportedEntries }
