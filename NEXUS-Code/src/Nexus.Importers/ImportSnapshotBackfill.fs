namespace Nexus.Importers

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open Nexus.Domain
open Nexus.EventStore

/// <summary>
/// Selects which imports should have normalized import snapshots rebuilt.
/// </summary>
type ImportSnapshotBackfillScope =
    | SpecificImport of ImportId
    | AllImports

/// <summary>
/// Requests a rebuild of normalized import snapshots from preserved raw provider exports.
/// </summary>
/// <remarks>
/// This rebuilds derived snapshot artifacts only. It does not append canonical events.
/// Full workflow notes: docs/how-to/rebuild-import-snapshots.md
/// </remarks>
type ImportSnapshotBackfillRequest =
    { EventStoreRoot: string
      ObjectsRoot: string
      Scope: ImportSnapshotBackfillScope
      Force: bool }

/// <summary>
/// Describes the outcome for one attempted import-snapshot rebuild.
/// </summary>
type ImportSnapshotBackfillOutcome =
    | Rebuilt
    | SkippedExisting
    | SkippedUnsupported
    | Failed

/// <summary>
/// Summarizes one import considered during snapshot rebuild.
/// </summary>
type ImportSnapshotBackfillImportResult =
    { ImportId: ImportId
      Provider: string option
      Outcome: ImportSnapshotBackfillOutcome
      ManifestRelativePath: string option
      ConversationsRelativePath: string option
      ConversationCount: int option
      MessageCount: int option
      ArtifactReferenceCount: int option
      Reason: string option }

/// <summary>
/// Summarizes one rebuild-import-snapshots run.
/// </summary>
type ImportSnapshotBackfillResult =
    { EventStoreRoot: string
      ObjectsRoot: string
      ScopeDescription: string
      ParserNormalizationVersion: string
      ProcessedCount: int
      RebuiltCount: int
      SkippedExistingCount: int
      SkippedUnsupportedCount: int
      FailedCount: int
      Imports: ImportSnapshotBackfillImportResult list }

/// <summary>
/// Rebuilds normalized import snapshots for older provider-export imports from preserved raw artifacts.
/// </summary>
[<RequireQualifiedAccess>]
module ImportSnapshotBackfill =
    /// <summary>
    /// Emits human-readable rebuild progress.
    /// </summary>
    type StatusReporter = string -> unit

    type private ImportManifestMetadata =
        { ImportId: ImportId
          Provider: ProviderKind option
          ProviderSlug: string option
          SourceAcquisition: string option
          Window: ImportWindowKind option
          ImportedAt: DateTimeOffset option
          RootArtifactRelativePath: string option }

    type private RawParseInputError =
        | UnsupportedImport of string
        | FailedInput of string

    let private currentNormalizationVersion = NormalizationNaming.current

    let private normalizePath (value: string) =
        value.Replace('\\', '/')

    let private emitStatus (status: StatusReporter) message =
        status message

    let private tryParseTimestamp value =
        value
        |> Option.bind (fun (rawValue: string) ->
            match DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private tryLoadImportManifestMetadata eventStoreRoot importId =
        let absolutePath =
            Path.Combine(Path.GetFullPath(eventStoreRoot), "imports", sprintf "%s.toml" (ImportId.format importId))

        if File.Exists(absolutePath) then
            let document = File.ReadAllText(absolutePath) |> TomlDocument.parse

            Some
                { ImportId = importId
                  Provider =
                    TomlDocument.tryScalar "provider" document
                    |> Option.bind ProviderNaming.tryParse
                  ProviderSlug = TomlDocument.tryScalar "provider" document
                  SourceAcquisition = TomlDocument.tryScalar "source_acquisition" document
                  Window =
                    TomlDocument.tryScalar "window_kind" document
                    |> Option.bind ImportWindowNaming.tryParse
                  ImportedAt = TomlDocument.tryScalar "imported_at" document |> tryParseTimestamp
                  RootArtifactRelativePath = TomlDocument.tryTableValue "root_artifact" "relative_path" document }
        else
            None

    let private loadSelectedManifests eventStoreRoot scope =
        match scope with
        | SpecificImport importId ->
            [ tryLoadImportManifestMetadata eventStoreRoot importId ]
            |> List.choose id
        | AllImports ->
            let importsRoot = Path.Combine(Path.GetFullPath(eventStoreRoot), "imports")

            if Directory.Exists(importsRoot) then
                Directory.EnumerateFiles(importsRoot, "*.toml", SearchOption.TopDirectoryOnly)
                |> Seq.sort
                |> Seq.choose (fun absolutePath ->
                    match Guid.TryParse(Path.GetFileNameWithoutExtension(absolutePath)) with
                    | true, _ ->
                        let importId = ImportId.parse (Path.GetFileNameWithoutExtension(absolutePath))
                        tryLoadImportManifestMetadata eventStoreRoot importId
                    | false, _ -> None)
                |> Seq.toList
            else
                []

    let private toSystemRelativePath (value: string) =
        value.Replace('/', Path.DirectorySeparatorChar)

    let private failedResult metadata reason =
        { ImportId = metadata.ImportId
          Provider = metadata.ProviderSlug
          Outcome = Failed
          ManifestRelativePath = None
          ConversationsRelativePath = None
          ConversationCount = None
          MessageCount = None
          ArtifactReferenceCount = None
          Reason = Some reason }

    let private unsupportedResult metadata reason =
        { ImportId = metadata.ImportId
          Provider = metadata.ProviderSlug
          Outcome = SkippedUnsupported
          ManifestRelativePath = None
          ConversationsRelativePath = None
          ConversationCount = None
          MessageCount = None
          ArtifactReferenceCount = None
          Reason = Some reason }

    let private tryBuildRawParseInput objectsRoot (metadata: ImportManifestMetadata) =
        match metadata.Provider, metadata.SourceAcquisition, metadata.RootArtifactRelativePath, metadata.ImportedAt with
        | None, _, _, _ ->
            Error (FailedInput "Import manifest is missing a supported provider value.")
        | Some _, None, _, _ ->
            Error (FailedInput "Import manifest is missing source_acquisition.")
        | Some _, Some "export_zip", None, _ ->
            Error (FailedInput "Import manifest is missing root_artifact.relative_path.")
        | Some _, Some "export_zip", _, None ->
            Error (FailedInput "Import manifest is missing imported_at.")
        | Some provider, Some "export_zip", Some rootArtifactRelativePath, Some importedAt ->
            match provider with
            | ChatGpt
            | Claude
            | Grok ->
                let zipAbsolutePath =
                    Path.Combine(Path.GetFullPath(objectsRoot), toSystemRelativePath rootArtifactRelativePath)

                if not (File.Exists(zipAbsolutePath)) then
                    Error (FailedInput (sprintf "Preserved provider export zip not found: %s" rootArtifactRelativePath))
                else
                    let archiveDirectoryAbsolutePath =
                        Path.GetDirectoryName(zipAbsolutePath)

                    let extractedDirectoryAbsolutePath =
                        Path.Combine(archiveDirectoryAbsolutePath, "extracted")

                    let extractedRelativePath =
                        normalizePath (Path.Combine(Path.GetDirectoryName(rootArtifactRelativePath), "extracted"))

                    if not (Directory.Exists(extractedDirectoryAbsolutePath)) then
                        Error (FailedInput (sprintf "Preserved extracted snapshot not found for import %s: %s" (ImportId.format metadata.ImportId) extractedRelativePath))
                    else
                        match ProviderAdapters.tryLocatePayload provider extractedDirectoryAbsolutePath with
                        | None ->
                            Error
                                (FailedInput
                                    (sprintf
                                        "Preserved %s provider payload not found for import %s under: %s"
                                        (ProviderNaming.slug provider)
                                        (ImportId.format metadata.ImportId)
                                        extractedRelativePath))
                        | Some providerPayloadLocation ->
                            let extractedFiles =
                                Directory.EnumerateFiles(extractedDirectoryAbsolutePath, "*", SearchOption.AllDirectories)
                                |> Seq.toList

                            let extractedEntries = extractedFiles.Length
                            let extractedNames =
                                extractedFiles
                                |> Seq.collect (fun absolutePath ->
                                    [ Path.GetFileName(absolutePath)
                                      Path.GetRelativePath(extractedDirectoryAbsolutePath, absolutePath) |> normalizePath ])
                                |> Seq.map (fun value -> value.Trim().ToLowerInvariant())
                                |> Set.ofSeq

                            Ok
                                (provider,
                                 metadata.Window,
                                 Path.GetFileName(zipAbsolutePath),
                                 FileInfo(zipAbsolutePath).Length,
                                 importedAt,
                                 rootArtifactRelativePath,
                                 extractedEntries,
                                 extractedNames,
                                 providerPayloadLocation.AbsolutePath)
            | Codex ->
                Error (UnsupportedImport "Codex local-session imports do not use provider-export normalized snapshots.")
            | OtherProvider value ->
                Error (UnsupportedImport (sprintf "Unsupported provider for snapshot rebuild: %s" value))
        | Some _, Some sourceAcquisition, _, _ ->
            Error (UnsupportedImport (sprintf "Only provider export imports are supported. Found source_acquisition=%s." sourceAcquisition))

    let private tryMapSnapshotConversationCanonicalId (index: ExistingEventStoreIndex) provider providerConversationId =
        let providerKey = ProviderKey.conversation provider providerConversationId

        match index.ConversationsByProviderKey.TryGetValue(providerKey) with
        | true, conversationId -> Some conversationId
        | false, _ -> None

    let private rebuildOne eventStoreRoot objectsRoot (index: ExistingEventStoreIndex) force (metadata: ImportManifestMetadata) =
        let existingSnapshot = ImportSnapshots.tryLoadReport eventStoreRoot metadata.ImportId

        if existingSnapshot.IsSome && not force then
            { ImportId = metadata.ImportId
              Provider = metadata.ProviderSlug
              Outcome = SkippedExisting
              ManifestRelativePath = existingSnapshot |> Option.map (fun value -> value.ManifestRelativePath)
              ConversationsRelativePath = existingSnapshot |> Option.map (fun value -> value.ConversationsRelativePath)
              ConversationCount = existingSnapshot |> Option.map (fun value -> value.ConversationCount)
              MessageCount = existingSnapshot |> Option.map (fun value -> value.MessageCount)
              ArtifactReferenceCount = existingSnapshot |> Option.map (fun value -> value.ArtifactReferenceCount)
              Reason = Some "Normalized import snapshot already exists. Use --force to rebuild it." }
        else
            try
                match tryBuildRawParseInput objectsRoot metadata with
                | Error (UnsupportedImport reason) ->
                    unsupportedResult metadata reason
                | Error (FailedInput reason) ->
                    failedResult metadata reason
                | Ok (provider, window, sourceFileName, sourceByteCount, importedAt, rootArtifactRelativePath, extractedEntries, extractedNames, conversationsJsonAbsolutePath) ->
                    let parsedImport =
                        ProviderAdapters.parse
                            provider
                            window
                            sourceFileName
                            sourceByteCount
                            extractedEntries
                            extractedNames
                            conversationsJsonAbsolutePath

                    let snapshotConversations =
                        parsedImport.Conversations
                        |> List.map (fun conversation ->
                            match tryMapSnapshotConversationCanonicalId index provider conversation.ProviderConversationId with
                            | Some canonicalConversationId ->
                                { CanonicalConversationId = ConversationId.format canonicalConversationId
                                  ProviderConversationId = conversation.ProviderConversationId
                                  Title = conversation.Title
                                  IsArchived = conversation.IsArchived
                                  OccurredAt = conversation.OccurredAt
                                  MessageCount = conversation.Messages.Length
                                  ArtifactReferenceCount =
                                    conversation.Messages
                                    |> List.sumBy (fun value -> value.ArtifactReferences.Length) }
                            | None ->
                                failwith
                                    (sprintf
                                        "Canonical conversation mapping not found for provider conversation %s while rebuilding import snapshot %s."
                                        conversation.ProviderConversationId
                                        (ImportId.format metadata.ImportId)))

                    let snapshot =
                        let logosMetadata = ProviderLogosImportMetadata.tryBuild provider

                        { ImportId = metadata.ImportId
                          Provider = ProviderNaming.slug provider
                          Window = window |> Option.map ImportWindowNaming.value
                          ImportedAt = importedAt
                          NormalizationVersion = Some (NormalizationNaming.value currentNormalizationVersion)
                          SourceArtifactRelativePath = Some rootArtifactRelativePath
                          LogosMetadata = logosMetadata
                          Conversations = snapshotConversations }

                    let writeResult = ImportSnapshots.write eventStoreRoot snapshot

                    { ImportId = metadata.ImportId
                      Provider = Some (ProviderNaming.slug provider)
                      Outcome = Rebuilt
                      ManifestRelativePath = Some writeResult.ManifestRelativePath
                      ConversationsRelativePath = Some writeResult.ConversationsRelativePath
                      ConversationCount = Some writeResult.ConversationCount
                      MessageCount = Some writeResult.MessageCount
                      ArtifactReferenceCount = Some writeResult.ArtifactReferenceCount
                      Reason =
                        if existingSnapshot.IsSome && force then
                            Some "Rebuilt normalized import snapshot from preserved raw export using the current parser rules."
                        else
                            Some "Created normalized import snapshot from preserved raw export using the current parser rules." }
            with ex ->
                failedResult metadata ex.Message

    /// <summary>
    /// Rebuilds normalized import snapshots from preserved provider-export artifacts, emitting progress messages.
    /// </summary>
    /// <param name="status">Receives human-readable workflow progress messages.</param>
    /// <param name="request">The event-store and object roots plus explicit rebuild scope.</param>
    /// <returns>A summary of rebuilt, skipped, and failed snapshot rebuild attempts.</returns>
    let runWithStatus (status: StatusReporter) (request: ImportSnapshotBackfillRequest) =
        let eventStoreRoot = Path.GetFullPath(request.EventStoreRoot)
        let objectsRoot = Path.GetFullPath(request.ObjectsRoot)
        let selectedManifests = loadSelectedManifests eventStoreRoot request.Scope
        let scopeDescription =
            match request.Scope with
            | SpecificImport importId -> sprintf "import %s" (ImportId.format importId)
            | AllImports -> "all imports"

        emitStatus status (sprintf "Preparing normalized import snapshot rebuild for %s." scopeDescription)

        let results =
            match request.Scope, selectedManifests with
            | SpecificImport importId, [] ->
                [ { ImportId = importId
                    Provider = None
                    Outcome = Failed
                    ManifestRelativePath = None
                    ConversationsRelativePath = None
                    ConversationCount = None
                    MessageCount = None
                    ArtifactReferenceCount = None
                    Reason = Some (sprintf "Import manifest not found for %s." (ImportId.format importId)) } ]
            | _, [] -> []
            | _, _ ->
                emitStatus status "Loading event-store index for canonical conversation mapping."
                let index = EventStoreIndex.load eventStoreRoot

                selectedManifests
                |> List.mapi (fun position metadata ->
                    emitStatus
                        status
                        (sprintf
                            "Rebuilding snapshot %d/%d for import %s."
                            (position + 1)
                            selectedManifests.Length
                            (ImportId.format metadata.ImportId))
                    rebuildOne eventStoreRoot objectsRoot index request.Force metadata)

        let rebuiltCount = results |> List.filter (fun item -> item.Outcome = Rebuilt) |> List.length
        let skippedExistingCount = results |> List.filter (fun item -> item.Outcome = SkippedExisting) |> List.length
        let skippedUnsupportedCount = results |> List.filter (fun item -> item.Outcome = SkippedUnsupported) |> List.length
        let failedCount = results |> List.filter (fun item -> item.Outcome = Failed) |> List.length

        emitStatus
            status
            (sprintf
                "Normalized import snapshot rebuild completed: %d rebuilt, %d existing skipped, %d unsupported skipped, %d failed."
                rebuiltCount
                skippedExistingCount
                skippedUnsupportedCount
                failedCount)

        { EventStoreRoot = eventStoreRoot
          ObjectsRoot = objectsRoot
          ScopeDescription = scopeDescription
          ParserNormalizationVersion = NormalizationNaming.value currentNormalizationVersion
          ProcessedCount = results.Length
          RebuiltCount = rebuiltCount
          SkippedExistingCount = skippedExistingCount
          SkippedUnsupportedCount = skippedUnsupportedCount
          FailedCount = failedCount
          Imports = results }

    /// <summary>
    /// Rebuilds normalized import snapshots from preserved provider-export artifacts.
    /// </summary>
    /// <param name="request">The event-store and object roots plus explicit rebuild scope.</param>
    /// <returns>A summary of rebuilt, skipped, and failed snapshot rebuild attempts.</returns>
    let run (request: ImportSnapshotBackfillRequest) =
        runWithStatus ignore request
