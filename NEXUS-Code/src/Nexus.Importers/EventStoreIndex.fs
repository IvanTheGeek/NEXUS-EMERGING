namespace Nexus.Importers

open System
open System.Collections.Generic
open System.IO
open Nexus.Domain
open Nexus.EventStore

type ExistingMessageState =
    { MessageId: MessageId
      ConversationId: ConversationId option
      ContentHashesByNormalizationVersion: Dictionary<string, ContentHash option> }

type ExistingArtifactState =
    { ArtifactId: ArtifactId
      mutable ConversationId: ConversationId option
      mutable MessageId: MessageId option
      mutable Provider: ProviderKind option
      mutable ConversationNativeId: string option
      mutable MessageNativeId: string option
      mutable ProviderArtifactId: string option
      mutable FileName: string option
      mutable MediaType: string option
      CapturedContentHashKeys: HashSet<string> }

type ExistingEventStoreIndex =
    { ConversationsByProviderKey: Dictionary<string, ConversationId>
      MessagesByProviderKey: Dictionary<string, ExistingMessageState>
      ArtifactsByProviderKey: Dictionary<string, ArtifactId>
      ArtifactsByFallbackKey: Dictionary<string, ArtifactId>
      ArtifactsById: Dictionary<ArtifactId, ExistingArtifactState> }

[<RequireQualifiedAccess>]
module EventStoreIndex =
    let empty () =
        { ConversationsByProviderKey = Dictionary(StringComparer.Ordinal)
          MessagesByProviderKey = Dictionary(StringComparer.Ordinal)
          ArtifactsByProviderKey = Dictionary(StringComparer.Ordinal)
          ArtifactsByFallbackKey = Dictionary(StringComparer.Ordinal)
          ArtifactsById = Dictionary() }

    let private tryScalar key (document: TomlDocument) =
        TomlDocument.tryScalar key document

    let private tryTableValue path key (document: TomlDocument) =
        TomlDocument.tryTableValue path key document

    let private tableArray path (document: TomlDocument) =
        TomlDocument.tableArray path document

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

    let private normalizationVersionValue document =
        tryScalar "normalization_version" document
        |> Option.map NormalizationVersion.parse
        |> Option.defaultValue NormalizationNaming.legacyDefault
        |> NormalizationNaming.value

    let private updateMessageStateHash normalizationVersionKey contentHash state =
        state.ContentHashesByNormalizationVersion[normalizationVersionKey] <- contentHash

    let private createMessageState messageId conversationId normalizationVersionKey contentHash =
        let hashes = Dictionary<string, ContentHash option>(StringComparer.Ordinal)
        hashes[normalizationVersionKey] <- contentHash

        { MessageId = messageId
          ConversationId = conversationId
          ContentHashesByNormalizationVersion = hashes }

    let private contentHashKey (contentHash: ContentHash) =
        $"{contentHash.Algorithm}:{contentHash.Value}"

    let private prefer existingValue newValue =
        match existingValue, newValue with
        | Some _, _ -> existingValue
        | None, _ -> newValue

    let private getOrAddArtifactState (index: ExistingEventStoreIndex) artifactId =
        match index.ArtifactsById.TryGetValue(artifactId) with
        | true, state -> state
        | false, _ ->
            let state =
                { ArtifactId = artifactId
                  ConversationId = None
                  MessageId = None
                  Provider = None
                  ConversationNativeId = None
                  MessageNativeId = None
                  ProviderArtifactId = None
                  FileName = None
                  MediaType = None
                  CapturedContentHashKeys = HashSet<string>(StringComparer.Ordinal) }

            index.ArtifactsById[artifactId] <- state
            state

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
                let normalizationVersionKey = normalizationVersionValue document

                let conversationId =
                    tryScalar "conversation_id" document
                    |> Option.map ConversationId.parse

                match index.MessagesByProviderKey.TryGetValue(providerKey) with
                | true, existingState ->
                    updateMessageStateHash normalizationVersionKey (tryContentHash "content_hash" document) existingState
                | false, _ ->
                    index.MessagesByProviderKey[providerKey] <-
                        createMessageState
                            (MessageId.parse messageIdValue)
                            conversationId
                            normalizationVersionKey
                            (tryContentHash "content_hash" document)
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
                let normalizationVersionKey = normalizationVersionValue document

                match index.MessagesByProviderKey.TryGetValue(providerKey), tryContentHash "body.revised_content_hash" document with
                | (true, current), Some revisedContentHash ->
                    updateMessageStateHash normalizationVersionKey (Some revisedContentHash) current
                | _ -> ()
            | _ -> ()
        | _ -> ()

    let tryGetObservedContentHash normalizationVersion state =
        let key = NormalizationNaming.value normalizationVersion

        match state.ContentHashesByNormalizationVersion.TryGetValue(key) with
        | true, contentHash -> Some contentHash
        | false, _ -> None

    let setObservedContentHash normalizationVersion contentHash state =
        let key = NormalizationNaming.value normalizationVersion
        updateMessageStateHash key contentHash state

    let private addArtifactIndex (index: ExistingEventStoreIndex) (document: TomlDocument) =
        match tryScalar "artifact_id" document with
        | Some artifactIdValue ->
            let artifactId = ArtifactId.parse artifactIdValue
            let providerArtifactRef = tryProviderRefByKind "artifact_object" document
            let messageRef = tryProviderRefByKind "message_object" document
            let providerRef = providerArtifactRef |> Option.orElse messageRef
            let state = getOrAddArtifactState index artifactId

            state.ConversationId <-
                prefer
                    state.ConversationId
                    (tryScalar "conversation_id" document |> Option.map ConversationId.parse)

            state.MessageId <-
                prefer
                    state.MessageId
                    (tryScalar "message_id" document |> Option.map MessageId.parse)

            state.Provider <-
                prefer
                    state.Provider
                    (providerRef |> Option.bind (fun value -> value.Provider |> Option.bind ProviderNaming.tryParse))

            state.ConversationNativeId <-
                prefer
                    state.ConversationNativeId
                    (providerArtifactRef
                     |> Option.bind (fun artifactRef -> artifactRef.ConversationNativeId)
                     |> Option.orElse (messageRef |> Option.bind (fun value -> value.ConversationNativeId)))

            state.MessageNativeId <-
                prefer
                    state.MessageNativeId
                    (providerArtifactRef
                     |> Option.bind (fun artifactRef -> artifactRef.MessageNativeId |> Option.orElse artifactRef.NativeId)
                     |> Option.orElse
                        (messageRef
                         |> Option.bind (fun value -> value.MessageNativeId |> Option.orElse value.NativeId)))

            state.ProviderArtifactId <-
                prefer
                    state.ProviderArtifactId
                    (providerArtifactRef
                     |> Option.bind (fun artifactRef -> artifactRef.ArtifactNativeId |> Option.orElse artifactRef.NativeId)
                     |> Option.orElse (tryTableValue "body" "provider_artifact_id" document))

            state.FileName <- prefer state.FileName (tryTableValue "body" "file_name" document)
            state.MediaType <- prefer state.MediaType (tryTableValue "body" "media_type" document)

            tryContentHash "content_hash" document
            |> Option.iter (contentHashKey >> state.CapturedContentHashKeys.Add >> ignore)

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

    let tryFindArtifactState artifactId (index: ExistingEventStoreIndex) =
        match index.ArtifactsById.TryGetValue(artifactId) with
        | true, state -> Some state
        | false, _ -> None

    let tryResolveArtifactId provider conversationNativeId messageNativeId providerArtifactId fileName (index: ExistingEventStoreIndex) =
        let tryFind (dictionary: Dictionary<string, ArtifactId>) key =
            match dictionary.TryGetValue(key) with
            | true, artifactId -> Some artifactId
            | false, _ -> None

        providerArtifactId
        |> Option.map (ProviderKey.artifact provider conversationNativeId messageNativeId)
        |> Option.bind (tryFind index.ArtifactsByProviderKey)
        |> Option.orElseWith (fun () ->
            fileName
            |> Option.map (ProviderKey.artifactFallback provider conversationNativeId messageNativeId)
            |> Option.bind (tryFind index.ArtifactsByFallbackKey))

    let hasCapturedContentHash contentHash (state: ExistingArtifactState) =
        state.CapturedContentHashKeys.Contains(contentHashKey contentHash)

    let load (eventStoreRoot: string) =
        let index = empty ()
        let eventsRoot = Path.Combine(eventStoreRoot, "events")

        if Directory.Exists(eventsRoot) then
            Directory.EnumerateFiles(eventsRoot, "*.toml", SearchOption.AllDirectories)
            |> Seq.sort
            |> Seq.iter (fun path ->
                let document = File.ReadAllText(path) |> TomlDocument.parse

                match tryScalar "event_kind" document with
                | Some "provider_conversation_observed" -> addConversationIndex index document
                | Some "provider_message_observed" -> addMessageObservedIndex index document
                | Some "provider_message_revision_observed" -> addMessageRevisionIndex index document
                | Some "artifact_referenced"
                | Some "artifact_payload_captured" -> addArtifactIndex index document
                | _ -> ())

        index
