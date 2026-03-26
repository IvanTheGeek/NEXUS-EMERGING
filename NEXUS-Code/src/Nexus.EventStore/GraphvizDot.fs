namespace Nexus.EventStore

open System
open System.Collections.Generic
open System.IO
open System.Text

[<RequireQualifiedAccess>]
module GraphvizDot =
    /// <summary>
    /// Narrows a Graphviz DOT export to a practical scope of the derived graph.
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

    let private graphWorkingImportAssertionsRoot rootPath importId =
        Path.Combine(Path.GetFullPath(rootPath), "graph", "working", "imports", importId, "assertions")

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
            "scope"
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

    let private defaultWorkingImportOutputPath rootPath importId =
        let fileName = $"nexus-working-graph__import-{sanitizeFileToken importId}.dot"
        Path.Combine(Path.GetFullPath(rootPath), "graph", "working", "exports", fileName)

    let private defaultWorkingNeighborhoodOutputPath rootPath importId nodeId =
        let fileName =
            $"nexus-working-graph__import-{sanitizeFileToken importId}__node-{sanitizeFileToken nodeId}.dot"

        Path.Combine(Path.GetFullPath(rootPath), "graph", "working", "exports", fileName)

    let private outputPathWithinRoot outputRoot fileName =
        Path.Combine(Path.GetFullPath(outputRoot), fileName)

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

    let private applyNodeNeighborhoodFilter rootNodeId assertions =
        let neighborhoodNodes = HashSet<string>(StringComparer.Ordinal)
        neighborhoodNodes.Add(rootNodeId) |> ignore

        assertions
        |> Array.iter (fun assertion ->
            if assertion.SubjectNodeId = rootNodeId || assertion.ObjectNodeId = Some rootNodeId then
                neighborhoodNodes.Add(assertion.SubjectNodeId) |> ignore

                assertion.ObjectNodeId
                    |> Option.iter (fun objectNodeId ->
                        neighborhoodNodes.Add(objectNodeId) |> ignore))

        if neighborhoodNodes.Count = 1 then
            invalidArg "rootNodeId" $"No graph neighborhood found for node {rootNodeId}."

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

    let private exportAssertions assertionsPath outputPath =
        let absoluteOutputPath = Path.GetFullPath(outputPath)
        let allAssertions = loadAssertions assertionsPath
        let nodeCount, edgeCount = writeDot absoluteOutputPath allAssertions

        { OutputPath = absoluteOutputPath
          NodeCount = nodeCount
          EdgeCount = edgeCount
          AssertionCount = allAssertions.Length
          ScannedAssertionCount = allAssertions.Length }

    /// <summary>
    /// Exports a filtered scope of the derived graph assertions under an event-store root as a Graphviz DOT file.
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
    let exportFilteredWithRoot rootPath outputPath outputRoot filter =
        let assertionsPath = graphAssertionsRoot rootPath
        let absoluteRoot = Path.GetFullPath(rootPath)
        let destinationPath =
            match outputPath, outputRoot with
            | Some explicitPath, None
            | Some explicitPath, Some _ -> explicitPath
            | None, Some rootDirectory -> outputPathWithinRoot rootDirectory (defaultFileName filter)
            | None, None -> defaultOutputPath absoluteRoot filter
        let absoluteOutputPath = Path.GetFullPath(destinationPath)
        let allAssertions = loadAssertions assertionsPath
        let provenanceFilteredAssertions =
            allAssertions
            |> Array.filter (matchesFilter filter)

        let selectedAssertions =
            match filter.ConversationId with
            | Some conversationId -> applyNodeNeighborhoodFilter conversationId provenanceFilteredAssertions
            | None -> provenanceFilteredAssertions

        let nodeCount, edgeCount = writeDot absoluteOutputPath selectedAssertions

        { OutputPath = absoluteOutputPath
          NodeCount = nodeCount
          EdgeCount = edgeCount
          AssertionCount = selectedAssertions.Length
          ScannedAssertionCount = allAssertions.Length }

    let exportFiltered rootPath outputPath filter =
        exportFilteredWithRoot rootPath outputPath None filter

    /// <summary>
    /// Exports one graph working import batch as a Graphviz DOT file without requiring a full durable graph rebuild.
    /// </summary>
    /// <param name="rootPath">Event-store root containing graph/working/imports/&lt;import-id&gt;/assertions.</param>
    /// <param name="importId">The import batch whose working graph batch should be exported.</param>
    /// <param name="outputPath">
    /// Optional DOT output path. When omitted, the exporter writes to graph/working/exports using an import-aware file name.
    /// </param>
    /// <remarks>
    /// This reads the secondary graph working layer directly and is useful immediately after imports.
    /// See docs/how-to/export-graphviz-dot.md for usage guidance.
    /// </remarks>
    let exportWorkingImportBatchWithRoot rootPath importId outputPath outputRoot =
        let assertionsPath = graphWorkingImportAssertionsRoot rootPath importId
        let absoluteRoot = Path.GetFullPath(rootPath)
        let destinationPath =
            match outputPath, outputRoot with
            | Some explicitPath, None
            | Some explicitPath, Some _ -> explicitPath
            | None, Some rootDirectory ->
                let fileName = Path.GetFileName(defaultWorkingImportOutputPath absoluteRoot importId)
                outputPathWithinRoot rootDirectory fileName
            | None, None -> defaultWorkingImportOutputPath absoluteRoot importId
        exportAssertions assertionsPath destinationPath

    let exportWorkingImportBatch rootPath importId outputPath =
        exportWorkingImportBatchWithRoot rootPath importId outputPath None

    /// <summary>
    /// Exports the immediate neighborhood of one node from a graph working import batch as a Graphviz DOT file.
    /// </summary>
    /// <param name="rootPath">Event-store root containing graph/working/imports/&lt;import-id&gt;/assertions.</param>
    /// <param name="importId">The import batch whose working graph batch should be inspected.</param>
    /// <param name="nodeId">The node whose immediate working-batch neighborhood should be exported.</param>
    /// <param name="outputPath">
    /// Optional DOT output path. When omitted, the exporter writes to graph/working/exports using an import-and-node-aware file name.
    /// </param>
    /// <remarks>
    /// This is a scoped visualization helper over the secondary graph working layer.
    /// See docs/how-to/export-graphviz-dot.md for usage guidance.
    /// </remarks>
    let exportWorkingNodeNeighborhoodWithRoot rootPath importId nodeId outputPath outputRoot =
        let assertionsPath = graphWorkingImportAssertionsRoot rootPath importId
        let absoluteRoot = Path.GetFullPath(rootPath)
        let destinationPath =
            match outputPath, outputRoot with
            | Some explicitPath, None
            | Some explicitPath, Some _ -> explicitPath
            | None, Some rootDirectory ->
                let fileName = Path.GetFileName(defaultWorkingNeighborhoodOutputPath absoluteRoot importId nodeId)
                outputPathWithinRoot rootDirectory fileName
            | None, None -> defaultWorkingNeighborhoodOutputPath absoluteRoot importId nodeId

        let absoluteOutputPath = Path.GetFullPath(destinationPath)
        let allAssertions = loadAssertions assertionsPath
        let selectedAssertions = applyNodeNeighborhoodFilter nodeId allAssertions
        let nodeCount, edgeCount = writeDot absoluteOutputPath selectedAssertions

        { OutputPath = absoluteOutputPath
          NodeCount = nodeCount
          EdgeCount = edgeCount
          AssertionCount = selectedAssertions.Length
          ScannedAssertionCount = allAssertions.Length }

    let exportWorkingNodeNeighborhood rootPath importId nodeId outputPath =
        exportWorkingNodeNeighborhoodWithRoot rootPath importId nodeId outputPath None

    /// <summary>
    /// Exports the full derived graph assertions under an event-store root as a Graphviz DOT file.
    /// </summary>
    /// <param name="rootPath">Event-store root containing graph/assertions.</param>
    /// <param name="outputPath">
    /// Optional DOT output path. When omitted, the exporter writes to graph/exports/nexus-graph.dot under the event-store root.
    /// </param>
    let export rootPath outputPath =
        exportFiltered rootPath outputPath ExportFilter.empty
