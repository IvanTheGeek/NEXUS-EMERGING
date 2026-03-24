namespace Nexus.Cli

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Nexus.Curation
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers

module Program =
    type CommandHelp =
        { Name: string
          Summary: string
          Usage: string list
          Options: (string * string) list
          Examples: string list
          Notes: string list }

    type Command =
        | ShowHelp of commandName: string option
        | WriteSampleEventStore of eventStoreRoot: string
        | ImportProviderExport of request: ImportRequest
        | ImportCodexSessions of request: CodexSessionImportRequest
        | CaptureArtifactPayload of request: ManualArtifactCaptureRequest
        | RebuildGraphAssertions of eventStoreRoot: string * approved: bool
        | ExportGraphvizDot of eventStoreRoot: string * outputPath: string option * provider: string option * providerConversationId: string option * conversationId: string option * importId: string option
        | RebuildArtifactProjections of eventStoreRoot: string
        | ReportUnresolvedArtifacts of eventStoreRoot: string * provider: string option * limit: int
        | ReportWorkingGraphImports of eventStoreRoot: string * limit: int
        | RebuildConversationProjections of eventStoreRoot: string
        | CreateConceptNote of request: CreateConceptNoteRequest

    let private repoRoot =
        Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

    let private defaultEventStoreRoot =
        Path.Combine(repoRoot, "NEXUS-EventStore")

    let private defaultObjectsRoot =
        Path.Combine(repoRoot, "NEXUS-Objects")

    let private defaultDocsRoot =
        Path.Combine(repoRoot, "docs")

    let private defaultCodexSnapshotRoot =
        Path.Combine(defaultObjectsRoot, "providers", "codex", "latest")

    let private cliInvocation =
        "dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj --"

    let private sha256ForText (value: string) =
        let bytes = Encoding.UTF8.GetBytes(value)
        let hash = SHA256.HashData(bytes)
        Convert.ToHexString(hash).ToLowerInvariant()

    let private containsHelpSwitch args =
        args
        |> List.exists (fun value -> value = "--help" || value = "-h")

    let private printRows (rows: (string * string) list) =
        match rows with
        | [] -> ()
        | _ ->
            let width =
                rows
                |> List.map (fun (label, _) -> label.Length)
                |> List.max

            rows
            |> List.iter (fun (label, description) ->
                printfn "  %-*s  %s" width label description)

    let private commandHelp name =
        match name with
        | "write-sample-event-store" ->
            Some
                { Name = name
                  Summary = "Write a small sample canonical history bundle for event-store smoke testing."
                  Usage =
                    [ sprintf "%s write-sample-event-store" cliInvocation
                      sprintf "%s write-sample-event-store --event-store-root /tmp/nexus-event-store" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>",
                      sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot ]
                  Examples =
                    [ sprintf "%s write-sample-event-store" cliInvocation
                      sprintf "%s write-sample-event-store --event-store-root /tmp/nexus-event-store" cliInvocation ]
                  Notes =
                    [ "Useful for validating TOML output, stream layout, and projection rebuilds without touching real imports."
                      "Detailed guide: docs/how-to/write-sample-event-store.md" ] }
        | "import-provider-export" ->
            Some
                { Name = name
                  Summary = "Archive a ChatGPT or Claude export zip, parse provider records, and append canonical observed history."
                  Usage =
                    [ sprintf "%s import-provider-export --provider <chatgpt|claude> --zip <path>" cliInvocation
                      sprintf "%s import-provider-export --provider claude --zip RawDataExports/claude-export.zip --window full" cliInvocation ]
                  Options =
                    [ "--provider <chatgpt|claude>", "Required. Select the provider adapter."
                      "--zip <path>", "Required. Path to the provider export zip to archive and import."
                      "--window <kind>", "Import window label. Defaults to full."
                      "--objects-root <path>", sprintf "Override the objects root. Defaults to %s." defaultObjectsRoot
                      "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot ]
                  Examples =
                    [ sprintf "%s import-provider-export --provider claude --zip RawDataExports/claude-export.zip" cliInvocation
                      sprintf "%s import-provider-export --provider chatgpt --zip RawDataExports/chatgpt-export.zip --window full" cliInvocation ]
                  Notes =
                    [ "Overlapping imports are deduped by provider identity and canonical content hash."
                      "The CLI emits phase progress plus periodic conversation-processing updates during larger imports."
                      "Parser-version changes append reparse observations instead of fake provider revisions."
                      "Detailed guide: docs/how-to/import-provider-export.md" ] }
        | "import-codex-sessions" ->
            Some
                { Name = name
                  Summary = "Import preserved Codex session snapshots into canonical observed history."
                  Usage =
                    [ sprintf "%s import-codex-sessions" cliInvocation
                      sprintf "%s import-codex-sessions --snapshot-root NEXUS-Objects/providers/codex/latest" cliInvocation ]
                  Options =
                    [ "--snapshot-root <path>", sprintf "Override the preserved Codex snapshot root. Defaults to %s." defaultCodexSnapshotRoot
                      "--objects-root <path>", sprintf "Override the objects root. Defaults to %s." defaultObjectsRoot
                      "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot ]
                  Examples =
                    [ sprintf "%s import-codex-sessions" cliInvocation
                      sprintf "%s import-codex-sessions --snapshot-root NEXUS-Objects/providers/codex/archive/2026-03-22T16-03-56Z" cliInvocation ]
                  Notes =
                    [ "Run the raw export script first if the latest Codex snapshot has not been staged yet."
                      "Detailed guide: docs/how-to/import-codex-sessions.md" ] }
        | "capture-artifact-payload" ->
            Some
                { Name = name
                  Summary = "Manually hydrate an artifact payload, archive it in NEXUS-Objects, and append ArtifactPayloadCaptured when new."
                  Usage =
                    [ sprintf "%s capture-artifact-payload --artifact-id <uuid> --file <path>" cliInvocation
                      sprintf "%s capture-artifact-payload --provider claude --provider-conversation-id <id> --provider-message-id <id> --file <path>" cliInvocation ]
                  Options =
                    [ "--file <path>", "Required. Path to the local artifact payload."
                      "--artifact-id <uuid>", "Hydrate a known internal artifact ID directly."
                      "--provider <chatgpt|claude>", "Provider for provider-key lookup when not using --artifact-id."
                      "--provider-conversation-id <id>", "Provider conversation ID for provider-key lookup."
                      "--provider-message-id <id>", "Provider message ID for provider-key lookup."
                      "--provider-artifact-id <id>", "Provider artifact ID when the export referenced one."
                      "--file-name <name>", "File-name fallback when provider artifact ID is absent."
                      "--media-type <type>", "Override or supply the media type recorded with the capture."
                      "--notes <text>", "Optional operator notes for the capture event."
                      "--objects-root <path>", sprintf "Override the objects root. Defaults to %s." defaultObjectsRoot
                      "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot ]
                  Examples =
                    [ sprintf "%s capture-artifact-payload --artifact-id 019d174f-2c81-71e3-9cec-527f951cd6cf --file recovered/spec.pdf" cliInvocation
                      sprintf "%s capture-artifact-payload --provider claude --provider-conversation-id conv_123 --provider-message-id msg_456 --file recovered/spec.pdf --file-name spec.pdf" cliInvocation ]
                  Notes =
                    [ "Choose either --artifact-id or provider/message lookup options, not both."
                      "Repeated capture of the same payload is skipped by content hash."
                      "Detailed guide: docs/how-to/capture-artifact-payload.md" ] }
        | "rebuild-artifact-projections" ->
            Some
                { Name = name
                  Summary = "Rebuild the artifact read model from canonical artifact events."
                  Usage =
                    [ sprintf "%s rebuild-artifact-projections" cliInvocation
                      sprintf "%s rebuild-artifact-projections --event-store-root /tmp/nexus-event-store" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot ]
                  Examples =
                    [ sprintf "%s rebuild-artifact-projections" cliInvocation ]
                  Notes =
                    [ "Projection files are rebuildable views, not source truth."
                      "Detailed guide: docs/how-to/rebuild-artifact-projections.md" ] }
        | "rebuild-graph-assertions" ->
            Some
                { Name = name
                  Summary = "Rebuild the first thin graph-assertion layer from canonical history."
                  Usage =
                    [ sprintf "%s rebuild-graph-assertions --yes" cliInvocation
                      sprintf "%s rebuild-graph-assertions --event-store-root /tmp/nexus-event-store" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--yes", "Required for heavyweight full rebuilds when the canonical store is large." ]
                  Examples =
                    [ sprintf "%s rebuild-graph-assertions --yes" cliInvocation
                      sprintf "%s rebuild-graph-assertions --event-store-root /tmp/nexus-event-store" cliInvocation ]
                  Notes =
                    [ "Graph assertions are derived and rebuildable, not the canonical source of truth."
                      "Large full-store rebuilds are intentionally guarded because they are heavyweight operations."
                      "Each full rebuild writes a rebuild manifest under graph/rebuilds/ with counts and timings."
                      "This first pass derives node-kind, relationship, and attribute assertions from canonical history."
                      "Detailed guide: docs/how-to/rebuild-graph-assertions.md" ] }
        | "export-graphviz-dot" ->
            Some
                { Name = name
                  Summary = "Export the derived graph assertions as a Graphviz DOT file for external visualization, with optional provenance-based slicing."
                  Usage =
                    [ sprintf "%s export-graphviz-dot" cliInvocation
                      sprintf "%s export-graphviz-dot --provider claude" cliInvocation
                      sprintf "%s export-graphviz-dot --conversation-id <conversation-id>" cliInvocation
                      sprintf "%s export-graphviz-dot --provider-conversation-id <provider-conversation-id>" cliInvocation
                      sprintf "%s export-graphviz-dot --import-id <import-id> --output /tmp/nexus-graph.dot" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--output <path>", "Optional DOT output path. Defaults to a filter-aware name under <event-store-root>/graph/exports."
                      "--provider <chatgpt|claude|codex>", "Only include assertions whose provenance references the selected provider."
                      "--conversation-id <uuid>", "Only include the selected canonical conversation and its immediate graph neighborhood."
                      "--provider-conversation-id <id>", "Only include assertions whose provenance references the selected provider-native conversation ID."
                      "--import-id <uuid>", "Only include assertions whose provenance import_id matches the selected import." ]
                  Examples =
                    [ sprintf "%s export-graphviz-dot" cliInvocation
                      sprintf "%s export-graphviz-dot --provider claude" cliInvocation
                      sprintf "%s export-graphviz-dot --conversation-id 019d174e-e960-7507-8aa6-06ee0064e499" cliInvocation
                      sprintf "%s export-graphviz-dot --provider codex --output /tmp/codex-graph.dot" cliInvocation ]
                  Notes =
                    [ "Run rebuild-graph-assertions first when the derived graph may be stale."
                      "Canonical conversation slices use the conversation_id from conversation projections and include the conversation plus its immediate graph neighborhood."
                      "Filters are applied from graph assertion provenance, which makes provider, conversation, and import slices practical without replaying canonical history."
                      "This is an external lens over derived graph assertions, useful for surfacing patterns outside current NEXUS views."
                      "Detailed guide: docs/how-to/export-graphviz-dot.md" ] }
        | "report-unresolved-artifacts" ->
            Some
                { Name = name
                  Summary = "Summarize artifact references whose payloads have not yet been captured."
                  Usage =
                    [ sprintf "%s report-unresolved-artifacts" cliInvocation
                      sprintf "%s report-unresolved-artifacts --provider claude --limit 10" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--provider <chatgpt|claude>", "Limit the report to a single provider."
                      "--limit <n>", "Limit detailed items. Defaults to 20." ]
                  Examples =
                    [ sprintf "%s report-unresolved-artifacts" cliInvocation
                      sprintf "%s report-unresolved-artifacts --provider chatgpt --limit 25" cliInvocation ]
                  Notes =
                    [ "This report reads artifact projections, so rebuild them after new imports if needed."
                      "Detailed guide: docs/how-to/report-unresolved-artifacts.md" ] }
        | "report-working-graph-imports" ->
            Some
                { Name = name
                  Summary = "Summarize the import-batch graph working slices without rereading every assertion TOML file."
                  Usage =
                    [ sprintf "%s report-working-graph-imports" cliInvocation
                      sprintf "%s report-working-graph-imports --limit 10" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--limit <n>", "Limit detailed items. Defaults to 20." ]
                  Examples =
                    [ sprintf "%s report-working-graph-imports" cliInvocation
                      sprintf "%s report-working-graph-imports --event-store-root /tmp/nexus-event-store --limit 5" cliInvocation ]
                  Notes =
                    [ "This report prefers the graph working catalog under graph/working/catalog/ and falls back to manifest scanning if needed."
                      "The detail list joins each working-slice entry back to the canonical import manifest when present."
                      "Detailed guide: docs/how-to/report-working-graph-imports.md" ] }
        | "rebuild-conversation-projections" ->
            Some
                { Name = name
                  Summary = "Rebuild the conversation read model from canonical conversation events."
                  Usage =
                    [ sprintf "%s rebuild-conversation-projections" cliInvocation
                      sprintf "%s rebuild-conversation-projections --event-store-root /tmp/nexus-event-store" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot ]
                  Examples =
                    [ sprintf "%s rebuild-conversation-projections" cliInvocation ]
                  Notes =
                    [ "Projection files are rebuildable views that summarize conversation streams for quick inspection."
                      "Detailed guide: docs/how-to/rebuild-conversation-projections.md" ] }
        | "create-concept-note" ->
            Some
                { Name = name
                  Summary = "Create a curated concept-note seed from one or more canonical conversation projections."
                  Usage =
                    [ sprintf "%s create-concept-note --slug <slug> --title <title> --conversation-id <uuid>" cliInvocation
                      sprintf "%s create-concept-note --slug fnhci --title FnHCI --conversation-id 019d174e-e960-7507-8aa6-06ee0064e499" cliInvocation ]
                  Options =
                    [ "--slug <slug>", "Required. File-safe note slug. It will be normalized to lowercase kebab-case."
                      "--title <title>", "Required. Human-readable concept title."
                      "--conversation-id <uuid>", "Required. Repeat to harvest more than one canonical conversation into the seed note."
                      "--domain <slug>", "Optional domain hint to record in the note front matter. Repeatable."
                      "--tag <slug>", "Optional tag to record in the note front matter. Repeatable."
                      "--docs-root <path>", sprintf "Override the docs root. Defaults to %s." defaultDocsRoot
                      "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot ]
                  Examples =
                    [ sprintf "%s create-concept-note --slug fnhci --title FnHCI --conversation-id 019d174e-e960-7507-8aa6-06ee0064e499" cliInvocation
                      sprintf "%s create-concept-note --slug nexus-graph-lenses --title \"Graph Lenses\" --conversation-id <uuid> --conversation-id <uuid> --domain interaction-design" cliInvocation ]
                  Notes =
                    [ "This writes a Markdown note under docs/concepts/ and keeps source provenance back to canonical conversation projections."
                      "Seed notes are meant to be edited and refined by humans and AI after creation."
                      "Detailed guide: docs/how-to/create-concept-note.md" ] }
        | _ -> None

    let private availableCommands () =
        [ "write-sample-event-store"
          "import-provider-export"
          "import-codex-sessions"
          "capture-artifact-payload"
          "rebuild-graph-assertions"
          "export-graphviz-dot"
          "rebuild-artifact-projections"
          "report-unresolved-artifacts"
          "report-working-graph-imports"
          "rebuild-conversation-projections"
          "create-concept-note" ]
        |> List.choose (fun name ->
            commandHelp name
            |> Option.map (fun help -> help.Name, help.Summary))

    let private printCommandHelp name =
        match commandHelp name with
        | None ->
            eprintfn "Unknown command: %s" name
            printfn ""
            printfn "Use one of these command names:"
            printRows (availableCommands ())
        | Some help ->
            printfn "Command: %s" help.Name
            printfn ""
            printfn "%s" help.Summary

            if not help.Usage.IsEmpty then
                printfn ""
                printfn "Usage:"
                help.Usage |> List.iter (printfn "  %s")

            if not help.Options.IsEmpty then
                printfn ""
                printfn "Options:"
                printRows help.Options

            if not help.Examples.IsEmpty then
                printfn ""
                printfn "Examples:"
                help.Examples |> List.iter (printfn "  %s")

            if not help.Notes.IsEmpty then
                printfn ""
                printfn "Notes:"
                help.Notes |> List.iter (printfn "  - %s")

    let private usage () =
        printfn "NEXUS CLI"
        printfn ""
        printfn "Usage:"
        printfn "  %s <command> [options]" cliInvocation
        printfn "  %s help <command>" cliInvocation
        printfn ""
        printfn "Commands:"
        printRows (availableCommands ())
        printfn ""
        printfn "Repository defaults:"
        printRows
            [ "event store", defaultEventStoreRoot
              "objects", defaultObjectsRoot
              "docs", defaultDocsRoot
              "codex snapshots", defaultCodexSnapshotRoot ]
        printfn ""
        printfn "Help:"
        printfn "  Use --help, -h, or help <command> for command-specific guidance."
        printfn "  See docs/how-to/cli-commands.md for the human-facing command guide."

    let private parseWriteSampleEventStore (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "write-sample-event-store"))
        else
            let rec loop currentRoot remaining =
                match remaining with
                | [] -> Ok (WriteSampleEventStore currentRoot)
                | "--event-store-root" :: value :: rest -> loop value rest
                | option :: _ ->
                    eprintfn "Unknown option for write-sample-event-store: %s" option
                    printCommandHelp "write-sample-event-store"
                    Error 1

            loop defaultEventStoreRoot args

    let private parseImportProviderExport (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "import-provider-export"))
        else
            let rec loop provider zipPath window objectsRoot eventStoreRoot remaining =
                match remaining with
                | [] ->
                    match provider, zipPath with
                    | Some providerKind, Some sourceZipPath ->
                        Ok
                            (ImportProviderExport
                                { Provider = providerKind
                                  SourceZipPath = sourceZipPath
                                  Window = window
                                  ObjectsRoot = objectsRoot
                                  EventStoreRoot = eventStoreRoot })
                    | None, _ ->
                        eprintfn "Missing required option for import-provider-export: --provider"
                        printCommandHelp "import-provider-export"
                        Error 1
                    | _, None ->
                        eprintfn "Missing required option for import-provider-export: --zip"
                        printCommandHelp "import-provider-export"
                        Error 1
                | "--provider" :: value :: rest ->
                    match ProviderNaming.tryParse value with
                    | Some providerKind -> loop (Some providerKind) zipPath window objectsRoot eventStoreRoot rest
                    | None ->
                        eprintfn "Unsupported provider: %s" value
                        printCommandHelp "import-provider-export"
                        Error 1
                | "--zip" :: value :: rest ->
                    loop provider (Some value) window objectsRoot eventStoreRoot rest
                | "--window" :: value :: rest ->
                    match ImportWindowNaming.tryParse value with
                    | Some parsedWindow -> loop provider zipPath (Some parsedWindow) objectsRoot eventStoreRoot rest
                    | None ->
                        eprintfn "Unsupported window: %s" value
                        printCommandHelp "import-provider-export"
                        Error 1
                | "--objects-root" :: value :: rest ->
                    loop provider zipPath window value eventStoreRoot rest
                | "--event-store-root" :: value :: rest ->
                    loop provider zipPath window objectsRoot value rest
                | option :: _ ->
                    eprintfn "Unknown option for import-provider-export: %s" option
                    printCommandHelp "import-provider-export"
                    Error 1

            loop None None (Some Full) defaultObjectsRoot defaultEventStoreRoot args

    let private parseCaptureArtifactPayload (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "capture-artifact-payload"))
        else
            let rec loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath mediaType notes objectsRoot eventStoreRoot remaining =
                match remaining with
                | [] ->
                    match sourceFilePath with
                    | None ->
                        eprintfn "Missing required option for capture-artifact-payload: --file"
                        printCommandHelp "capture-artifact-payload"
                        Error 1
                    | Some filePath ->
                        let target =
                            match artifactId, provider, conversationNativeId, messageNativeId with
                            | Some internalArtifactId, None, None, None ->
                                Ok (ExistingArtifactId internalArtifactId)
                            | Some _, _, _, _ ->
                                eprintfn "Use either --artifact-id or provider/message lookup options, not both."
                                printCommandHelp "capture-artifact-payload"
                                Error 1
                            | None, Some providerKind, Some conversationId, Some messageId ->
                                Ok
                                    (ProviderArtifactReference(
                                        providerKind,
                                        conversationId,
                                        messageId,
                                        providerArtifactId,
                                        fileName))
                            | None, Some _, _, _ ->
                                eprintfn "Provider lookup requires --provider-conversation-id and --provider-message-id."
                                printCommandHelp "capture-artifact-payload"
                                Error 1
                            | None, None, _, _ ->
                                eprintfn "Missing target for capture-artifact-payload. Use --artifact-id or provider/message lookup options."
                                printCommandHelp "capture-artifact-payload"
                                Error 1

                        target
                        |> Result.map (fun resolvedTarget ->
                            CaptureArtifactPayload
                                { Target = resolvedTarget
                                  SourceFilePath = filePath
                                  ObjectsRoot = objectsRoot
                                  EventStoreRoot = eventStoreRoot
                                  MediaType = mediaType
                                  Notes = notes })
                | "--artifact-id" :: value :: rest ->
                    loop
                        (Some (ArtifactId.parse value))
                        provider
                        conversationNativeId
                        messageNativeId
                        providerArtifactId
                        fileName
                        sourceFilePath
                        mediaType
                        notes
                        objectsRoot
                        eventStoreRoot
                        rest
                | "--provider" :: value :: rest ->
                    match ProviderNaming.tryParse value with
                    | Some providerKind ->
                        loop artifactId (Some providerKind) conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath mediaType notes objectsRoot eventStoreRoot rest
                    | None ->
                        eprintfn "Unsupported provider: %s" value
                        printCommandHelp "capture-artifact-payload"
                        Error 1
                | "--provider-conversation-id" :: value :: rest ->
                    loop artifactId provider (Some value) messageNativeId providerArtifactId fileName sourceFilePath mediaType notes objectsRoot eventStoreRoot rest
                | "--provider-message-id" :: value :: rest ->
                    loop artifactId provider conversationNativeId (Some value) providerArtifactId fileName sourceFilePath mediaType notes objectsRoot eventStoreRoot rest
                | "--provider-artifact-id" :: value :: rest ->
                    loop artifactId provider conversationNativeId messageNativeId (Some value) fileName sourceFilePath mediaType notes objectsRoot eventStoreRoot rest
                | "--file-name" :: value :: rest ->
                    loop artifactId provider conversationNativeId messageNativeId providerArtifactId (Some value) sourceFilePath mediaType notes objectsRoot eventStoreRoot rest
                | "--file" :: value :: rest ->
                    loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName (Some value) mediaType notes objectsRoot eventStoreRoot rest
                | "--media-type" :: value :: rest ->
                    loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath (Some value) notes objectsRoot eventStoreRoot rest
                | "--notes" :: value :: rest ->
                    loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath mediaType (Some value) objectsRoot eventStoreRoot rest
                | "--objects-root" :: value :: rest ->
                    loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath mediaType notes value eventStoreRoot rest
                | "--event-store-root" :: value :: rest ->
                    loop artifactId provider conversationNativeId messageNativeId providerArtifactId fileName sourceFilePath mediaType notes objectsRoot value rest
                | option :: _ ->
                    eprintfn "Unknown option for capture-artifact-payload: %s" option
                    printCommandHelp "capture-artifact-payload"
                    Error 1

            loop None None None None None None None None None defaultObjectsRoot defaultEventStoreRoot args

    let private parseImportCodexSessions (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "import-codex-sessions"))
        else
            let rec loop snapshotRoot objectsRoot eventStoreRoot remaining =
                match remaining with
                | [] ->
                    Ok
                        (ImportCodexSessions
                            { SnapshotRoot = snapshotRoot
                              ObjectsRoot = objectsRoot
                              EventStoreRoot = eventStoreRoot })
                | "--snapshot-root" :: value :: rest ->
                    loop value objectsRoot eventStoreRoot rest
                | "--objects-root" :: value :: rest ->
                    loop snapshotRoot value eventStoreRoot rest
                | "--event-store-root" :: value :: rest ->
                    loop snapshotRoot objectsRoot value rest
                | option :: _ ->
                    eprintfn "Unknown option for import-codex-sessions: %s" option
                    printCommandHelp "import-codex-sessions"
                    Error 1

            loop defaultCodexSnapshotRoot defaultObjectsRoot defaultEventStoreRoot args

    let private parseRebuildConversationProjections (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "rebuild-conversation-projections"))
        else
            let rec loop eventStoreRoot remaining =
                match remaining with
                | [] -> Ok (RebuildConversationProjections eventStoreRoot)
                | "--event-store-root" :: value :: rest ->
                    loop value rest
                | option :: _ ->
                    eprintfn "Unknown option for rebuild-conversation-projections: %s" option
                    printCommandHelp "rebuild-conversation-projections"
                    Error 1

            loop defaultEventStoreRoot args

    let private parseCreateConceptNote (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "create-concept-note"))
        else
            let rec loop slug title conversationIds domains tags docsRoot eventStoreRoot remaining =
                match remaining with
                | [] ->
                    match slug, title with
                    | Some noteSlug, Some noteTitle ->
                        Ok
                            (CreateConceptNote
                                { DocsRoot = docsRoot
                                  EventStoreRoot = eventStoreRoot
                                  Slug = noteSlug
                                  Title = noteTitle
                                  Domains = List.rev domains
                                  Tags = List.rev tags
                                  SourceConversationIds = List.rev conversationIds })
                    | None, _ ->
                        eprintfn "Missing required option for create-concept-note: --slug"
                        printCommandHelp "create-concept-note"
                        Error 1
                    | _, None ->
                        eprintfn "Missing required option for create-concept-note: --title"
                        printCommandHelp "create-concept-note"
                        Error 1
                | "--slug" :: value :: rest ->
                    loop (Some value) title conversationIds domains tags docsRoot eventStoreRoot rest
                | "--title" :: value :: rest ->
                    loop slug (Some value) conversationIds domains tags docsRoot eventStoreRoot rest
                | "--conversation-id" :: value :: rest ->
                    loop slug title (value :: conversationIds) domains tags docsRoot eventStoreRoot rest
                | "--domain" :: value :: rest ->
                    loop slug title conversationIds (value :: domains) tags docsRoot eventStoreRoot rest
                | "--tag" :: value :: rest ->
                    loop slug title conversationIds domains (value :: tags) docsRoot eventStoreRoot rest
                | "--docs-root" :: value :: rest ->
                    loop slug title conversationIds domains tags value eventStoreRoot rest
                | "--event-store-root" :: value :: rest ->
                    loop slug title conversationIds domains tags docsRoot value rest
                | option :: _ ->
                    eprintfn "Unknown option for create-concept-note: %s" option
                    printCommandHelp "create-concept-note"
                    Error 1

            loop None None [] [] [] defaultDocsRoot defaultEventStoreRoot args

    let private parseRebuildArtifactProjections (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "rebuild-artifact-projections"))
        else
            let rec loop eventStoreRoot remaining =
                match remaining with
                | [] -> Ok (RebuildArtifactProjections eventStoreRoot)
                | "--event-store-root" :: value :: rest ->
                    loop value rest
                | option :: _ ->
                    eprintfn "Unknown option for rebuild-artifact-projections: %s" option
                    printCommandHelp "rebuild-artifact-projections"
                    Error 1

            loop defaultEventStoreRoot args

    let private parseRebuildGraphAssertions (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "rebuild-graph-assertions"))
        else
            let rec loop eventStoreRoot approved remaining =
                match remaining with
                | [] -> Ok (RebuildGraphAssertions(eventStoreRoot, approved))
                | "--event-store-root" :: value :: rest ->
                    loop value approved rest
                | "--yes" :: rest ->
                    loop eventStoreRoot true rest
                | option :: _ ->
                    eprintfn "Unknown option for rebuild-graph-assertions: %s" option
                    printCommandHelp "rebuild-graph-assertions"
                    Error 1

            loop defaultEventStoreRoot false args

    let private parseExportGraphvizDot (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "export-graphviz-dot"))
        else
            let rec loop eventStoreRoot outputPath provider providerConversationId conversationId importId remaining =
                match remaining with
                | [] -> Ok (ExportGraphvizDot(eventStoreRoot, outputPath, provider, providerConversationId, conversationId, importId))
                | "--event-store-root" :: value :: rest ->
                    loop value outputPath provider providerConversationId conversationId importId rest
                | "--output" :: value :: rest ->
                    loop eventStoreRoot (Some value) provider providerConversationId conversationId importId rest
                | "--provider" :: value :: rest ->
                    match ProviderNaming.tryParse value with
                    | Some providerKind ->
                        loop eventStoreRoot outputPath (Some (ProviderNaming.slug providerKind)) providerConversationId conversationId importId rest
                    | None ->
                        eprintfn "Unsupported provider: %s" value
                        printCommandHelp "export-graphviz-dot"
                        Error 1
                | "--conversation-id" :: value :: rest ->
                    loop eventStoreRoot outputPath provider providerConversationId (Some value) importId rest
                | "--provider-conversation-id" :: value :: rest ->
                    loop eventStoreRoot outputPath provider (Some value) conversationId importId rest
                | "--import-id" :: value :: rest ->
                    loop eventStoreRoot outputPath provider providerConversationId conversationId (Some value) rest
                | option :: _ ->
                    eprintfn "Unknown option for export-graphviz-dot: %s" option
                    printCommandHelp "export-graphviz-dot"
                    Error 1

            loop defaultEventStoreRoot None None None None None args

    let private parseReportUnresolvedArtifacts (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "report-unresolved-artifacts"))
        else
            let rec loop eventStoreRoot provider limit remaining =
                match remaining with
                | [] -> Ok (ReportUnresolvedArtifacts(eventStoreRoot, provider, limit))
                | "--event-store-root" :: value :: rest ->
                    loop value provider limit rest
                | "--provider" :: value :: rest ->
                    loop eventStoreRoot (Some (value.Trim().ToLowerInvariant())) limit rest
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop eventStoreRoot provider parsedValue rest
                    | _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "report-unresolved-artifacts"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for report-unresolved-artifacts: %s" option
                    printCommandHelp "report-unresolved-artifacts"
                    Error 1

            loop defaultEventStoreRoot None 20 args

    let private parseReportWorkingGraphImports (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "report-working-graph-imports"))
        else
            let rec loop eventStoreRoot limit remaining =
                match remaining with
                | [] -> Ok (ReportWorkingGraphImports(eventStoreRoot, limit))
                | "--event-store-root" :: value :: rest ->
                    loop value limit rest
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop eventStoreRoot parsedValue rest
                    | true, _
                    | false, _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "report-working-graph-imports"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for report-working-graph-imports: %s" option
                    printCommandHelp "report-working-graph-imports"
                    Error 1

            loop defaultEventStoreRoot 20 args

    let private parseCommand args =
        match args with
        | [] ->
            usage ()
            Error 1
        | [ "--help" ]
        | [ "-h" ] ->
            Ok (ShowHelp None)
        | [ "help" ] ->
            Ok (ShowHelp None)
        | [ "help"; commandName ] ->
            match commandHelp commandName with
            | Some _ -> Ok (ShowHelp (Some commandName))
            | None ->
                eprintfn "Unknown command: %s" commandName
                usage ()
                Error 1
        | "write-sample-event-store" :: rest ->
            parseWriteSampleEventStore rest
        | "import-provider-export" :: rest ->
            parseImportProviderExport rest
        | "import-codex-sessions" :: rest ->
            parseImportCodexSessions rest
        | "capture-artifact-payload" :: rest ->
            parseCaptureArtifactPayload rest
        | "rebuild-graph-assertions" :: rest ->
            parseRebuildGraphAssertions rest
        | "export-graphviz-dot" :: rest ->
            parseExportGraphvizDot rest
        | "rebuild-artifact-projections" :: rest ->
            parseRebuildArtifactProjections rest
        | "report-unresolved-artifacts" :: rest ->
            parseReportUnresolvedArtifacts rest
        | "report-working-graph-imports" :: rest ->
            parseReportWorkingGraphImports rest
        | "rebuild-conversation-projections" :: rest ->
            parseRebuildConversationProjections rest
        | "create-concept-note" :: rest ->
            parseCreateConceptNote rest
        | command :: _ ->
            eprintfn "Unknown command: %s" command
            usage ()
            Error 1

    let private occurredAt value = OccurredAt value
    let private observedAt value = ObservedAt value
    let private importedAt value = ImportedAt value

    let private contentHashForText value =
        { Algorithm = "sha256"
          Value = sha256ForText value }

    let private buildSampleData () =
        let now = DateTimeOffset.UtcNow
        let importId = ImportId.create ()
        let importArtifactId = ArtifactId.create ()
        let conversationId = ConversationId.create ()
        let firstMessageId = MessageId.create ()
        let secondMessageId = MessageId.create ()
        let artifactId = ArtifactId.create ()

        let importedAtValue = importedAt now
        let observedAtValue = observedAt now

        let rootArtifact =
            { RawObjectId = Some (ArtifactId.format importArtifactId)
              Kind = ProviderExportZip
              RelativePath = "providers/claude/archive/2026-03-22T12-13-53Z/data-2026-03-22-12-13-53-batch-0000.zip"
              ArchivedAt = Some importedAtValue
              SourceDescription = Some "Original Claude export zip" }

        let conversationRef =
            { Provider = Claude
              ObjectKind = ConversationObject
              NativeId = Some "bb96fb46-e42e-4bde-a885-4b59d5290fc0"
              ConversationNativeId = Some "bb96fb46-e42e-4bde-a885-4b59d5290fc0"
              MessageNativeId = None
              ArtifactNativeId = None }

        let firstMessageRef =
            { Provider = Claude
              ObjectKind = MessageObject
              NativeId = Some "019cce65-86c1-7007-bfa7-2847c82547d1"
              ConversationNativeId = conversationRef.ConversationNativeId
              MessageNativeId = Some "019cce65-86c1-7007-bfa7-2847c82547d1"
              ArtifactNativeId = None }

        let secondMessageRef =
            { Provider = Claude
              ObjectKind = MessageObject
              NativeId = Some "019cce65-86c1-7dd4-9b8b-4fd51c2d038e"
              ConversationNativeId = conversationRef.ConversationNativeId
              MessageNativeId = Some "019cce65-86c1-7dd4-9b8b-4fd51c2d038e"
              ArtifactNativeId = None }

        let attachmentRef =
            { Provider = Claude
              ObjectKind = ArtifactObject
              NativeId = Some "6-slices-adam.txt"
              ConversationNativeId = conversationRef.ConversationNativeId
              MessageNativeId = Some "019ccfa2-269f-7274-aa84-b4992c72a09c"
              ArtifactNativeId = Some "6-slices-adam.txt" }

        let baseEnvelope eventId =
            { EventId = eventId
              ConversationId = None
              MessageId = None
              ArtifactId = None
              TurnId = None
              DomainId = Some (DomainId.create "ingestion")
              BoundedContextId = Some (BoundedContextId.create "canonical-history")
              OccurredAt = Some (occurredAt now)
              ObservedAt = observedAtValue
              ImportedAt = Some importedAtValue
              SourceAcquisition = ExportZip
              NormalizationVersion = Some NormalizationNaming.current
              ContentHash = None
              ImportId = Some importId
              ProviderRefs = []
              RawObjects = [ rootArtifact ] }

        let firstMessageText = "can you make a mermaid sequence diagram that renders in this chat?"
        let secondMessageText = "Sure! Here's a simple example using Mermaid syntax."

        let providerArtifactReceivedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ArtifactId = Some importArtifactId }
              Body =
                ProviderArtifactReceived
                    { ArtifactId = importArtifactId
                      Provider = Claude
                      FileName = "data-2026-03-22-12-13-53-batch-0000.zip"
                      Window = Some Full
                      ByteCount = Some 36619935L } }

        let rawSnapshotExtractedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ArtifactId = Some importArtifactId }
              Body =
                RawSnapshotExtracted
                    { ArtifactId = importArtifactId
                      ExtractedEntries = Some 4
                      Notes = Some "Top-level JSON payloads extracted for parsing" } }

        let conversationObservedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ConversationId = Some conversationId
                    ProviderRefs = [ conversationRef ] }
              Body =
                ProviderConversationObserved
                    { ConversationId = conversationId
                      ProviderConversation = conversationRef
                      Title = Some "Mermaid sequence diagram for chat"
                      IsArchived = Some false
                      MessageCountHint = Some 4 } }

        let firstMessageObservedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ConversationId = Some conversationId
                    MessageId = Some firstMessageId
                    ContentHash = Some (contentHashForText firstMessageText)
                    ProviderRefs = [ conversationRef; firstMessageRef ] }
              Body =
                ProviderMessageObserved
                    { MessageId = firstMessageId
                      ConversationId = conversationId
                      ProviderMessage = firstMessageRef
                      Role = Human
                      Segments =
                        [ { Kind = PlainText
                            Text = firstMessageText } ]
                      ModelName = None
                      SequenceHint = Some 1 } }

        let secondMessageObservedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ConversationId = Some conversationId
                    MessageId = Some secondMessageId
                    ContentHash = Some (contentHashForText secondMessageText)
                    ProviderRefs = [ conversationRef; secondMessageRef ] }
              Body =
                ProviderMessageObserved
                    { MessageId = secondMessageId
                      ConversationId = conversationId
                      ProviderMessage = secondMessageRef
                      Role = Assistant
                      Segments =
                        [ { Kind = PlainText
                            Text = secondMessageText } ]
                      ModelName = Some "claude-3-7-sonnet"
                      SequenceHint = Some 2 } }

        let artifactReferencedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ConversationId = Some conversationId
                    MessageId = Some secondMessageId
                    ArtifactId = Some artifactId
                    ProviderRefs = [ conversationRef; secondMessageRef; attachmentRef ] }
              Body =
                ArtifactReferenced
                    { ArtifactId = artifactId
                      ConversationId = Some conversationId
                      MessageId = Some secondMessageId
                      FileName = Some "6-slices-adam.txt"
                      MediaType = Some "text/plain"
                      Disposition = PayloadIncluded
                      ProviderArtifact = Some attachmentRef } }

        let appendedEventCount = 7

        let counts =
            { ConversationsSeen = 1
              MessagesSeen = 2
              ArtifactsReferenced = 1
              NewEventsAppended = appendedEventCount
              DuplicatesSkipped = 0
              RevisionsObserved = 0
              ReparseObservationsAppended = 0 }

        let importCompletedEvent =
            { Envelope =
                { baseEnvelope (CanonicalEventId.create ()) with
                    ArtifactId = Some importArtifactId }
              Body =
                ImportCompleted
                    { ImportId = importId
                      Window = Some Full
                      Counts = counts
                      Notes = Some "Sample event-store smoke test" } }

        let events =
            [ providerArtifactReceivedEvent
              rawSnapshotExtractedEvent
              conversationObservedEvent
              firstMessageObservedEvent
              secondMessageObservedEvent
              artifactReferencedEvent
              importCompletedEvent ]

        let manifest =
            { ImportId = importId
              Provider = Claude
              SourceAcquisition = ExportZip
              NormalizationVersion = Some NormalizationNaming.current
              Window = Some Full
              ImportedAt = importedAtValue
              RootArtifact = rootArtifact
              Counts = counts
              NewCanonicalEventIds = events |> List.map (fun event -> event.Envelope.EventId)
              Notes = [ "Sample event-store smoke test" ] }

        events, manifest

    let private writeSampleEventStore eventStoreRoot =
        let events, manifest = buildSampleData ()
        Directory.CreateDirectory(eventStoreRoot) |> ignore
        let eventPaths = CanonicalStore.writeCanonicalEvents eventStoreRoot events
        let manifestPath = CanonicalStore.writeImportManifest eventStoreRoot manifest

        printfn "Sample canonical history written."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Events written: %d" eventPaths.Length

        for relativePath in eventPaths do
            printfn "    %s" relativePath

        printfn "  Manifest written: %s" manifestPath
        0

    let private importProviderExport request =
        let result = ImportWorkflow.runWithStatus (fun message -> printfn "  %s" message) request

        printfn "Provider export imported."
        printfn "  Provider: %s" (ProviderNaming.slug result.Provider)
        printfn "  Import ID: %s" (ImportId.format result.ImportId)
        printfn "  Archived zip: %s" result.ArchivedZipRelativePath
        printfn "  Latest zip: %s" result.LatestZipRelativePath

        match result.ExtractedConversationRelativePath with
        | Some path -> printfn "  Extracted conversations.json: %s" path
        | None -> ()

        printfn "  Event manifest: %s" result.ManifestRelativePath
        match result.WorkingGraphManifestRelativePath, result.WorkingGraphCatalogRelativePath, result.WorkingGraphAssertionCount with
        | Some manifestPath, Some catalogPath, Some assertionCount ->
            printfn "  Working graph manifest: %s" manifestPath
            printfn "  Working graph catalog: %s" catalogPath
            printfn "  Working graph assertions written: %d" assertionCount
        | Some manifestPath, None, Some assertionCount ->
            printfn "  Working graph manifest: %s" manifestPath
            printfn "  Working graph assertions written: %d" assertionCount
        | None, Some catalogPath, Some assertionCount ->
            printfn "  Working graph catalog: %s" catalogPath
            printfn "  Working graph assertions written: %d" assertionCount
        | Some manifestPath, Some catalogPath, None ->
            printfn "  Working graph manifest: %s" manifestPath
            printfn "  Working graph catalog: %s" catalogPath
        | Some manifestPath, None, None ->
            printfn "  Working graph manifest: %s" manifestPath
        | None, Some catalogPath, None ->
            printfn "  Working graph catalog: %s" catalogPath
        | None, None, Some assertionCount ->
            printfn "  Working graph assertions written: %d" assertionCount
        | None, None, None -> ()
        printfn "  Events written: %d" result.EventPaths.Length
        printfn "  Conversations seen: %d" result.Counts.ConversationsSeen
        printfn "  Messages seen: %d" result.Counts.MessagesSeen
        printfn "  Artifact references seen: %d" result.Counts.ArtifactsReferenced
        printfn "  New events appended: %d" result.Counts.NewEventsAppended
        printfn "  Duplicates skipped: %d" result.Counts.DuplicatesSkipped
        printfn "  Revisions observed: %d" result.Counts.RevisionsObserved
        printfn "  Reparse observations appended: %d" result.Counts.ReparseObservationsAppended

        if not result.EventPaths.IsEmpty then
            printfn "  First event files:"

            result.EventPaths
            |> List.truncate 5
            |> List.iter (printfn "    %s")

        0

    let private importCodexSessions request =
        let result = CodexImportWorkflow.run request

        printfn "Codex sessions imported."
        printfn "  Provider: codex"
        printfn "  Import ID: %s" (ImportId.format result.ImportId)
        printfn "  Snapshot root: %s" result.SnapshotRoot
        printfn "  Root artifact: %s" result.RootArtifactRelativePath
        printfn "  Event manifest: %s" result.ManifestRelativePath
        printfn "  Events written: %d" result.EventPaths.Length
        printfn "  Conversations seen: %d" result.Counts.ConversationsSeen
        printfn "  Messages seen: %d" result.Counts.MessagesSeen
        printfn "  New events appended: %d" result.Counts.NewEventsAppended
        printfn "  Duplicates skipped: %d" result.Counts.DuplicatesSkipped
        printfn "  Revisions observed: %d" result.Counts.RevisionsObserved
        printfn "  Reparse observations appended: %d" result.Counts.ReparseObservationsAppended

        if not result.EventPaths.IsEmpty then
            printfn "  First event files:"

            result.EventPaths
            |> List.truncate 5
            |> List.iter (printfn "    %s")

        0

    let private captureArtifactPayload request =
        let result = ManualArtifactWorkflow.run request

        if result.DuplicateSkipped then
            printfn "Artifact payload already known."
            printfn "  Artifact ID: %s" (ArtifactId.format result.ArtifactId)
            printfn "  Byte count: %d" result.ByteCount
            printfn "  Content hash: %s:%s" result.ContentHash.Algorithm result.ContentHash.Value
        else
            printfn "Artifact payload captured."
            printfn "  Artifact ID: %s" (ArtifactId.format result.ArtifactId)

            match result.Provider with
            | Some provider -> printfn "  Provider: %s" (ProviderNaming.slug provider)
            | None -> ()

            match result.ArchivedRelativePath with
            | Some relativePath -> printfn "  Archived file: %s" relativePath
            | None -> ()

            match result.EventPath with
            | Some relativePath -> printfn "  Event written: %s" relativePath
            | None -> ()

            printfn "  Byte count: %d" result.ByteCount
            printfn "  Content hash: %s:%s" result.ContentHash.Algorithm result.ContentHash.Value

        0

    let private rebuildConversationProjections eventStoreRoot =
        let projectionPaths = ConversationProjections.rebuild eventStoreRoot
        printfn "Conversation projections rebuilt."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Projection files written: %d" projectionPaths.Length

        projectionPaths
        |> List.truncate 5
        |> List.iter (printfn "    %s")

        0

    let private rebuildArtifactProjections eventStoreRoot =
        let projectionPaths = ArtifactProjections.rebuild eventStoreRoot
        printfn "Artifact projections rebuilt."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Projection files written: %d" projectionPaths.Length

        projectionPaths
        |> List.truncate 5
        |> List.iter (printfn "    %s")

        0

    let private rebuildGraphAssertions eventStoreRoot approved =
        let estimate = GraphMaterialization.estimateFullRebuild eventStoreRoot

        if estimate.RequiresExplicitApproval && not approved then
            eprintfn "Heavyweight graph rebuild refused without explicit approval."
            eprintfn "  Event store root: %s" eventStoreRoot
            eprintfn "  Canonical event files: %d" estimate.CanonicalEventFileCount
            eprintfn "  Threshold: %d" estimate.HeavyweightThreshold
            eprintfn "  Re-run with --yes to proceed."
            2
        else
            let result =
                GraphMaterialization.rebuildFullWithStatus
                    (fun message -> printfn "  %s" message)
                    approved
                    eventStoreRoot

            if estimate.RequiresExplicitApproval then
                printfn "  Heavyweight full graph rebuild approved with --yes."

            printfn "Graph assertions rebuilt."
            printfn "  Event store root: %s" eventStoreRoot
            printfn "  Canonical event files scanned: %d" result.CanonicalEventFileCount
            printfn "  Assertion files written: %d" result.GraphAssertionCount
            printfn "  Derivation elapsed: %O" result.DerivationElapsed
            printfn "  Total elapsed: %O" result.TotalElapsed
            printfn "  Rebuild manifest: %s" result.ManifestRelativePath

            result.AssertionPaths
            |> List.truncate 5
            |> List.iter (printfn "    %s")

            0

    let private exportGraphvizDot eventStoreRoot outputPath provider providerConversationId conversationId importId =
        let filter =
            { GraphvizDot.ExportFilter.empty with
                Provider = provider
                ProviderConversationId = providerConversationId
                ConversationId = conversationId
                ImportId = importId }

        let result = GraphvizDot.exportFiltered eventStoreRoot outputPath filter
        printfn "Graphviz DOT exported."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Output path: %s" result.OutputPath
        printfn "  Assertions scanned: %d" result.ScannedAssertionCount
        printfn "  Assertions exported: %d" result.AssertionCount
        printfn "  Nodes written: %d" result.NodeCount
        printfn "  Edges written: %d" result.EdgeCount
        0

    let private reportUnresolvedArtifacts eventStoreRoot provider limit =
        let report = ArtifactProjectionReports.buildUnresolvedReport eventStoreRoot provider limit

        printfn "Unresolved artifact report."
        printfn "  Event store root: %s" eventStoreRoot

        match provider with
        | Some providerValue -> printfn "  Provider filter: %s" providerValue
        | None -> ()

        printfn "  Total artifacts: %d" report.TotalArtifacts
        printfn "  Payload captured: %d" report.CapturedArtifacts
        printfn "  Unresolved artifacts: %d" report.UnresolvedArtifacts

        if not report.ProviderCounts.IsEmpty then
            printfn "  Unresolved by provider:"

            report.ProviderCounts
            |> List.iter (fun (providerValue, count) ->
                printfn "    %s: %d" providerValue count)

        if not report.Items.IsEmpty then
            printfn "  Showing up to %d unresolved artifacts:" report.Items.Length

            report.Items
            |> List.iter (fun item ->
                let providerLabel =
                    match item.Providers with
                    | head :: _ -> head
                    | [] -> "unknown"

                let fileLabel = item.FileName |> Option.defaultValue "<no file name>"
                let providerArtifactLabel =
                    match item.ProviderArtifactIds with
                    | head :: _ -> head
                    | [] -> "<no provider artifact id>"

                printfn "    %s | %s | %s" item.ArtifactId providerLabel fileLabel
                printfn "      provider_artifact_id: %s" providerArtifactLabel

                item.ConversationId
                |> Option.iter (printfn "      conversation_id: %s")

                item.MessageId
                |> Option.iter (printfn "      message_id: %s")

                item.LastObservedAt
                |> Option.iter (fun timestamp -> printfn "      last_observed_at: %s" (timestamp.ToUniversalTime().ToString("O")))
                )

        0

    let private reportWorkingGraphImports eventStoreRoot limit =
        let report = GraphWorkingCatalog.buildReport eventStoreRoot limit

        printfn "Graph working imports report."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Catalog: %s" report.CatalogRelativePath
        printfn "  Working slices: %d" report.WorkingSliceCount
        printfn "  Total canonical events: %d" report.TotalCanonicalEvents
        printfn "  Total graph assertions: %d" report.TotalGraphAssertions

        if not report.ProviderCounts.IsEmpty then
            printfn "  Providers:"

            report.ProviderCounts
            |> List.iter (fun (providerValue, count) ->
                printfn "    %s: %d" providerValue count)

        if not report.Items.IsEmpty then
            printfn "  Recent slices:"

            report.Items
            |> List.iter (fun item ->
                let providerText =
                    match item.Provider with
                    | Some providerValue -> providerValue
                    | None -> "unknown"

                let windowText =
                    match item.Window with
                    | Some windowValue -> windowValue
                    | None -> "unknown"

                let importedAtText =
                    match item.ImportedAt with
                    | Some importedAtValue -> importedAtValue.ToUniversalTime().ToString("O")
                    | None -> "unknown"

                printfn "    %s" (ImportId.format item.ImportId)
                printfn
                    "      provider=%s window=%s imported_at=%s materialized_at=%s"
                    providerText
                    windowText
                    importedAtText
                    (item.MaterializedAt.ToUniversalTime().ToString("O"))
                printfn
                    "      canonical_events=%d graph_assertions=%d"
                    item.CanonicalEventCount
                    item.GraphAssertionCount
                printfn "      manifest=%s" item.ManifestRelativePath)

        0

    let private createConceptNote request =
        match ConceptNotes.create request with
        | Error error ->
            eprintfn "%s" error
            1
        | Ok result ->
            printfn "Concept note created."
            printfn "  Output path: %s" result.OutputPath
            printfn "  Slug: %s" result.NormalizedSlug
            printfn "  Source conversations: %d" result.SourceConversations.Length

            result.SourceConversations
            |> List.iter (fun source ->
                printfn "    %s | %s" source.ConversationId source.Title)

            0

    [<EntryPoint>]
    let main argv =
        match parseCommand (argv |> Array.toList) with
        | Ok (ShowHelp None) ->
            usage ()
            0
        | Ok (ShowHelp (Some commandName)) ->
            printCommandHelp commandName
            0
        | Ok (WriteSampleEventStore eventStoreRoot) ->
            writeSampleEventStore eventStoreRoot
        | Ok (ImportProviderExport request) ->
            importProviderExport request
        | Ok (ImportCodexSessions request) ->
            importCodexSessions request
        | Ok (CaptureArtifactPayload request) ->
            captureArtifactPayload request
        | Ok (RebuildGraphAssertions(eventStoreRoot, approved)) ->
            rebuildGraphAssertions eventStoreRoot approved
        | Ok (ExportGraphvizDot(eventStoreRoot, outputPath, provider, providerConversationId, conversationId, importId)) ->
            exportGraphvizDot eventStoreRoot outputPath provider providerConversationId conversationId importId
        | Ok (RebuildArtifactProjections eventStoreRoot) ->
            rebuildArtifactProjections eventStoreRoot
        | Ok (ReportUnresolvedArtifacts(eventStoreRoot, provider, limit)) ->
            reportUnresolvedArtifacts eventStoreRoot provider limit
        | Ok (ReportWorkingGraphImports(eventStoreRoot, limit)) ->
            reportWorkingGraphImports eventStoreRoot limit
        | Ok (RebuildConversationProjections eventStoreRoot) ->
            rebuildConversationProjections eventStoreRoot
        | Ok (CreateConceptNote request) ->
            createConceptNote request
        | Error exitCode ->
            exitCode
