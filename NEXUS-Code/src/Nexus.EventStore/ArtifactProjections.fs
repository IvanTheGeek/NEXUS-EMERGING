namespace Nexus.EventStore

open System
open System.Collections.Generic
open System.Globalization
open System.IO

[<RequireQualifiedAccess>]
module ArtifactProjections =
    type private ArtifactProjectionState =
        { ArtifactId: string
          Providers: HashSet<string>
          ProviderConversationIds: HashSet<string>
          ProviderMessageIds: HashSet<string>
          ProviderArtifactIds: HashSet<string>
          ImportIds: HashSet<string>
          CapturedContentHashes: HashSet<string>
          mutable ConversationId: string option
          mutable MessageId: string option
          mutable FileName: string option
          mutable MediaType: string option
          mutable ReferenceDisposition: string option
          mutable ReferenceCount: int
          mutable CaptureCount: int
          mutable PayloadCaptured: bool
          mutable FirstObservedAt: DateTimeOffset option
          mutable LastObservedAt: DateTimeOffset option
          mutable LastCapturedAt: DateTimeOffset option
          mutable LatestCapturedPath: string option }

    let private tryParseTimestamp (value: string option) =
        value
        |> Option.bind (fun raw ->
            match DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
            | true, parsedValue -> Some parsedValue
            | _ -> None)

    let private setEarlier current candidate =
        match current, candidate with
        | None, value -> value
        | value, None -> value
        | Some currentValue, Some candidateValue -> Some(min currentValue candidateValue)

    let private setLater current candidate =
        match current, candidate with
        | None, value -> value
        | value, None -> value
        | Some currentValue, Some candidateValue -> Some(max currentValue candidateValue)

    let private projectionState artifactId =
        { ArtifactId = artifactId
          Providers = HashSet(StringComparer.Ordinal)
          ProviderConversationIds = HashSet(StringComparer.Ordinal)
          ProviderMessageIds = HashSet(StringComparer.Ordinal)
          ProviderArtifactIds = HashSet(StringComparer.Ordinal)
          ImportIds = HashSet(StringComparer.Ordinal)
          CapturedContentHashes = HashSet(StringComparer.Ordinal)
          ConversationId = None
          MessageId = None
          FileName = None
          MediaType = None
          ReferenceDisposition = None
          ReferenceCount = 0
          CaptureCount = 0
          PayloadCaptured = false
          FirstObservedAt = None
          LastObservedAt = None
          LastCapturedAt = None
          LatestCapturedPath = None }

    let private getOrAddState (states: Dictionary<string, ArtifactProjectionState>) artifactId =
        match states.TryGetValue(artifactId) with
        | true, state -> state
        | false, _ ->
            let state = projectionState artifactId
            states[artifactId] <- state
            state

    let private setIfMissing current candidate =
        match current, candidate with
        | Some _, _ -> current
        | None, value -> value

    let private addImportId state document =
        TomlDocument.tryScalar "import_id" document
        |> Option.iter (fun importId -> state.ImportIds.Add(importId) |> ignore)

    let private addObservedTimes state document =
        let observedAt = TomlDocument.tryScalar "observed_at" document |> tryParseTimestamp
        state.FirstObservedAt <- setEarlier state.FirstObservedAt observedAt
        state.LastObservedAt <- setLater state.LastObservedAt observedAt
        observedAt

    let private addProviderRefs state document =
        for table in TomlDocument.tableArray "provider_refs" document do
            let tryValue key =
                match table.TryGetValue(key) with
                | true, value -> Some value
                | false, _ -> None

            tryValue "provider"
            |> Option.iter (fun provider -> state.Providers.Add(provider) |> ignore)

            tryValue "conversation_native_id"
            |> Option.iter (fun conversationId -> state.ProviderConversationIds.Add(conversationId) |> ignore)

            match tryValue "object_kind" with
            | Some "message_object" ->
                tryValue "message_native_id"
                |> Option.orElseWith (fun () -> tryValue "native_id")
                |> Option.iter (fun messageId -> state.ProviderMessageIds.Add(messageId) |> ignore)
            | Some "artifact_object" ->
                tryValue "artifact_native_id"
                |> Option.orElseWith (fun () -> tryValue "native_id")
                |> Option.iter (fun artifactId -> state.ProviderArtifactIds.Add(artifactId) |> ignore)
            | _ -> ()

    let private addArtifactReferenced state document =
        state.ConversationId <- setIfMissing state.ConversationId (TomlDocument.tryScalar "conversation_id" document)
        state.MessageId <- setIfMissing state.MessageId (TomlDocument.tryScalar "message_id" document)
        state.FileName <- setIfMissing state.FileName (TomlDocument.tryTableValue "body" "file_name" document)
        state.MediaType <- setIfMissing state.MediaType (TomlDocument.tryTableValue "body" "media_type" document)
        state.ReferenceDisposition <- setIfMissing state.ReferenceDisposition (TomlDocument.tryTableValue "body" "disposition" document)
        state.ReferenceCount <- state.ReferenceCount + 1

    let private addArtifactPayloadCaptured state document observedAt =
        state.ConversationId <- setIfMissing state.ConversationId (TomlDocument.tryScalar "conversation_id" document)
        state.MessageId <- setIfMissing state.MessageId (TomlDocument.tryScalar "message_id" document)
        state.MediaType <- setIfMissing state.MediaType (TomlDocument.tryTableValue "body" "media_type" document)
        state.LatestCapturedPath <- TomlDocument.tryTableValue "body.captured_object" "relative_path" document
        state.PayloadCaptured <- true
        state.CaptureCount <- state.CaptureCount + 1
        state.LastCapturedAt <- setLater state.LastCapturedAt observedAt

        match TomlDocument.tryTableValue "content_hash" "algorithm" document, TomlDocument.tryTableValue "content_hash" "value" document with
        | Some algorithm, Some value ->
            state.CapturedContentHashes.Add($"{algorithm}:{value}") |> ignore
        | _ -> ()

    let private addArtifactEvent state document =
        addProviderRefs state document
        addImportId state document
        let observedAt = addObservedTimes state document

        match TomlDocument.tryScalar "event_kind" document with
        | Some "artifact_referenced" ->
            addArtifactReferenced state document
        | Some "artifact_payload_captured" ->
            addArtifactPayloadCaptured state document observedAt
        | _ -> ()

    let private projectionRelativePath artifactId =
        Path.Combine("projections", "artifacts", $"{artifactId}.toml").Replace('\\', '/')

    let private writeProjection rootPath (state: ArtifactProjectionState) =
        let builder = create ()

        appendAssignment builder "schema_version" "1"
        appendString builder "projection_kind" "artifact_projection"
        appendString builder "artifact_id" state.ArtifactId
        appendStringOption builder "conversation_id" state.ConversationId
        appendStringOption builder "message_id" state.MessageId
        appendStringOption builder "file_name" state.FileName
        appendStringOption builder "media_type" state.MediaType
        appendStringOption builder "reference_disposition" state.ReferenceDisposition
        appendInt builder "reference_count" state.ReferenceCount
        appendInt builder "capture_count" state.CaptureCount
        appendBool builder "payload_captured" state.PayloadCaptured
        appendTimestampOption builder "first_observed_at" state.FirstObservedAt
        appendTimestampOption builder "last_observed_at" state.LastObservedAt
        appendTimestampOption builder "last_captured_at" state.LastCapturedAt
        appendStringOption builder "latest_captured_path" state.LatestCapturedPath

        if state.Providers.Count > 0 then
            state.Providers |> Seq.sort |> Seq.toList |> appendStringList builder "providers"

        if state.ProviderConversationIds.Count > 0 then
            state.ProviderConversationIds |> Seq.sort |> Seq.toList |> appendStringList builder "provider_conversation_ids"

        if state.ProviderMessageIds.Count > 0 then
            state.ProviderMessageIds |> Seq.sort |> Seq.toList |> appendStringList builder "provider_message_ids"

        if state.ProviderArtifactIds.Count > 0 then
            state.ProviderArtifactIds |> Seq.sort |> Seq.toList |> appendStringList builder "provider_artifact_ids"

        if state.ImportIds.Count > 0 then
            state.ImportIds |> Seq.sort |> Seq.toList |> appendStringList builder "import_ids"

        if state.CapturedContentHashes.Count > 0 then
            state.CapturedContentHashes |> Seq.sort |> Seq.toList |> appendStringList builder "captured_content_hashes"

        let relativePath = projectionRelativePath state.ArtifactId
        let absolutePath = Path.Combine(rootPath, relativePath)
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)) |> ignore
        File.WriteAllText(absolutePath, render builder)
        relativePath

    /// <summary>
    /// Rebuilds artifact projections from canonical artifact events.
    /// </summary>
    /// <param name="rootPath">The root of the event-store workspace to rebuild into.</param>
    /// <returns>The relative paths of all written artifact projection files.</returns>
    /// <remarks>
    /// Projections are rebuildable views, not source truth.
    /// Full workflow notes: docs/how-to/rebuild-artifact-projections.md
    /// </remarks>
    let rebuild rootPath =
        let absoluteRoot = Path.GetFullPath(rootPath)
        let states = Dictionary<string, ArtifactProjectionState>(StringComparer.Ordinal)
        let artifactEventsRoot = Path.Combine(absoluteRoot, "events", "artifacts")
        let projectionsRoot = Path.Combine(absoluteRoot, "projections", "artifacts")

        if Directory.Exists(artifactEventsRoot) then
            Directory.EnumerateFiles(artifactEventsRoot, "*.toml", SearchOption.AllDirectories)
            |> Seq.iter (fun path ->
                let document = File.ReadAllText(path) |> TomlDocument.parse

                match TomlDocument.tryScalar "artifact_id" document with
                | Some artifactId ->
                    let state = getOrAddState states artifactId
                    addArtifactEvent state document
                | None -> ())

        if Directory.Exists(projectionsRoot) then
            Directory.Delete(projectionsRoot, true)

        Directory.CreateDirectory(projectionsRoot) |> ignore

        states.Values
        |> Seq.sortBy (fun state -> state.ArtifactId)
        |> Seq.map (writeProjection absoluteRoot)
        |> Seq.toList
