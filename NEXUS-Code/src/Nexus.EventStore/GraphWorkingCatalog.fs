namespace Nexus.EventStore

open System
open System.Globalization
open System.IO
open Nexus.Domain

/// <summary>
/// Summarizes one import-batch-local graph working batch.
/// </summary>
type WorkingImportCatalogEntry =
    { ImportId: ImportId
      MaterializedAt: DateTimeOffset
      CanonicalEventCount: int
      GraphAssertionCount: int
      DerivationElapsedMilliseconds: int64 option
      TotalElapsedMilliseconds: int64 option
      MaterializerVersion: string
      WorkingRootRelativePath: string
      ManifestRelativePath: string
      ImportManifestRelativePath: string }

/// <summary>
/// Represents the durable catalog over graph working import batches.
/// </summary>
type WorkingImportCatalog =
    { CatalogRelativePath: string
      GeneratedAt: DateTimeOffset option
      Entries: WorkingImportCatalogEntry list }

/// <summary>
/// Represents one operator-facing graph working batch report item.
/// </summary>
type WorkingImportReportItem =
    { ImportId: ImportId
      Provider: string option
      Window: string option
      ImportedAt: DateTimeOffset option
      MaterializedAt: DateTimeOffset
      CanonicalEventCount: int
      GraphAssertionCount: int
      MaterializerVersion: string
      ManifestRelativePath: string
      WorkingRootRelativePath: string
      ImportManifestRelativePath: string }

/// <summary>
/// Summarizes the current graph working import batches for operator-facing reporting.
/// </summary>
type WorkingImportReport =
    { CatalogRelativePath: string
      WorkingBatchCount: int
      TotalCanonicalEvents: int
      TotalGraphAssertions: int
      ProviderCounts: (string * int) list
      Items: WorkingImportReportItem list }

/// <summary>
/// Reads, writes, and summarizes the graph working-batch catalog.
/// </summary>
/// <remarks>
/// Full working-layer notes: docs/nexus-graph-materialization-plan.md
/// </remarks>
[<RequireQualifiedAccess>]
module GraphWorkingCatalog =
    let private catalogVersion = "graph-working-import-catalog-v1"
    let private catalogRelativePath = Path.Combine("graph", "working", "catalog", "import-batches.toml")

    let private normalizePath (path: string) =
        path.Replace('\\', '/')

    let private importsRootAbsolutePath eventStoreRoot =
        Path.Combine(Path.GetFullPath(eventStoreRoot), "graph", "working", "imports")

    let private catalogAbsolutePath eventStoreRoot =
        Path.Combine(Path.GetFullPath(eventStoreRoot), catalogRelativePath)

    let private tryParseInt (value: string option) =
        value
        |> Option.bind (fun raw ->
            match Int32.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private tryParseInt64 (value: string option) =
        value
        |> Option.bind (fun raw ->
            match Int64.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private tryParseTimestamp (value: string option) =
        value
        |> Option.bind (fun raw ->
            match DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private tryGetValue key (table: Collections.Generic.Dictionary<string, string>) =
        match table.TryGetValue(key) with
        | true, value -> Some value
        | false, _ -> None

    let private parseEntryTable (table: Collections.Generic.Dictionary<string, string>) =
        match table.TryGetValue("import_id"), table.TryGetValue("materialized_at"), table.TryGetValue("materializer"), table.TryGetValue("working_root"), table.TryGetValue("manifest_relative_path"), table.TryGetValue("import_manifest_relative_path") with
        | (true, importIdRaw), (true, materializedAtRaw), (true, materializer), (true, workingRoot), (true, manifestRelativePath), (true, importManifestRelativePath) ->
            match DateTimeOffset.TryParse(materializedAtRaw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
            | true, materializedAt ->
                Some
                    { ImportId = ImportId.parse importIdRaw
                      MaterializedAt = materializedAt
                      CanonicalEventCount = tryGetValue "canonical_event_count" table |> tryParseInt |> Option.defaultValue 0
                      GraphAssertionCount = tryGetValue "graph_assertions_written" table |> tryParseInt |> Option.defaultValue 0
                      DerivationElapsedMilliseconds = tryGetValue "derivation_elapsed_ms" table |> tryParseInt64
                      TotalElapsedMilliseconds = tryGetValue "total_elapsed_ms" table |> tryParseInt64
                      MaterializerVersion = materializer
                      WorkingRootRelativePath = workingRoot
                      ManifestRelativePath = manifestRelativePath
                      ImportManifestRelativePath = importManifestRelativePath }
            | false, _ -> None
        | (false, _), _, _, _, _, _
        | _, (false, _), _, _, _, _
        | _, _, (false, _), _, _, _
        | _, _, _, (false, _), _, _
        | _, _, _, _, (false, _), _
        | _, _, _, _, _, (false, _) -> None

    let private loadCatalogDocument eventStoreRoot =
        let absolutePath = catalogAbsolutePath eventStoreRoot

        if File.Exists(absolutePath) then
            File.ReadAllText(absolutePath) |> TomlDocument.parse |> Some
        else
            None

    let private loadCatalogEntriesFromDocument document =
        TomlDocument.tableArray "imports" document
        |> List.choose parseEntryTable
        |> List.sortByDescending (fun entry -> entry.MaterializedAt, ImportId.format entry.ImportId)

    let private scanImportBatchManifests eventStoreRoot =
        let root = importsRootAbsolutePath eventStoreRoot

        if Directory.Exists(root) then
            Directory.EnumerateFiles(root, "manifest.toml", SearchOption.AllDirectories)
            |> Seq.choose (fun path ->
                let document = File.ReadAllText(path) |> TomlDocument.parse

                match TomlDocument.tryScalar "mode" document with
                | Some "incremental_import_batch" ->
                    let table = Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal)

                    for key in
                        [ "import_id"
                          "materialized_at"
                          "canonical_event_count"
                          "graph_assertions_written"
                          "derivation_elapsed_ms"
                          "total_elapsed_ms"
                          "materializer"
                          "working_root"
                          "manifest_relative_path" ] do
                        match TomlDocument.tryScalar key document with
                        | Some value -> table[key] <- value
                        | None -> ()

                    let importId =
                        match TomlDocument.tryScalar "import_id" document with
                        | Some value -> value
                        | None -> Path.GetFileName(Path.GetDirectoryName(path))

                    table["import_manifest_relative_path"] <- normalizePath (Path.Combine("imports", $"{importId}.toml"))
                    parseEntryTable table
                | Some _
                | None -> None)
            |> Seq.sortByDescending (fun entry -> entry.MaterializedAt, ImportId.format entry.ImportId)
            |> Seq.toList
        else
            []

    let private writeCatalog eventStoreRoot generatedAt (entries: WorkingImportCatalogEntry list) =
        let builder = create ()
        appendString builder "catalog_version" catalogVersion
        appendTimestamp builder "generated_at" generatedAt
        appendInt builder "entry_count" entries.Length
        appendBlank builder

        for entry in entries do
            appendArrayTableHeader builder "imports"
            appendString builder "import_id" (ImportId.format entry.ImportId)
            appendTimestamp builder "materialized_at" entry.MaterializedAt
            appendInt builder "canonical_event_count" entry.CanonicalEventCount
            appendInt builder "graph_assertions_written" entry.GraphAssertionCount
            appendInt64Option builder "derivation_elapsed_ms" entry.DerivationElapsedMilliseconds
            appendInt64Option builder "total_elapsed_ms" entry.TotalElapsedMilliseconds
            appendString builder "materializer" entry.MaterializerVersion
            appendString builder "working_root" entry.WorkingRootRelativePath
            appendString builder "manifest_relative_path" entry.ManifestRelativePath
            appendString builder "import_manifest_relative_path" entry.ImportManifestRelativePath
            appendBlank builder

        let absolutePath = catalogAbsolutePath eventStoreRoot
        let directory = Path.GetDirectoryName(absolutePath)

        if not (String.IsNullOrWhiteSpace(directory)) then
            Directory.CreateDirectory(directory) |> ignore

        File.WriteAllText(absolutePath, render builder)

    let private importManifestInfo eventStoreRoot (entry: WorkingImportCatalogEntry) =
        let absolutePath = Path.Combine(Path.GetFullPath(eventStoreRoot), entry.ImportManifestRelativePath)

        if File.Exists(absolutePath) then
            let document = File.ReadAllText(absolutePath) |> TomlDocument.parse

            {|
                Provider = TomlDocument.tryScalar "provider" document
                Window = TomlDocument.tryScalar "window_kind" document
                ImportedAt = TomlDocument.tryScalar "imported_at" document |> tryParseTimestamp
            |}
            |> Some
        else
            None

    /// <summary>
    /// Loads the graph working catalog, falling back to manifest scanning when the catalog file does not yet exist.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <returns>The current graph working import catalog.</returns>
    let load eventStoreRoot =
        match loadCatalogDocument eventStoreRoot with
        | Some document ->
            { CatalogRelativePath = normalizePath catalogRelativePath
              GeneratedAt = TomlDocument.tryScalar "generated_at" document |> tryParseTimestamp
              Entries = loadCatalogEntriesFromDocument document }
        | None ->
            { CatalogRelativePath = normalizePath catalogRelativePath
              GeneratedAt = None
              Entries = scanImportBatchManifests eventStoreRoot }

    /// <summary>
    /// Upserts one import-batch graph working batch into the durable catalog.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="result">The import-batch materialization result to record.</param>
    /// <returns>The relative path to the catalog file.</returns>
    let upsertImportBatch eventStoreRoot (result: GraphMaterialization.ImportBatchResult) =
        let existing = load eventStoreRoot
        let entry =
            { ImportId = result.ImportId
              MaterializedAt = result.MaterializedAt
              CanonicalEventCount = result.CanonicalEventCount
              GraphAssertionCount = result.GraphAssertionCount
              DerivationElapsedMilliseconds = Some (int64 result.DerivationElapsed.TotalMilliseconds)
              TotalElapsedMilliseconds = Some (int64 result.TotalElapsed.TotalMilliseconds)
              MaterializerVersion = result.MaterializerVersion
              WorkingRootRelativePath = normalizePath (Path.GetDirectoryName(result.ManifestRelativePath))
              ManifestRelativePath = result.ManifestRelativePath
              ImportManifestRelativePath = normalizePath (Path.Combine("imports", $"{ImportId.format result.ImportId}.toml")) }

        let mergedEntries =
            existing.Entries
            |> List.filter (fun currentEntry -> currentEntry.ImportId <> result.ImportId)
            |> fun remainingEntries -> entry :: remainingEntries
            |> List.sortByDescending (fun currentEntry -> currentEntry.MaterializedAt, ImportId.format currentEntry.ImportId)

        writeCatalog eventStoreRoot DateTimeOffset.UtcNow mergedEntries
        normalizePath catalogRelativePath

    /// <summary>
    /// Builds an operator-facing report over graph working import batches.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="limit">The maximum number of detailed items to include.</param>
    /// <returns>A summarized report of the current graph working layer.</returns>
    /// <remarks>
    /// Full working-layer notes: docs/nexus-graph-materialization-plan.md
    /// </remarks>
    let buildReport eventStoreRoot limit =
        let catalog = load eventStoreRoot

        let reportItems : WorkingImportReportItem list =
            catalog.Entries
            |> List.sortByDescending (fun (entry: WorkingImportCatalogEntry) -> entry.MaterializedAt, ImportId.format entry.ImportId)
            |> List.map (fun (entry: WorkingImportCatalogEntry) ->
                match importManifestInfo eventStoreRoot entry with
                | Some manifestInfo ->
                    { ImportId = entry.ImportId
                      Provider = manifestInfo.Provider
                      Window = manifestInfo.Window
                      ImportedAt = manifestInfo.ImportedAt
                      MaterializedAt = entry.MaterializedAt
                      CanonicalEventCount = entry.CanonicalEventCount
                      GraphAssertionCount = entry.GraphAssertionCount
                      MaterializerVersion = entry.MaterializerVersion
                      ManifestRelativePath = entry.ManifestRelativePath
                      WorkingRootRelativePath = entry.WorkingRootRelativePath
                      ImportManifestRelativePath = entry.ImportManifestRelativePath }
                | None ->
                    { ImportId = entry.ImportId
                      Provider = None
                      Window = None
                      ImportedAt = None
                      MaterializedAt = entry.MaterializedAt
                      CanonicalEventCount = entry.CanonicalEventCount
                      GraphAssertionCount = entry.GraphAssertionCount
                      MaterializerVersion = entry.MaterializerVersion
                      ManifestRelativePath = entry.ManifestRelativePath
                      WorkingRootRelativePath = entry.WorkingRootRelativePath
                      ImportManifestRelativePath = entry.ImportManifestRelativePath })

        let providerCounts =
            reportItems
            |> Seq.choose (fun item -> item.Provider)
            |> Seq.countBy id
            |> Seq.sortBy fst
            |> Seq.toList

        { CatalogRelativePath = normalizePath catalogRelativePath
          WorkingBatchCount = catalog.Entries.Length
          TotalCanonicalEvents = catalog.Entries |> List.sumBy (fun entry -> entry.CanonicalEventCount)
          TotalGraphAssertions = catalog.Entries |> List.sumBy (fun entry -> entry.GraphAssertionCount)
          ProviderCounts = providerCounts
          Items = reportItems |> List.truncate limit }
