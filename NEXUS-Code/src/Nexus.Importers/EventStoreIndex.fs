namespace Nexus.Importers

open System
open System.Collections.Generic
open System.IO
open Nexus.Domain

type ExistingMessageState =
    { MessageId: MessageId
      ConversationId: ConversationId option
      ContentHash: ContentHash option }

type ExistingEventStoreIndex =
    { ConversationsByProviderKey: Dictionary<string, ConversationId>
      MessagesByProviderKey: Dictionary<string, ExistingMessageState>
      ArtifactsByProviderKey: Dictionary<string, ArtifactId>
      ArtifactsByFallbackKey: Dictionary<string, ArtifactId> }

[<RequireQualifiedAccess>]
module EventStoreIndex =
    type private TomlDocument =
        { Scalars: Dictionary<string, string>
          Tables: Dictionary<string, Dictionary<string, string>>
          TableArrays: Dictionary<string, ResizeArray<Dictionary<string, string>>> }

    let empty () =
        { ConversationsByProviderKey = Dictionary(StringComparer.Ordinal)
          MessagesByProviderKey = Dictionary(StringComparer.Ordinal)
          ArtifactsByProviderKey = Dictionary(StringComparer.Ordinal)
          ArtifactsByFallbackKey = Dictionary(StringComparer.Ordinal) }

    let private parseTomlValue (rawValue: string) =
        let trimmed = rawValue.Trim()

        if trimmed.StartsWith("\"", StringComparison.Ordinal)
           && trimmed.EndsWith("\"", StringComparison.Ordinal)
           && trimmed.Length >= 2 then
            trimmed.Substring(1, trimmed.Length - 2)
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
        else
            trimmed

    let private parseToml (text: string) =
        let scalars = Dictionary<string, string>(StringComparer.Ordinal)
        let tables = Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal)
        let tableArrays = Dictionary<string, ResizeArray<Dictionary<string, string>>>(StringComparer.Ordinal)
        let mutable currentTable: Dictionary<string, string> option = None
        let mutable currentArrayTable: Dictionary<string, string> option = None

        let ensureTable path =
            match tables.TryGetValue(path) with
            | true, table -> table
            | false, _ ->
                let table = Dictionary<string, string>(StringComparer.Ordinal)
                tables[path] <- table
                table

        for rawLine in text.Replace("\r\n", "\n").Split('\n') do
            let line = rawLine.Trim()

            if not (String.IsNullOrWhiteSpace(line)) && not (line.StartsWith("#", StringComparison.Ordinal)) then
                if line.StartsWith("[[", StringComparison.Ordinal) && line.EndsWith("]]", StringComparison.Ordinal) then
                    let path = line.Substring(2, line.Length - 4).Trim()
                    let target =
                        match tableArrays.TryGetValue(path) with
                        | true, arrayTables -> arrayTables
                        | false, _ ->
                            let arrayTables = ResizeArray()
                            tableArrays[path] <- arrayTables
                            arrayTables

                    let table = Dictionary<string, string>(StringComparer.Ordinal)
                    target.Add(table)
                    currentTable <- None
                    currentArrayTable <- Some table
                elif line.StartsWith("[", StringComparison.Ordinal) && line.EndsWith("]", StringComparison.Ordinal) then
                    let path = line.Substring(1, line.Length - 2).Trim()
                    currentTable <- Some (ensureTable path)
                    currentArrayTable <- None
                else
                    let separatorIndex = line.IndexOf('=')

                    if separatorIndex > 0 then
                        let key = line.Substring(0, separatorIndex).Trim()
                        let value = line.Substring(separatorIndex + 1) |> parseTomlValue

                        match currentArrayTable, currentTable with
                        | Some arrayTable, _ -> arrayTable[key] <- value
                        | None, Some table -> table[key] <- value
                        | None, None -> scalars[key] <- value

        { Scalars = scalars
          Tables = tables
          TableArrays = tableArrays }

    let private tryScalar key (document: TomlDocument) =
        match document.Scalars.TryGetValue(key) with
        | true, value -> Some value
        | false, _ -> None

    let private tryTableValue path key (document: TomlDocument) =
        match document.Tables.TryGetValue(path) with
        | true, table ->
            match table.TryGetValue(key) with
            | true, value -> Some value
            | false, _ -> None
        | false, _ -> None

    let private tableArray path (document: TomlDocument) =
        match document.TableArrays.TryGetValue(path) with
        | true, tables -> tables |> Seq.toList
        | false, _ -> []

    let private providerRefs document =
        tableArray "provider_refs" document
        |> List.map (fun table ->
            let tryGet key =
                match table.TryGetValue(key) with
                | true, value -> Some value
                | false, _ -> None

            {| Provider = tryGet "provider"
               ObjectKind = tryGet "object_kind"
               NativeId = tryGet "native_id"
               ConversationNativeId = tryGet "conversation_native_id"
               MessageNativeId = tryGet "message_native_id"
               ArtifactNativeId = tryGet "artifact_native_id" |})

    let private tryProviderRefByKind objectKind document =
        providerRefs document
        |> List.tryFind (fun providerRef -> providerRef.ObjectKind = Some objectKind)

    let private tryContentHash path document =
        match tryTableValue path "algorithm" document, tryTableValue path "value" document with
        | Some algorithm, Some value ->
            Some
                { Algorithm = algorithm
                  Value = value }
        | _ -> None

    let private addConversationIndex (index: ExistingEventStoreIndex) (document: TomlDocument) =
        match tryScalar "conversation_id" document, tryProviderRefByKind "conversation_object" document with
        | Some conversationIdValue, Some providerRef ->
            let provider = providerRef.Provider |> Option.bind ProviderNaming.tryParse
            let conversationNativeId = providerRef.ConversationNativeId |> Option.orElse providerRef.NativeId

            match provider, conversationNativeId with
            | Some providerKind, Some nativeId ->
                let providerKey = ProviderKey.conversation providerKind nativeId
                index.ConversationsByProviderKey[providerKey] <- ConversationId.parse conversationIdValue
            | _ -> ()
        | _ -> ()

    let private addMessageObservedIndex (index: ExistingEventStoreIndex) (document: TomlDocument) =
        match tryScalar "message_id" document, tryProviderRefByKind "message_object" document with
        | Some messageIdValue, Some providerRef ->
            let provider = providerRef.Provider |> Option.bind ProviderNaming.tryParse
            let conversationNativeId = providerRef.ConversationNativeId
            let messageNativeId = providerRef.MessageNativeId |> Option.orElse providerRef.NativeId

            match provider, conversationNativeId, messageNativeId with
            | Some providerKind, Some conversationId, Some messageId ->
                let providerKey = ProviderKey.message providerKind conversationId messageId

                let conversationId =
                    tryScalar "conversation_id" document
                    |> Option.map ConversationId.parse

                index.MessagesByProviderKey[providerKey] <-
                    { MessageId = MessageId.parse messageIdValue
                      ConversationId = conversationId
                      ContentHash = tryContentHash "content_hash" document }
            | _ -> ()
        | _ -> ()

    let private addMessageRevisionIndex (index: ExistingEventStoreIndex) (document: TomlDocument) =
        match tryScalar "message_id" document, tryProviderRefByKind "message_object" document with
        | Some _, Some providerRef ->
            let provider = providerRef.Provider |> Option.bind ProviderNaming.tryParse
            let conversationNativeId = providerRef.ConversationNativeId
            let messageNativeId = providerRef.MessageNativeId |> Option.orElse providerRef.NativeId

            match provider, conversationNativeId, messageNativeId with
            | Some providerKind, Some conversationId, Some messageId ->
                let providerKey = ProviderKey.message providerKind conversationId messageId

                match index.MessagesByProviderKey.TryGetValue(providerKey), tryContentHash "body.revised_content_hash" document with
                | (true, current), Some revisedContentHash ->
                    index.MessagesByProviderKey[providerKey] <-
                        { current with
                            ContentHash = Some revisedContentHash }
                | _ -> ()
            | _ -> ()
        | _ -> ()

    let private addArtifactIndex (index: ExistingEventStoreIndex) (document: TomlDocument) =
        match tryScalar "artifact_id" document with
        | Some artifactIdValue ->
            let artifactId = ArtifactId.parse artifactIdValue
            let providerArtifactRef = tryProviderRefByKind "artifact_object" document
            let messageRef = tryProviderRefByKind "message_object" document

            match providerArtifactRef, messageRef with
            | Some artifactRef, Some messageRef ->
                let provider = artifactRef.Provider |> Option.bind ProviderNaming.tryParse
                let conversationNativeId = artifactRef.ConversationNativeId |> Option.orElse messageRef.ConversationNativeId
                let messageNativeId = artifactRef.MessageNativeId |> Option.orElse messageRef.MessageNativeId |> Option.orElse messageRef.NativeId
                let artifactNativeId = artifactRef.ArtifactNativeId |> Option.orElse artifactRef.NativeId

                match provider, conversationNativeId, messageNativeId, artifactNativeId with
                | Some providerKind, Some conversationId, Some messageId, Some artifactNativeId ->
                    let providerKey = ProviderKey.artifact providerKind conversationId messageId artifactNativeId
                    index.ArtifactsByProviderKey[providerKey] <- artifactId
                | _ -> ()
            | _ -> ()

            match tryProviderRefByKind "message_object" document, tryTableValue "body" "file_name" document with
            | Some messageRef, Some fileName ->
                let provider = messageRef.Provider |> Option.bind ProviderNaming.tryParse
                let conversationNativeId = messageRef.ConversationNativeId
                let messageNativeId = messageRef.MessageNativeId |> Option.orElse messageRef.NativeId

                match provider, conversationNativeId, messageNativeId with
                | Some providerKind, Some conversationId, Some messageId ->
                    let fallbackKey = ProviderKey.artifactFallback providerKind conversationId messageId fileName
                    index.ArtifactsByFallbackKey[fallbackKey] <- artifactId
                | _ -> ()
            | _ -> ()
        | None -> ()

    let load (eventStoreRoot: string) =
        let index = empty ()
        let eventsRoot = Path.Combine(eventStoreRoot, "events")

        if Directory.Exists(eventsRoot) then
            Directory.EnumerateFiles(eventsRoot, "*.toml", SearchOption.AllDirectories)
            |> Seq.sort
            |> Seq.iter (fun path ->
                let document = File.ReadAllText(path) |> parseToml

                match tryScalar "event_kind" document with
                | Some "provider_conversation_observed" -> addConversationIndex index document
                | Some "provider_message_observed" -> addMessageObservedIndex index document
                | Some "provider_message_revision_observed" -> addMessageRevisionIndex index document
                | Some "artifact_referenced"
                | Some "artifact_payload_captured" -> addArtifactIndex index document
                | _ -> ())

        index
