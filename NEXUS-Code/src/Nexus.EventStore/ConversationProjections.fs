namespace Nexus.EventStore

open System
open System.Collections.Generic
open System.Globalization
open System.IO
open System.Text.Json
open System.Text.Json.Nodes

[<RequireQualifiedAccess>]
module ConversationProjections =
    type private MessagePreviewState =
        { MessageId: string
          mutable Role: string option
          mutable SequenceHint: int option
          mutable OccurredAt: DateTimeOffset option
          mutable Excerpt: string option
          mutable ArtifactReferenceCount: int }

    type private ConversationProjectionState =
        { ConversationId: string
          Providers: HashSet<string>
          ProviderConversationIds: HashSet<string>
          ImportIds: HashSet<string>
          MessagesById: Dictionary<string, MessagePreviewState>
          mutable Title: string option
          mutable MessageCount: int
          mutable ArtifactReferenceCount: int
          mutable RevisionCount: int
          mutable FirstOccurredAt: DateTimeOffset option
          mutable LastOccurredAt: DateTimeOffset option
          mutable LastObservedAt: DateTimeOffset option }

    let private tryParseInt (value: string option) =
        value
        |> Option.bind (fun raw ->
            match Int32.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture) with
            | true, parsedValue -> Some parsedValue
            | _ -> None)

    let private tryParseTimestamp (value: string option) =
        value
        |> Option.bind (fun raw ->
            match DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
            | true, parsedValue -> Some parsedValue
            | _ -> None)

    let private tryParseJsonNode (value: string) =
        try
            JsonNode.Parse(value) |> Some
        with _ ->
            None

    let private truncate limit (value: string) =
        if String.IsNullOrWhiteSpace(value) then
            None
        elif value.Length <= limit then
            Some value
        else
            Some (value.Substring(0, limit - 1) + "…")

    let private projectionState conversationId =
        { ConversationId = conversationId
          Providers = HashSet(StringComparer.Ordinal)
          ProviderConversationIds = HashSet(StringComparer.Ordinal)
          ImportIds = HashSet(StringComparer.Ordinal)
          MessagesById = Dictionary(StringComparer.Ordinal)
          Title = None
          MessageCount = 0
          ArtifactReferenceCount = 0
          RevisionCount = 0
          FirstOccurredAt = None
          LastOccurredAt = None
          LastObservedAt = None }

    let private getOrAddState (states: Dictionary<string, ConversationProjectionState>) conversationId =
        match states.TryGetValue(conversationId) with
        | true, state -> state
        | false, _ ->
            let state = projectionState conversationId
            states[conversationId] <- state
            state

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

    let private addProviderRefs state document =
        for table in TomlDocument.tableArray "provider_refs" document do
            match table.TryGetValue("provider") with
            | true, providerValue -> state.Providers.Add(providerValue) |> ignore
            | _ -> ()

            match table.TryGetValue("conversation_native_id") with
            | true, conversationNativeId -> state.ProviderConversationIds.Add(conversationNativeId) |> ignore
            | _ -> ()

    let private addImportId state document =
        TomlDocument.tryScalar "import_id" document
        |> Option.iter (fun importId -> state.ImportIds.Add(importId) |> ignore)

    let private addObservedTimes state document =
        let observedAt = TomlDocument.tryScalar "observed_at" document |> tryParseTimestamp
        let occurredAt = TomlDocument.tryScalar "occurred_at" document |> tryParseTimestamp

        state.LastObservedAt <- setLater state.LastObservedAt observedAt
        state.FirstOccurredAt <- setEarlier state.FirstOccurredAt occurredAt
        state.LastOccurredAt <- setLater state.LastOccurredAt occurredAt

    let private tryJsonStringProperty propertyName (node: JsonNode) =
        match node with
        | :? JsonObject as jsonObject ->
            let mutable value = Unchecked.defaultof<JsonNode>

            if jsonObject.TryGetPropertyValue(propertyName, &value) && not (isNull value) then
                match value with
                | :? JsonValue as jsonValue ->
                    match jsonValue.TryGetValue<string>() with
                    | true, parsedValue when not (String.IsNullOrWhiteSpace(parsedValue)) -> Some parsedValue
                    | _ -> None
                | _ -> None
            else
                None
        | _ -> None

    let private compactJsonForPreview (node: JsonNode) =
        node.ToJsonString(JsonSerializerOptions(WriteIndented = false))

    let private isProviderWrapperJson (text: string) =
        match tryParseJsonNode text with
        | Some (:? JsonObject as jsonObject) ->
            let hasProperty name =
                let mutable value = Unchecked.defaultof<JsonNode>
                jsonObject.TryGetPropertyValue(name, &value) && not (isNull value)

            hasProperty "type"
            && [ "start_timestamp"; "stop_timestamp"; "flags"; "citations"; "tool_use_id"; "display_content" ]
               |> List.exists hasProperty
        | _ -> false

    let rec private parseJsonSegmentPreview kind (text: string) =
        let fromStringifiedJson candidate =
            candidate
            |> tryParseJsonNode
            |> Option.bind (fun parsedNode -> parseJsonSegmentPreview kind (compactJsonForPreview parsedNode))

        let parseContentItems (node: JsonNode) =
            match node with
            | :? JsonArray as jsonArray ->
                jsonArray
                |> Seq.choose (fun item ->
                    [ tryJsonStringProperty "text" item
                      tryJsonStringProperty "summary" item
                      tryJsonStringProperty "message" item ]
                    |> List.tryPick id)
                |> Seq.toList
            | _ -> []

        match tryParseJsonNode text with
        | None -> None
        | Some jsonNode ->
            let objectValues =
                [ tryJsonStringProperty "text" jsonNode
                  tryJsonStringProperty "prompt" jsonNode
                  tryJsonStringProperty "message" jsonNode
                  tryJsonStringProperty "thinking" jsonNode
                  tryJsonStringProperty "summary" jsonNode
                  tryJsonStringProperty "name" jsonNode ]
                |> List.choose id

            let summaryValues =
                match jsonNode with
                | :? JsonObject as jsonObject ->
                    let mutable summariesNode = Unchecked.defaultof<JsonNode>

                    if jsonObject.TryGetPropertyValue("summaries", &summariesNode) && not (isNull summariesNode) then
                        parseContentItems summariesNode
                    else
                        []
                | _ -> []

            let contentValues =
                match jsonNode with
                | :? JsonObject as jsonObject ->
                    let mutable contentNode = Unchecked.defaultof<JsonNode>

                    if jsonObject.TryGetPropertyValue("content", &contentNode) && not (isNull contentNode) then
                        parseContentItems contentNode
                    else
                        []
                | _ -> []

            let displayValues =
                match jsonNode with
                | :? JsonObject as jsonObject ->
                    let mutable displayNode = Unchecked.defaultof<JsonNode>

                    if jsonObject.TryGetPropertyValue("display_content", &displayNode) && not (isNull displayNode) then
                        [ tryJsonStringProperty "text" displayNode ] |> List.choose id
                    else
                        []
                | _ -> []

            let candidates = objectValues @ summaryValues @ contentValues @ displayValues

            candidates
            |> List.tryPick (fun candidate ->
                let trimmed = candidate.Trim()

                if String.IsNullOrWhiteSpace(trimmed) then
                    None
                elif trimmed = "\"\"" then
                    None
                elif (trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal))
                     && kind <> "plain_text"
                     && kind <> "markdown"
                     && kind <> "quote"
                     && kind <> "code" then
                    fromStringifiedJson trimmed
                else
                    Some trimmed)

    let private normalizeSegmentText kind (text: string) =
        let trimmed = text.Trim()
        let looksLikeJson =
            trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal)

        if String.IsNullOrWhiteSpace(trimmed) || trimmed = "\"\"" || trimmed = "null" then
            None
        else
            match kind with
            | "plain_text"
            | "markdown"
            | "quote"
            | "code" ->
                if looksLikeJson then
                    match parseJsonSegmentPreview kind trimmed with
                    | Some preview -> Some preview
                    | None when isProviderWrapperJson trimmed -> None
                    | None -> Some trimmed
                else
                    Some trimmed
            | "thinking"
            | "reasoning"
            | "tool_use"
            | "tool_result"
            | "multimodal"
            | "unknown"
            | "token_budget" -> parseJsonSegmentPreview kind trimmed
            | _ when trimmed.StartsWith("{", StringComparison.Ordinal) || trimmed.StartsWith("[", StringComparison.Ordinal) ->
                parseJsonSegmentPreview kind trimmed
            | _ -> Some trimmed

    let private messageExcerpt document =
        let segments =
            TomlDocument.tableArray "body.segments" document
            |> List.choose (fun table ->
                match table.TryGetValue("kind"), table.TryGetValue("text") with
                | (true, kind), (true, value) ->
                    normalizeSegmentText kind value
                    |> Option.map (fun normalizedText -> kind, normalizedText)
                | _ -> None)

        let preferredKinds =
            [ "plain_text"; "markdown"; "quote"; "code" ]

        let fallbackKinds =
            [ "multimodal"; "tool_result"; "tool_use" ]

        let reasoningKinds =
            [ "thinking"; "reasoning" ]

        let collect kinds =
            segments
            |> List.choose (fun (kind, text) ->
                if kinds |> List.contains kind then Some text else None)

        let excerptTexts =
            let preferred = collect preferredKinds

            if not preferred.IsEmpty then
                preferred
            else
                let fallback = collect fallbackKinds

                if not fallback.IsEmpty then
                    fallback
                else
                    collect reasoningKinds

        excerptTexts
        |> String.concat "\n"
        |> truncate 160

    let private addMessageObserved state document =
        match TomlDocument.tryScalar "message_id" document with
        | Some messageId ->
            let role = TomlDocument.tryTableValue "body" "role" document
            let sequenceHint = TomlDocument.tryTableValue "body" "sequence_hint" document |> tryParseInt
            let occurredAt = TomlDocument.tryScalar "occurred_at" document |> tryParseTimestamp
            let excerpt = messageExcerpt document

            match state.MessagesById.TryGetValue(messageId) with
            | true, preview ->
                preview.Role <- role |> Option.orElse preview.Role
                preview.SequenceHint <- sequenceHint |> Option.orElse preview.SequenceHint
                preview.OccurredAt <- occurredAt |> Option.orElse preview.OccurredAt
                preview.Excerpt <- excerpt |> Option.orElse preview.Excerpt
            | false, _ ->
                let preview =
                    { MessageId = messageId
                      Role = role
                      SequenceHint = sequenceHint
                      OccurredAt = occurredAt
                      Excerpt = excerpt
                      ArtifactReferenceCount = 0 }

                state.MessagesById[messageId] <- preview
                state.MessageCount <- state.MessageCount + 1
        | None -> ()

    let private addArtifactReference state document =
        state.ArtifactReferenceCount <- state.ArtifactReferenceCount + 1

        match TomlDocument.tryScalar "message_id" document with
        | Some messageId ->
            match state.MessagesById.TryGetValue(messageId) with
            | true, preview ->
                preview.ArtifactReferenceCount <- preview.ArtifactReferenceCount + 1
            | _ -> ()
        | None -> ()

    let private addConversationEvent state document =
        addProviderRefs state document
        addImportId state document
        addObservedTimes state document

        match TomlDocument.tryScalar "event_kind" document with
        | Some "provider_conversation_observed" ->
            match state.Title, TomlDocument.tryTableValue "body" "title" document with
            | None, Some title -> state.Title <- Some title
            | _ -> ()
        | Some "provider_message_observed" ->
            addMessageObserved state document
        | Some "provider_message_revision_observed" ->
            state.RevisionCount <- state.RevisionCount + 1
        | _ -> ()

    let private addArtifactEvent state document =
        addProviderRefs state document
        addImportId state document
        addObservedTimes state document

        match TomlDocument.tryScalar "event_kind" document with
        | Some "artifact_referenced" -> addArtifactReference state document
        | _ -> ()

    let private projectionRelativePath conversationId =
        Path.Combine("projections", "conversations", $"{conversationId}.toml")
            .Replace('\\', '/')

    let private writeProjection rootPath (state: ConversationProjectionState) =
        let builder = create ()

        appendAssignment builder "schema_version" "1"
        appendString builder "projection_kind" "conversation_projection"
        appendString builder "conversation_id" state.ConversationId
        appendStringOption builder "title" state.Title
        appendInt builder "message_count" state.MessageCount
        appendInt builder "artifact_reference_count" state.ArtifactReferenceCount
        appendInt builder "revision_count" state.RevisionCount
        appendTimestampOption builder "first_occurred_at" state.FirstOccurredAt
        appendTimestampOption builder "last_occurred_at" state.LastOccurredAt
        appendTimestampOption builder "last_observed_at" state.LastObservedAt

        if state.Providers.Count > 0 then
            state.Providers |> Seq.sort |> Seq.toList |> appendStringList builder "providers"

        if state.ProviderConversationIds.Count > 0 then
            state.ProviderConversationIds |> Seq.sort |> Seq.toList |> appendStringList builder "provider_conversation_ids"

        if state.ImportIds.Count > 0 then
            state.ImportIds |> Seq.sort |> Seq.toList |> appendStringList builder "import_ids"

        appendBlank builder

        state.MessagesById.Values
        |> Seq.sortBy (fun preview ->
            preview.SequenceHint |> Option.defaultValue Int32.MaxValue,
            preview.OccurredAt |> Option.defaultValue DateTimeOffset.MaxValue,
            preview.MessageId)
        |> Seq.iter (fun preview ->
            appendArrayTableHeader builder "messages"
            appendString builder "message_id" preview.MessageId
            appendStringOption builder "role" preview.Role
            appendIntOption builder "sequence_hint" preview.SequenceHint
            appendTimestampOption builder "occurred_at" preview.OccurredAt
            appendStringOption builder "excerpt" preview.Excerpt
            appendInt builder "artifact_reference_count" preview.ArtifactReferenceCount
            appendBlank builder)

        let relativePath = projectionRelativePath state.ConversationId
        let absolutePath = Path.Combine(rootPath, relativePath)
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)) |> ignore
        File.WriteAllText(absolutePath, render builder)
        relativePath

    let rebuild rootPath =
        let absoluteRoot = Path.GetFullPath(rootPath)
        let states = Dictionary<string, ConversationProjectionState>(StringComparer.Ordinal)
        let conversationEventsRoot = Path.Combine(absoluteRoot, "events", "conversations")
        let artifactEventsRoot = Path.Combine(absoluteRoot, "events", "artifacts")
        let projectionsRoot = Path.Combine(absoluteRoot, "projections", "conversations")

        if Directory.Exists(conversationEventsRoot) then
            Directory.EnumerateFiles(conversationEventsRoot, "*.toml", SearchOption.AllDirectories)
            |> Seq.iter (fun path ->
                let document = File.ReadAllText(path) |> TomlDocument.parse

                match TomlDocument.tryScalar "conversation_id" document with
                | Some conversationId ->
                    let state = getOrAddState states conversationId
                    addConversationEvent state document
                | None -> ())

        if Directory.Exists(artifactEventsRoot) then
            Directory.EnumerateFiles(artifactEventsRoot, "*.toml", SearchOption.AllDirectories)
            |> Seq.iter (fun path ->
                let document = File.ReadAllText(path) |> TomlDocument.parse

                match TomlDocument.tryScalar "conversation_id" document with
                | Some conversationId ->
                    let state = getOrAddState states conversationId
                    addArtifactEvent state document
                | None -> ())

        if Directory.Exists(projectionsRoot) then
            Directory.Delete(projectionsRoot, true)

        Directory.CreateDirectory(projectionsRoot) |> ignore

        states.Values
        |> Seq.sortBy (fun state -> state.ConversationId)
        |> Seq.map (writeProjection absoluteRoot)
        |> Seq.toList
