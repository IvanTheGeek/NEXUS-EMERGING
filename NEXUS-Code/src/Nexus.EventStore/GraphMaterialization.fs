namespace Nexus.EventStore

open System
open System.Globalization
open System.IO
open Nexus.Domain

[<RequireQualifiedAccess>]
module GraphMaterialization =
    /// <summary>
    /// Emits human-readable graph materialization progress.
    /// </summary>
    type StatusReporter = string -> unit

    /// <summary>
    /// Describes whether a full graph rebuild should be treated as a heavyweight operation.
    /// </summary>
    type FullRebuildEstimate =
        { CanonicalEventFileCount: int
          HeavyweightThreshold: int
          RequiresExplicitApproval: bool }

    /// <summary>
    /// Describes the durable result of a full graph materialization run.
    /// </summary>
    type FullRebuildResult =
        { CanonicalEventFileCount: int
          GraphAssertionCount: int
          DerivationElapsed: TimeSpan
          TotalElapsed: TimeSpan
          AssertionPaths: string list
          ManifestRelativePath: string
          RebuiltAt: DateTimeOffset
          ApprovedByFlag: bool
          RequiredExplicitApproval: bool
          MaterializerVersion: string }

    /// <summary>
    /// Describes the durable result of incremental graph working-layer materialization for a single canonical import batch.
    /// </summary>
    type ImportBatchResult =
        { ImportId: ImportId
          CanonicalEventCount: int
          GraphAssertionCount: int
          DerivationElapsed: TimeSpan
          TotalElapsed: TimeSpan
          AssertionPaths: string list
          ManifestRelativePath: string
          MaterializedAt: DateTimeOffset
          MaterializerVersion: string }

    let private heavyweightThreshold = 1000
    let private materializerVersion = "graph-assertions-v1"
    let private importBatchMaterializerVersion = "graph-working-import-batch-v1"

    let private canonicalEventsRoot eventStoreRoot =
        Path.Combine(eventStoreRoot, "events")

    let private normalizePath (path: string) =
        path.Replace('\\', '/')

    let private timestampFileName (timestamp: DateTimeOffset) =
        timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ssZ", CultureInfo.InvariantCulture)

    let private workingImportRootRelativePath importId =
        normalizePath (Path.Combine("graph", "working", "imports", ImportId.format importId))

    let private workingImportAssertionRelativePath importId (assertion: GraphAssertion) =
        normalizePath (Path.Combine(workingImportRootRelativePath importId, "assertions", $"{FactId.format assertion.FactId}.toml"))

    let private writeFullRebuildManifest eventStoreRoot (result: FullRebuildResult) =
        let builder = create ()
        appendString builder "mode" "full"
        appendString builder "materializer" result.MaterializerVersion
        appendTimestamp builder "rebuilt_at" result.RebuiltAt
        appendInt builder "canonical_event_files_scanned" result.CanonicalEventFileCount
        appendInt builder "graph_assertions_written" result.GraphAssertionCount
        appendInt64Option builder "derivation_elapsed_ms" (Some (int64 result.DerivationElapsed.TotalMilliseconds))
        appendInt64Option builder "total_elapsed_ms" (Some (int64 result.TotalElapsed.TotalMilliseconds))
        appendBool builder "required_explicit_approval" result.RequiredExplicitApproval
        appendBool builder "approved_by_flag" result.ApprovedByFlag
        appendString builder "event_store_root" (Path.GetFullPath(eventStoreRoot))
        appendString builder "assertions_root" (normalizePath (Path.Combine("graph", "assertions")))
        appendString builder "manifest_relative_path" result.ManifestRelativePath
        appendBlank builder

        let rebuildDirectoryAbsolutePath = Path.Combine(eventStoreRoot, "graph", "rebuilds")
        Directory.CreateDirectory(rebuildDirectoryAbsolutePath) |> ignore

        let manifestAbsolutePath = Path.Combine(eventStoreRoot, result.ManifestRelativePath)
        let manifestDirectory = Path.GetDirectoryName(manifestAbsolutePath)

        if not (String.IsNullOrWhiteSpace(manifestDirectory)) then
            Directory.CreateDirectory(manifestDirectory) |> ignore

        File.WriteAllText(manifestAbsolutePath, render builder)

    let private writeImportBatchManifest eventStoreRoot (result: ImportBatchResult) =
        let builder = create ()
        appendString builder "mode" "incremental_import_batch"
        appendString builder "materializer" result.MaterializerVersion
        appendString builder "import_id" (ImportId.format result.ImportId)
        appendTimestamp builder "materialized_at" result.MaterializedAt
        appendInt builder "canonical_event_count" result.CanonicalEventCount
        appendInt builder "graph_assertions_written" result.GraphAssertionCount
        appendInt64Option builder "derivation_elapsed_ms" (Some (int64 result.DerivationElapsed.TotalMilliseconds))
        appendInt64Option builder "total_elapsed_ms" (Some (int64 result.TotalElapsed.TotalMilliseconds))
        appendString builder "event_store_root" (Path.GetFullPath(eventStoreRoot))
        appendString builder "working_root" (workingImportRootRelativePath result.ImportId)
        appendString builder "manifest_relative_path" result.ManifestRelativePath
        appendBlank builder

        let manifestAbsolutePath = Path.Combine(eventStoreRoot, result.ManifestRelativePath)
        let manifestDirectory = Path.GetDirectoryName(manifestAbsolutePath)

        if not (String.IsNullOrWhiteSpace(manifestDirectory)) then
            Directory.CreateDirectory(manifestDirectory) |> ignore

        File.WriteAllText(manifestAbsolutePath, render builder)

    /// <summary>
    /// Estimates the size of a full graph rebuild from the canonical event store.
    /// </summary>
    /// <remarks>
    /// This is the first boundary for future graph materialization strategies.
    /// Full planning notes: docs/nexus-graph-materialization-plan.md
    /// </remarks>
    let estimateFullRebuild eventStoreRoot =
        let eventsRoot = canonicalEventsRoot eventStoreRoot

        let canonicalEventFileCount =
            if Directory.Exists(eventsRoot) then
                Directory.EnumerateFiles(eventsRoot, "*.toml", SearchOption.AllDirectories)
                |> Seq.length
            else
                0

        { CanonicalEventFileCount = canonicalEventFileCount
          HeavyweightThreshold = heavyweightThreshold
          RequiresExplicitApproval = canonicalEventFileCount >= heavyweightThreshold }

    /// <summary>
    /// Rebuilds the full durable graph-assertion layer from canonical history.
    /// </summary>
    /// <remarks>
    /// This is currently backed by the existing GraphAssertions module.
    /// It is intentionally separated here so future incremental or alternate materializers have a stable boundary to hang from.
    /// Full planning notes: docs/nexus-graph-materialization-plan.md
    /// </remarks>
    let rebuildFullWithStatus (status: StatusReporter) approvedByFlag eventStoreRoot =
        let rebuiltAt = DateTimeOffset.UtcNow
        let estimate = estimateFullRebuild eventStoreRoot
        let graphResult = GraphAssertions.rebuildDetailedWithStatus status eventStoreRoot
        let manifestRelativePath =
            normalizePath (Path.Combine("graph", "rebuilds", $"{timestampFileName rebuiltAt}__full-rebuild.toml"))

        let result =
            { CanonicalEventFileCount = graphResult.CanonicalEventFileCount
              GraphAssertionCount = graphResult.GraphAssertionCount
              DerivationElapsed = graphResult.DerivationElapsed
              TotalElapsed = graphResult.TotalElapsed
              AssertionPaths = graphResult.AssertionPaths
              ManifestRelativePath = manifestRelativePath
              RebuiltAt = rebuiltAt
              ApprovedByFlag = approvedByFlag
              RequiredExplicitApproval = estimate.RequiresExplicitApproval
              MaterializerVersion = materializerVersion }

        writeFullRebuildManifest eventStoreRoot result
        result

    /// <summary>
    /// Materializes a single canonical import batch into the secondary graph working layer.
    /// </summary>
    /// <remarks>
    /// This is the first incremental graph path behind the graph materialization boundary.
    /// It writes import-batch-local assertion files under <c>graph/working/imports/&lt;import-id&gt;/</c>.
    /// Full planning notes: docs/nexus-graph-materialization-plan.md
    /// </remarks>
    let materializeImportBatchWithStatus (status: StatusReporter) eventStoreRoot importId (events: CanonicalEvent list) =
        let stopwatch = Diagnostics.Stopwatch.StartNew()
        let materializedAt = DateTimeOffset.UtcNow
        let derivation =
            status $"Deriving graph working batch from {events.Length} canonical events for import {ImportId.format importId}."
            GraphAssertions.deriveFromCanonicalEventsWithStatus status events

        let workingRootRelativePath = workingImportRootRelativePath importId
        let workingRootAbsolutePath = Path.Combine(eventStoreRoot, workingRootRelativePath)

        if Directory.Exists(workingRootAbsolutePath) then
            status $"Removing previous graph working batch at {workingRootAbsolutePath}"
            Directory.Delete(workingRootAbsolutePath, true)

        Directory.CreateDirectory(workingRootAbsolutePath) |> ignore
        status $"Writing {derivation.GraphAssertionCount} graph working assertions to {workingRootAbsolutePath}"

        let assertionPaths =
            derivation.Assertions
            |> List.map (fun (assertion: GraphAssertion) ->
                let relativePath = workingImportAssertionRelativePath importId assertion
                let absolutePath = Path.Combine(eventStoreRoot, relativePath)
                let directory = Path.GetDirectoryName(absolutePath)

                if not (String.IsNullOrWhiteSpace(directory)) then
                    Directory.CreateDirectory(directory) |> ignore

                File.WriteAllText(absolutePath, CanonicalStore.serializeGraphAssertion assertion)
                relativePath)

        let totalElapsed = stopwatch.Elapsed
        let manifestRelativePath = normalizePath (Path.Combine(workingRootRelativePath, "manifest.toml"))
        let result =
            { ImportId = importId
              CanonicalEventCount = derivation.CanonicalEventCount
              GraphAssertionCount = derivation.GraphAssertionCount
              DerivationElapsed = derivation.DerivationElapsed
              TotalElapsed = totalElapsed
              AssertionPaths = assertionPaths
              ManifestRelativePath = manifestRelativePath
              MaterializedAt = materializedAt
              MaterializerVersion = importBatchMaterializerVersion }

        writeImportBatchManifest eventStoreRoot result
        status $"Graph working batch materialized in {totalElapsed}"
        result
