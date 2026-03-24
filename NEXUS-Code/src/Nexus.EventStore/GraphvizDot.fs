namespace Nexus.EventStore

open System
open System.Collections.Generic
open System.IO
open System.Text

[<RequireQualifiedAccess>]
module GraphvizDot =
    /// <summary>
    /// Narrows a Graphviz DOT export to a practical slice of the derived graph.
    /// </summary>
    type ExportFilter =
        { Provider: string option
          ProviderConversationId: string option
          ConversationId: string option
          ImportId: string option }

    [<RequireQualifiedAccess>]
    module ExportFilter =
        /// <summary>
        /// The full derived graph with no additional filtering.
        /// </summary>
        let empty =
            { Provider = None
              ProviderConversationId = None
              ConversationId = None
              ImportId = None }

    /// <summary>
    /// Describes the result of exporting graph assertions into a Graphviz DOT file.
    /// </summary>
    /// <remarks>
    /// Full workflow notes: docs/how-to/export-graphviz-dot.md
    /// </remarks>
    type ExportResult =
        { OutputPath: string
          NodeCount: int
          EdgeCount: int
          AssertionCount: int
          ScannedAssertionCount: int }

    type private NodeState =
        { NodeId: string
          mutable NodeKind: string option
          Titles: HashSet<string>
          Slugs: HashSet<string>
          SemanticRoles: HashSet<string>
          MessageRoles: HashSet<string>
          FileNames: HashSet<string>
          MediaTypes: HashSet<string> }

    type private EdgeState =
        { FromNodeId: string
          ToNodeId: string
          Predicate: string }

    type private AssertionRecord =
        { SubjectNodeId: string
          Predicate: string
          ObjectNodeId: string option
          LiteralValue: string option
          Providers: Set<string>
          ProviderConversationIds: Set<string>
          ImportId: string option }

    let private graphAssertionsRoot rootPath =
        Path.Combine(Path.GetFullPath(rootPath), "graph", "assertions")

    let private normalizeFilterValue (value: string) =
        value.Trim().ToLowerInvariant()

    let private sanitizeFileToken (value: string) =
        let normalized = normalizeFilterValue value

        let builder = StringBuilder(normalized.Length)

        normalized
        |> Seq.iter (fun ch ->
            if Char.IsLetterOrDigit(ch) then
                builder.Append(ch) |> ignore
            elif ch = '-' || ch = '_' then
                builder.Append(ch) |> ignore
            else
                builder.Append('-') |> ignore)

        let sanitized =
            builder.ToString().Trim('-')

        if String.IsNullOrWhiteSpace(sanitized) then
            "slice"
        elif sanitized.Length <= 40 then
            sanitized
        else
            sanitized.Substring(0, 40)

    let private defaultFileName filter =
        let tokens =
            [ filter.Provider |> Option.map (fun value -> $"provider-{sanitizeFileToken value}")
              filter.ProviderConversationId |> Option.map (fun value -> $"conversation-{sanitizeFileToken value}")
              filter.ConversationId |> Option.map (fun value -> $"canonical-conversation-{sanitizeFileToken value}")
              filter.ImportId |> Option.map (fun value -> $"import-{sanitizeFileToken value}") ]
            |> List.choose id

        match tokens with
        | [] -> "nexus-graph.dot"
        | _ ->
            let suffix = String.concat "__" tokens
            $"nexus-graph__{suffix}.dot"

    let private defaultOutputPath rootPath filter =
        Path.Combine(Path.GetFullPath(rootPath), "graph", "exports", defaultFileName filter)

    let private getOrAddNode (nodes: Dictionary<string, NodeState>) nodeId =
        match nodes.TryGetValue(nodeId) with
        | true, node -> node
        | false, _ ->
            let node =
                { NodeId = nodeId
                  NodeKind = None
                  Titles = HashSet(StringComparer.Ordinal)
                  Slugs = HashSet(StringComparer.Ordinal)
                  SemanticRoles = HashSet(StringComparer.Ordinal)
                  MessageRoles = HashSet(StringComparer.Ordinal)
                  FileNames = HashSet(StringComparer.Ordinal)
                  MediaTypes = HashSet(StringComparer.Ordinal) }

            nodes[nodeId] <- node
            node

    let private dotEscape (value: string) =
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal)
            .Replace("\r", "", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)

    let private shortId (value: string) =
        if String.IsNullOrWhiteSpace(value) || value.Length <= 8 then
            value
        else
            value.Substring(0, 8)

    let private primaryLabel (node: NodeState) =
        let tryFirst (values: HashSet<string>) =
            values |> Seq.sort |> Seq.tryHead

        [ tryFirst node.Titles
          tryFirst node.FileNames
          tryFirst node.Slugs
          node.NodeKind ]
        |> List.tryPick id
        |> Option.defaultValue $"node {shortId node.NodeId}"

    let private nodeDetails (node: NodeState) =
        let append prefix (values: HashSet<string>) =
            values
            |> Seq.sort
            |> Seq.map (fun value -> $"{prefix}{value}")
            |> Seq.toList

        [ node.NodeKind |> Option.map (fun kind -> $"kind: {kind}")
          append "semantic: " node.SemanticRoles |> List.tryHead
          append "message: " node.MessageRoles |> List.tryHead
          append "media: " node.MediaTypes |> List.tryHead ]
        |> List.choose id

    let private nodeShapeAndColor nodeKind =
        match nodeKind with
        | Some "conversation_node" -> "box", "#dbeafe"
        | Some "message_node" -> "note", "#fef3c7"
        | Some "artifact_node" -> "folder", "#dcfce7"
        | Some "import_node" -> "cylinder", "#ede9fe"
        | Some "domain_node" -> "ellipse", "#fee2e2"
        | Some "bounded_context_node" -> "ellipse", "#e0f2fe"
        | _ -> "box", "#f3f4f6"

    let private nodeLabel node =
        let lines = primaryLabel node :: nodeDetails node
        String.concat "\n" lines

    let private edgeKey edge =
        $"{edge.FromNodeId}|{edge.Predicate}|{edge.ToNodeId}"

    let private addLiteralAssertion (node: NodeState) predicate value =
        match predicate with
        | "has_node_kind" -> node.NodeKind <- Some value
        | "has_title" -> node.Titles.Add(value) |> ignore
        | "has_slug" -> node.Slugs.Add(value) |> ignore
        | "has_semantic_role" -> node.SemanticRoles.Add(value) |> ignore
        | "has_role" -> node.MessageRoles.Add(value) |> ignore
        | "has_file_name" -> node.FileNames.Add(value) |> ignore
        | "has_media_type" -> node.MediaTypes.Add(value) |> ignore
        | _ -> ()

    let private parseProviders document =
        TomlDocument.tableArray "provider_refs" document
        |> List.choose (fun table ->
            match table.TryGetValue("provider") with
            | true, value -> Some (normalizeFilterValue value)
            | false, _ -> None)
        |> Set.ofList

    let private parseProviderConversationIds document =
        TomlDocument.tableArray "provider_refs" document
        |> List.choose (fun table ->
            match table.TryGetValue("conversation_native_id") with
            | true, value when not (String.IsNullOrWhiteSpace(value)) -> Some value
            | _ -> None)
        |> Set.ofList

    let private tryParseAssertion document =
        match TomlDocument.tryScalar "subject_node_id" document, TomlDocument.tryScalar "predicate" document with
        | Some subjectNodeId, Some predicate ->
            let objectNodeId, literalValue =
                match TomlDocument.tryTableValue "object" "kind" document with
                | Some "node_ref" ->
                    TomlDocument.tryTableValue "object" "node_id" document, None
                | Some "literal" ->
                    None, TomlDocument.tryTableValue "object" "value" document
                | _ -> None, None

            Some
                { SubjectNodeId = subjectNodeId
                  Predicate = predicate
                  ObjectNodeId = objectNodeId
                  LiteralValue = literalValue
                  Providers = parseProviders document
                  ProviderConversationIds = parseProviderConversationIds document
                  ImportId = TomlDocument.tryTableValue "provenance" "import_id" document }
        | _ -> None

    let private loadAssertions assertionsPath =
        if Directory.Exists(assertionsPath) then
            Directory.EnumerateFiles(assertionsPath, "*.toml", SearchOption.AllDirectories)
            |> Seq.sort
            |> Seq.choose (fun path ->
                File.ReadAllText(path)
                |> TomlDocument.parse
                |> tryParseAssertion)
            |> Seq.toArray
        else
            [||]

    let private matchesFilter filter assertion =
        let providerMatch =
            match filter.Provider with
            | None -> true
            | Some provider -> assertion.Providers.Contains(normalizeFilterValue provider)

        let providerConversationMatch =
            match filter.ProviderConversationId with
            | None -> true
            | Some conversationId -> assertion.ProviderConversationIds.Contains(conversationId)

        let importMatch =
            match filter.ImportId with
            | None -> true
            | Some importId -> assertion.ImportId = Some importId

        providerMatch && providerConversationMatch && importMatch

    let private applyConversationNeighborhoodFilter conversationId assertions =
        let neighborhoodNodes = HashSet<string>(StringComparer.Ordinal)
        neighborhoodNodes.Add(conversationId) |> ignore

        assertions
        |> Array.iter (fun assertion ->
            if assertion.SubjectNodeId = conversationId || assertion.ObjectNodeId = Some conversationId then
                neighborhoodNodes.Add(assertion.SubjectNodeId) |> ignore

                assertion.ObjectNodeId
                |> Option.iter (fun objectNodeId ->
                    neighborhoodNodes.Add(objectNodeId) |> ignore))

        assertions
        |> Array.filter (fun assertion ->
            neighborhoodNodes.Contains(assertion.SubjectNodeId)
            &&
            match assertion.ObjectNodeId with
            | Some objectNodeId -> neighborhoodNodes.Contains(objectNodeId)
            | None -> true)

    let private writeDot absoluteOutputPath assertions =
        let nodes = Dictionary<string, NodeState>(StringComparer.Ordinal)
        let edges = Dictionary<string, EdgeState>(StringComparer.Ordinal)

        assertions
        |> Array.iter (fun assertion ->
            let subjectNode = getOrAddNode nodes assertion.SubjectNodeId

            match assertion.ObjectNodeId, assertion.LiteralValue with
            | Some objectNodeId, _ ->
                let _ = getOrAddNode nodes objectNodeId
                let edge =
                    { FromNodeId = assertion.SubjectNodeId
                      ToNodeId = objectNodeId
                      Predicate = assertion.Predicate }

                edges[edgeKey edge] <- edge
            | None, Some value ->
                addLiteralAssertion subjectNode assertion.Predicate value
            | None, None -> ())

        let outputDirectory = Path.GetDirectoryName(absoluteOutputPath : string)
        Directory.CreateDirectory(outputDirectory) |> ignore

        let builder = StringBuilder()
        builder.AppendLine("digraph nexus_graph {") |> ignore
        builder.AppendLine("  graph [rankdir=LR, overlap=false, splines=true];") |> ignore
        builder.AppendLine("  node [style=\"rounded,filled\", color=\"#4b5563\", fontname=\"Helvetica\"];") |> ignore
        builder.AppendLine("  edge [color=\"#6b7280\", fontname=\"Helvetica\"];") |> ignore

        nodes.Values
        |> Seq.sortBy (fun node -> node.NodeId)
        |> Seq.iter (fun node ->
            let shape, fillColor = nodeShapeAndColor node.NodeKind
            builder.AppendLine($"  \"{dotEscape node.NodeId}\" [shape={shape}, fillcolor=\"{fillColor}\", label=\"{dotEscape (nodeLabel node)}\"];")
            |> ignore)

        edges.Values
        |> Seq.sortBy edgeKey
        |> Seq.iter (fun edge ->
            builder.AppendLine($"  \"{dotEscape edge.FromNodeId}\" -> \"{dotEscape edge.ToNodeId}\" [label=\"{dotEscape edge.Predicate}\"];")
            |> ignore)

        builder.AppendLine("}") |> ignore

        File.WriteAllText(absoluteOutputPath, builder.ToString())

        nodes.Count, edges.Count

    /// <summary>
    /// Exports a filtered slice of the derived graph assertions under an event-store root as a Graphviz DOT file.
    /// </summary>
    /// <param name="rootPath">Event-store root containing graph/assertions.</param>
    /// <param name="outputPath">
    /// Optional DOT output path. When omitted, the exporter writes to graph/exports using a filter-aware file name.
    /// </param>
    /// <param name="filter">Optional provenance-aware filter for narrowing the exported graph.</param>
    /// <remarks>
    /// This is an external lens over derived graph assertions, not a canonical source of truth.
    /// See docs/how-to/export-graphviz-dot.md for usage guidance.
    /// </remarks>
    let exportFiltered rootPath outputPath filter =
        let assertionsPath = graphAssertionsRoot rootPath
        let absoluteRoot = Path.GetFullPath(rootPath)
        let destinationPath = outputPath |> Option.defaultWith (fun () -> defaultOutputPath absoluteRoot filter)
        let absoluteOutputPath = Path.GetFullPath(destinationPath)
        let allAssertions = loadAssertions assertionsPath
        let provenanceFilteredAssertions =
            allAssertions
            |> Array.filter (matchesFilter filter)

        let selectedAssertions =
            match filter.ConversationId with
            | Some conversationId -> applyConversationNeighborhoodFilter conversationId provenanceFilteredAssertions
            | None -> provenanceFilteredAssertions

        let nodeCount, edgeCount = writeDot absoluteOutputPath selectedAssertions

        { OutputPath = absoluteOutputPath
          NodeCount = nodeCount
          EdgeCount = edgeCount
          AssertionCount = selectedAssertions.Length
          ScannedAssertionCount = allAssertions.Length }

    /// <summary>
    /// Exports the full derived graph assertions under an event-store root as a Graphviz DOT file.
    /// </summary>
    /// <param name="rootPath">Event-store root containing graph/assertions.</param>
    /// <param name="outputPath">
    /// Optional DOT output path. When omitted, the exporter writes to graph/exports/nexus-graph.dot under the event-store root.
    /// </param>
    let export rootPath outputPath =
        exportFiltered rootPath outputPath ExportFilter.empty
