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
        | "markdown" -> Markdown
        | "quote" -> Quote
        | "code" -> Code
        | "reasoning" -> Reasoning
        | "tool_use" -> ToolUse
        | "tool_result" -> ToolResult
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

    let private parseClaudeSegments (messageNode: JsonNode) =
        let segmentsFromContent =
            Json.tryProperty "content" messageNode
            |> Option.bind Json.tryArray
            |> Option.map (fun contentArray ->
                contentArray
                |> Seq.choose (fun contentNode ->
                    let segmentKind =
                        Json.tryProperty "type" contentNode
                        |> Option.bind Json.tryString
                        |> Option.map messageSegmentKindFromText
                        |> Option.defaultValue PlainText

                    let explicitText =
                        [ "text"; "input"; "content"; "value" ]
                        |> List.tryPick (fun propertyName ->
                            Json.tryProperty propertyName contentNode |> Option.bind Json.tryString)

                    match explicitText with
                    | Some text when not (String.IsNullOrWhiteSpace(text)) ->
                        Some
                            { Kind = segmentKind
                              Text = text }
                    | _ when Json.compactJson contentNode |> String.IsNullOrWhiteSpace |> not ->
                        Some
                            { Kind = segmentKind
                              Text = Json.compactJson contentNode }
                    | _ -> None)
                |> Seq.toList)
            |> Option.defaultValue []

        if segmentsFromContent.IsEmpty then
            Json.tryProperty "text" messageNode
            |> Option.bind Json.tryString
            |> Option.filter (fun text -> not (String.IsNullOrWhiteSpace(text)))
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

        let partsSegments =
            contentNode
            |> Option.bind (Json.tryProperty "parts")
            |> Option.bind Json.tryArray
            |> Option.map (fun parts ->
                parts
                |> Seq.choose (fun part ->
                    match Json.tryString part with
                    | Some value when not (String.IsNullOrWhiteSpace(value)) ->
                        Some
                            { Kind = PlainText
                              Text = value }
                    | _ ->
                        let raw = Json.compactJson part

                        if String.IsNullOrWhiteSpace(raw) || raw = "null" then
                            None
                        else
                            Some
                                { Kind = Multimodal
                                  Text = raw })
                |> Seq.toList)
            |> Option.defaultValue []

        if not partsSegments.IsEmpty then
            partsSegments
        else
            let fallbackText =
                [ contentNode |> Option.bind (Json.tryProperty "text")
                  contentNode |> Option.bind (Json.tryProperty "result")
                  Json.tryProperty "text" messageNode ]
                |> List.tryPick id
                |> Option.bind Json.tryString

            match fallbackText with
            | Some value when not (String.IsNullOrWhiteSpace(value)) ->
                [ { Kind = PlainText
                    Text = value } ]
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
