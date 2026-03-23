namespace Nexus.EventStore

open System
open System.Collections.Generic
open System.IO
open System.Security.Cryptography
open System.Text
open Nexus.Domain
open Nexus.Kernel

[<RequireQualifiedAccess>]
module GraphAssertions =
    type private EventDocument =
        { EventId: CanonicalEventId
          EventKind: string
          ConversationId: NodeId option
          MessageId: NodeId option
          ArtifactId: NodeId option
          ImportId: NodeId option
          DomainId: DomainId option
          BoundedContextId: BoundedContextId option
          SourceAcquisition: SourceAcquisitionKind
          ProviderRefs: ProviderRef list
          RawObjects: RawObjectRef list
          Document: TomlDocument }

    let private stableGuid namespaceName value =
        let input = Encoding.UTF8.GetBytes($"{namespaceName}:{value}")
        let hash = SHA1.HashData(input)
        let guidBytes = Array.zeroCreate<byte> 16
        Array.Copy(hash, guidBytes, 16)
        guidBytes[7] <- (guidBytes[7] &&& 0x0Fuy) ||| 0x50uy
        guidBytes[8] <- (guidBytes[8] &&& 0x3Fuy) ||| 0x80uy
        Guid(guidBytes)

    let private nodeIdFromSlug namespaceName value =
        NodeId(stableGuid namespaceName value)

    let private conversationNodeId value = NodeId.parse value
    let private messageNodeId value = NodeId.parse value
    let private artifactNodeId value = NodeId.parse value
    let private importNodeId value = NodeId.parse value
    let private domainNodeId (value: DomainId) = nodeIdFromSlug "domain" (DomainId.value value)
    let private boundedContextNodeId (value: BoundedContextId) = nodeIdFromSlug "bounded-context" (BoundedContextId.value value)

    let private edgeValue =
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

    let private graphValueKey =
        function
        | StringValue value -> $"string:{value}"
        | IntValue value -> $"int64:{value}"
        | DecimalValue value -> $"decimal:{value}"
        | BoolValue value -> $"bool:{value}"
        | TimestampValue value -> $"timestamp:{value:O}"

    let private graphTermKey =
        function
        | NodeRef nodeId -> $"node:{NodeId.format nodeId}"
        | Literal value -> $"literal:{graphValueKey value}"

    let private providerKindFromValue =
        function
        | "chatgpt" -> ChatGpt
        | "claude" -> Claude
        | "codex" -> Codex
        | value -> OtherProvider value

    let private providerObjectKindFromValue =
        function
        | "export_artifact" -> ExportArtifact
        | "conversation_object" -> ConversationObject
        | "message_object" -> MessageObject
        | "artifact_object" -> ArtifactObject
        | "project_object" -> ProjectObject
        | "memory_object" -> MemoryObject
        | "user_object" -> UserObject
        | value -> OtherProviderObject value

    let private sourceAcquisitionFromValue =
        function
        | "export_zip" -> ExportZip
        | "local_session_export" -> LocalSessionExport
        | "manual_artifact_add" -> ManualArtifactAdd
        | "api_capture" -> ApiCapture
        | "browser_capture" -> BrowserCapture
        | value -> OtherAcquisition value

    let private rawObjectKindFromValue =
        function
        | "provider_export_zip" -> ProviderExportZip
        | "extracted_snapshot" -> ExtractedSnapshot
        | "session_index" -> SessionIndex
        | "session_transcript" -> SessionTranscript
        | "attachment_payload" -> AttachmentPayload
        | "audio_payload" -> AudioPayload
        | "manual_artifact" -> ManualArtifact
        | value -> OtherRawObject value

    let private providerRefKey (providerRef: ProviderRef) =
        let field = Option.defaultValue ""

        String.concat "|"
            [ match providerRef.Provider with
              | ChatGpt -> "chatgpt"
              | Claude -> "claude"
              | Codex -> "codex"
              | OtherProvider value -> value
              match providerRef.ObjectKind with
              | ExportArtifact -> "export_artifact"
              | ConversationObject -> "conversation_object"
              | MessageObject -> "message_object"
              | ArtifactObject -> "artifact_object"
              | ProjectObject -> "project_object"
              | MemoryObject -> "memory_object"
              | UserObject -> "user_object"
              | OtherProviderObject value -> value
              field providerRef.NativeId
              field providerRef.ConversationNativeId
              field providerRef.MessageNativeId
              field providerRef.ArtifactNativeId ]

    let private rawObjectKey (rawObject: RawObjectRef) =
        let field = Option.defaultValue ""

        String.concat "|"
            [ field rawObject.RawObjectId
              match rawObject.Kind with
              | ProviderExportZip -> "provider_export_zip"
              | ExtractedSnapshot -> "extracted_snapshot"
              | SessionIndex -> "session_index"
              | SessionTranscript -> "session_transcript"
              | AttachmentPayload -> "attachment_payload"
              | AudioPayload -> "audio_payload"
              | ManualArtifact -> "manual_artifact"
              | OtherRawObject value -> value
              rawObject.RelativePath
              rawObject.ArchivedAt |> Option.map (fun (ImportedAt value) -> value.ToString("O")) |> field
              field rawObject.SourceDescription ]

    let private sortProviderRefs providerRefs =
        providerRefs
        |> List.distinct
        |> List.sortBy providerRefKey

    let private sortRawObjects rawObjects =
        rawObjects
        |> List.distinct
        |> List.sortBy rawObjectKey

    let private sortEventIds eventIds =
        eventIds
        |> List.distinct
        |> List.sortBy CanonicalEventId.format

    let private mergeImportIds left right =
        match left, right with
        | Some currentValue, Some candidateValue when currentValue = candidateValue -> Some currentValue
        | None, value
        | value, None -> value
        | Some _, Some _ -> None

    let private mergeSourceAcquisition left right =
        if left = right then
            left
        else
            OtherAcquisition "mixed"

    let private parseProviderRefs document =
        TomlDocument.tableArray "provider_refs" document
        |> List.choose (fun table ->
            match table.TryGetValue("provider"), table.TryGetValue("object_kind") with
            | (true, provider), (true, objectKind) ->
                Some
                    { Provider = providerKindFromValue provider
                      ObjectKind = providerObjectKindFromValue objectKind
                      NativeId =
                        match table.TryGetValue("native_id") with
                        | true, value -> Some value
                        | false, _ -> None
                      ConversationNativeId =
                        match table.TryGetValue("conversation_native_id") with
                        | true, value -> Some value
                        | false, _ -> None
                      MessageNativeId =
                        match table.TryGetValue("message_native_id") with
                        | true, value -> Some value
                        | false, _ -> None
                      ArtifactNativeId =
                        match table.TryGetValue("artifact_native_id") with
                        | true, value -> Some value
                        | false, _ -> None }
            | _ -> None)
        |> sortProviderRefs

    let private parseRawObjects document =
        TomlDocument.tableArray "raw_objects" document
        |> List.choose (fun table ->
            match table.TryGetValue("kind"), table.TryGetValue("relative_path") with
            | (true, kind), (true, relativePath) ->
                Some
                    { RawObjectId =
                        match table.TryGetValue("raw_object_id") with
                        | true, value -> Some value
                        | false, _ -> None
                      Kind = rawObjectKindFromValue kind
                      RelativePath = relativePath
                      ArchivedAt =
                        match table.TryGetValue("archived_at") with
                        | true, value ->
                            match DateTimeOffset.TryParse(value) with
                            | true, parsedValue -> Some (ImportedAt parsedValue)
                            | _ -> None
                        | false, _ -> None
                      SourceDescription =
                        match table.TryGetValue("source_description") with
                        | true, value -> Some value
                        | false, _ -> None }
            | _ -> None)
        |> sortRawObjects

    let private parseEventDocument path =
        let document = File.ReadAllText(path) |> TomlDocument.parse

        { EventId = TomlDocument.tryScalar "event_id" document |> Option.map CanonicalEventId.parse |> Option.defaultWith CanonicalEventId.create
          EventKind = TomlDocument.tryScalar "event_kind" document |> Option.defaultValue ""
          ConversationId = TomlDocument.tryScalar "conversation_id" document |> Option.map conversationNodeId
          MessageId = TomlDocument.tryScalar "message_id" document |> Option.map messageNodeId
          ArtifactId = TomlDocument.tryScalar "artifact_id" document |> Option.map artifactNodeId
          ImportId = TomlDocument.tryScalar "import_id" document |> Option.map importNodeId
          DomainId = TomlDocument.tryScalar "domain_id" document |> Option.map DomainId.create
          BoundedContextId = TomlDocument.tryScalar "bounded_context_id" document |> Option.map BoundedContextId.create
          SourceAcquisition =
            TomlDocument.tryScalar "source_acquisition" document
            |> Option.map sourceAcquisitionFromValue
            |> Option.defaultValue (OtherAcquisition "unknown")
          ProviderRefs = parseProviderRefs document
          RawObjects = parseRawObjects document
          Document = document }

    let private buildProvenance eventDocument =
        { ImportId = eventDocument.Document |> TomlDocument.tryScalar "import_id" |> Option.map ImportId.parse
          SourceAcquisition = eventDocument.SourceAcquisition
          ProviderRefs = eventDocument.ProviderRefs
          RawObjects = eventDocument.RawObjects
          SupportingEventIds = [ eventDocument.EventId ] }

    let private stringLiteral value = Literal(StringValue value)
    let private intLiteral value = Literal(IntValue(int64 value))

    let private assertionKey (assertion: GraphAssertion) =
        String.concat "|"
            [ NodeId.format assertion.Subject
              edgeValue assertion.Predicate
              graphTermKey assertion.Object
              assertion.DomainId |> Option.map DomainId.value |> Option.defaultValue ""
              assertion.BoundedContextId |> Option.map BoundedContextId.value |> Option.defaultValue ""
              assertion.LensId |> Option.map LensId.value |> Option.defaultValue "" ]

    let private assertion subject predicate objectValue domainId boundedContextId provenance =
        let domainKey =
            domainId |> Option.map DomainId.value |> Option.defaultValue ""

        let boundedContextKey =
            boundedContextId |> Option.map BoundedContextId.value |> Option.defaultValue ""

        let factKey =
            String.concat "|"
                [ NodeId.format subject
                  edgeValue predicate
                  graphTermKey objectValue
                  domainKey
                  boundedContextKey ]

        { FactId = FactId(stableGuid "fact" factKey)
          Subject = subject
          Predicate = predicate
          Object = objectValue
          DomainId = domainId
          BoundedContextId = boundedContextId
          LensId = None
          Provenance = provenance }

    let private semanticRoleAnnotation subject roleId domainId boundedContextId provenance =
        let domainKey =
            domainId |> Option.map DomainId.value |> Option.defaultValue ""

        let boundedContextKey =
            boundedContextId |> Option.map BoundedContextId.value |> Option.defaultValue ""

        let factKey =
            String.concat "|"
                [ NodeId.format subject
                  RoleId.value roleId
                  domainKey
                  boundedContextKey ]

        { FactId = FactId(stableGuid "semantic-role" factKey)
          Subject = subject
          RoleId = roleId
          DomainId = domainId
          BoundedContextId = boundedContextId
          LensId = None
          Provenance = provenance }

    let private mergeAssertion (current: GraphAssertion) (incoming: GraphAssertion) =
        { current with
            Provenance =
                { ImportId = mergeImportIds current.Provenance.ImportId incoming.Provenance.ImportId
                  SourceAcquisition = mergeSourceAcquisition current.Provenance.SourceAcquisition incoming.Provenance.SourceAcquisition
                  ProviderRefs = sortProviderRefs (current.Provenance.ProviderRefs @ incoming.Provenance.ProviderRefs)
                  RawObjects = sortRawObjects (current.Provenance.RawObjects @ incoming.Provenance.RawObjects)
                  SupportingEventIds = sortEventIds (current.Provenance.SupportingEventIds @ incoming.Provenance.SupportingEventIds) } }

    let private addAssertion (assertions: Dictionary<string, GraphAssertion>) assertion =
        let key = assertionKey assertion

        match assertions.TryGetValue(key) with
        | true, existing -> assertions[key] <- mergeAssertion existing assertion
        | false, _ -> assertions[key] <- assertion

    let private addSemanticRole (assertions: Dictionary<string, GraphAssertion>) (annotation: SemanticRoleAnnotation) =
        annotation
        |> SemanticRoleAnnotation.toGraphAssertion
        |> addAssertion assertions

    let private addNodeMetadata assertions eventDocument nodeId nodeKind =
        let provenance = buildProvenance eventDocument
        addAssertion assertions (assertion nodeId (OtherEdge "has_node_kind") (stringLiteral nodeKind) None None provenance)

    let private addDomainMetadata assertions eventDocument (value: DomainId) =
        let nodeId = domainNodeId value
        let provenance = buildProvenance eventDocument
        addNodeMetadata assertions eventDocument nodeId "domain_node"
        addAssertion assertions (assertion nodeId (OtherEdge "has_slug") (stringLiteral (DomainId.value value)) None None provenance)

    let private addBoundedContextMetadata assertions eventDocument (value: BoundedContextId) =
        let nodeId = boundedContextNodeId value
        let provenance = buildProvenance eventDocument
        addNodeMetadata assertions eventDocument nodeId "bounded_context_node"
        addAssertion assertions (assertion nodeId (OtherEdge "has_slug") (stringLiteral (BoundedContextId.value value)) None None provenance)

    let private addSubjectLinks assertions eventDocument nodeId =
        let provenance = buildProvenance eventDocument

        eventDocument.ImportId
        |> Option.iter (fun importNode ->
            addAssertion assertions (assertion importNode (OtherEdge "has_node_kind") (stringLiteral "import_node") None None provenance)

            if nodeId <> importNode then
                addAssertion assertions (assertion nodeId ObservedDuringImport (NodeRef importNode) eventDocument.DomainId eventDocument.BoundedContextId provenance))

        eventDocument.DomainId
        |> Option.iter (fun domainId ->
            let domainNode = domainNodeId domainId
            addDomainMetadata assertions eventDocument domainId
            addAssertion assertions (assertion nodeId LocatedInDomain (NodeRef domainNode) eventDocument.DomainId eventDocument.BoundedContextId provenance))

        eventDocument.BoundedContextId
        |> Option.iter (fun boundedContextId ->
            let boundedContextNode = boundedContextNodeId boundedContextId
            addBoundedContextMetadata assertions eventDocument boundedContextId
            addAssertion assertions (assertion nodeId InterpretedWithinContext (NodeRef boundedContextNode) eventDocument.DomainId eventDocument.BoundedContextId provenance)

            eventDocument.DomainId
            |> Option.iter (fun domainId ->
                let domainNode = domainNodeId domainId
                addAssertion assertions (assertion boundedContextNode LocatedInDomain (NodeRef domainNode) None None provenance)))

    let private addConversationAssertions assertions eventDocument conversationNode =
        addNodeMetadata assertions eventDocument conversationNode "conversation_node"
        addSubjectLinks assertions eventDocument conversationNode

        TomlDocument.tryTableValue "body" "title" eventDocument.Document
        |> Option.iter (fun title ->
            addAssertion assertions (assertion conversationNode (OtherEdge "has_title") (stringLiteral title) eventDocument.DomainId eventDocument.BoundedContextId (buildProvenance eventDocument)))

    let private addMessageAssertions assertions eventDocument messageNode =
        addNodeMetadata assertions eventDocument messageNode "message_node"
        addSubjectLinks assertions eventDocument messageNode
        addSemanticRole
            assertions
            (semanticRoleAnnotation
                messageNode
                CoreRoles.imprint
                eventDocument.DomainId
                eventDocument.BoundedContextId
                (buildProvenance eventDocument))

        eventDocument.ConversationId
        |> Option.iter (fun conversationNode ->
            addAssertion assertions (assertion messageNode BelongsToConversation (NodeRef conversationNode) eventDocument.DomainId eventDocument.BoundedContextId (buildProvenance eventDocument)))

        match eventDocument.EventKind with
        | "provider_message_observed" ->
            TomlDocument.tryTableValue "body" "role" eventDocument.Document
            |> Option.iter (fun role ->
                addAssertion assertions (assertion messageNode (OtherEdge "has_role") (stringLiteral role) eventDocument.DomainId eventDocument.BoundedContextId (buildProvenance eventDocument)))

            TomlDocument.tryTableValue "body" "model_name" eventDocument.Document
            |> Option.iter (fun modelName ->
                addAssertion assertions (assertion messageNode (OtherEdge "has_model_name") (stringLiteral modelName) eventDocument.DomainId eventDocument.BoundedContextId (buildProvenance eventDocument)))

            TomlDocument.tryTableValue "body" "sequence_hint" eventDocument.Document
            |> Option.bind (fun value ->
                match Int32.TryParse(value) with
                | true, parsedValue -> Some parsedValue
                | _ -> None)
            |> Option.iter (fun sequenceHint ->
                addAssertion assertions (assertion messageNode (OtherEdge "has_sequence_hint") (intLiteral sequenceHint) eventDocument.DomainId eventDocument.BoundedContextId (buildProvenance eventDocument)))
        | _ -> ()

    let private addArtifactAssertions assertions eventDocument artifactNode =
        addNodeMetadata assertions eventDocument artifactNode "artifact_node"
        addSubjectLinks assertions eventDocument artifactNode
        addSemanticRole
            assertions
            (semanticRoleAnnotation
                artifactNode
                CoreRoles.imprint
                eventDocument.DomainId
                eventDocument.BoundedContextId
                (buildProvenance eventDocument))

        eventDocument.ConversationId
        |> Option.iter (fun conversationNode ->
            addAssertion assertions (assertion artifactNode BelongsToConversation (NodeRef conversationNode) eventDocument.DomainId eventDocument.BoundedContextId (buildProvenance eventDocument)))

        match eventDocument.MessageId with
        | Some messageNode ->
            addAssertion assertions (assertion messageNode ReferencesArtifact (NodeRef artifactNode) eventDocument.DomainId eventDocument.BoundedContextId (buildProvenance eventDocument))
        | None -> ()

        TomlDocument.tryTableValue "body" "file_name" eventDocument.Document
        |> Option.iter (fun fileName ->
            addAssertion assertions (assertion artifactNode (OtherEdge "has_file_name") (stringLiteral fileName) eventDocument.DomainId eventDocument.BoundedContextId (buildProvenance eventDocument)))

        TomlDocument.tryTableValue "body" "media_type" eventDocument.Document
        |> Option.iter (fun mediaType ->
            addAssertion assertions (assertion artifactNode (OtherEdge "has_media_type") (stringLiteral mediaType) eventDocument.DomainId eventDocument.BoundedContextId (buildProvenance eventDocument)))

        TomlDocument.tryTableValue "body" "disposition" eventDocument.Document
        |> Option.iter (fun disposition ->
            addAssertion assertions (assertion artifactNode (OtherEdge "has_reference_disposition") (stringLiteral disposition) eventDocument.DomainId eventDocument.BoundedContextId (buildProvenance eventDocument)))

    let private addImportAssertions assertions eventDocument importNode =
        addNodeMetadata assertions eventDocument importNode "import_node"
        addSubjectLinks assertions eventDocument importNode

    let private processEvent assertions eventDocument =
        eventDocument.ImportId |> Option.iter (addImportAssertions assertions eventDocument)
        eventDocument.ConversationId |> Option.iter (addConversationAssertions assertions eventDocument)
        eventDocument.MessageId |> Option.iter (addMessageAssertions assertions eventDocument)
        eventDocument.ArtifactId |> Option.iter (addArtifactAssertions assertions eventDocument)

    /// <summary>
    /// Rebuilds graph assertions from canonical history.
    /// </summary>
    /// <param name="rootPath">The root of the event-store workspace to rebuild into.</param>
    /// <returns>The relative paths of all written graph assertion files.</returns>
    /// <remarks>
    /// Graph assertions are derived from canonical history and are intended to be rebuildable.
    /// Full conceptual notes: docs/nexus-core-conceptual-layers.md
    /// </remarks>
    let rebuild rootPath =
        let absoluteRoot = Path.GetFullPath(rootPath)
        let assertions = Dictionary<string, GraphAssertion>(StringComparer.Ordinal)
        let eventsRoot = Path.Combine(absoluteRoot, "events")
        let assertionsRoot = Path.Combine(absoluteRoot, "graph", "assertions")

        if Directory.Exists(eventsRoot) then
            Directory.EnumerateFiles(eventsRoot, "*.toml", SearchOption.AllDirectories)
            |> Seq.sort
            |> Seq.map parseEventDocument
            |> Seq.iter (processEvent assertions)

        if Directory.Exists(assertionsRoot) then
            Directory.Delete(assertionsRoot, true)

        Directory.CreateDirectory(assertionsRoot) |> ignore

        assertions.Values
        |> Seq.sortBy (fun assertion -> FactId.format assertion.FactId)
        |> Seq.map (CanonicalStore.writeGraphAssertion absoluteRoot)
        |> Seq.toList
