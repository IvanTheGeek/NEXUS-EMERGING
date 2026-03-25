namespace Nexus.EventStore

open System
open System.IO
open Nexus.Domain

/// <summary>
/// The canonical stream families used for append-only event-store layout.
/// </summary>
type StreamKind =
    | ConversationStream
    | ArtifactStream
    | ImportStream

/// <summary>
/// Identifies a specific stream inside the canonical event store.
/// </summary>
type StreamRef =
    { Kind: StreamKind
      StreamId: string }

/// <summary>
/// Serializes canonical history and graph artifacts to TOML and writes them into the event-store layout.
/// </summary>
/// <remarks>
/// Full layout notes: NEXUS-EventStore/docs/v0-layout-and-toml.md
/// </remarks>
module CanonicalStore =
    let private normalizePath (path: string) =
        path.Replace('\\', '/')

    let private ensureDirectoryForFile (path: string) =
        let directory = Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace(directory)) then
            Directory.CreateDirectory(directory) |> ignore

    let private providerKindValue =
        function
        | ChatGpt -> "chatgpt"
        | Claude -> "claude"
        | Grok -> "grok"
        | Codex -> "codex"
        | OtherProvider value -> value

    let private providerObjectKindValue =
        function
        | ExportArtifact -> "export_artifact"
        | ConversationObject -> "conversation_object"
        | MessageObject -> "message_object"
        | ArtifactObject -> "artifact_object"
        | ProjectObject -> "project_object"
        | MemoryObject -> "memory_object"
        | UserObject -> "user_object"
        | OtherProviderObject value -> value

    let private sourceAcquisitionValue =
        function
        | ExportZip -> "export_zip"
        | LocalSessionExport -> "local_session_export"
        | ManualArtifactAdd -> "manual_artifact_add"
        | ApiCapture -> "api_capture"
        | BrowserCapture -> "browser_capture"
        | OtherAcquisition value -> value

    let private importWindowValue =
        function
        | Full -> "full"
        | Rolling value -> value
        | Incremental value -> value
        | ManualWindow value -> value

    let private rawObjectKindValue =
        function
        | ProviderExportZip -> "provider_export_zip"
        | ExtractedSnapshot -> "extracted_snapshot"
        | SessionIndex -> "session_index"
        | SessionTranscript -> "session_transcript"
        | AttachmentPayload -> "attachment_payload"
        | AudioPayload -> "audio_payload"
        | ManualArtifact -> "manual_artifact"
        | OtherRawObject value -> value

    let private messageRoleValue =
        function
        | Human -> "human"
        | Assistant -> "assistant"
        | System -> "system"
        | Tool -> "tool"
        | OtherRole value -> value

    let private messageSegmentKindValue =
        function
        | PlainText -> "plain_text"
        | Markdown -> "markdown"
        | Quote -> "quote"
        | Code -> "code"
        | Reasoning -> "reasoning"
        | ToolUse -> "tool_use"
        | ToolResult -> "tool_result"
        | Multimodal -> "multimodal"
        | UnknownSegment value -> value

    let private artifactDispositionValue =
        function
        | PayloadIncluded -> "payload_included"
        | PayloadMissing -> "payload_missing"
        | PayloadUnknown -> "payload_unknown"

    let private nodeKindValue =
        function
        | ConversationNode -> "conversation_node"
        | MessageNode -> "message_node"
        | ArtifactNode -> "artifact_node"
        | DomainNode -> "domain_node"
        | BoundedContextNode -> "bounded_context_node"
        | LensNode -> "lens_node"
        | FactNode -> "fact_node"
        | OtherNode value -> value

    let private edgeKindValue =
        function
        | BelongsToConversation -> "belongs_to_conversation"
        | ReferencesArtifact -> "references_artifact"
        | ObservedDuringImport -> "observed_during_import"
        | HasSemanticRole -> "has_semantic_role"
        | SupportsFact -> "supports_fact"
        | LocatedInDomain -> "located_in_domain"
        | InterpretedWithinContext -> "interpreted_within_context"
        | ViewedThroughLens -> "viewed_through_lens"
        | OtherEdge value -> value

    let private graphValueLiteral =
        function
        | StringValue value -> stringLiteral value
        | IntValue value -> int64Literal value
        | DecimalValue value -> decimalLiteral value
        | BoolValue value -> boolLiteral value
        | TimestampValue value -> timestampLiteral value

    let private occurredAtValue (OccurredAt value) = value
    let private observedAtValue (ObservedAt value) = value
    let private importedAtValue (ImportedAt value) = value
    let private normalizationVersionValue (value: NormalizationVersion) = NormalizationVersion.value value

    let private eventKindValue =
        function
        | ProviderArtifactReceived _ -> "provider_artifact_received"
        | RawSnapshotExtracted _ -> "raw_snapshot_extracted"
        | ProviderConversationObserved _ -> "provider_conversation_observed"
        | ProviderMessageObserved _ -> "provider_message_observed"
        | ProviderMessageRevisionObserved _ -> "provider_message_revision_observed"
        | ArtifactReferenced _ -> "artifact_referenced"
        | ArtifactPayloadCaptured _ -> "artifact_payload_captured"
        | ImportCompleted _ -> "import_completed"

    let private fileEventKindValue body =
        eventKindValue body |> fun value -> value.Replace('_', '-')

    let private appendContentHash builder path (contentHash: ContentHash) =
        appendTableHeader builder path
        appendString builder "algorithm" contentHash.Algorithm
        appendString builder "value" contentHash.Value
        appendBlank builder

    let private appendProviderRefs builder (providerRefs: ProviderRef list) =
        for providerRef in providerRefs do
            appendArrayTableHeader builder "provider_refs"
            appendString builder "provider" (providerKindValue providerRef.Provider)
            appendString builder "object_kind" (providerObjectKindValue providerRef.ObjectKind)
            appendStringOption builder "native_id" providerRef.NativeId
            appendStringOption builder "conversation_native_id" providerRef.ConversationNativeId
            appendStringOption builder "message_native_id" providerRef.MessageNativeId
            appendStringOption builder "artifact_native_id" providerRef.ArtifactNativeId
            appendBlank builder

    let private appendRawObjectRef builder pathPrefix (rawObject: RawObjectRef) =
        appendTableHeader builder pathPrefix
        appendStringOption builder "raw_object_id" rawObject.RawObjectId
        appendString builder "kind" (rawObjectKindValue rawObject.Kind)
        appendString builder "relative_path" rawObject.RelativePath
        appendTimestampOption builder "archived_at" (rawObject.ArchivedAt |> Option.map importedAtValue)
        appendStringOption builder "source_description" rawObject.SourceDescription
        appendBlank builder

    let private appendRawObjects builder (rawObjects: RawObjectRef list) =
        for rawObject in rawObjects do
            appendArrayTableHeader builder "raw_objects"
            appendStringOption builder "raw_object_id" rawObject.RawObjectId
            appendString builder "kind" (rawObjectKindValue rawObject.Kind)
            appendString builder "relative_path" rawObject.RelativePath
            appendTimestampOption builder "archived_at" (rawObject.ArchivedAt |> Option.map importedAtValue)
            appendStringOption builder "source_description" rawObject.SourceDescription
            appendBlank builder

    let private appendImportCounts builder path (counts: ImportCounts) =
        appendTableHeader builder path
        appendInt builder "conversations_seen" counts.ConversationsSeen
        appendInt builder "messages_seen" counts.MessagesSeen
        appendInt builder "artifacts_referenced" counts.ArtifactsReferenced
        appendInt builder "new_events_appended" counts.NewEventsAppended
        appendInt builder "duplicates_skipped" counts.DuplicatesSkipped
        appendInt builder "revisions_observed" counts.RevisionsObserved
        appendInt builder "reparse_observations_appended" counts.ReparseObservationsAppended
        appendBlank builder

    let private streamRefForEvent (event: CanonicalEvent) =
        match event.Body with
        | ProviderConversationObserved _ ->
            match event.Envelope.ConversationId with
            | Some conversationId ->
                { Kind = ConversationStream
                  StreamId = ConversationId.format conversationId }
            | None ->
                invalidArg "event" "Conversation stream events require a conversation ID in the envelope."
        | ProviderMessageObserved _
        | ProviderMessageRevisionObserved _ ->
            match event.Envelope.ConversationId with
            | Some conversationId ->
                { Kind = ConversationStream
                  StreamId = ConversationId.format conversationId }
            | None ->
                invalidArg "event" "Message events require a conversation ID in the envelope."
        | ArtifactReferenced _
        | ArtifactPayloadCaptured _ ->
            match event.Envelope.ArtifactId with
            | Some artifactId ->
                { Kind = ArtifactStream
                  StreamId = ArtifactId.format artifactId }
            | None ->
                invalidArg "event" "Artifact events require an artifact ID in the envelope."
        | ProviderArtifactReceived _
        | RawSnapshotExtracted _
        | ImportCompleted _ ->
            match event.Envelope.ImportId with
            | Some importId ->
                { Kind = ImportStream
                  StreamId = ImportId.format importId }
            | None ->
                invalidArg "event" "Import events require an import ID in the envelope."

    let private streamKindValue =
        function
        | ConversationStream -> "conversation"
        | ArtifactStream -> "artifact"
        | ImportStream -> "import"

    let private appendEnvelope builder (streamRef: StreamRef) (event: CanonicalEvent) =
        appendAssignment builder "schema_version" "1"
        appendString builder "event_id" (CanonicalEventId.format event.Envelope.EventId)
        appendString builder "event_kind" (eventKindValue event.Body)
        appendString builder "stream_kind" (streamKindValue streamRef.Kind)
        appendString builder "stream_id" streamRef.StreamId
        appendStringOption builder "conversation_id" (event.Envelope.ConversationId |> Option.map ConversationId.format)
        appendStringOption builder "message_id" (event.Envelope.MessageId |> Option.map MessageId.format)
        appendStringOption builder "artifact_id" (event.Envelope.ArtifactId |> Option.map ArtifactId.format)
        appendStringOption builder "turn_id" (event.Envelope.TurnId |> Option.map TurnId.format)
        appendStringOption builder "domain_id" (event.Envelope.DomainId |> Option.map DomainId.value)
        appendStringOption builder "bounded_context_id" (event.Envelope.BoundedContextId |> Option.map BoundedContextId.value)
        appendTimestampOption builder "occurred_at" (event.Envelope.OccurredAt |> Option.map occurredAtValue)
        appendTimestamp builder "observed_at" (observedAtValue event.Envelope.ObservedAt)
        appendTimestampOption builder "imported_at" (event.Envelope.ImportedAt |> Option.map importedAtValue)
        appendString builder "source_acquisition" (sourceAcquisitionValue event.Envelope.SourceAcquisition)
        appendStringOption builder "normalization_version" (event.Envelope.NormalizationVersion |> Option.map normalizationVersionValue)
        appendStringOption builder "import_id" (event.Envelope.ImportId |> Option.map ImportId.format)
        appendBlank builder

        event.Envelope.ContentHash
        |> Option.iter (appendContentHash builder "content_hash")

        appendProviderRefs builder event.Envelope.ProviderRefs
        appendRawObjects builder event.Envelope.RawObjects

    let private appendBody builder (body: CanonicalEventBody) =
        match body with
        | ProviderArtifactReceived value ->
            appendTableHeader builder "body"
            appendString builder "provider" (providerKindValue value.Provider)
            appendString builder "file_name" value.FileName
            appendStringOption builder "window_kind" (value.Window |> Option.map importWindowValue)
            appendInt64Option builder "byte_count" value.ByteCount
            appendBlank builder
        | RawSnapshotExtracted value ->
            appendTableHeader builder "body"
            appendString builder "artifact_id" (ArtifactId.format value.ArtifactId)
            appendIntOption builder "extracted_entries" value.ExtractedEntries
            appendStringOption builder "notes" value.Notes
            appendBlank builder
        | ProviderConversationObserved value ->
            appendTableHeader builder "body"
            appendStringOption builder "provider_conversation_id" (value.ProviderConversation.ConversationNativeId |> Option.orElse value.ProviderConversation.NativeId)
            appendStringOption builder "title" value.Title
            appendBoolOption builder "is_archived" value.IsArchived
            appendIntOption builder "message_count_hint" value.MessageCountHint
            appendBlank builder
        | ProviderMessageObserved value ->
            appendTableHeader builder "body"
            appendStringOption builder "provider_message_id" (value.ProviderMessage.MessageNativeId |> Option.orElse value.ProviderMessage.NativeId)
            appendString builder "role" (messageRoleValue value.Role)
            appendStringOption builder "model_name" value.ModelName
            appendIntOption builder "sequence_hint" value.SequenceHint
            appendBlank builder

            for segment in value.Segments do
                appendArrayTableHeader builder "body.segments"
                appendString builder "kind" (messageSegmentKindValue segment.Kind)
                appendString builder "text" segment.Text
                appendBlank builder
        | ProviderMessageRevisionObserved value ->
            appendTableHeader builder "body"
            appendString builder "message_id" (MessageId.format value.MessageId)
            appendStringOption builder "revision_reason" value.RevisionReason
            appendBlank builder
            value.PriorContentHash |> Option.iter (appendContentHash builder "body.prior_content_hash")
            appendContentHash builder "body.revised_content_hash" value.RevisedContentHash
        | ArtifactReferenced value ->
            appendTableHeader builder "body"
            appendStringOption builder "file_name" value.FileName
            appendStringOption builder "media_type" value.MediaType
            appendString builder "disposition" (artifactDispositionValue value.Disposition)
            appendStringOption builder "provider_artifact_id" (value.ProviderArtifact |> Option.bind (fun artifact -> artifact.ArtifactNativeId |> Option.orElse artifact.NativeId))
            appendBlank builder
        | ArtifactPayloadCaptured value ->
            appendTableHeader builder "body"
            appendStringOption builder "media_type" value.MediaType
            appendInt64Option builder "byte_count" value.ByteCount
            appendStringOption builder "capture_notes" value.CaptureNotes
            appendBlank builder
            appendRawObjectRef builder "body.captured_object" value.CapturedObject
        | ImportCompleted value ->
            appendTableHeader builder "body"
            appendString builder "import_id" (ImportId.format value.ImportId)
            appendStringOption builder "window_kind" (value.Window |> Option.map importWindowValue)
            appendStringOption builder "notes" value.Notes
            appendBlank builder
            appendImportCounts builder "body.counts" value.Counts

    /// <summary>
    /// Serializes a canonical event into its persisted TOML form.
    /// </summary>
    let serializeCanonicalEvent (event: CanonicalEvent) =
        let builder = create ()
        let streamRef = streamRefForEvent event
        appendEnvelope builder streamRef event
        appendBody builder event.Body
        render builder

    /// <summary>
    /// Serializes an import manifest into its persisted TOML form.
    /// </summary>
    let serializeImportManifest (manifest: ImportManifest) =
        let builder = create ()

        appendAssignment builder "schema_version" "1"
        appendString builder "manifest_kind" "import_manifest"
        appendString builder "import_id" (ImportId.format manifest.ImportId)
        appendString builder "provider" (providerKindValue manifest.Provider)
        appendString builder "source_acquisition" (sourceAcquisitionValue manifest.SourceAcquisition)
        appendStringOption builder "normalization_version" (manifest.NormalizationVersion |> Option.map normalizationVersionValue)
        appendStringOption builder "window_kind" (manifest.Window |> Option.map importWindowValue)
        appendTimestamp builder "imported_at" (importedAtValue manifest.ImportedAt)

        if not manifest.Notes.IsEmpty then
            appendStringList builder "notes" manifest.Notes

        if not manifest.NewCanonicalEventIds.IsEmpty then
            manifest.NewCanonicalEventIds
            |> List.map CanonicalEventId.format
            |> appendStringList builder "new_canonical_event_ids"

        appendBlank builder
        appendRawObjectRef builder "root_artifact" manifest.RootArtifact
        appendImportCounts builder "counts" manifest.Counts
        render builder

    /// <summary>
    /// Serializes a graph assertion into its persisted TOML form.
    /// </summary>
    let serializeGraphAssertion (assertion: GraphAssertion) =
        let builder = create ()

        appendAssignment builder "schema_version" "1"
        appendString builder "assertion_kind" "graph_assertion"
        appendString builder "fact_id" (FactId.format assertion.FactId)
        appendString builder "subject_node_id" (NodeId.format assertion.Subject)
        appendString builder "predicate" (edgeKindValue assertion.Predicate)
        appendStringOption builder "domain_id" (assertion.DomainId |> Option.map DomainId.value)
        appendStringOption builder "bounded_context_id" (assertion.BoundedContextId |> Option.map BoundedContextId.value)
        appendStringOption builder "lens_id" (assertion.LensId |> Option.map LensId.value)
        appendBlank builder

        appendTableHeader builder "object"

        match assertion.Object with
        | NodeRef nodeId ->
            appendString builder "kind" "node_ref"
            appendString builder "node_id" (NodeId.format nodeId)
        | Literal literal ->
            appendString builder "kind" "literal"

            match literal with
            | StringValue _ -> appendString builder "value_type" "string"
            | IntValue _ -> appendString builder "value_type" "int64"
            | DecimalValue _ -> appendString builder "value_type" "decimal"
            | BoolValue _ -> appendString builder "value_type" "bool"
            | TimestampValue _ -> appendString builder "value_type" "timestamp"

            appendAssignment builder "value" (graphValueLiteral literal)

        appendBlank builder
        appendTableHeader builder "provenance"
        appendStringOption builder "import_id" (assertion.Provenance.ImportId |> Option.map ImportId.format)
        appendString builder "source_acquisition" (sourceAcquisitionValue assertion.Provenance.SourceAcquisition)

        if not assertion.Provenance.SupportingEventIds.IsEmpty then
            assertion.Provenance.SupportingEventIds
            |> List.map CanonicalEventId.format
            |> appendStringList builder "supporting_event_ids"

        appendBlank builder
        appendProviderRefs builder assertion.Provenance.ProviderRefs
        appendRawObjects builder assertion.Provenance.RawObjects
        render builder

    /// <summary>
    /// Computes the canonical relative path for an event based on its stream kind and event identifier.
    /// </summary>
    let canonicalEventRelativePath (event: CanonicalEvent) =
        let streamRef = streamRefForEvent event
        let eventFile = $"{CanonicalEventId.format event.Envelope.EventId}__{fileEventKindValue event.Body}.toml"

        let directory =
            match streamRef.Kind with
            | ConversationStream -> Path.Combine("events", "conversations", streamRef.StreamId)
            | ArtifactStream -> Path.Combine("events", "artifacts", streamRef.StreamId)
            | ImportStream -> Path.Combine("events", "imports", streamRef.StreamId)

        Path.Combine(directory, eventFile) |> normalizePath

    /// <summary>
    /// Computes the canonical relative path for an import manifest.
    /// </summary>
    let importManifestRelativePath (manifest: ImportManifest) =
        Path.Combine("imports", $"{ImportId.format manifest.ImportId}.toml") |> normalizePath

    /// <summary>
    /// Computes the canonical relative path for a graph assertion file.
    /// </summary>
    let graphAssertionRelativePath (assertion: GraphAssertion) =
        Path.Combine("graph", "assertions", $"{FactId.format assertion.FactId}.toml") |> normalizePath

    /// <summary>
    /// Writes one canonical event to the event store and returns its relative path.
    /// </summary>
    let writeCanonicalEvent (rootPath: string) (event: CanonicalEvent) =
        let relativePath = canonicalEventRelativePath event
        let destinationPath = Path.Combine(rootPath, relativePath)
        ensureDirectoryForFile destinationPath
        File.WriteAllText(destinationPath, serializeCanonicalEvent event)
        relativePath

    /// <summary>
    /// Writes a batch of canonical events and returns their relative paths in order.
    /// </summary>
    let writeCanonicalEvents (rootPath: string) (events: CanonicalEvent list) =
        events |> List.map (writeCanonicalEvent rootPath)

    /// <summary>
    /// Writes an import manifest and returns its relative path.
    /// </summary>
    let writeImportManifest (rootPath: string) (manifest: ImportManifest) =
        let relativePath = importManifestRelativePath manifest
        let destinationPath = Path.Combine(rootPath, relativePath)
        ensureDirectoryForFile destinationPath
        File.WriteAllText(destinationPath, serializeImportManifest manifest)
        relativePath

    /// <summary>
    /// Writes a graph assertion file and returns its relative path.
    /// </summary>
    let writeGraphAssertion (rootPath: string) (assertion: GraphAssertion) =
        let relativePath = graphAssertionRelativePath assertion
        let destinationPath = Path.Combine(rootPath, relativePath)
        ensureDirectoryForFile destinationPath
        File.WriteAllText(destinationPath, serializeGraphAssertion assertion)
        relativePath
