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

/// <summary>
/// Summarizes one graph node match discovered through the persisted SQLite working index.
/// </summary>
type WorkingGraphNodeMatch =
    { ImportId: ImportId
      Provider: string option
      Window: string option
      ImportedAt: DateTimeOffset option
      MaterializedAt: DateTimeOffset
      NodeId: string
      NodeKind: string option
      Title: string option
      Slug: string option
      SemanticRoles: string list
      MessageRoles: string list
      MatchReasons: string list }

/// <summary>
/// Describes one literal assertion attached directly to a node inside a graph working slice.
/// </summary>
type WorkingGraphNeighborhoodLiteral =
    { FactId: FactId
      Predicate: string
      ValueType: string option
      Value: string }

/// <summary>
/// Describes one node-to-node connection inside a graph working slice neighborhood.
/// </summary>
type WorkingGraphNeighborhoodConnection =
    { FactId: FactId
      Direction: string
      Predicate: string
      RelatedNodeId: string
      RelatedNodeKind: string option
      RelatedTitle: string option
      RelatedSlug: string option
      RelatedSemanticRoles: string list
      RelatedMessageRoles: string list }

/// <summary>
/// Summarizes the local neighborhood of one node inside a graph working import slice.
/// </summary>
type WorkingGraphNeighborhoodReport =
    { IndexRelativePath: string
      ImportId: ImportId
      Provider: string option
      Window: string option
      ImportedAt: DateTimeOffset option
      MaterializedAt: DateTimeOffset
      NodeId: string
      NodeKind: string option
      Title: string option
      Slug: string option
      SemanticRoles: string list
      MessageRoles: string list
      WorkingRootRelativePath: string
      ManifestRelativePath: string
      TotalLiteralAssertionCount: int
      TotalOutgoingConnectionCount: int
      TotalIncomingConnectionCount: int
      Literals: WorkingGraphNeighborhoodLiteral list
      OutgoingConnections: WorkingGraphNeighborhoodConnection list
      IncomingConnections: WorkingGraphNeighborhoodConnection list }

/// <summary>
/// Summarizes one conversation node inside an import-local graph working slice.
/// </summary>
type WorkingImportConversationSummary =
    { ConversationNodeId: string
      Title: string option
      Slug: string option
      SemanticRoles: string list
      MessageCount: int
      ArtifactCount: int }

/// <summary>
/// Summarizes the conversation nodes present inside one import-local graph working slice.
/// </summary>
type WorkingImportConversationReport =
    { IndexRelativePath: string
      ImportId: ImportId
      Provider: string option
      Window: string option
      ImportedAt: DateTimeOffset option
      MaterializedAt: DateTimeOffset
      WorkingRootRelativePath: string
      ManifestRelativePath: string
      ConversationCount: int
      Items: WorkingImportConversationSummary list }

/// <summary>
/// Describes one conversation whose contribution counts differ between two import-local graph working slices.
/// </summary>
type WorkingImportConversationDelta =
    { ConversationNodeId: string
      BaseTitle: string option
      CurrentTitle: string option
      BaseSlug: string option
      CurrentSlug: string option
      BaseSemanticRoles: string list
      CurrentSemanticRoles: string list
      BaseMessageCount: int
      CurrentMessageCount: int
      BaseArtifactCount: int
      CurrentArtifactCount: int }

/// <summary>
/// Compares the conversation contributions present in two import-local graph working slices.
/// </summary>
/// <remarks>
/// This compares batch-local working-slice contributions, not full provider snapshot truth.
/// Use it to understand what changed between two import batches inside the derived working layer.
/// </remarks>
type WorkingImportConversationComparisonReport =
    { IndexRelativePath: string
      BaseImportId: ImportId
      CurrentImportId: ImportId
      BaseProvider: string option
      CurrentProvider: string option
      BaseWindow: string option
      CurrentWindow: string option
      BaseImportedAt: DateTimeOffset option
      CurrentImportedAt: DateTimeOffset option
      BaseMaterializedAt: DateTimeOffset
      CurrentMaterializedAt: DateTimeOffset
      AddedConversationCount: int
      RemovedConversationCount: int
      ChangedConversationCount: int
      UnchangedConversationCount: int
      AddedConversations: WorkingImportConversationSummary list
      RemovedConversations: WorkingImportConversationSummary list
      ChangedConversations: WorkingImportConversationDelta list }

/// <summary>
/// Summarizes one full rebuild of the persisted graph working SQLite index.
/// </summary>
type WorkingGraphIndexRebuildResult =
    { IndexRelativePath: string
      CatalogRelativePath: string
      WorkingSliceCount: int
      GraphAssertionCount: int }

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

type private NodeSummary =
    { NodeId: string
      NodeKind: string option
      Title: string option
      Slug: string option
      SemanticRoles: string list
      MessageRoles: string list }

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

    /// <summary>
    /// Emits human-readable graph working-index rebuild progress.
    /// </summary>
    type StatusReporter = string -> unit

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

    let private readOptionalString (reader: SqliteDataReader) index =
        if reader.IsDBNull(index) then None else Some (reader.GetString(index))

    let private splitConcatenatedList (value: string option) =
        value
        |> Option.map (fun rawValue ->
            rawValue.Split(',', StringSplitOptions.RemoveEmptyEntries)
            |> Array.toList
            |> List.map (fun item -> item.Trim())
            |> List.filter (fun item -> not (String.IsNullOrWhiteSpace(item)))
            |> List.distinct)
        |> Option.defaultValue []

    let private conversationSummaryLabel (value: WorkingImportConversationSummary) =
        value.Title
        |> Option.orElse value.Slug
        |> Option.defaultValue value.ConversationNodeId

    let private conversationDeltaLabel (value: WorkingImportConversationDelta) =
        value.CurrentTitle
        |> Option.orElse value.BaseTitle
        |> Option.orElse value.CurrentSlug
        |> Option.orElse value.BaseSlug
        |> Option.defaultValue value.ConversationNodeId

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
        executeNonQuery connection "CREATE INDEX IF NOT EXISTS idx_assertions_object_value ON assertions(import_id, object_kind, object_value);"
        executeNonQuery connection "CREATE INDEX IF NOT EXISTS idx_assertions_literal_lookup ON assertions(import_id, predicate, object_kind, object_value);"

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

    let private timeSpanFromMilliseconds (value: int64 option) =
        value
        |> Option.map (fun milliseconds -> TimeSpan.FromMilliseconds(float milliseconds))
        |> Option.defaultValue TimeSpan.Zero

    let private importBatchResultFromCatalogEntry eventStoreRoot (entry: WorkingImportCatalogEntry) =
        let assertionsRootAbsolutePath =
            Path.Combine(Path.GetFullPath(eventStoreRoot), entry.WorkingRootRelativePath, "assertions")

        let assertionPaths =
            if Directory.Exists(assertionsRootAbsolutePath) then
                Directory.EnumerateFiles(assertionsRootAbsolutePath, "*.toml", SearchOption.AllDirectories)
                |> Seq.sort
                |> Seq.map (fun absolutePath -> Path.GetRelativePath(Path.GetFullPath(eventStoreRoot), absolutePath) |> normalizePath)
                |> Seq.toList
            else
                []

        let result: GraphMaterialization.ImportBatchResult =
            { ImportId = entry.ImportId
              CanonicalEventCount = entry.CanonicalEventCount
              GraphAssertionCount = assertionPaths.Length
              DerivationElapsed = timeSpanFromMilliseconds entry.DerivationElapsedMilliseconds
              TotalElapsed = timeSpanFromMilliseconds entry.TotalElapsedMilliseconds
              AssertionPaths = assertionPaths
              ManifestRelativePath = entry.ManifestRelativePath
              MaterializedAt = entry.MaterializedAt
              MaterializerVersion = entry.MaterializerVersion }

        result

    let private scalarCount (connection: SqliteConnection) (sql: string) (parameterSetter: SqliteCommand -> unit) =
        use command = connection.CreateCommand()
        command.CommandText <- sql
        parameterSetter command

        match command.ExecuteScalar() with
        | null
        | :? DBNull -> 0
        | :? int64 as value -> int value
        | :? int as value -> value
        | value -> Convert.ToInt32(value, CultureInfo.InvariantCulture)

    let private tryLoadNodeSummary (connection: SqliteConnection) importId nodeId =
        use command = connection.CreateCommand()
        command.CommandText <-
            """
SELECT
    COUNT(*),
    MAX(CASE WHEN predicate = 'has_node_kind' AND object_kind = 'literal' THEN object_value END),
    MAX(CASE WHEN predicate = 'has_title' AND object_kind = 'literal' THEN object_value END),
    MAX(CASE WHEN predicate = 'has_slug' AND object_kind = 'literal' THEN object_value END),
    GROUP_CONCAT(DISTINCT CASE WHEN predicate = 'has_semantic_role' AND object_kind = 'literal' THEN object_value END),
    GROUP_CONCAT(DISTINCT CASE WHEN predicate = 'has_role' AND object_kind = 'literal' THEN object_value END)
FROM assertions
WHERE import_id = $import_id
  AND subject = $node_id;
"""
        command.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore
        command.Parameters.AddWithValue("$node_id", nodeId) |> ignore

        use reader = command.ExecuteReader()

        if reader.Read() then
            let rowCount =
                if reader.IsDBNull(0) then
                    0
                else
                    int (reader.GetInt64(0))

            if rowCount > 0 then
                Some
                    { NodeId = nodeId
                      NodeKind = readOptionalString reader 1
                      Title = readOptionalString reader 2
                      Slug = readOptionalString reader 3
                      SemanticRoles = readOptionalString reader 4 |> splitConcatenatedList
                      MessageRoles = readOptionalString reader 5 |> splitConcatenatedList }
            else
                None
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
    /// Rebuilds the persisted SQLite working index from the current graph working import slices.
    /// </summary>
    /// <param name="status">Receives human-readable rebuild progress updates.</param>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <returns>A summary of the rebuilt working index contents.</returns>
    let rebuildFromCatalogWithStatus (status: StatusReporter) eventStoreRoot =
        let catalog = GraphWorkingCatalog.load eventStoreRoot
        let absolutePath = indexAbsolutePath eventStoreRoot

        if File.Exists(absolutePath) then
            status $"Removing previous graph working SQLite index at {absolutePath}"
            File.Delete(absolutePath)

        status $"Rebuilding graph working SQLite index from {catalog.Entries.Length} working slices."

        let mutable totalAssertionCount = 0

        for entry in catalog.Entries |> List.sortBy (fun currentEntry -> currentEntry.MaterializedAt, ImportId.format currentEntry.ImportId) do
            status $"Indexing working slice {ImportId.format entry.ImportId} from {entry.WorkingRootRelativePath}"
            let importBatchResult = importBatchResultFromCatalogEntry eventStoreRoot entry
            totalAssertionCount <- totalAssertionCount + importBatchResult.GraphAssertionCount
            refreshImportBatch eventStoreRoot importBatchResult |> ignore

        status $"Graph working SQLite index rebuilt: {catalog.Entries.Length} slices, {totalAssertionCount} assertions."

        { IndexRelativePath = relativePath
          CatalogRelativePath = catalog.CatalogRelativePath
          WorkingSliceCount = catalog.Entries.Length
          GraphAssertionCount = totalAssertionCount }

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

    /// <summary>
    /// Finds graph nodes in the SQLite working index by title/slug text and explicit role filters.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="importId">Optional import-batch filter.</param>
    /// <param name="provider">Optional provider filter.</param>
    /// <param name="matchText">Optional text filter applied to node titles and slugs.</param>
    /// <param name="semanticRole">Optional semantic-role filter.</param>
    /// <param name="messageRole">Optional message-role filter.</param>
    /// <param name="limit">The maximum number of matches to return.</param>
    /// <returns>The matching graph nodes from the persisted working index.</returns>
    let findNodes eventStoreRoot importId provider matchText semanticRole messageRole limit =
        withReadableConnection eventStoreRoot (fun connection ->
            use command = connection.CreateCommand()
            command.CommandText <-
                """
WITH node_matches AS (
    SELECT
        b.import_id,
        b.provider,
        b.window_kind,
        b.imported_at,
        b.materialized_at,
        a.subject AS node_id,
        MAX(CASE WHEN a.predicate = 'has_node_kind' AND a.object_kind = 'literal' THEN a.object_value END) AS node_kind,
        MAX(CASE WHEN a.predicate = 'has_title' AND a.object_kind = 'literal' THEN a.object_value END) AS title,
        MAX(CASE WHEN a.predicate = 'has_slug' AND a.object_kind = 'literal' THEN a.object_value END) AS slug,
        GROUP_CONCAT(DISTINCT CASE WHEN a.predicate = 'has_semantic_role' AND a.object_kind = 'literal' THEN a.object_value END) AS semantic_roles,
        GROUP_CONCAT(DISTINCT CASE WHEN a.predicate = 'has_role' AND a.object_kind = 'literal' THEN a.object_value END) AS message_roles,
        SUM(CASE WHEN a.predicate IN ('has_title', 'has_slug') AND a.object_kind = 'literal' AND lower(a.object_value) LIKE '%' || lower($match_text) || '%' THEN 1 ELSE 0 END) AS text_matches,
        SUM(CASE WHEN a.predicate = 'has_semantic_role' AND a.object_kind = 'literal' AND lower(a.object_value) = lower($semantic_role) THEN 1 ELSE 0 END) AS semantic_matches,
        SUM(CASE WHEN a.predicate = 'has_role' AND a.object_kind = 'literal' AND lower(a.object_value) = lower($message_role) THEN 1 ELSE 0 END) AS message_matches
    FROM assertions a
    INNER JOIN import_batches b ON b.import_id = a.import_id
    WHERE ($import_id IS NULL OR a.import_id = $import_id)
      AND ($provider IS NULL OR b.provider = $provider)
    GROUP BY
        b.import_id,
        b.provider,
        b.window_kind,
        b.imported_at,
        b.materialized_at,
        a.subject
)
SELECT
    import_id,
    provider,
    window_kind,
    imported_at,
    materialized_at,
    node_id,
    node_kind,
    title,
    slug,
    semantic_roles,
    message_roles,
    text_matches,
    semantic_matches,
    message_matches
FROM node_matches
WHERE ($match_text IS NULL OR text_matches > 0)
  AND ($semantic_role IS NULL OR semantic_matches > 0)
  AND ($message_role IS NULL OR message_matches > 0)
ORDER BY materialized_at DESC, COALESCE(title, slug, node_id) ASC
LIMIT $limit;
"""
            command.Parameters.AddWithValue("$import_id", dbValue (importId |> Option.map ImportId.format)) |> ignore
            command.Parameters.AddWithValue("$provider", dbValue provider) |> ignore
            command.Parameters.AddWithValue("$match_text", dbValue matchText) |> ignore
            command.Parameters.AddWithValue("$semantic_role", dbValue semanticRole) |> ignore
            command.Parameters.AddWithValue("$message_role", dbValue messageRole) |> ignore
            command.Parameters.AddWithValue("$limit", limit) |> ignore

            use reader = command.ExecuteReader()
            let matches = ResizeArray<WorkingGraphNodeMatch>()

            while reader.Read() do
                let textMatchCount =
                    if reader.IsDBNull(11) then
                        0
                    else
                        int (reader.GetInt64(11))

                let semanticMatchCount =
                    if reader.IsDBNull(12) then
                        0
                    else
                        int (reader.GetInt64(12))

                let messageMatchCount =
                    if reader.IsDBNull(13) then
                        0
                    else
                        int (reader.GetInt64(13))

                let matchReasons =
                    [ match matchText with
                      | Some _ when textMatchCount > 0 -> Some "title_or_slug"
                      | _ -> None
                      match semanticRole with
                      | Some _ when semanticMatchCount > 0 -> Some "semantic_role"
                      | _ -> None
                      match messageRole with
                      | Some _ when messageMatchCount > 0 -> Some "message_role"
                      | _ -> None ]
                    |> List.choose id

                matches.Add
                    { ImportId = ImportId.parse (reader.GetString(0))
                      Provider = readOptionalString reader 1
                      Window = readOptionalString reader 2
                      ImportedAt = readOptionalString reader 3 |> tryParseTimestamp
                      MaterializedAt = parseTimestamp (reader.GetString(4))
                      NodeId = reader.GetString(5)
                      NodeKind = readOptionalString reader 6
                      Title = readOptionalString reader 7
                      Slug = readOptionalString reader 8
                      SemanticRoles = readOptionalString reader 9 |> splitConcatenatedList
                      MessageRoles = readOptionalString reader 10 |> splitConcatenatedList
                      MatchReasons = matchReasons }

            matches |> Seq.toList)
        |> Option.defaultValue []

    /// <summary>
    /// Builds the local neighborhood of one node from the persisted SQLite working index.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="importId">The import batch whose working slice should be explored.</param>
    /// <param name="nodeId">The node whose local neighborhood should be reported.</param>
    /// <param name="limit">The maximum number of literals, incoming connections, and outgoing connections to include.</param>
    /// <returns>The local neighborhood report when both the slice and node exist in the working index.</returns>
    let tryBuildNeighborhoodReport eventStoreRoot importId nodeId limit =
        withReadableConnection eventStoreRoot (fun connection ->
            use batchCommand = connection.CreateCommand()
            batchCommand.CommandText <-
                """
SELECT
    provider,
    window_kind,
    imported_at,
    materialized_at,
    working_root_relative_path,
    manifest_relative_path
FROM import_batches
WHERE import_id = $import_id;
"""
            batchCommand.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore

            use batchReader = batchCommand.ExecuteReader()

            if batchReader.Read() then
                let provider = readOptionalString batchReader 0
                let window = readOptionalString batchReader 1
                let importedAt = readOptionalString batchReader 2 |> tryParseTimestamp
                let materializedAt = parseTimestamp (batchReader.GetString(3))
                let workingRootRelativePath = batchReader.GetString(4)
                let manifestRelativePath = batchReader.GetString(5)

                match tryLoadNodeSummary connection importId nodeId with
                | None -> None
                | Some summary ->
                    let totalLiteralAssertionCount =
                        scalarCount
                            connection
                            """
SELECT COUNT(*)
FROM assertions
WHERE import_id = $import_id
  AND subject = $node_id
  AND object_kind = 'literal';
"""
                            (fun command ->
                                command.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore
                                command.Parameters.AddWithValue("$node_id", nodeId) |> ignore)

                    let totalOutgoingConnectionCount =
                        scalarCount
                            connection
                            """
SELECT COUNT(*)
FROM assertions
WHERE import_id = $import_id
  AND subject = $node_id
  AND object_kind = 'node_ref';
"""
                            (fun command ->
                                command.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore
                                command.Parameters.AddWithValue("$node_id", nodeId) |> ignore)

                    let totalIncomingConnectionCount =
                        scalarCount
                            connection
                            """
SELECT COUNT(*)
FROM assertions
WHERE import_id = $import_id
  AND object_kind = 'node_ref'
  AND object_value = $node_id;
"""
                            (fun command ->
                                command.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore
                                command.Parameters.AddWithValue("$node_id", nodeId) |> ignore)

                    let summaryCache = System.Collections.Generic.Dictionary<string, NodeSummary>(StringComparer.Ordinal)
                    summaryCache[nodeId] <- summary

                    let getNodeSummary currentNodeId =
                        match summaryCache.TryGetValue(currentNodeId) with
                        | true, cached -> Some cached
                        | false, _ ->
                            let loaded = tryLoadNodeSummary connection importId currentNodeId

                            loaded
                            |> Option.iter (fun currentSummary -> summaryCache[currentNodeId] <- currentSummary)

                            loaded

                    use literalCommand = connection.CreateCommand()
                    literalCommand.CommandText <-
                        """
SELECT fact_id, predicate, object_value_type, object_value
FROM assertions
WHERE import_id = $import_id
  AND subject = $node_id
  AND object_kind = 'literal'
ORDER BY predicate ASC, object_value ASC
LIMIT $limit;
"""
                    literalCommand.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore
                    literalCommand.Parameters.AddWithValue("$node_id", nodeId) |> ignore
                    literalCommand.Parameters.AddWithValue("$limit", limit) |> ignore

                    use literalReader = literalCommand.ExecuteReader()
                    let literals = ResizeArray<WorkingGraphNeighborhoodLiteral>()

                    while literalReader.Read() do
                        literals.Add
                            { FactId = FactId.parse (literalReader.GetString(0))
                              Predicate = literalReader.GetString(1)
                              ValueType = readOptionalString literalReader 2
                              Value = literalReader.GetString(3) }

                    use outgoingCommand = connection.CreateCommand()
                    outgoingCommand.CommandText <-
                        """
SELECT fact_id, predicate, object_value
FROM assertions
WHERE import_id = $import_id
  AND subject = $node_id
  AND object_kind = 'node_ref'
ORDER BY predicate ASC, object_value ASC
LIMIT $limit;
"""
                    outgoingCommand.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore
                    outgoingCommand.Parameters.AddWithValue("$node_id", nodeId) |> ignore
                    outgoingCommand.Parameters.AddWithValue("$limit", limit) |> ignore

                    use outgoingReader = outgoingCommand.ExecuteReader()
                    let outgoingConnections = ResizeArray<WorkingGraphNeighborhoodConnection>()

                    while outgoingReader.Read() do
                        let relatedNodeId = outgoingReader.GetString(2)

                        let relatedSummary =
                            getNodeSummary relatedNodeId
                            |> Option.defaultValue
                                { NodeId = relatedNodeId
                                  NodeKind = None
                                  Title = None
                                  Slug = None
                                  SemanticRoles = []
                                  MessageRoles = [] }

                        outgoingConnections.Add
                            { FactId = FactId.parse (outgoingReader.GetString(0))
                              Direction = "outgoing"
                              Predicate = outgoingReader.GetString(1)
                              RelatedNodeId = relatedNodeId
                              RelatedNodeKind = relatedSummary.NodeKind
                              RelatedTitle = relatedSummary.Title
                              RelatedSlug = relatedSummary.Slug
                              RelatedSemanticRoles = relatedSummary.SemanticRoles
                              RelatedMessageRoles = relatedSummary.MessageRoles }

                    use incomingCommand = connection.CreateCommand()
                    incomingCommand.CommandText <-
                        """
SELECT fact_id, subject, predicate
FROM assertions
WHERE import_id = $import_id
  AND object_kind = 'node_ref'
  AND object_value = $node_id
ORDER BY predicate ASC, subject ASC
LIMIT $limit;
"""
                    incomingCommand.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore
                    incomingCommand.Parameters.AddWithValue("$node_id", nodeId) |> ignore
                    incomingCommand.Parameters.AddWithValue("$limit", limit) |> ignore

                    use incomingReader = incomingCommand.ExecuteReader()
                    let incomingConnections = ResizeArray<WorkingGraphNeighborhoodConnection>()

                    while incomingReader.Read() do
                        let relatedNodeId = incomingReader.GetString(1)

                        let relatedSummary =
                            getNodeSummary relatedNodeId
                            |> Option.defaultValue
                                { NodeId = relatedNodeId
                                  NodeKind = None
                                  Title = None
                                  Slug = None
                                  SemanticRoles = []
                                  MessageRoles = [] }

                        incomingConnections.Add
                            { FactId = FactId.parse (incomingReader.GetString(0))
                              Direction = "incoming"
                              Predicate = incomingReader.GetString(2)
                              RelatedNodeId = relatedNodeId
                              RelatedNodeKind = relatedSummary.NodeKind
                              RelatedTitle = relatedSummary.Title
                              RelatedSlug = relatedSummary.Slug
                              RelatedSemanticRoles = relatedSummary.SemanticRoles
                              RelatedMessageRoles = relatedSummary.MessageRoles }

                    Some
                        { IndexRelativePath = relativePath
                          ImportId = importId
                          Provider = provider
                          Window = window
                          ImportedAt = importedAt
                          MaterializedAt = materializedAt
                          NodeId = summary.NodeId
                          NodeKind = summary.NodeKind
                          Title = summary.Title
                          Slug = summary.Slug
                          SemanticRoles = summary.SemanticRoles
                          MessageRoles = summary.MessageRoles
                          WorkingRootRelativePath = workingRootRelativePath
                          ManifestRelativePath = manifestRelativePath
                          TotalLiteralAssertionCount = totalLiteralAssertionCount
                          TotalOutgoingConnectionCount = totalOutgoingConnectionCount
                          TotalIncomingConnectionCount = totalIncomingConnectionCount
                          Literals = literals |> Seq.toList
                          OutgoingConnections = outgoingConnections |> Seq.toList
                          IncomingConnections = incomingConnections |> Seq.toList }
            else
                None)
        |> Option.flatten

    /// <summary>
    /// Builds a conversation-centric summary for one import-local graph working slice from the persisted SQLite index.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="importId">The import batch whose conversation nodes should be summarized.</param>
    /// <param name="limit">The maximum number of conversation rows to include.</param>
    /// <returns>The conversation summary when the import exists in the working index.</returns>
    let tryBuildImportConversationReport eventStoreRoot importId limit =
        withReadableConnection eventStoreRoot (fun connection ->
            use batchCommand = connection.CreateCommand()
            batchCommand.CommandText <-
                """
SELECT
    provider,
    window_kind,
    imported_at,
    materialized_at,
    working_root_relative_path,
    manifest_relative_path
FROM import_batches
WHERE import_id = $import_id;
"""
            batchCommand.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore

            use batchReader = batchCommand.ExecuteReader()

            if batchReader.Read() then
                let provider = readOptionalString batchReader 0
                let window = readOptionalString batchReader 1
                let importedAt = readOptionalString batchReader 2 |> tryParseTimestamp
                let materializedAt = parseTimestamp (batchReader.GetString(3))
                let workingRootRelativePath = batchReader.GetString(4)
                let manifestRelativePath = batchReader.GetString(5)

                let conversationCount =
                    scalarCount
                        connection
                        """
SELECT COUNT(*)
FROM (
    SELECT subject
    FROM assertions
    WHERE import_id = $import_id
      AND predicate = 'has_node_kind'
      AND object_kind = 'literal'
      AND object_value = 'conversation_node'
    GROUP BY subject
);
"""
                        (fun command ->
                            command.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore)

                use command = connection.CreateCommand()
                command.CommandText <-
                    """
WITH conversation_nodes AS (
    SELECT
        a.subject AS conversation_node_id,
        MAX(CASE WHEN a.predicate = 'has_title' AND a.object_kind = 'literal' THEN a.object_value END) AS title,
        MAX(CASE WHEN a.predicate = 'has_slug' AND a.object_kind = 'literal' THEN a.object_value END) AS slug,
        GROUP_CONCAT(DISTINCT CASE WHEN a.predicate = 'has_semantic_role' AND a.object_kind = 'literal' THEN a.object_value END) AS semantic_roles
    FROM assertions a
    WHERE a.import_id = $import_id
    GROUP BY a.subject
    HAVING SUM(CASE WHEN a.predicate = 'has_node_kind' AND a.object_kind = 'literal' AND a.object_value = 'conversation_node' THEN 1 ELSE 0 END) > 0
),
message_counts AS (
    SELECT
        belongs.object_value AS conversation_node_id,
        COUNT(DISTINCT belongs.subject) AS message_count
    FROM assertions belongs
    INNER JOIN assertions kinds
        ON kinds.import_id = belongs.import_id
       AND kinds.subject = belongs.subject
    WHERE belongs.import_id = $import_id
      AND belongs.predicate = 'belongs_to_conversation'
      AND belongs.object_kind = 'node_ref'
      AND kinds.predicate = 'has_node_kind'
      AND kinds.object_kind = 'literal'
      AND kinds.object_value = 'message_node'
    GROUP BY belongs.object_value
),
artifact_counts AS (
    SELECT
        belongs.object_value AS conversation_node_id,
        COUNT(DISTINCT refs.object_value) AS artifact_count
    FROM assertions belongs
    INNER JOIN assertions refs
        ON refs.import_id = belongs.import_id
       AND refs.subject = belongs.subject
    WHERE belongs.import_id = $import_id
      AND belongs.predicate = 'belongs_to_conversation'
      AND belongs.object_kind = 'node_ref'
      AND refs.predicate = 'references_artifact'
      AND refs.object_kind = 'node_ref'
    GROUP BY belongs.object_value
)
SELECT
    conversation_nodes.conversation_node_id,
    conversation_nodes.title,
    conversation_nodes.slug,
    conversation_nodes.semantic_roles,
    COALESCE(message_counts.message_count, 0),
    COALESCE(artifact_counts.artifact_count, 0)
FROM conversation_nodes
LEFT JOIN message_counts
    ON message_counts.conversation_node_id = conversation_nodes.conversation_node_id
LEFT JOIN artifact_counts
    ON artifact_counts.conversation_node_id = conversation_nodes.conversation_node_id
ORDER BY COALESCE(conversation_nodes.title, conversation_nodes.slug, conversation_nodes.conversation_node_id) ASC
LIMIT $limit;
"""
                command.Parameters.AddWithValue("$import_id", ImportId.format importId) |> ignore
                command.Parameters.AddWithValue("$limit", limit) |> ignore

                use reader = command.ExecuteReader()
                let items = ResizeArray<WorkingImportConversationSummary>()

                while reader.Read() do
                    items.Add
                        { ConversationNodeId = reader.GetString(0)
                          Title = readOptionalString reader 1
                          Slug = readOptionalString reader 2
                          SemanticRoles = readOptionalString reader 3 |> splitConcatenatedList
                          MessageCount = reader.GetInt32(4)
                          ArtifactCount = reader.GetInt32(5) }

                Some
                    { IndexRelativePath = relativePath
                      ImportId = importId
                      Provider = provider
                      Window = window
                      ImportedAt = importedAt
                      MaterializedAt = materializedAt
                      WorkingRootRelativePath = workingRootRelativePath
                      ManifestRelativePath = manifestRelativePath
                      ConversationCount = conversationCount
                      Items = items |> Seq.toList }
            else
                None)
        |> Option.flatten

    /// <summary>
    /// Compares the conversation contributions present in two import-local graph working slices.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="baseImportId">The older or reference import batch.</param>
    /// <param name="currentImportId">The newer or comparison import batch.</param>
    /// <param name="limit">The maximum number of detailed rows to include in each result bucket.</param>
    /// <returns>The comparison report when both import-local working slices exist in the SQLite index.</returns>
    let tryBuildImportConversationComparisonReport eventStoreRoot baseImportId currentImportId limit =
        match
            tryBuildImportConversationReport eventStoreRoot baseImportId Int32.MaxValue,
            tryBuildImportConversationReport eventStoreRoot currentImportId Int32.MaxValue
            with
        | Some baseReport, Some currentReport ->
            let baseByConversation =
                baseReport.Items
                |> List.map (fun item -> item.ConversationNodeId, item)
                |> Map.ofList

            let currentByConversation =
                currentReport.Items
                |> List.map (fun item -> item.ConversationNodeId, item)
                |> Map.ofList

            let addedConversations =
                currentReport.Items
                |> List.filter (fun item -> not (baseByConversation.ContainsKey item.ConversationNodeId))
                |> List.sortBy conversationSummaryLabel

            let removedConversations =
                baseReport.Items
                |> List.filter (fun item -> not (currentByConversation.ContainsKey item.ConversationNodeId))
                |> List.sortBy conversationSummaryLabel

            let changedConversations =
                baseReport.Items
                |> List.choose (fun baseItem ->
                    match currentByConversation.TryFind baseItem.ConversationNodeId with
                    | Some currentItem
                        when baseItem.MessageCount <> currentItem.MessageCount
                             || baseItem.ArtifactCount <> currentItem.ArtifactCount
                             || baseItem.Title <> currentItem.Title
                             || baseItem.Slug <> currentItem.Slug
                             || baseItem.SemanticRoles <> currentItem.SemanticRoles ->
                        Some
                            { ConversationNodeId = baseItem.ConversationNodeId
                              BaseTitle = baseItem.Title
                              CurrentTitle = currentItem.Title
                              BaseSlug = baseItem.Slug
                              CurrentSlug = currentItem.Slug
                              BaseSemanticRoles = baseItem.SemanticRoles
                              CurrentSemanticRoles = currentItem.SemanticRoles
                              BaseMessageCount = baseItem.MessageCount
                              CurrentMessageCount = currentItem.MessageCount
                              BaseArtifactCount = baseItem.ArtifactCount
                              CurrentArtifactCount = currentItem.ArtifactCount }
                    | Some _
                    | None -> None)
                |> List.sortBy conversationDeltaLabel

            let sharedConversationCount =
                baseByConversation.Keys
                |> Seq.filter (fun conversationNodeId -> currentByConversation.ContainsKey conversationNodeId)
                |> Seq.length

            Some
                { IndexRelativePath = relativePath
                  BaseImportId = baseImportId
                  CurrentImportId = currentImportId
                  BaseProvider = baseReport.Provider
                  CurrentProvider = currentReport.Provider
                  BaseWindow = baseReport.Window
                  CurrentWindow = currentReport.Window
                  BaseImportedAt = baseReport.ImportedAt
                  CurrentImportedAt = currentReport.ImportedAt
                  BaseMaterializedAt = baseReport.MaterializedAt
                  CurrentMaterializedAt = currentReport.MaterializedAt
                  AddedConversationCount = addedConversations.Length
                  RemovedConversationCount = removedConversations.Length
                  ChangedConversationCount = changedConversations.Length
                  UnchangedConversationCount = sharedConversationCount - changedConversations.Length
                  AddedConversations = addedConversations |> List.truncate limit
                  RemovedConversations = removedConversations |> List.truncate limit
                  ChangedConversations = changedConversations |> List.truncate limit }
        | _ -> None
