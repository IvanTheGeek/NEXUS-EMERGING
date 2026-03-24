namespace Nexus.Curation

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Text.RegularExpressions
open Nexus.EventStore

/// <summary>
/// The projection-backed source details used to seed a curated concept note.
/// </summary>
type ConceptSourceConversation =
    { ConversationId: string
      Title: string
      Providers: string list
      ProviderConversationIds: string list
      MessageCount: int option
      FirstOccurredAt: string option
      LastOccurredAt: string option
      ProjectionRelativePath: string
      Excerpts: (string * string) list }

/// <summary>
/// The input used to create a concept note seed from canonical conversation projections.
/// </summary>
/// <remarks>
/// Full workflow notes: docs/how-to/create-concept-note.md
/// </remarks>
type CreateConceptNoteRequest =
    { DocsRoot: string
      EventStoreRoot: string
      Slug: string
      Title: string
      Domains: string list
      Tags: string list
      SourceConversationIds: string list }

/// <summary>
/// The result of creating a new concept note seed.
/// </summary>
type CreateConceptNoteResult =
    { OutputPath: string
      NormalizedSlug: string
      SourceConversations: ConceptSourceConversation list }

/// <summary>
/// Creates curated concept-note seeds from canonical conversation projections.
/// </summary>
[<RequireQualifiedAccess>]
module ConceptNotes =
    let private cliGraphSliceCommand conversationId =
        sprintf
            "dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- export-graphviz-dot --conversation-id %s"
            conversationId

    let private normalizeSlug (value: string) =
        let trimmed = value.Trim().ToLowerInvariant()
        let normalized = Regex.Replace(trimmed, "[^a-z0-9]+", "-").Trim('-')

        if String.IsNullOrWhiteSpace(normalized) then
            None
        else
            Some normalized

    let private tryParseInt (value: string) =
        match Int32.TryParse(value) with
        | true, parsedValue -> Some parsedValue
        | false, _ -> None

    let private tryDictionaryValue key (table: Dictionary<string, string>) =
        match table.TryGetValue(key) with
        | true, value -> Some value
        | false, _ -> None

    let private parseStringArray (rawValue: string) =
        let trimmed = rawValue.Trim()

        if not (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal)) then
            [ trimmed ]
        else
            let inner = trimmed.Substring(1, trimmed.Length - 2)
            let values = ResizeArray<string>()
            let current = StringBuilder()
            let mutable inString = false
            let mutable escaping = false

            for character in inner do
                if escaping then
                    current.Append(character) |> ignore
                    escaping <- false
                elif inString then
                    match character with
                    | '\\' ->
                        escaping <- true
                    | '"' ->
                        values.Add(current.ToString())
                        current.Clear() |> ignore
                        inString <- false
                    | _ ->
                        current.Append(character) |> ignore
                elif character = '"' then
                    inString <- true

            values |> Seq.toList

    let private tomlEscape (value: string) =
        value.Replace("\\", "\\\\").Replace("\"", "\\\"")

    let private markdownEscapeInline (value: string) =
        value.Replace("`", "\\`")

    let private appendTomlStringArray (builder: StringBuilder) key values =
        let rendered =
            values
            |> List.map (fun value -> sprintf "\"%s\"" (tomlEscape value))
            |> String.concat ", "

        builder.AppendLine(sprintf "%s = [%s]" key rendered) |> ignore

    let private loadSourceConversation noteDirectory eventStoreRoot conversationId =
        let projectionPath =
            Path.Combine(eventStoreRoot, "projections", "conversations", sprintf "%s.toml" conversationId)

        if not (File.Exists(projectionPath)) then
            Error(sprintf "No conversation projection exists for canonical conversation_id %s." conversationId)
        else
            let document =
                projectionPath
                |> File.ReadAllText
                |> TomlDocument.parse

            let title =
                document
                |> TomlDocument.tryScalar "title"
                |> Option.defaultValue conversationId

            let providers =
                document
                |> TomlDocument.tryScalar "providers"
                |> Option.map parseStringArray
                |> Option.defaultValue []

            let providerConversationIds =
                document
                |> TomlDocument.tryScalar "provider_conversation_ids"
                |> Option.map parseStringArray
                |> Option.defaultValue []

            let messageCount =
                document
                |> TomlDocument.tryScalar "message_count"
                |> Option.bind tryParseInt

            let excerpts =
                document
                |> TomlDocument.tableArray "messages"
                |> List.truncate 4
                |> List.choose (fun table ->
                    match tryDictionaryValue "role" table, tryDictionaryValue "excerpt" table with
                    | Some role, Some excerpt -> Some (role, excerpt)
                    | _ -> None)

            Ok
                { ConversationId = conversationId
                  Title = title
                  Providers = providers
                  ProviderConversationIds = providerConversationIds
                  MessageCount = messageCount
                  FirstOccurredAt = TomlDocument.tryScalar "first_occurred_at" document
                  LastOccurredAt = TomlDocument.tryScalar "last_occurred_at" document
                  ProjectionRelativePath =
                    Path.GetRelativePath(noteDirectory, projectionPath)
                        .Replace('\\', '/')
                  Excerpts = excerpts }

    let private renderSourceConversation (builder: StringBuilder) (source: ConceptSourceConversation) =
        builder.AppendLine(sprintf "### %s" source.Title) |> ignore
        builder.AppendLine(sprintf "- canonical conversation id: `%s`" source.ConversationId) |> ignore

        source.MessageCount
        |> Option.iter (fun count ->
            builder.AppendLine(sprintf "- message count: `%d`" count) |> ignore)

        if not source.Providers.IsEmpty then
            builder.AppendLine(sprintf "- providers: `%s`" (String.concat "`, `" source.Providers)) |> ignore

        if not source.ProviderConversationIds.IsEmpty then
            builder.AppendLine(
                sprintf
                    "- provider conversation ids: `%s`"
                    (source.ProviderConversationIds |> List.map markdownEscapeInline |> String.concat "`, `"))
            |> ignore

        source.FirstOccurredAt
        |> Option.iter (fun value ->
            builder.AppendLine(sprintf "- first occurred at: `%s`" value) |> ignore)

        source.LastOccurredAt
        |> Option.iter (fun value ->
            builder.AppendLine(sprintf "- last occurred at: `%s`" value) |> ignore)

        builder.AppendLine(sprintf "- projection file: `%s`" source.ProjectionRelativePath) |> ignore
        builder.AppendLine(sprintf "- graph slice command: `%s`" (cliGraphSliceCommand source.ConversationId)) |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("#### Excerpts") |> ignore

        if source.Excerpts.IsEmpty then
            builder.AppendLine("- No message excerpts were available in the projection.") |> ignore
        else
            source.Excerpts
            |> List.iter (fun (role, excerpt) ->
                builder.AppendLine(
                    sprintf "- `%s`: %s" (markdownEscapeInline role) (excerpt.Trim().Replace("\n", " "))
                )
                |> ignore)

        builder.AppendLine() |> ignore

    let private renderConceptNote (request: CreateConceptNoteRequest) (normalizedSlug: string) (now: DateTimeOffset) (sourceConversations: ConceptSourceConversation list) =
        let builder = StringBuilder()

        builder.AppendLine("+++") |> ignore
        builder.AppendLine("note_kind = \"concept_seed\"") |> ignore
        builder.AppendLine(sprintf "title = \"%s\"" (tomlEscape request.Title)) |> ignore
        builder.AppendLine(sprintf "slug = \"%s\"" normalizedSlug) |> ignore
        builder.AppendLine("status = \"seed\"") |> ignore
        builder.AppendLine(sprintf "created_at = \"%s\"" (now.ToString("O"))) |> ignore
        builder.AppendLine(sprintf "updated_at = \"%s\"" (now.ToString("O"))) |> ignore
        appendTomlStringArray builder "domains" request.Domains
        appendTomlStringArray builder "tags" request.Tags
        appendTomlStringArray builder "canonical_conversation_ids" (sourceConversations |> List.map (fun source -> source.ConversationId))
        builder.AppendLine("+++") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine(sprintf "# %s" request.Title) |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("## Summary") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("Seed concept note created from canonical conversation history. Refine this summary as the concept becomes clearer.") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("## Working Notes") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("- What is the core idea here?") |> ignore
        builder.AppendLine("- Why does it matter inside NEXUS?") |> ignore
        builder.AppendLine("- Which domains, bounded contexts, or lenses does it connect to?") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("## Source Conversations") |> ignore
        builder.AppendLine() |> ignore
        sourceConversations |> List.iter (renderSourceConversation builder)
        builder.AppendLine("## Next Questions") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("- What should this concept mean structurally?") |> ignore
        builder.AppendLine("- Which parts are stable enough to move into durable project memory?") |> ignore
        builder.AppendLine("- What additional conversations or artifacts should be harvested into this note?") |> ignore
        builder.ToString()

    /// <summary>
    /// Creates a new concept note seed from one or more canonical conversation projections.
    /// </summary>
    /// <param name="request">The note title, slug, doc root, and source conversation IDs to use.</param>
    /// <returns>The created note path and the harvested conversation summaries.</returns>
    /// <remarks>
    /// Full workflow notes: docs/how-to/create-concept-note.md
    /// </remarks>
    let create request =
        let normalizedSlugResult =
            match normalizeSlug request.Slug with
            | Some normalizedSlug -> Ok normalizedSlug
            | None -> Error "Concept note slug must contain at least one letter or digit."

        normalizedSlugResult
        |> Result.bind (fun normalizedSlug ->
            let sourceConversationIds =
                request.SourceConversationIds
                |> List.map (fun value -> value.Trim())
                |> List.filter (String.IsNullOrWhiteSpace >> not)
                |> List.distinct

            if String.IsNullOrWhiteSpace(request.Title) then
                Error "Concept note title cannot be empty."
            elif sourceConversationIds.IsEmpty then
                Error "At least one --conversation-id is required to seed a concept note."
            else
                let conceptsDirectory = Path.Combine(request.DocsRoot, "concepts")
                let outputPath = Path.Combine(conceptsDirectory, sprintf "%s.md" normalizedSlug)

                if File.Exists(outputPath) then
                    Error(sprintf "Concept note already exists: %s" outputPath)
                else
                    Directory.CreateDirectory(conceptsDirectory) |> ignore

                    let noteDirectory = Path.GetDirectoryName(outputPath)

                    sourceConversationIds
                    |> List.map (loadSourceConversation noteDirectory request.EventStoreRoot)
                    |> List.fold
                        (fun state next ->
                            match state, next with
                            | Ok accumulated, Ok source -> Ok (source :: accumulated)
                            | Error error, _ -> Error error
                            | _, Error error -> Error error)
                        (Ok [])
                    |> Result.map List.rev
                    |> Result.map (fun sourceConversations ->
                        let now = DateTimeOffset.UtcNow
                        let markdown = renderConceptNote request normalizedSlug now sourceConversations
                        File.WriteAllText(outputPath, markdown)

                        { OutputPath = outputPath
                          NormalizedSlug = normalizedSlug
                          SourceConversations = sourceConversations }))
