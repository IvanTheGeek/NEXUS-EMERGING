namespace Nexus.Importers

open System
open System.IO
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open Nexus.Domain

[<RequireQualifiedAccess>]
module ProviderAdapters =
    let private jsonOptions = JsonSerializerOptions(WriteIndented = false)

    module private Json =
        let private tryAsValue<'T> (node: JsonNode) =
            if isNull node then
                None
            else
                match node with
                | :? JsonValue as value ->
                    match value.TryGetValue<'T>() with
                    | true, parsedValue -> Some parsedValue
                    | _ -> None
                | _ -> None

        let tryObject (node: JsonNode) =
            match node with
            | null -> None
            | :? JsonObject as value -> Some value
            | _ -> None

        let tryArray (node: JsonNode) =
            match node with
            | null -> None
            | :? JsonArray as value -> Some value
            | _ -> None

        let tryProperty (name: string) (node: JsonNode) =
            match tryObject node with
            | Some jsonObject ->
                let mutable value = Unchecked.defaultof<JsonNode>

                if jsonObject.TryGetPropertyValue(name, &value) && not (isNull value) then
                    Some value
                else
                    None
            | None -> None

        let tryString (node: JsonNode) =
            tryAsValue<string> node
            |> Option.orElseWith (fun () -> tryAsValue<int64> node |> Option.map string)
            |> Option.orElseWith (fun () -> tryAsValue<int> node |> Option.map string)
            |> Option.orElseWith (fun () -> tryAsValue<double> node |> Option.map (fun value -> value.ToString("0.################", Globalization.CultureInfo.InvariantCulture)))

        let tryBool (node: JsonNode) = tryAsValue<bool> node

        let tryFloat (node: JsonNode) = tryAsValue<double> node

        let tryDateTimeOffset (node: JsonNode) =
            tryString node
            |> Option.bind (fun value ->
                match DateTimeOffset.TryParse(value, Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.AssumeUniversal) with
                | true, parsedValue -> Some parsedValue
                | _ -> None)

        let compactJson (node: JsonNode) =
            if isNull node then String.Empty else node.ToJsonString(jsonOptions)

        let tryPropertyPath (path: string list) (node: JsonNode) =
            (Some node, path)
            ||> List.fold (fun currentNode pathPart ->
                currentNode |> Option.bind (tryProperty pathPart))

        let tryStringProperty (name: string) (node: JsonNode) =
            tryProperty name node |> Option.bind tryString

    let private tryParseJsonNodeFromText (value: string) =
        try
            JsonNode.Parse(value) |> Some
        with _ ->
            None

    let private normalizeTextCandidate (value: string) =
        let normalized = value.Trim()

        if String.IsNullOrWhiteSpace(normalized) || normalized = "\"\"" || normalized = "null" then
            None
        else
            Some normalized

    let private firstNonBlank (values: string option list) =
        values |> List.tryPick (Option.bind normalizeTextCandidate)

    let private joinNonBlank (values: string list) =
        values
        |> List.choose normalizeTextCandidate
        |> List.distinct
        |> function
            | [] -> None
            | cleaned -> cleaned |> String.concat "\n" |> Some

    let private messageRoleFromText (value: string) =
        match value.Trim().ToLowerInvariant() with
        | "human"
        | "user" -> Human
        | "assistant" -> Assistant
        | "system" -> System
        | "tool" -> Tool
        | other -> OtherRole other

    let private messageSegmentKindFromText (value: string) =
        match value.Trim().ToLowerInvariant() with
        | "text" -> PlainText
        | "text/markdown" -> Markdown
        | "markdown" -> Markdown
        | "quote" -> Quote
        | "tether_quote" -> Quote
        | "code" -> Code
        | "code_block" -> Code
        | "json_block" -> Code
        | "execution_output" -> Code
        | "thinking"
        | "reasoning" -> Reasoning
        | "thoughts"
        | "reasoning_recap" -> Reasoning
        | "tool_use" -> ToolUse
        | "tool_result" -> ToolResult
        | "multimodal_text"
        | "image_asset_pointer"
        | "audio_asset_pointer"
        | "audio_transcription"
        | "real_time_user_audio_video_asset_pointer"
        | "multimodal" -> Multimodal
        | other -> UnknownSegment other

    let private buildContentSignature (role: MessageRole) (modelName: string option) (segments: MessageSegment list) (artifacts: ParsedArtifactReference list) =
        let builder = StringBuilder()
        let roleValue =
            match role with
            | Human -> "human"
            | Assistant -> "assistant"
            | System -> "system"
            | Tool -> "tool"
            | OtherRole value -> value

        builder.Append("role=") |> ignore
        builder.Append(roleValue) |> ignore

        match modelName with
        | Some value -> builder.Append("|model=").Append(value) |> ignore
        | None -> ()

        for segment in segments do
            builder.Append("|segment=").Append(segment.Kind.ToString()).Append(':').Append(segment.Text) |> ignore

        for artifact in artifacts do
            builder.Append("|artifact=")
                .Append(artifact.ProviderArtifactId |> Option.defaultValue String.Empty)
                .Append(':')
                .Append(artifact.FileName |> Option.defaultValue String.Empty)
                .Append(':')
                .Append(artifact.MediaType |> Option.defaultValue String.Empty)
                .Append(':')
                .Append(artifact.Disposition.ToString())
            |> ignore

        builder.ToString()

    let private attachmentDisposition (extractedNames: Set<string>) (fileName: string option) =
        match fileName with
        | Some value when extractedNames.Contains(value.Trim().ToLowerInvariant()) -> PayloadIncluded
        | Some _ -> PayloadMissing
        | None -> PayloadUnknown

    let private claudeArrayTexts propertyName candidateProperties (node: JsonNode) =
        Json.tryProperty propertyName node
        |> Option.bind Json.tryArray
        |> Option.map (fun values ->
            values
            |> Seq.choose (fun value ->
                candidateProperties
                |> List.tryPick (fun propertyName -> Json.tryStringProperty propertyName value |> Option.bind normalizeTextCandidate))
            |> Seq.toList)
        |> Option.defaultValue []

    let private extractClaudeSegmentText segmentType (contentNode: JsonNode) =
        let textFields =
            [ "text"; "thinking"; "message"; "content"; "value"; "summary"; "subtitle"; "title" ]
            |> List.map (fun propertyName -> Json.tryStringProperty propertyName contentNode)

        let displayText =
            Json.tryPropertyPath [ "display_content"; "text" ] contentNode
            |> Option.bind Json.tryString

        let summaryTexts = claudeArrayTexts "summaries" [ "summary"; "text" ] contentNode
        let contentTexts = claudeArrayTexts "content" [ "text"; "summary"; "title"; "subtitle" ] contentNode
        let richContentTexts = claudeArrayTexts "items" [ "title"; "subtitle"; "text" ] contentNode

        match segmentType |> Option.map (fun (value: string) -> value.Trim().ToLowerInvariant()) with
        | Some "text" ->
            firstNonBlank textFields
        | Some "thinking" ->
            firstNonBlank [ Json.tryStringProperty "thinking" contentNode; joinNonBlank summaryTexts ]
        | Some "tool_use" ->
            firstNonBlank
                [ Json.tryStringProperty "message" contentNode
                  displayText
                  Json.tryStringProperty "name" contentNode |> Option.map (fun name -> $"Tool used: {name}") ]
        | Some "tool_result" ->
            firstNonBlank
                [ joinNonBlank contentTexts
                  displayText
                  Json.tryStringProperty "message" contentNode ]
        | Some "code_block"
        | Some "json_block"
        | Some "table"
        | Some "rich_content"
        | Some "rich_link"
        | Some "local_resource" ->
            firstNonBlank [ firstNonBlank textFields; joinNonBlank contentTexts; joinNonBlank richContentTexts ]
        | Some "generic_metadata"
        | Some "webpage_metadata"
        | Some "knowledge"
        | Some "web_search_citation"
        | Some "token_budget" -> None
        | _ ->
            firstNonBlank [ firstNonBlank textFields; displayText; joinNonBlank contentTexts; joinNonBlank summaryTexts; joinNonBlank richContentTexts ]

    let private parseClaudeSegments (messageNode: JsonNode) =
        let segmentsFromContent =
            Json.tryProperty "content" messageNode
            |> Option.bind Json.tryArray
            |> Option.map (fun contentArray ->
                contentArray
                |> Seq.choose (fun contentNode ->
                    let rawType =
                        Json.tryProperty "type" contentNode
                        |> Option.bind Json.tryString

                    let segmentKind =
                        rawType
                        |> Option.map messageSegmentKindFromText
                        |> Option.defaultValue PlainText

                    match extractClaudeSegmentText rawType contentNode with
                    | Some text ->
                        Some
                            { Kind = segmentKind
                              Text = text }
                    | _ -> None)
                |> Seq.toList)
            |> Option.defaultValue []

        if segmentsFromContent.IsEmpty then
            Json.tryProperty "text" messageNode
            |> Option.bind Json.tryString
            |> Option.bind normalizeTextCandidate
            |> Option.map (fun text ->
                [ { Kind = PlainText
                    Text = text } ])
            |> Option.defaultValue []
        else
            segmentsFromContent

    let private parseClaudeArtifacts extractedNames (messageNode: JsonNode) =
        let fromArray propertyName fileNameProperty mediaTypeProperty =
            Json.tryProperty propertyName messageNode
            |> Option.bind Json.tryArray
            |> Option.map (fun values ->
                values
                |> Seq.map (fun value ->
                    let fileName =
                        Json.tryProperty fileNameProperty value
                        |> Option.bind Json.tryString
                        |> Option.orElseWith (fun () -> Json.tryProperty "name" value |> Option.bind Json.tryString)

                    let mediaType =
                        Json.tryProperty mediaTypeProperty value
                        |> Option.bind Json.tryString
                        |> Option.orElseWith (fun () -> Json.tryProperty "mime_type" value |> Option.bind Json.tryString)

                    let providerArtifactId =
                        Json.tryProperty "uuid" value
                        |> Option.bind Json.tryString
                        |> Option.orElse fileName

                    { ProviderArtifactId = providerArtifactId
                      FileName = fileName
                      MediaType = mediaType
                      Disposition = attachmentDisposition extractedNames fileName })
                |> Seq.toList)
            |> Option.defaultValue []

        (fromArray "attachments" "file_name" "content_type")
        @ (fromArray "files" "file_name" "content_type")

    let private parseClaudeMessage extractedNames (messageNode: JsonNode) =
        match Json.tryProperty "uuid" messageNode |> Option.bind Json.tryString with
        | None -> None
        | Some messageId ->
            let role =
                Json.tryProperty "sender" messageNode
                |> Option.bind Json.tryString
                |> Option.map messageRoleFromText
                |> Option.defaultValue (OtherRole "unknown")

            let segments = parseClaudeSegments messageNode
            let modelName = Json.tryProperty "model" messageNode |> Option.bind Json.tryString
            let artifacts = parseClaudeArtifacts extractedNames messageNode

            Some
                { ProviderMessageId = messageId
                  Role = role
                  Segments = segments
                  OccurredAt = Json.tryProperty "created_at" messageNode |> Option.bind Json.tryDateTimeOffset
                  ModelName = modelName
                  SequenceHint = None
                  ContentSignature = buildContentSignature role modelName segments artifacts
                  ArtifactReferences = artifacts }

    let private parseClaudeConversation extractedNames (conversationNode: JsonNode) =
        match Json.tryProperty "uuid" conversationNode |> Option.bind Json.tryString with
        | None -> None
        | Some conversationId ->
            let rawMessages =
                Json.tryProperty "chat_messages" conversationNode
                |> Option.bind Json.tryArray
                |> Option.map Seq.toList
                |> Option.defaultValue []

            let messages =
                rawMessages
                |> List.choose (parseClaudeMessage extractedNames)
                |> List.mapi (fun index message -> { message with SequenceHint = Some(index + 1) })

            Some
                { ProviderConversationId = conversationId
                  Title = Json.tryProperty "name" conversationNode |> Option.bind Json.tryString
                  IsArchived = None
                  OccurredAt = Json.tryProperty "created_at" conversationNode |> Option.bind Json.tryDateTimeOffset
                  MessageCountHint = Some messages.Length
                  Messages = messages }

    let private parseClaude (window: ImportWindowKind option) (sourceFileName: string) (sourceByteCount: int64) extractedEntries extractedNames (conversationsJsonPath: string) =
        let rootNode = JsonNode.Parse(File.ReadAllText(conversationsJsonPath))

        let conversations =
            match Json.tryArray rootNode with
            | Some conversationArray -> conversationArray |> Seq.choose (parseClaudeConversation extractedNames) |> Seq.toList
            | None -> []

        { Provider = Claude
          Window = window
          SourceFileName = sourceFileName
          SourceByteCount = sourceByteCount
          ExtractedEntries = extractedEntries
          Conversations = conversations
          Notes =
            [ "Parsed Claude export conversations.json"
              "Projects, memories, and users remain preserved in raw form for now." ] }

    let private parseChatGptSegments (messageNode: JsonNode) =
        let contentNode = Json.tryProperty "content" messageNode
        let contentType =
            contentNode
            |> Option.bind (Json.tryProperty "content_type")
            |> Option.bind Json.tryString

        let rec extractChatGptTextFromNode contentType (node: JsonNode) =
            let directText =
                [ Json.tryStringProperty "text" node
                  Json.tryStringProperty "content" node
                  Json.tryStringProperty "result" node
                  Json.tryStringProperty "prompt" node
                  Json.tryStringProperty "caption" node
                  Json.tryStringProperty "title" node
                  Json.tryStringProperty "alt" node ]
                |> firstNonBlank

            let thoughtTexts =
                Json.tryProperty "thoughts" node
                |> Option.bind Json.tryArray
                |> Option.map (fun thoughts ->
                    thoughts
                    |> Seq.choose (fun thought ->
                        [ Json.tryStringProperty "summary" thought
                          Json.tryStringProperty "content" thought ]
                        |> List.tryPick (Option.bind normalizeTextCandidate))
                    |> Seq.toList)
                |> Option.defaultValue []

            match contentType |> Option.map (fun (value: string) -> value.Trim().ToLowerInvariant()) with
            | Some "thoughts" ->
                joinNonBlank thoughtTexts |> Option.orElse directText
            | Some "reasoning_recap" ->
                firstNonBlank [ directText; Json.tryStringProperty "content" node ]
            | Some "audio_transcription" ->
                directText
            | Some "image_asset_pointer"
            | Some "audio_asset_pointer"
            | Some "real_time_user_audio_video_asset_pointer" -> None
            | _ ->
                directText

        let parseChatGptPart (part: JsonNode) =
            match Json.tryString part with
            | Some value ->
                match normalizeTextCandidate value with
                | Some text ->
                    Some
                        { Kind = PlainText
                          Text = text }
                | None ->
                    tryParseJsonNodeFromText value
                    |> Option.bind (fun parsedNode ->
                        let parsedContentType = Json.tryStringProperty "content_type" parsedNode

                        extractChatGptTextFromNode parsedContentType parsedNode
                        |> Option.map (fun text ->
                            let segmentKind =
                                parsedContentType
                                |> Option.map messageSegmentKindFromText
                                |> Option.defaultValue Multimodal

                            { Kind = segmentKind
                              Text = text }))
            | None ->
                let partContentType = Json.tryStringProperty "content_type" part

                extractChatGptTextFromNode partContentType part
                |> Option.map (fun text ->
                    let segmentKind =
                        partContentType
                        |> Option.map messageSegmentKindFromText
                        |> Option.defaultValue Multimodal

                    { Kind = segmentKind
                      Text = text })

        let partsSegments =
            contentNode
            |> Option.bind (Json.tryProperty "parts")
            |> Option.bind Json.tryArray
            |> Option.map (fun parts ->
                parts
                |> Seq.choose parseChatGptPart
                |> Seq.toList)
            |> Option.defaultValue []

        if not partsSegments.IsEmpty then
            partsSegments
        else
            let fallbackSegment =
                contentNode
                |> Option.bind (fun node ->
                    extractChatGptTextFromNode contentType node
                    |> Option.map (fun text ->
                        { Kind =
                            contentType
                            |> Option.map messageSegmentKindFromText
                            |> Option.defaultValue PlainText
                          Text = text }))
                |> Option.orElseWith (fun () ->
                    Json.tryProperty "text" messageNode
                    |> Option.bind Json.tryString
                    |> Option.bind normalizeTextCandidate
                    |> Option.map (fun text ->
                        { Kind = PlainText
                          Text = text }))

            match fallbackSegment with
            | Some segment ->
                [ segment ]
            | _ -> []

    let private parseChatGptArtifacts extractedNames (messageNode: JsonNode) =
        Json.tryProperty "metadata" messageNode
        |> Option.bind Json.tryObject
        |> Option.bind (fun metadata ->
            Json.tryProperty "attachments" (metadata :> JsonNode))
        |> Option.bind Json.tryArray
        |> Option.map (fun attachments ->
            attachments
            |> Seq.map (fun attachment ->
                let fileName = Json.tryProperty "name" attachment |> Option.bind Json.tryString

                let mediaType =
                    Json.tryProperty "mime_type" attachment
                    |> Option.bind Json.tryString

                let providerArtifactId =
                    Json.tryProperty "id" attachment
                    |> Option.bind Json.tryString
                    |> Option.orElse fileName

                { ProviderArtifactId = providerArtifactId
                  FileName = fileName
                  MediaType = mediaType
                  Disposition = attachmentDisposition extractedNames fileName })
            |> Seq.toList)
        |> Option.defaultValue []

    let private tryUnixSecondsDateTime (node: JsonNode option) =
        node
        |> Option.bind Json.tryFloat
        |> Option.map (fun seconds -> DateTimeOffset.UnixEpoch.AddSeconds(seconds))

    let private parseChatGptMessage extractedNames index (node: JsonNode) =
        match Json.tryProperty "message" node |> Option.bind Json.tryObject with
        | None -> None
        | Some messageObject ->
            let messageNode = messageObject :> JsonNode
            let providerMessageId =
                Json.tryProperty "id" messageNode
                |> Option.bind Json.tryString
                |> Option.defaultValue String.Empty

            if String.IsNullOrWhiteSpace(providerMessageId) then
                None
            else
                let role =
                    Json.tryProperty "author" messageNode
                    |> Option.bind (Json.tryProperty "role")
                    |> Option.bind Json.tryString
                    |> Option.map messageRoleFromText
                    |> Option.defaultValue (OtherRole "unknown")

                let segments = parseChatGptSegments messageNode

                let modelName =
                    [ Json.tryProperty "metadata" messageNode |> Option.bind (Json.tryProperty "model_slug")
                      Json.tryProperty "metadata" messageNode |> Option.bind (Json.tryProperty "default_model_slug")
                      Json.tryProperty "metadata" messageNode |> Option.bind (Json.tryProperty "resolved_model_slug") ]
                    |> List.tryPick (Option.bind Json.tryString)

                let artifacts = parseChatGptArtifacts extractedNames messageNode
                let occurredAt = Json.tryProperty "create_time" messageNode |> tryUnixSecondsDateTime

                Some
                    (
                    index,
                    occurredAt,
                    { ProviderMessageId = providerMessageId
                      Role = role
                      Segments = segments
                      OccurredAt = occurredAt
                      ModelName = modelName
                      SequenceHint = None
                      ContentSignature = buildContentSignature role modelName segments artifacts
                      ArtifactReferences = artifacts }
                    )

    let private parseChatGptConversation extractedNames (conversationNode: JsonNode) =
        let providerConversationId =
            [ Json.tryProperty "id" conversationNode
              Json.tryProperty "conversation_id" conversationNode ]
            |> List.tryPick (Option.bind Json.tryString)

        match providerConversationId with
        | None -> None
        | Some conversationId ->
            let messages =
                Json.tryProperty "mapping" conversationNode
                |> Option.bind Json.tryObject
                |> Option.map (fun mapping ->
                    mapping
                    |> Seq.toList
                    |> List.mapi (fun index keyValue -> parseChatGptMessage extractedNames index keyValue.Value)
                    |> List.choose id
                    |> List.sortBy (fun (index, occurredAt, _) -> occurredAt |> Option.defaultValue DateTimeOffset.MaxValue, index)
                    |> List.mapi (fun index (_, _, message) -> { message with SequenceHint = Some(index + 1) }))
                |> Option.defaultValue []

            Some
                { ProviderConversationId = conversationId
                  Title = Json.tryProperty "title" conversationNode |> Option.bind Json.tryString
                  IsArchived = Json.tryProperty "is_archived" conversationNode |> Option.bind Json.tryBool
                  OccurredAt = Json.tryProperty "create_time" conversationNode |> tryUnixSecondsDateTime
                  MessageCountHint = Some messages.Length
                  Messages = messages }

    let private parseChatGpt (window: ImportWindowKind option) (sourceFileName: string) (sourceByteCount: int64) extractedEntries extractedNames (conversationsJsonPath: string) =
        let rootNode = JsonNode.Parse(File.ReadAllText(conversationsJsonPath))

        let conversations =
            match Json.tryArray rootNode with
            | Some conversationArray -> conversationArray |> Seq.choose (parseChatGptConversation extractedNames) |> Seq.toList
            | None -> []

        { Provider = ChatGpt
          Window = window
          SourceFileName = sourceFileName
          SourceByteCount = sourceByteCount
          ExtractedEntries = extractedEntries
          Conversations = conversations
          Notes =
            [ "Parsed ChatGPT export conversations.json"
              "Only message text and attachment references are normalized in v0." ] }

    let parse provider window sourceFileName sourceByteCount extractedEntries extractedNames conversationsJsonPath =
        match provider with
        | Claude ->
            parseClaude window sourceFileName sourceByteCount extractedEntries extractedNames conversationsJsonPath
        | ChatGpt ->
            parseChatGpt window sourceFileName sourceByteCount extractedEntries extractedNames conversationsJsonPath
        | OtherProvider value ->
            invalidArg "provider" $"Unsupported provider adapter: {value}"
