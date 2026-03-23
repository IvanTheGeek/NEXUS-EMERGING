namespace Nexus.EventStore

open System
open System.Collections.Generic
open System.IO
open System.Text

[<RequireQualifiedAccess>]
module GraphvizDot =
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
          AssertionCount: int }

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

    let private graphAssertionsRoot rootPath =
        Path.Combine(Path.GetFullPath(rootPath), "graph", "assertions")

    let private defaultOutputPath rootPath =
        Path.Combine(Path.GetFullPath(rootPath), "graph", "exports", "nexus-graph.dot")

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

        let details =
            [ node.NodeKind |> Option.map (fun kind -> $"kind: {kind}")
              append "semantic: " node.SemanticRoles |> List.tryHead
              append "message: " node.MessageRoles |> List.tryHead
              append "media: " node.MediaTypes |> List.tryHead ]
            |> List.choose id

        details

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

    /// <summary>
    /// Exports the derived graph assertions under an event-store root as a Graphviz DOT file.
    /// </summary>
    /// <param name="rootPath">Event-store root containing graph/assertions.</param>
    /// <param name="outputPath">
    /// Optional DOT output path. When omitted, the exporter writes to graph/exports/nexus-graph.dot under the event-store root.
    /// </param>
    /// <remarks>
    /// This is an external lens over derived graph assertions, not a canonical source of truth.
    /// See docs/how-to/export-graphviz-dot.md for usage guidance.
    /// </remarks>
    let export rootPath outputPath =
        let assertionsPath = graphAssertionsRoot rootPath
        let destinationPath = outputPath |> Option.defaultWith (fun () -> defaultOutputPath rootPath)
        let absoluteOutputPath = Path.GetFullPath(destinationPath)
        let nodes = Dictionary<string, NodeState>(StringComparer.Ordinal)
        let edges = Dictionary<string, EdgeState>(StringComparer.Ordinal)
        let mutable assertionCount = 0

        if Directory.Exists(assertionsPath) then
            Directory.EnumerateFiles(assertionsPath, "*.toml", SearchOption.AllDirectories)
            |> Seq.sort
            |> Seq.iter (fun path ->
                let document = File.ReadAllText(path) |> TomlDocument.parse

                match TomlDocument.tryScalar "subject_node_id" document, TomlDocument.tryScalar "predicate" document with
                | Some subjectNodeId, Some predicate ->
                    assertionCount <- assertionCount + 1
                    let subjectNode = getOrAddNode nodes subjectNodeId

                    match TomlDocument.tryTableValue "object" "kind" document with
                    | Some "node_ref" ->
                        match TomlDocument.tryTableValue "object" "node_id" document with
                        | Some objectNodeId ->
                            let _ = getOrAddNode nodes objectNodeId
                            let edge =
                                { FromNodeId = subjectNodeId
                                  ToNodeId = objectNodeId
                                  Predicate = predicate }

                            edges[edgeKey edge] <- edge
                        | None -> ()
                    | Some "literal" ->
                        match TomlDocument.tryTableValue "object" "value" document with
                        | Some value -> addLiteralAssertion subjectNode predicate value
                        | None -> ()
                    | _ -> ()
                | _ -> ())

        Directory.CreateDirectory(Path.GetDirectoryName(absoluteOutputPath)) |> ignore

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
        |> Seq.sortBy (fun edge -> edgeKey edge)
        |> Seq.iter (fun edge ->
            builder.AppendLine($"  \"{dotEscape edge.FromNodeId}\" -> \"{dotEscape edge.ToNodeId}\" [label=\"{dotEscape edge.Predicate}\"];")
            |> ignore)

        builder.AppendLine("}") |> ignore

        File.WriteAllText(absoluteOutputPath, builder.ToString())

        { OutputPath = absoluteOutputPath
          NodeCount = nodes.Count
          EdgeCount = edges.Count
          AssertionCount = assertionCount }
