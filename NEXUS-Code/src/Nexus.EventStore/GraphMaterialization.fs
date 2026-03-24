namespace Nexus.EventStore

open System.IO

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

    let private heavyweightThreshold = 1000

    let private canonicalEventsRoot eventStoreRoot =
        Path.Combine(eventStoreRoot, "events")

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
    let rebuildFullWithStatus (status: StatusReporter) eventStoreRoot =
        GraphAssertions.rebuildWithStatus status eventStoreRoot
