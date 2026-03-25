namespace Nexus.EventStore

open System
open System.Globalization
open System.IO
open System.Security.Cryptography
open Nexus.Domain

/// <summary>
/// Captures the canonical import counts recorded for one provider import.
/// </summary>
type CurrentIngestionImportCounts =
    { ConversationsSeen: int
      MessagesSeen: int
      ArtifactsReferenced: int
      NewEventsAppended: int
      DuplicatesSkipped: int
      RevisionsObserved: int
      ReparseObservationsAppended: int }

/// <summary>
/// Describes the newest known import for one provider in the current event store.
/// </summary>
type CurrentIngestionProviderEntry =
    { Provider: string
      ImportId: ImportId
      ImportedAt: DateTimeOffset
      SourceAcquisition: string option
      Window: string option
      NormalizationVersion: string option
      RootArtifactRelativePath: string option
      RootArtifactExists: bool option
      RootArtifactSha256: string option
      Counts: CurrentIngestionImportCounts
      SnapshotAvailable: bool
      SnapshotConversationCount: int option
      SnapshotMessageCount: int option
      SnapshotArtifactReferenceCount: int option }

/// <summary>
/// Summarizes the latest known ingestion state across providers.
/// </summary>
type CurrentIngestionReport =
    { ImportManifestCount: int
      ProviderCount: int
      Entries: CurrentIngestionProviderEntry list
      MissingKnownProviders: string list }

/// <summary>
/// Builds a current-ingestion view from import manifests and normalized snapshots.
/// </summary>
/// <remarks>
/// Canonical history remains the authority. This report is an operational read model over the latest known import for each provider.
/// Full workflow notes: docs/how-to/report-current-ingestion.md
/// </remarks>
[<RequireQualifiedAccess>]
module CurrentIngestion =
    type private ImportManifestInfo =
        { Provider: string
          ImportId: ImportId
          ImportedAt: DateTimeOffset
          SourceAcquisition: string option
          Window: string option
          NormalizationVersion: string option
          RootArtifactRelativePath: string option
          Counts: CurrentIngestionImportCounts }

    let private knownProviders =
        [ "chatgpt"; "claude"; "grok"; "codex" ]

    let private tryParseTimestamp value =
        value
        |> Option.bind (fun (rawValue: string) ->
            match DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private tryParseInt value =
        value
        |> Option.bind (fun (rawValue: string) ->
            match Int32.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private sha256ForFile path =
        use stream = File.OpenRead(path)
        SHA256.HashData(stream) |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

    let private importSortKey (value: ImportManifestInfo) =
        value.ImportedAt, ImportId.format value.ImportId

    let private providerOrderKey provider =
        match knownProviders |> List.tryFindIndex ((=) provider) with
        | Some index -> index, provider
        | None -> Int32.MaxValue, provider

    let private tryLoadImportManifestInfo path =
        let document = File.ReadAllText(path) |> TomlDocument.parse

        match TomlDocument.tryScalar "provider" document, TomlDocument.tryScalar "imported_at" document |> tryParseTimestamp with
        | Some provider, Some importedAt ->
            let importId = ImportId.parse (Path.GetFileNameWithoutExtension(path))

            Some
                { Provider = provider
                  ImportId = importId
                  ImportedAt = importedAt
                  SourceAcquisition = TomlDocument.tryScalar "source_acquisition" document
                  Window = TomlDocument.tryScalar "window_kind" document
                  NormalizationVersion = TomlDocument.tryScalar "normalization_version" document
                  RootArtifactRelativePath = TomlDocument.tryTableValue "root_artifact" "relative_path" document
                  Counts =
                    { ConversationsSeen = TomlDocument.tryTableValue "counts" "conversations_seen" document |> tryParseInt |> Option.defaultValue 0
                      MessagesSeen = TomlDocument.tryTableValue "counts" "messages_seen" document |> tryParseInt |> Option.defaultValue 0
                      ArtifactsReferenced = TomlDocument.tryTableValue "counts" "artifacts_referenced" document |> tryParseInt |> Option.defaultValue 0
                      NewEventsAppended = TomlDocument.tryTableValue "counts" "new_events_appended" document |> tryParseInt |> Option.defaultValue 0
                      DuplicatesSkipped = TomlDocument.tryTableValue "counts" "duplicates_skipped" document |> tryParseInt |> Option.defaultValue 0
                      RevisionsObserved = TomlDocument.tryTableValue "counts" "revisions_observed" document |> tryParseInt |> Option.defaultValue 0
                      ReparseObservationsAppended =
                        TomlDocument.tryTableValue "counts" "reparse_observations_appended" document
                        |> tryParseInt
                        |> Option.defaultValue 0 } }
        | _ -> None

    let private loadImportManifests eventStoreRoot =
        let importsRoot = Path.Combine(Path.GetFullPath(eventStoreRoot), "imports")

        if Directory.Exists(importsRoot) then
            Directory.EnumerateFiles(importsRoot, "*.toml", SearchOption.TopDirectoryOnly)
            |> Seq.sort
            |> Seq.choose tryLoadImportManifestInfo
            |> Seq.toList
        else
            []

    /// <summary>
    /// Builds the latest-known ingestion status per provider from import manifests and normalized snapshots.
    /// </summary>
    /// <param name="eventStoreRoot">The event-store root that contains canonical import manifests.</param>
    /// <param name="objectsRoot">The objects root used to resolve preserved raw artifacts for hashing.</param>
    /// <returns>A current-ingestion report, including providers with missing imports.</returns>
    let buildReport eventStoreRoot objectsRoot =
        let importManifests = loadImportManifests eventStoreRoot

        let latestByProvider =
            importManifests
            |> Seq.groupBy (fun value -> value.Provider)
            |> Seq.map (fun (provider, values) -> provider, (values |> Seq.sortBy importSortKey |> Seq.last))
            |> Seq.toList

        let entries =
            latestByProvider
            |> List.sortBy (fun (provider, _) -> providerOrderKey provider)
            |> List.map (fun (_, value) ->
                let snapshot = ImportSnapshots.tryLoadReport eventStoreRoot value.ImportId

                let rootArtifactExists, rootArtifactSha256 =
                    match value.RootArtifactRelativePath with
                    | Some relativePath ->
                        let absolutePath =
                            Path.Combine(Path.GetFullPath(objectsRoot), relativePath.Replace('/', Path.DirectorySeparatorChar))

                        if File.Exists(absolutePath) then
                            Some true, Some(sha256ForFile absolutePath)
                        else
                            Some false, None
                    | None -> None, None

                { Provider = value.Provider
                  ImportId = value.ImportId
                  ImportedAt = value.ImportedAt
                  SourceAcquisition = value.SourceAcquisition
                  Window = value.Window
                  NormalizationVersion = value.NormalizationVersion
                  RootArtifactRelativePath = value.RootArtifactRelativePath
                  RootArtifactExists = rootArtifactExists
                  RootArtifactSha256 = rootArtifactSha256
                  Counts = value.Counts
                  SnapshotAvailable = snapshot.IsSome
                  SnapshotConversationCount = snapshot |> Option.map (fun report -> report.ConversationCount)
                  SnapshotMessageCount = snapshot |> Option.map (fun report -> report.MessageCount)
                  SnapshotArtifactReferenceCount = snapshot |> Option.map (fun report -> report.ArtifactReferenceCount) })

        let missingKnownProviders =
            knownProviders
            |> List.filter (fun provider -> entries |> List.exists (fun entry -> entry.Provider = provider) |> not)

        { ImportManifestCount = importManifests.Length
          ProviderCount = entries.Length
          Entries = entries
          MissingKnownProviders = missingKnownProviders }
