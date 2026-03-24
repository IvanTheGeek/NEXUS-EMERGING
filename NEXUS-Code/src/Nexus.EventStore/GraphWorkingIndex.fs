namespace Nexus.EventStore

open System
open System.Globalization
open System.IO
open Microsoft.Data.Sqlite
open Nexus.Domain

/// <summary>
/// Summarizes one predicate count inside a graph working import slice.
/// </summary>
type WorkingGraphSlicePredicateCount =
    { Predicate: string
      Count: int }

/// <summary>
/// Summarizes one import-local graph working slice from the persisted SQLite index.
/// </summary>
type WorkingGraphSliceReport =
    { IndexRelativePath: string
      ImportId: ImportId
      Provider: string option
      Window: string option
      ImportedAt: DateTimeOffset option
      MaterializedAt: DateTimeOffset
      CanonicalEventCount: int
      GraphAssertionCount: int
      DistinctSubjectCount: int
      NodeRefAssertionCount: int
      LiteralAssertionCount: int
      WorkingRootRelativePath: string
      ManifestRelativePath: string
      PredicateCounts: WorkingGraphSlicePredicateCount list }

type private ImportManifestMetadata =
    { Provider: string option
      Window: string option
      ImportedAt: DateTimeOffset option }

type private IndexedAssertionRow =
    { FactId: string
      Subject: string
      Predicate: string
      ObjectKind: string
      ObjectValueType: string option
      ObjectValue: string
      DomainId: string option
      BoundedContextId: string option
      LensId: string option }

/// <summary>
/// Maintains a persisted SQLite index over graph working import slices.
/// </summary>
/// <remarks>
/// This is a rebuildable working index for the derived graph layer.
/// It is not canonical truth.
/// Full notes: docs/decisions/0006-storage-roles-by-bounded-context.md
/// </remarks>
[<RequireQualifiedAccess>]
module GraphWorkingIndex =
    let private indexRelativePath = Path.Combine("graph", "working", "index", "graph-working.sqlite")

    let private normalizePath (path: string) =
        path.Replace('\\', '/')

    let private indexAbsolutePath eventStoreRoot =
        Path.Combine(Path.GetFullPath(eventStoreRoot), indexRelativePath)

    let private importManifestAbsolutePath eventStoreRoot importId =
        Path.Combine(Path.GetFullPath(eventStoreRoot), "imports", $"{ImportId.format importId}.toml")

    let private dbValue value =
        match value with
        | Some boxedValue -> box boxedValue
        | None -> box DBNull.Value

    let private parseTimestamp (value: string) =
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)

    let private tryParseTimestamp (value: string option) =
        value
        |> Option.bind (fun rawValue ->
            match DateTimeOffset.TryParse(rawValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
            | true, parsedValue -> Some parsedValue
            | false, _ -> None)

    let private loadImportManifestMetadata eventStoreRoot importId =
        let absolutePath = importManifestAbsolutePath eventStoreRoot importId

        if File.Exists(absolutePath) then
            let document = File.ReadAllText(absolutePath) |> TomlDocument.parse

            { Provider = TomlDocument.tryScalar "provider" document
              Window = TomlDocument.tryScalar "window_kind" document
              ImportedAt = TomlDocument.tryScalar "imported_at" document |> tryParseTimestamp }
        else
            { Provider = None
              Window = None
              ImportedAt = None }

    let private assertionRowFromPath absolutePath =
        let document = File.ReadAllText(absolutePath) |> TomlDocument.parse

        let factId =
            TomlDocument.tryScalar "fact_id" document
            |> Option.defaultWith (fun () -> failwith $"Missing fact_id in graph assertion: {absolutePath}")

        let subject =
            TomlDocument.tryScalar "subject_node_id" document
            |> Option.defaultWith (fun () -> failwith $"Missing subject_node_id in graph assertion: {absolutePath}")

        let predicate =
            TomlDocument.tryScalar "predicate" document
            |> Option.defaultWith (fun () -> failwith $"Missing predicate in graph assertion: {absolutePath}")

        let domainId = TomlDocument.tryScalar "domain_id" document
        let boundedContextId = TomlDocument.tryScalar "bounded_context_id" document
        let lensId = TomlDocument.tryScalar "lens_id" document

        match TomlDocument.tryTableValue "object" "kind" document with
        | Some "node_ref" ->
            let nodeId =
                TomlDocument.tryTableValue "object" "node_id" document
                |> Option.defaultWith (fun () -> failwith $"Missing object.node_id in graph assertion: {absolutePath}")

            { FactId = factId
              Subject = subject
              Predicate = predicate
              ObjectKind = "node_ref"
              ObjectValueType = None
              ObjectValue = nodeId
              DomainId = domainId
              BoundedContextId = boundedContextId
              LensId = lensId }
        | Some "literal" ->
            let valueType =
                TomlDocument.tryTableValue "object" "value_type" document
                |> Option.defaultWith (fun () -> failwith $"Missing object.value_type in graph assertion: {absolutePath}")

            let objectValue =
                TomlDocument.tryTableValue "object" "value" document
                |> Option.defaultWith (fun () -> failwith $"Missing object.value in graph assertion: {absolutePath}")

            match valueType with
            | "string"
            | "int64"
            | "decimal"
            | "bool"
            | "timestamp" ->
                { FactId = factId
                  Subject = subject
                  Predicate = predicate
                  ObjectKind = "literal"
                  ObjectValueType = Some valueType
                  ObjectValue = objectValue
                  DomainId = domainId
                  BoundedContextId = boundedContextId
                  LensId = lensId }
            | unsupportedValueType ->
                failwith $"Unsupported graph assertion literal value_type '{unsupportedValueType}' in {absolutePath}"
        | Some unsupportedKind ->
            failwith $"Unsupported graph assertion object kind '{unsupportedKind}' in {absolutePath}"
        | None ->
            failwith $"Missing object.kind in graph assertion: {absolutePath}"

    let private createConnection absolutePath =
        let builder = SqliteConnectionStringBuilder()
        builder.DataSource <- absolutePath
        let connection = new SqliteConnection(builder.ConnectionString)
        connection.Open()
        connection

    let private executeNonQuery (connection: SqliteConnection) (sql: string) =
        use command = connection.CreateCommand()
        command.CommandText <- sql
        command.ExecuteNonQuery() |> ignore

    let private ensureSchema (connection: SqliteConnection) =
        executeNonQuery connection "PRAGMA foreign_keys = ON;"
        executeNonQuery connection """
CREATE TABLE IF NOT EXISTS import_batches (
    import_id TEXT NOT NULL PRIMARY KEY,
    provider TEXT NULL,
    window_kind TEXT NULL,
    imported_at TEXT NULL,
    materialized_at TEXT NOT NULL,
    canonical_event_count INTEGER NOT NULL,
    graph_assertion_count INTEGER NOT NULL,
    materializer_version TEXT NOT NULL,
    working_root_relative_path TEXT NOT NULL,
    manifest_relative_path TEXT NOT NULL,
    import_manifest_relative_path TEXT NOT NULL
);"""
        executeNonQuery connection """
CREATE TABLE IF NOT EXISTS assertions (
    import_id TEXT NOT NULL,
    fact_id TEXT NOT NULL,
    subject TEXT NOT NULL,
    predicate TEXT NOT NULL,
    object_kind TEXT NOT NULL,
    object_value_type TEXT NULL,
    object_value TEXT NOT NULL,
    domain_id TEXT NULL,
    bounded_context_id TEXT NULL,
    lens_id TEXT NULL,
    PRIMARY KEY (import_id, fact_id),
    FOREIGN KEY (import_id) REFERENCES import_batches(import_id) ON DELETE CASCADE
);"""
        executeNonQuery connection "CREATE INDEX IF NOT EXISTS idx_assertions_import_id ON assertions(import_id);"
        executeNonQuery connection "CREATE INDEX IF NOT EXISTS idx_assertions_subject ON assertions(import_id, subject);"
        executeNonQuery connection "CREATE INDEX IF NOT EXISTS idx_assertions_predicate ON assertions(import_id, predicate);"

    let private withWritableConnection eventStoreRoot action =
        let absolutePath = indexAbsolutePath eventStoreRoot
        let directory = Path.GetDirectoryName(absolutePath)

        if not (String.IsNullOrWhiteSpace(directory)) then
            Directory.CreateDirectory(directory) |> ignore

        use connection = createConnection absolutePath
        ensureSchema connection
        action connection

    let private withReadableConnection eventStoreRoot action =
        let absolutePath = indexAbsolutePath eventStoreRoot

        if File.Exists(absolutePath) then
            use connection = createConnection absolutePath
            ensureSchema connection
            action connection |> Some
        else
            None

    /// <summary>
    /// Returns the stable relative path for the persisted graph working SQLite index.
    /// </summary>
    let relativePath =
        normalizePath indexRelativePath

    /// <summary>
    /// Refreshes one import-local graph working slice inside the persisted SQLite index.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="result">The import-batch materialization result whose assertion files should be indexed.</param>
    /// <returns>The stable relative path to the SQLite index file.</returns>
    let refreshImportBatch eventStoreRoot (result: GraphMaterialization.ImportBatchResult) =
        let importManifestMetadata = loadImportManifestMetadata eventStoreRoot result.ImportId

        withWritableConnection eventStoreRoot (fun connection ->
            use transaction = connection.BeginTransaction()

            use upsertBatch = connection.CreateCommand()
            upsertBatch.Transaction <- transaction
            upsertBatch.CommandText <-
                """
INSERT INTO import_batches (
    import_id,
    provider,
    window_kind,
    imported_at,
    materialized_at,
    canonical_event_count,
    graph_assertion_count,
    materializer_version,
    working_root_relative_path,
    manifest_relative_path,
    import_manifest_relative_path
) VALUES (
    $import_id,
    $provider,
    $window_kind,
    $imported_at,
    $materialized_at,
    $canonical_event_count,
    $graph_assertion_count,
    $materializer_version,
    $working_root_relative_path,
    $manifest_relative_path,
    $import_manifest_relative_path
)
ON CONFLICT(import_id) DO UPDATE SET
    provider = excluded.provider,
    window_kind = excluded.window_kind,
    imported_at = excluded.imported_at,
    materialized_at = excluded.materialized_at,
    canonical_event_count = excluded.canonical_event_count,
    graph_assertion_count = excluded.graph_assertion_count,
    materializer_version = excluded.materializer_version,
    working_root_relative_path = excluded.working_root_relative_path,
    manifest_relative_path = excluded.manifest_relative_path,
    import_manifest_relative_path = excluded.import_manifest_relative_path;
"""
            upsertBatch.Parameters.AddWithValue("$import_id", ImportId.format result.ImportId) |> ignore
            upsertBatch.Parameters.AddWithValue("$provider", dbValue importManifestMetadata.Provider) |> ignore
            upsertBatch.Parameters.AddWithValue("$window_kind", dbValue importManifestMetadata.Window) |> ignore
            upsertBatch.Parameters.AddWithValue("$imported_at", dbValue (importManifestMetadata.ImportedAt |> Option.map (fun value -> value.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)))) |> ignore
            upsertBatch.Parameters.AddWithValue("$materialized_at", result.MaterializedAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)) |> ignore
            upsertBatch.Parameters.AddWithValue("$canonical_event_count", result.CanonicalEventCount) |> ignore
            upsertBatch.Parameters.AddWithValue("$graph_assertion_count", result.GraphAssertionCount) |> ignore
            upsertBatch.Parameters.AddWithValue("$materializer_version", result.MaterializerVersion) |> ignore
            upsertBatch.Parameters.AddWithValue("$working_root_relative_path", normalizePath (Path.GetDirectoryName(result.ManifestRelativePath))) |> ignore
            upsertBatch.Parameters.AddWithValue("$manifest_relative_path", result.ManifestRelativePath) |> ignore
            upsertBatch.Parameters.AddWithValue("$import_manifest_relative_path", normalizePath (Path.Combine("imports", $"{ImportId.format result.ImportId}.toml"))) |> ignore
            upsertBatch.ExecuteNonQuery() |> ignore

            use deleteAssertions = connection.CreateCommand()
            deleteAssertions.Transaction <- transaction
            deleteAssertions.CommandText <- "DELETE FROM assertions WHERE import_id = $import_id;"
            deleteAssertions.Parameters.AddWithValue("$import_id", ImportId.format result.ImportId) |> ignore
            deleteAssertions.ExecuteNonQuery() |> ignore

            use insertAssertion = connection.CreateCommand()
            insertAssertion.Transaction <- transaction
            insertAssertion.CommandText <-
                """
INSERT INTO assertions (
    import_id,
    fact_id,
    subject,
    predicate,
    object_kind,
    object_value_type,
    object_value,
    domain_id,
    bounded_context_id,
    lens_id
) VALUES (
    $import_id,
    $fact_id,
    $subject,
    $predicate,
    $object_kind,
    $object_value_type,
    $object_value,
    $domain_id,
    $bounded_context_id,
    $lens_id
);
"""

            let importIdValue = ImportId.format result.ImportId
            let importIdParameter = insertAssertion.Parameters.Add("$import_id", SqliteType.Text)
            let factIdParameter = insertAssertion.Parameters.Add("$fact_id", SqliteType.Text)
            let subjectParameter = insertAssertion.Parameters.Add("$subject", SqliteType.Text)
            let predicateParameter = insertAssertion.Parameters.Add("$predicate", SqliteType.Text)
            let objectKindParameter = insertAssertion.Parameters.Add("$object_kind", SqliteType.Text)
            let objectValueTypeParameter = insertAssertion.Parameters.Add("$object_value_type", SqliteType.Text)
            let objectValueParameter = insertAssertion.Parameters.Add("$object_value", SqliteType.Text)
            let domainIdParameter = insertAssertion.Parameters.Add("$domain_id", SqliteType.Text)
            let boundedContextIdParameter = insertAssertion.Parameters.Add("$bounded_context_id", SqliteType.Text)
            let lensIdParameter = insertAssertion.Parameters.Add("$lens_id", SqliteType.Text)

            for relativePath in result.AssertionPaths do
                let absolutePath = Path.Combine(eventStoreRoot, relativePath)
                let row = assertionRowFromPath absolutePath

                importIdParameter.Value <- importIdValue
                factIdParameter.Value <- row.FactId
                subjectParameter.Value <- row.Subject
                predicateParameter.Value <- row.Predicate
                objectKindParameter.Value <- row.ObjectKind
                objectValueTypeParameter.Value <- dbValue row.ObjectValueType
                objectValueParameter.Value <- row.ObjectValue
                domainIdParameter.Value <- dbValue row.DomainId
                boundedContextIdParameter.Value <- dbValue row.BoundedContextId
                lensIdParameter.Value <- dbValue row.LensId

                insertAssertion.ExecuteNonQuery() |> ignore

            transaction.Commit())

        relativePath

    /// <summary>
    /// Builds a summary for one import-local graph working slice from the persisted SQLite index.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="importId">The import batch to summarize.</param>
    /// <param name="limit">The maximum number of predicate counts to include.</param>
    /// <returns>The slice summary when the import exists in the working index.</returns>
    let tryBuildImportSliceReport eventStoreRoot importId limit =
        withReadableConnection eventStoreRoot (fun connection ->
            use batchCommand = connection.CreateCommand()
            batchCommand.CommandText <-
                """
SELECT
    provider,
    window_kind,
    imported_at,
    materialized_at,
    canonical_event_count,
    graph_assertion_count,
    working_root_relative_path,
    manifest_relative_path
FROM import_batches
WHERE import_id = $import_id;
"""
            batchCommand.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore

            use batchReader = batchCommand.ExecuteReader()

            if batchReader.Read() then
                let provider =
                    if batchReader.IsDBNull(0) then None else Some (batchReader.GetString(0))

                let window =
                    if batchReader.IsDBNull(1) then None else Some (batchReader.GetString(1))

                let importedAt =
                    if batchReader.IsDBNull(2) then
                        None
                    else
                        Some (parseTimestamp (batchReader.GetString(2)))

                let materializedAt = parseTimestamp (batchReader.GetString(3))
                let canonicalEventCount = batchReader.GetInt32(4)
                let graphAssertionCount = batchReader.GetInt32(5)
                let workingRootRelativePath = batchReader.GetString(6)
                let manifestRelativePath = batchReader.GetString(7)

                use summaryCommand = connection.CreateCommand()
                summaryCommand.CommandText <-
                    """
SELECT
    COUNT(DISTINCT subject),
    SUM(CASE WHEN object_kind = 'node_ref' THEN 1 ELSE 0 END),
    SUM(CASE WHEN object_kind = 'literal' THEN 1 ELSE 0 END)
FROM assertions
WHERE import_id = $import_id;
"""
                summaryCommand.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore

                use summaryReader = summaryCommand.ExecuteReader()
                let mutable distinctSubjectCount = 0
                let mutable nodeRefAssertionCount = 0
                let mutable literalAssertionCount = 0

                if summaryReader.Read() then
                    distinctSubjectCount <- if summaryReader.IsDBNull(0) then 0 else summaryReader.GetInt32(0)
                    nodeRefAssertionCount <- if summaryReader.IsDBNull(1) then 0 else summaryReader.GetInt32(1)
                    literalAssertionCount <- if summaryReader.IsDBNull(2) then 0 else summaryReader.GetInt32(2)

                use predicateCommand = connection.CreateCommand()
                predicateCommand.CommandText <-
                    """
SELECT predicate, COUNT(*) AS assertion_count
FROM assertions
WHERE import_id = $import_id
GROUP BY predicate
ORDER BY assertion_count DESC, predicate ASC
LIMIT $limit;
"""
                predicateCommand.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore
                predicateCommand.Parameters.AddWithValue("$limit", limit) |> ignore

                use predicateReader = predicateCommand.ExecuteReader()
                let predicateCounts = ResizeArray<WorkingGraphSlicePredicateCount>()

                while predicateReader.Read() do
                    predicateCounts.Add
                        { Predicate = predicateReader.GetString(0)
                          Count = predicateReader.GetInt32(1) }

                Some
                    { IndexRelativePath = relativePath
                      ImportId = importId
                      Provider = provider
                      Window = window
                      ImportedAt = importedAt
                      MaterializedAt = materializedAt
                      CanonicalEventCount = canonicalEventCount
                      GraphAssertionCount = graphAssertionCount
                      DistinctSubjectCount = distinctSubjectCount
                      NodeRefAssertionCount = nodeRefAssertionCount
                      LiteralAssertionCount = literalAssertionCount
                      WorkingRootRelativePath = workingRootRelativePath
                      ManifestRelativePath = manifestRelativePath
                      PredicateCounts = predicateCounts |> Seq.toList }
            else
                None)
        |> Option.flatten
