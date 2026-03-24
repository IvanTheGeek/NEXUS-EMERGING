namespace Nexus.Importers

open System
open System.Globalization
open System.IO
open System.Text
open System.Text.Json.Nodes
open Nexus.Domain
open Nexus.EventStore

[<RequireQualifiedAccess>]
module CodexSessions =
    /// <summary>
    /// The parsed shape of a preserved Codex raw-session snapshot.
    /// </summary>
    type ParsedSnapshot =
        { RootArtifact: RawObjectRef
          SourceFileName: string
          SourceByteCount: int64
          SnapshotName: string option
          Conversations: ParsedConversation list
          Notes: string list }

    let private normalizePath (path: string) =
        path.Replace('\\', '/')

    let private tryRelativeToRoot objectsRoot absolutePath =
        let root = Path.GetFullPath(objectsRoot)
        let fullPath = Path.GetFullPath(absolutePath)
        let relativePath = Path.GetRelativePath(root, fullPath) |> normalizePath

        if relativePath.StartsWith("..", StringComparison.Ordinal) then
            None
        else
            Some relativePath

    let private requireRelativeToRoot objectsRoot absolutePath =
        match tryRelativeToRoot objectsRoot absolutePath with
        | Some relativePath -> relativePath
        | None -> invalidArg "absolutePath" $"Path is not under the objects root: {absolutePath}"

    let private tryParseDateTimeOffset (value: string) =
        match DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
        | true, parsedValue -> Some parsedValue
        | _ -> None

    let private buildContentSignature (role: MessageRole) (modelName: string option) (segments: MessageSegment list) =
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

        builder.ToString()

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

    type private SessionIndexEntry =
        { SessionId: string
          ThreadName: string option }

    type private TranscriptMessage =
        { ProviderMessageId: string
          Role: MessageRole
          Segments: MessageSegment list
          OccurredAt: DateTimeOffset option
          ModelName: string option
          SequenceHint: int option
          ContentSignature: string }

    let private parseSessionIndex (path: string) =
        if not (File.Exists(path)) then
            Map.empty
        else
            File.ReadLines(path)
            |> Seq.choose (fun line ->
                if String.IsNullOrWhiteSpace(line) then
                    None
                else
                    let node = JsonNode.Parse(line)
                    let sessionId =
                        Json.tryProperty "id" node
                        |> Option.bind Json.tryString

                    sessionId
                    |> Option.map (fun id ->
                        let threadName =
                            Json.tryProperty "thread_name" node
                            |> Option.bind Json.tryString
                            |> Option.bind (fun value ->
                                let trimmed = value.Trim()

                                if String.IsNullOrWhiteSpace(trimmed) then None else Some trimmed)

                        id,
                        { SessionId = id
                          ThreadName = threadName }))
            |> Map.ofSeq

    let private parseTranscript exportedAt titleBySessionId objectsRoot transcriptPath =
        let transcriptRelativePath = requireRelativeToRoot objectsRoot transcriptPath

        let transcriptRawObject =
            { RawObjectId = None
              Kind = SessionTranscript
              RelativePath = transcriptRelativePath
              ArchivedAt = exportedAt
              SourceDescription = Some "Codex session transcript JSONL" }

        let messages = ResizeArray<TranscriptMessage>()
        let mutable sessionId = None
        let mutable sessionOccurredAt = None
        let mutable currentModelName = None
        let mutable nextSequence = 1

        for (lineIndex, line) in File.ReadLines(transcriptPath) |> Seq.indexed do
            if not (String.IsNullOrWhiteSpace(line)) then
                let node = JsonNode.Parse(line)
                let recordType =
                    Json.tryProperty "type" node
                    |> Option.bind Json.tryString

                match recordType with
                | Some "session_meta" ->
                    let payload =
                        Json.tryProperty "payload" node
                        |> Option.defaultWith (fun () -> upcast JsonObject())

                    sessionId <-
                        Json.tryProperty "id" payload
                        |> Option.bind Json.tryString
                        |> Option.orElse sessionId

                    sessionOccurredAt <-
                        Json.tryProperty "timestamp" payload
                        |> Option.bind Json.tryString
                        |> Option.bind tryParseDateTimeOffset
                        |> Option.orElse sessionOccurredAt
                | Some "turn_context" ->
                    let payload =
                        Json.tryProperty "payload" node
                        |> Option.defaultWith (fun () -> upcast JsonObject())

                    currentModelName <-
                        Json.tryProperty "model" payload
                        |> Option.bind Json.tryString
                        |> Option.orElse currentModelName
                | Some "event_msg" ->
                    let payload =
                        Json.tryProperty "payload" node
                        |> Option.defaultWith (fun () -> upcast JsonObject())

                    match Json.tryProperty "type" payload |> Option.bind Json.tryString with
                    | Some "user_message"
                    | Some "agent_message" as messageType ->
                        let role, modelName =
                            match messageType with
                            | Some "user_message" -> Human, None
                            | _ -> Assistant, currentModelName

                        let messageText =
                            Json.tryProperty "message" payload
                            |> Option.bind Json.tryString
                            |> Option.map (fun value -> value.Trim())

                        let segments =
                            messageText
                            |> Option.bind (fun value ->
                                if String.IsNullOrWhiteSpace(value) then None else Some value)
                            |> Option.map (fun value ->
                                [ { Kind = PlainText
                                    Text = value } ])
                            |> Option.defaultValue []

                        let providerMessageId =
                            let suffix =
                                match messageType with
                                | Some value -> value
                                | None -> "message"

                            sprintf "line-%06d-%s" (lineIndex + 1) suffix

                        let occurredAt =
                            Json.tryProperty "timestamp" node
                            |> Option.bind Json.tryString
                            |> Option.bind tryParseDateTimeOffset

                        messages.Add
                            { ProviderMessageId = providerMessageId
                              Role = role
                              Segments = segments
                              OccurredAt = occurredAt
                              ModelName = modelName
                              SequenceHint = Some nextSequence
                              ContentSignature = buildContentSignature role modelName segments }

                        nextSequence <- nextSequence + 1
                    | _ -> ()
                | _ -> ()

        let resolvedSessionId =
            match sessionId with
            | Some value when not (String.IsNullOrWhiteSpace(value)) -> value
            | _ -> Path.GetFileNameWithoutExtension(transcriptPath)

        { ProviderConversationId = resolvedSessionId
          Title =
            titleBySessionId
            |> Map.tryFind resolvedSessionId
            |> Option.bind (fun entry -> entry.ThreadName)
            |> Option.orElseWith (fun () ->
                let fallback = Path.GetFileNameWithoutExtension(transcriptPath).Trim()

                if String.IsNullOrWhiteSpace(fallback) then None else Some fallback)
          IsArchived = None
          OccurredAt =
            sessionOccurredAt
            |> Option.orElseWith (fun () -> messages |> Seq.tryPick (fun message -> message.OccurredAt))
          MessageCountHint = Some messages.Count
          Messages =
            messages
            |> Seq.map (fun message ->
                { ProviderMessageId = message.ProviderMessageId
                  Role = message.Role
                  Segments = message.Segments
                  OccurredAt = message.OccurredAt
                  ModelName = message.ModelName
                  SequenceHint = message.SequenceHint
                  ContentSignature = message.ContentSignature
                  ArtifactReferences = [] })
            |> Seq.toList
          RawObjects = [ transcriptRawObject ] }

    /// <summary>
    /// Parses a preserved Codex raw-session snapshot into the shared conversation/message shape.
    /// </summary>
    /// <param name="objectsRoot">The object-layer root used to compute stable raw-object references.</param>
    /// <param name="snapshotRoot">The preserved Codex snapshot directory to read.</param>
    /// <returns>A parsed snapshot ready for canonical event import.</returns>
    /// <remarks>
    /// Only <c>user_message</c> and <c>agent_message</c> records are canonicalized in the current version.
    /// Full workflow notes: docs/how-to/import-codex-sessions.md
    /// </remarks>
    let parse objectsRoot snapshotRoot =
        let snapshotAbsolutePath = Path.GetFullPath(snapshotRoot)

        if not (Directory.Exists(snapshotAbsolutePath)) then
            invalidArg "snapshotRoot" $"Codex snapshot root not found: {snapshotAbsolutePath}"

        let manifestPath = Path.Combine(snapshotAbsolutePath, "export-manifest.toml")
        let manifestDocument =
            if File.Exists(manifestPath) then
                File.ReadAllText(manifestPath) |> TomlDocument.parse |> Some
            else
                None

        let exportedAt =
            manifestDocument
            |> Option.bind (TomlDocument.tryScalar "exported_at")
            |> Option.bind tryParseDateTimeOffset
            |> Option.map ImportedAt

        let sessionIndexPath = Path.Combine(snapshotAbsolutePath, "session_index.jsonl")
        let titleBySessionId = parseSessionIndex sessionIndexPath

        let transcriptPaths =
            let sessionsRoot = Path.Combine(snapshotAbsolutePath, "sessions")

            if Directory.Exists(sessionsRoot) then
                Directory.EnumerateFiles(sessionsRoot, "*.jsonl", SearchOption.AllDirectories)
                |> Seq.sort
                |> Seq.toList
            else
                []

        if List.isEmpty transcriptPaths && not (File.Exists(sessionIndexPath)) then
            failwith $"No Codex session_index.jsonl or transcript files were found under: {snapshotAbsolutePath}"

        let rootArtifactPath, rootArtifactKind, rootDescription =
            if File.Exists(sessionIndexPath) then
                sessionIndexPath, SessionIndex, "Codex session index JSONL"
            elif not transcriptPaths.IsEmpty then
                transcriptPaths.Head, SessionTranscript, "Codex session transcript JSONL"
            else
                failwith $"No importable Codex raw files were found under: {snapshotAbsolutePath}"

        let rootArtifactRelativePath = requireRelativeToRoot objectsRoot rootArtifactPath
        let rootArtifactInfo = FileInfo(rootArtifactPath)

        let rootArtifact =
            { RawObjectId = None
              Kind = rootArtifactKind
              RelativePath = rootArtifactRelativePath
              ArchivedAt = exportedAt
              SourceDescription = Some rootDescription }

        let conversations =
            transcriptPaths
            |> List.map (parseTranscript exportedAt titleBySessionId objectsRoot)

        { RootArtifact = rootArtifact
          SourceFileName = Path.GetFileName(rootArtifactPath)
          SourceByteCount = rootArtifactInfo.Length
          SnapshotName = manifestDocument |> Option.bind (TomlDocument.tryScalar "snapshot_name")
          Conversations = conversations
          Notes =
            [ "Imported Codex local session transcripts from a preserved raw snapshot."
              "Only user_message and agent_message records are canonicalized in v1; richer runtime and tool events remain preserved in raw JSONL." ] }
