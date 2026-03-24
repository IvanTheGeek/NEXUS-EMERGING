namespace Nexus.EventStore

open System
open System.Collections.Generic
open System.IO
open Nexus.Domain

/// <summary>
/// Describes one missing canonical event referenced by a graph working slice.
/// </summary>
type MissingCanonicalEventReference =
    { EventId: CanonicalEventId
      ReferencedByFactId: FactId }

/// <summary>
/// Describes one missing raw object referenced by a graph working slice.
/// </summary>
type MissingRawObjectReference =
    { RelativePath: string
      ReferencedByFactId: FactId
      Kind: string option }

/// <summary>
/// Summarizes verification of one graph working import slice back to canonical and raw layers.
/// </summary>
type WorkingGraphSliceVerificationReport =
    { ImportId: ImportId
      WorkingRootRelativePath: string
      ManifestRelativePath: string
      AssertionCount: int
      AssertionImportMismatches: int
      AssertionsWithoutSupportingEvents: int
      SupportingEventReferenceCount: int
      DistinctSupportingEventCount: int
      MissingCanonicalEventReferences: MissingCanonicalEventReference list
      RawObjectReferenceCount: int
      DistinctRawObjectCount: int
      MissingRawObjectReferences: MissingRawObjectReference list
      VerifiedAt: DateTimeOffset
      IndexAvailable: bool }

/// <summary>
/// Verifies graph working slices against canonical history and preserved raw objects.
/// </summary>
/// <remarks>
/// This module supports the NEXUS rule that derived indexes and derived slices must not sever
/// the verification path back to canonical and raw source layers.
/// Full notes: docs/decisions/0007-traceable-verification-over-derived-indexes.md
/// </remarks>
[<RequireQualifiedAccess>]
module GraphWorkingVerification =
    let private normalizePath (path: string) =
        path.Replace('\\', '/')

    let private objectKindValue =
        function
        | ProviderExportZip -> "provider_export_zip"
        | ExtractedSnapshot -> "extracted_snapshot"
        | SessionIndex -> "session_index"
        | SessionTranscript -> "session_transcript"
        | AttachmentPayload -> "attachment_payload"
        | AudioPayload -> "audio_payload"
        | ManualArtifact -> "manual_artifact"
        | OtherRawObject value -> value

    let private tryImportIdFromAssertion document =
        TomlDocument.tryTableValue "provenance" "import_id" document
        |> Option.map ImportId.parse

    let private supportingEventIds document =
        TomlDocument.tryTableStringList "provenance" "supporting_event_ids" document
        |> Option.defaultValue []
        |> List.map CanonicalEventId.parse

    let private rawObjectRefs document =
        TomlDocument.tableArray "raw_objects" document
        |> List.choose (fun table ->
            match table.TryGetValue("relative_path"), table.TryGetValue("kind") with
            | (true, relativePath), (true, kind) ->
                Some(relativePath, Some kind)
            | (true, relativePath), (false, _) ->
                Some(relativePath, None)
            | (false, _), _ -> None)

    let private canonicalEventIdsByPath eventStoreRoot =
        let eventsRoot = Path.Combine(Path.GetFullPath(eventStoreRoot), "events")

        if Directory.Exists(eventsRoot) then
            Directory.EnumerateFiles(eventsRoot, "*.toml", SearchOption.AllDirectories)
            |> Seq.choose (fun absolutePath ->
                let fileName = Path.GetFileNameWithoutExtension(absolutePath)
                match fileName.Split([| "__" |], StringSplitOptions.None) |> Array.toList with
                | eventIdRaw :: _ ->
                    Some (CanonicalEventId.parse eventIdRaw, normalizePath (Path.GetRelativePath(Path.GetFullPath(eventStoreRoot), absolutePath)))
                | [] -> None)
            |> dict
        else
            dict []

    let private distinctBy projection values =
        let seen = HashSet<string>(StringComparer.Ordinal)

        values
        |> List.filter (fun value ->
            let key = projection value
            seen.Add(key))

    /// <summary>
    /// Verifies one graph working import slice against canonical history and preserved raw objects.
    /// </summary>
    /// <param name="eventStoreRoot">The root of the event-store workspace.</param>
    /// <param name="objectsRoot">The root of the preserved object workspace.</param>
    /// <param name="importId">The import-local working slice to verify.</param>
    /// <returns>A verification summary for the selected working slice.</returns>
    let verifyImportSlice eventStoreRoot objectsRoot importId =
        let catalog = GraphWorkingCatalog.load eventStoreRoot

        let entry =
            catalog.Entries
            |> List.tryFind (fun currentEntry -> currentEntry.ImportId = importId)
            |> Option.defaultWith (fun () -> invalidArg "importId" $"No graph working slice found for import {ImportId.format importId}.")

        let assertionsRoot =
            Path.Combine(Path.GetFullPath(eventStoreRoot), entry.WorkingRootRelativePath, "assertions")

        if not (Directory.Exists(assertionsRoot)) then
            invalidArg "importId" $"Graph working assertions directory not found for import {ImportId.format importId}: {assertionsRoot}"

        let indexAvailable =
            GraphWorkingIndex.tryBuildImportSliceReport eventStoreRoot importId 1
            |> Option.isSome

        let canonicalEvents = canonicalEventIdsByPath eventStoreRoot
        let mutable assertionCount = 0
        let mutable assertionImportMismatches = 0
        let mutable assertionsWithoutSupportingEvents = 0
        let mutable supportingEventReferenceCount = 0
        let mutable rawObjectReferenceCount = 0
        let missingCanonical = ResizeArray<MissingCanonicalEventReference>()
        let missingRawObjects = ResizeArray<MissingRawObjectReference>()
        let distinctSupportingEvents = ResizeArray<CanonicalEventId>()
        let distinctRawObjects = ResizeArray<string>()

        for absolutePath in Directory.EnumerateFiles(assertionsRoot, "*.toml", SearchOption.AllDirectories) do
            assertionCount <- assertionCount + 1
            let document = File.ReadAllText(absolutePath) |> TomlDocument.parse

            let factId =
                TomlDocument.tryScalar "fact_id" document
                |> Option.map FactId.parse
                |> Option.defaultWith (fun () -> failwith $"Missing fact_id in graph working assertion: {absolutePath}")

            match tryImportIdFromAssertion document with
            | Some provenanceImportId when provenanceImportId = importId -> ()
            | Some _
            | None -> assertionImportMismatches <- assertionImportMismatches + 1

            let supportingEvents = supportingEventIds document
            supportingEventReferenceCount <- supportingEventReferenceCount + supportingEvents.Length

            if supportingEvents.IsEmpty then
                assertionsWithoutSupportingEvents <- assertionsWithoutSupportingEvents + 1

            for eventId in supportingEvents do
                distinctSupportingEvents.Add(eventId)

                if not (canonicalEvents.ContainsKey(eventId)) then
                    missingCanonical.Add
                        { EventId = eventId
                          ReferencedByFactId = factId }

            let rawObjects = rawObjectRefs document
            rawObjectReferenceCount <- rawObjectReferenceCount + rawObjects.Length

            for relativePath, kind in rawObjects do
                distinctRawObjects.Add(relativePath)

                let absoluteRawPath = Path.Combine(Path.GetFullPath(objectsRoot), relativePath)

                if not (File.Exists(absoluteRawPath) || Directory.Exists(absoluteRawPath)) then
                    missingRawObjects.Add
                        { RelativePath = relativePath
                          ReferencedByFactId = factId
                          Kind = kind }

        { ImportId = importId
          WorkingRootRelativePath = entry.WorkingRootRelativePath
          ManifestRelativePath = entry.ManifestRelativePath
          AssertionCount = assertionCount
          AssertionImportMismatches = assertionImportMismatches
          AssertionsWithoutSupportingEvents = assertionsWithoutSupportingEvents
          SupportingEventReferenceCount = supportingEventReferenceCount
          DistinctSupportingEventCount =
            distinctSupportingEvents
            |> Seq.map CanonicalEventId.format
            |> Seq.toList
            |> distinctBy id
            |> List.length
          MissingCanonicalEventReferences =
            missingCanonical
            |> Seq.toList
            |> distinctBy (fun item -> $"{CanonicalEventId.format item.EventId}|{FactId.format item.ReferencedByFactId}")
          RawObjectReferenceCount = rawObjectReferenceCount
          DistinctRawObjectCount =
            distinctRawObjects
            |> Seq.toList
            |> distinctBy id
            |> List.length
          MissingRawObjectReferences =
            missingRawObjects
            |> Seq.toList
            |> distinctBy (fun item ->
                let kind = item.Kind |> Option.defaultValue ""
                $"{item.RelativePath}|{FactId.format item.ReferencedByFactId}|{kind}")
          VerifiedAt = DateTimeOffset.UtcNow
          IndexAvailable = indexAvailable }

    /// <summary>
    /// Returns whether a verification report is fully clean.
    /// </summary>
    let isClean report =
        report.AssertionImportMismatches = 0
        && report.AssertionsWithoutSupportingEvents = 0
        && report.MissingCanonicalEventReferences.IsEmpty
        && report.MissingRawObjectReferences.IsEmpty
