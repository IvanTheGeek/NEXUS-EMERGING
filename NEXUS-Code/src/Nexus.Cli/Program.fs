namespace Nexus.Cli

open System
open System.IO
open System.Security.Cryptography
open System.Text
open Nexus.Curation
open Nexus.Domain
open Nexus.EventStore
open Nexus.Importers
open Nexus.Logos

module Program =
    type private CommandHelp =
        { Name: string
          Summary: string
          Usage: string list
          Options: (string * string) list
          Examples: string list
          Notes: string list }

    type private GraphvizExportVerification =
        | NoVerification
        | TraceableWorkingSlice

    type private Command =
        | ShowHelp of commandName: string option
        | WriteSampleEventStore of eventStoreRoot: string
        | CompareProviderExports of provider: ProviderKind * baseZipPath: string * currentZipPath: string * limit: int
        | CompareImportSnapshots of eventStoreRoot: string * baseImportId: ImportId * currentImportId: ImportId * limit: int
        | ReportProviderImportHistory of eventStoreRoot: string * objectsRoot: string * provider: string * limit: int
        | ReportCurrentIngestion of eventStoreRoot: string * objectsRoot: string
        | ReportLogosCatalog
        | ReportConversationOverlapCandidates of eventStoreRoot: string * leftProvider: string * rightProvider: string * limit: int
        | RebuildImportSnapshots of eventStoreRoot: string * objectsRoot: string * scope: ImportSnapshotBackfillScope * force: bool
        | ImportProviderExport of request: ImportRequest
        | ImportCodexSessions of request: CodexSessionImportRequest
        | CaptureArtifactPayload of request: ManualArtifactCaptureRequest
        | RebuildGraphAssertions of eventStoreRoot: string * approved: bool
        | ExportGraphvizDot of
            eventStoreRoot: string *
            objectsRoot: string *
            outputPath: string option *
            outputRoot: string option *
            verification: GraphvizExportVerification *
            provider: string option *
            providerConversationId: string option *
            conversationId: string option *
            importId: string option *
            workingImportId: string option *
            workingNodeId: string option
        | RenderGraphvizDot of inputPath: string * outputPath: string option * outputRoot: string option * engine: GraphvizEngine * format: GraphvizFormat
        | RebuildArtifactProjections of eventStoreRoot: string
        | ReportUnresolvedArtifacts of eventStoreRoot: string * provider: string option * limit: int
        | ReportWorkingGraphImports of eventStoreRoot: string * limit: int
        | ReportWorkingImportConversations of eventStoreRoot: string * importId: ImportId * limit: int
        | CompareWorkingImportConversations of eventStoreRoot: string * baseImportId: ImportId * currentImportId: ImportId * limit: int
        | FindWorkingGraphNodes of eventStoreRoot: string * importId: ImportId option * provider: string option * matchText: string option * semanticRole: string option * messageRole: string option * limit: int
        | ReportWorkingGraphSlice of eventStoreRoot: string * importId: ImportId * limit: int
        | ReportWorkingGraphNeighborhood of eventStoreRoot: string * importId: ImportId * nodeId: string * limit: int
        | VerifyWorkingGraphSlice of eventStoreRoot: string * objectsRoot: string * importId: ImportId
        | RebuildWorkingGraphIndex of eventStoreRoot: string
        | RebuildConversationProjections of eventStoreRoot: string
        | CreateLogosIntakeNote of request: CreateLogosIntakeNoteRequest
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

    let private sha256ForFile path =
        use stream = File.OpenRead(path)
        SHA256.HashData(stream) |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

    let private containsHelpSwitch args =
        args
        |> List.exists (fun value -> value = "--help" || value = "-h")

    let private parseGraphvizExportVerification (value: string) : GraphvizExportVerification option =
        let normalized = value.Trim().ToLowerInvariant()

        match normalized with
        | "none" -> Some NoVerification
        | "traceable" -> Some TraceableWorkingSlice
        | _ -> None

    let private tryNormalizeGraphSlugFilter (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if String.IsNullOrWhiteSpace(normalized) then
            None
        elif normalized |> Seq.forall (fun character -> Char.IsLetterOrDigit(character) || character = '-' || character = '_') then
            Some normalized
        else
            None

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
        | "compare-provider-exports" ->
            Some
                { Name = name
                  Summary = "Compare two raw ChatGPT, Claude, or Grok export zips before canonical import."
                  Usage =
                    [ sprintf "%s compare-provider-exports --provider <chatgpt|claude|grok> --base-zip <path> --current-zip <path>" cliInvocation
                      sprintf "%s compare-provider-exports --provider chatgpt --base-zip RawDataExports/older.zip --current-zip RawDataExports/newer.zip --limit 10" cliInvocation ]
                  Options =
                    [ "--provider <chatgpt|claude|grok>", "Required. Select the provider adapter used to parse both zips."
                      "--base-zip <path>", "Required. The older or reference raw export zip."
                      "--current-zip <path>", "Required. The newer or comparison raw export zip."
                      "--limit <n>", "Limit detailed rows per bucket. Defaults to 20." ]
                  Examples =
                    [ sprintf "%s compare-provider-exports --provider chatgpt --base-zip RawDataExports/chatgpt-older.zip --current-zip RawDataExports/chatgpt-newer.zip" cliInvocation
                      sprintf "%s compare-provider-exports --provider claude --base-zip RawDataExports/claude-export-a.zip --current-zip RawDataExports/claude-export-b.zip --limit 10" cliInvocation ]
                  Notes =
                    [ "This is a raw source-layer comparison over provider-native conversations and messages."
                      "It does not import, archive, or append anything to canonical history."
                      "Use it when you want to reason about export-window behavior before canonical import."
                      "Detailed guide: docs/how-to/compare-provider-exports.md" ] }
        | "compare-import-snapshots" ->
            Some
                { Name = name
                  Summary = "Compare two normalized import snapshots keyed by provider-native conversation identity."
                  Usage =
                    [ sprintf "%s compare-import-snapshots --base-import-id <uuid> --current-import-id <uuid>" cliInvocation
                      sprintf "%s compare-import-snapshots --base-import-id <uuid> --current-import-id <uuid> --limit 10" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--base-import-id <uuid>", "Required. The older or reference import snapshot."
                      "--current-import-id <uuid>", "Required. The newer or comparison import snapshot."
                      "--limit <n>", "Limit detailed rows per bucket. Defaults to 20." ]
                  Examples =
                    [ sprintf "%s compare-import-snapshots --base-import-id 019d21d6-5036-732d-973a-ef40df7f9003 --current-import-id 019d21df-d8b9-7a0a-a7c0-36686a412d40" cliInvocation
                      sprintf "%s compare-import-snapshots --event-store-root /tmp/nexus-event-store --base-import-id <uuid> --current-import-id <uuid> --limit 10" cliInvocation ]
                  Notes =
                    [ "This compares normalized per-import snapshots derived from parsed provider payloads before canonical dedupe."
                      "Use it when you want snapshot semantics at the normalized layer, rather than additive batch-local working-batch contributions."
                      "Absence from the current snapshot means absence from that import payload only; it does not imply canonical deletion."
                      "Older imports may predate snapshot materialization and can be backfilled with rebuild-import-snapshots."
                      "Detailed guide: docs/how-to/compare-import-snapshots.md" ] }
        | "report-provider-import-history" ->
            Some
                { Name = name
                  Summary = "Report one provider's normalized import snapshots in chronological order with adjacent deltas."
                  Usage =
                    [ sprintf "%s report-provider-import-history --provider <chatgpt|claude|grok|codex>" cliInvocation
                      sprintf "%s report-provider-import-history --provider chatgpt --limit 10" cliInvocation ]
                  Options =
                    [ "--provider <chatgpt|claude|grok|codex>", "Required. Select the provider whose normalized import history you want to inspect."
                      "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--objects-root <path>", sprintf "Override the objects root. Defaults to %s." defaultObjectsRoot
                      "--limit <n>", "Limit the report to the newest N history rows. Defaults to 20." ]
                  Examples =
                    [ sprintf "%s report-provider-import-history --provider chatgpt" cliInvocation
                      sprintf "%s report-provider-import-history --provider claude --event-store-root /tmp/nexus-event-store --objects-root /tmp/nexus-objects --limit 10" cliInvocation ]
                  Notes =
                    [ "This uses normalized import snapshots, not additive working-batch contributions."
                      "When the preserved raw artifact still exists, the report also prints its SHA-256."
                      "Each row shows one import snapshot plus the delta from the previous snapshot for that provider."
                      "If older imports are missing snapshot files, run rebuild-import-snapshots first."
                      "Detailed guide: docs/how-to/report-provider-import-history.md" ] }
        | "report-current-ingestion" ->
            Some
                { Name = name
                  Summary = "Report the latest known import state across providers from canonical import manifests."
                  Usage =
                    [ sprintf "%s report-current-ingestion" cliInvocation
                      sprintf "%s report-current-ingestion --event-store-root /tmp/nexus-event-store --objects-root /tmp/nexus-objects" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--objects-root <path>", sprintf "Override the objects root. Defaults to %s." defaultObjectsRoot ]
                  Examples =
                    [ sprintf "%s report-current-ingestion" cliInvocation
                      sprintf "%s report-current-ingestion --event-store-root /tmp/nexus-event-store --objects-root /tmp/nexus-objects" cliInvocation ]
                  Notes =
                    [ "This reads the newest import manifest for each provider and augments it with normalized snapshot totals when available."
                      "Providers like Codex currently report from import-manifest counts because they do not yet write normalized import snapshots."
                      "When the preserved root artifact still exists, the report also prints its SHA-256."
                      "Detailed guide: docs/how-to/report-current-ingestion.md" ] }
        | "report-logos-catalog" ->
            Some
                { Name = name
                  Summary = "Report the explicit allowlisted LOGOS source systems, intake channels, signal kinds, and handling-policy dimensions."
                  Usage = [ sprintf "%s report-logos-catalog" cliInvocation ]
                  Options = []
                  Examples = [ sprintf "%s report-logos-catalog" cliInvocation ]
                  Notes =
                    [ "This is the concrete LOGOS intake vocabulary currently recognized by the codebase."
                      "Use it before seeding intake notes so source-system, intake-channel, and signal-kind choices stay explicit."
                      "Detailed guide: docs/how-to/report-logos-catalog.md" ] }
        | "report-conversation-overlap-candidates" ->
            Some
                { Name = name
                  Summary = "Report conservative conversation-level overlap candidates between two providers' projection sets."
                  Usage =
                    [ sprintf "%s report-conversation-overlap-candidates --left-provider <chatgpt|claude|grok|codex> --right-provider <chatgpt|claude|grok|codex>" cliInvocation
                      sprintf "%s report-conversation-overlap-candidates --left-provider codex --right-provider chatgpt --limit 10" cliInvocation ]
                  Options =
                    [ "--left-provider <chatgpt|claude|grok|codex>", "Required. The first provider projection set to inspect."
                      "--right-provider <chatgpt|claude|grok|codex>", "Required. The second provider projection set to inspect."
                      "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--limit <n>", "Limit reported candidates. Defaults to 20." ]
                  Examples =
                    [ sprintf "%s report-conversation-overlap-candidates --left-provider codex --right-provider chatgpt" cliInvocation
                      sprintf "%s report-conversation-overlap-candidates --left-provider codex --right-provider claude --event-store-root /tmp/nexus-event-store --limit 10" cliInvocation ]
                  Notes =
                    [ "This is a heuristic candidate report only. It does not reconcile, merge, or delete anything."
                      "Candidates are based on explainable signals such as normalized title similarity, time overlap, and message-count closeness."
                      "Use it to spot likely cross-source overlap before any explicit reconciliation workflow exists."
                      "Detailed guide: docs/how-to/report-conversation-overlap-candidates.md" ] }
        | "rebuild-import-snapshots" ->
            Some
                { Name = name
                  Summary = "Rebuild normalized import snapshots for older provider-export imports from preserved raw artifacts."
                  Usage =
                    [ sprintf "%s rebuild-import-snapshots --import-id <uuid>" cliInvocation
                      sprintf "%s rebuild-import-snapshots --all" cliInvocation
                      sprintf "%s rebuild-import-snapshots --import-id <uuid> --force" cliInvocation ]
                  Options =
                    [ "--import-id <uuid>", "Rebuild one specific import snapshot."
                      "--all", "Rebuild snapshots across all import manifests in the event store."
                      "--force", "Overwrite existing normalized snapshot files instead of skipping them."
                      "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--objects-root <path>", sprintf "Override the objects root. Defaults to %s." defaultObjectsRoot ]
                  Examples =
                    [ sprintf "%s rebuild-import-snapshots --import-id 019d21d6-5036-732d-973a-ef40df7f9003" cliInvocation
                      sprintf "%s rebuild-import-snapshots --all --event-store-root /tmp/nexus-event-store --objects-root /tmp/nexus-objects" cliInvocation
                      sprintf "%s rebuild-import-snapshots --import-id <uuid> --force" cliInvocation ]
                  Notes =
                    [ "This rebuilds derived normalized snapshot artifacts only. It does not append canonical events."
                      "Backfilled snapshots are reparsed from preserved raw exports using the current provider-export parser rules."
                      "Use this when older imports predate snapshot materialization and compare-import-snapshots reports missing snapshot files."
                      "Detailed guide: docs/how-to/rebuild-import-snapshots.md" ] }
        | "import-provider-export" ->
            Some
                { Name = name
                  Summary = "Archive a ChatGPT, Claude, or Grok export zip, parse provider records, and append canonical observed history."
                  Usage =
                    [ sprintf "%s import-provider-export --provider <chatgpt|claude|grok> --zip <path>" cliInvocation
                      sprintf "%s import-provider-export --provider claude --zip RawDataExports/claude-export.zip --window full" cliInvocation ]
                  Options =
                    [ "--provider <chatgpt|claude|grok>", "Required. Select the provider adapter."
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
                      "--provider <chatgpt|claude|grok>", "Provider for provider-key lookup when not using --artifact-id."
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
                      sprintf "%s export-graphviz-dot --working-import-id <import-id>" cliInvocation
                      sprintf "%s export-graphviz-dot --working-import-id <import-id> --working-node-id <node-id>" cliInvocation
                      sprintf "%s export-graphviz-dot --working-import-id <import-id> --verification traceable" cliInvocation
                      sprintf "%s export-graphviz-dot --provider-conversation-id <provider-conversation-id>" cliInvocation
                      sprintf "%s export-graphviz-dot --import-id <import-id> --output /tmp/nexus-graph.dot" cliInvocation
                      sprintf "%s export-graphviz-dot --provider claude --output-root /tmp/nexus-graph-exports" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--objects-root <path>", sprintf "Override the objects root for traceable working-batch verification. Defaults to %s." defaultObjectsRoot
                      "--output <path>", "Optional DOT output path. Defaults to a filter-aware name under <event-store-root>/graph/exports."
                      "--output-root <path>", "Optional output directory root. NEXUS will place the default file name under this directory."
                      "--verification <none|traceable>", "Verification mode. Defaults to none. traceable is currently supported only with --working-import-id."
                      "--provider <chatgpt|claude|grok|codex>", "Only include assertions whose provenance references the selected provider."
                      "--conversation-id <uuid>", "Only include the selected canonical conversation and its immediate graph neighborhood."
                      "--provider-conversation-id <id>", "Only include assertions whose provenance references the selected provider-native conversation ID."
                      "--import-id <uuid>", "Only include assertions whose provenance import_id matches the selected import."
                      "--working-import-id <uuid>", "Export one graph working import batch directly, without reading graph/assertions/."
                      "--working-node-id <node-id>", "When used with --working-import-id, export only that node's immediate neighborhood scope from the working batch." ]
                  Examples =
                    [ sprintf "%s export-graphviz-dot" cliInvocation
                      sprintf "%s export-graphviz-dot --provider claude" cliInvocation
                      sprintf "%s export-graphviz-dot --conversation-id 019d174e-e960-7507-8aa6-06ee0064e499" cliInvocation
                      sprintf "%s export-graphviz-dot --working-import-id 019d174e-e953-7e8b-b506-5f1475399fc7" cliInvocation
                      sprintf "%s export-graphviz-dot --working-import-id 019d174e-e953-7e8b-b506-5f1475399fc7 --working-node-id 019d174e-e960-7507-8aa6-06ee0064e499" cliInvocation
                      sprintf "%s export-graphviz-dot --working-import-id 019d174e-e953-7e8b-b506-5f1475399fc7 --verification traceable" cliInvocation
                      sprintf "%s export-graphviz-dot --provider claude --output-root /tmp/nexus-graph-exports" cliInvocation
                      sprintf "%s export-graphviz-dot --provider codex --output /tmp/codex-graph.dot" cliInvocation ]
                  Notes =
                    [ "Run rebuild-graph-assertions first when the derived graph may be stale."
                      "Use --working-import-id when you want the graph working batch from a fresh import batch without a full durable-graph rebuild."
                      "Use --working-node-id together with --working-import-id when you want just one node's immediate neighborhood scope from the working batch."
                      "Traceable verification currently applies only to --working-import-id exports and checks the working batch back to canonical events and raw object refs before writing DOT output."
                      "Use either --output or --output-root, not both."
                      "Canonical conversation slices use the conversation_id from conversation projections and include the conversation plus its immediate graph neighborhood."
                      "Filters are applied from graph assertion provenance, which makes provider, conversation, and import slices practical without replaying canonical history."
                      "This is an external lens over derived graph assertions, useful for surfacing patterns outside current NEXUS views."
                      "Detailed guide: docs/how-to/export-graphviz-dot.md" ] }
        | "render-graphviz-dot" ->
            Some
                { Name = name
                  Summary = "Render a DOT file into SVG or PNG using an explicitly allowlisted Graphviz engine."
                  Usage =
                    [ sprintf "%s render-graphviz-dot --input <path-to-dot>" cliInvocation
                      sprintf "%s render-graphviz-dot --input /tmp/nexus-graph.dot --format svg --engine sfdp" cliInvocation
                      sprintf "%s render-graphviz-dot --input /tmp/nexus-graph.dot --output-root /tmp/nexus-rendered" cliInvocation ]
                  Options =
                    [ "--input <path>", "Required. Path to the source DOT file."
                      "--output <path>", "Optional rendered output path. Defaults to the input DOT path with the selected format extension."
                      "--output-root <path>", "Optional output directory root. NEXUS will place the default rendered file name under this directory."
                      "--engine <dot|sfdp>", "Graphviz engine. Defaults to dot."
                      "--format <svg|png>", "Rendered output format. Defaults to svg." ]
                  Examples =
                    [ sprintf "%s render-graphviz-dot --input NEXUS-EventStore/graph/exports/nexus-graph.dot" cliInvocation
                      sprintf "%s render-graphviz-dot --input NEXUS-EventStore/graph/exports/nexus-graph.dot --output-root /tmp/nexus-rendered" cliInvocation
                      sprintf "%s render-graphviz-dot --input NEXUS-EventStore/graph/working/exports/nexus-working-graph__import-019d....dot --engine sfdp --format png" cliInvocation ]
                  Notes =
                    [ "This command renders an existing DOT file. Use export-graphviz-dot first if you still need to generate the DOT source."
                      "Use either --output or --output-root, not both."
                      "Only explicitly allowlisted engines and formats are supported."
                      "Detailed guide: docs/how-to/render-graphviz-dot.md" ] }
        | "report-unresolved-artifacts" ->
            Some
                { Name = name
                  Summary = "Summarize artifact references whose payloads have not yet been captured."
                  Usage =
                    [ sprintf "%s report-unresolved-artifacts" cliInvocation
                      sprintf "%s report-unresolved-artifacts --provider claude --limit 10" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--provider <chatgpt|claude|grok>", "Limit the report to a single provider."
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
                  Summary = "Summarize graph working batches by import without rereading every assertion TOML file."
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
                      "The detail list joins each working-batch entry back to the canonical import manifest when present."
                      "Detailed guide: docs/how-to/report-working-graph-imports.md" ] }
        | "report-working-import-conversations" ->
            Some
                { Name = name
                  Summary = "Summarize the conversation nodes present in one import-local graph working batch."
                  Usage =
                    [ sprintf "%s report-working-import-conversations --import-id <uuid>" cliInvocation
                      sprintf "%s report-working-import-conversations --import-id <uuid> --limit 10" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--import-id <uuid>", "Required. Select the import-local graph working batch to summarize."
                      "--limit <n>", "Limit conversation rows. Defaults to 20." ]
                  Examples =
                    [ sprintf "%s report-working-import-conversations --import-id 019d174e-e953-7e8b-b506-5f1475399fc7" cliInvocation
                      sprintf "%s report-working-import-conversations --event-store-root /tmp/nexus-event-store --import-id <uuid> --limit 10" cliInvocation ]
                  Notes =
                    [ "This report reads the SQLite working index and presents the import batch in conversation terms rather than graph-node terms."
                      "It is useful for understanding what a fresh provider export contributed before you dive into individual neighborhoods."
                      "Detailed guide: docs/how-to/report-working-import-conversations.md" ] }
        | "compare-working-import-conversations" ->
            Some
                { Name = name
                  Summary = "Compare the conversation contributions present in two import-local graph working batches."
                  Usage =
                    [ sprintf "%s compare-working-import-conversations --base-import-id <uuid> --current-import-id <uuid>" cliInvocation
                      sprintf "%s compare-working-import-conversations --base-import-id <uuid> --current-import-id <uuid> --limit 10" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--base-import-id <uuid>", "Required. Select the older or reference import-local graph working batch."
                      "--current-import-id <uuid>", "Required. Select the newer or comparison import-local graph working batch."
                      "--limit <n>", "Limit detailed rows per bucket. Defaults to 20." ]
                  Examples =
                    [ sprintf "%s compare-working-import-conversations --base-import-id 019d174e-e953-7e8b-b506-5f1475399fc7 --current-import-id 019d1f70-b326-7d1e-a106-73dbd399f911" cliInvocation
                      sprintf "%s compare-working-import-conversations --event-store-root /tmp/nexus-event-store --base-import-id <uuid> --current-import-id <uuid> --limit 10" cliInvocation ]
                  Notes =
                    [ "This compares batch-local working-batch conversation contributions, not full provider snapshot truth."
                      "Use it to understand what changed between two import batches before deciding whether you need deeper canonical or raw-layer inspection."
                      "Detailed guide: docs/how-to/compare-working-import-conversations.md" ] }
        | "find-working-graph-nodes" ->
            Some
                { Name = name
                  Summary = "Find graph nodes in the SQLite working index by title/slug text plus explicit role and batch filters."
                  Usage =
                    [ sprintf "%s find-working-graph-nodes --match fixture" cliInvocation
                      sprintf "%s find-working-graph-nodes --semantic-role imprint --provider claude" cliInvocation
                      sprintf "%s find-working-graph-nodes --message-role assistant --import-id <uuid>" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--match <text>", "Optional title/slug substring match."
                      "--semantic-role <slug>", "Optional semantic-role slug filter."
                      "--message-role <slug>", "Optional message-role slug filter."
                      "--provider <chatgpt|claude|grok|codex>", "Optional provider filter."
                      "--import-id <uuid>", "Optional import-batch filter."
                      "--limit <n>", "Limit matches. Defaults to 20." ]
                  Examples =
                    [ sprintf "%s find-working-graph-nodes --match fixture" cliInvocation
                      sprintf "%s find-working-graph-nodes --semantic-role imprint --provider claude" cliInvocation
                      sprintf "%s find-working-graph-nodes --message-role assistant --import-id 019d174e-e953-7e8b-b506-5f1475399fc7" cliInvocation ]
                  Notes =
                    [ "At least one of --match, --semantic-role, or --message-role is required."
                      "Use this to discover candidate node IDs before running report-working-graph-neighborhood."
                      "Detailed guide: docs/how-to/find-working-graph-nodes.md" ] }
        | "report-working-graph-batch"
        | "report-working-graph-slice" ->
            Some
                { Name = "report-working-graph-batch"
                  Summary = "Summarize one import-batch graph working batch from the persisted SQLite index."
                  Usage =
                    [ sprintf "%s report-working-graph-batch --import-id <uuid>" cliInvocation
                      sprintf "%s report-working-graph-batch --import-id <uuid> --limit 10" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--import-id <uuid>", "Required. Select the graph working import batch to summarize."
                      "--limit <n>", "Limit predicate-count rows. Defaults to 10." ]
                  Examples =
                    [ sprintf "%s report-working-graph-batch --import-id 019d174e-e953-7e8b-b506-5f1475399fc7" cliInvocation
                      sprintf "%s report-working-graph-batch --event-store-root /tmp/nexus-event-store --import-id <uuid> --limit 5" cliInvocation ]
                  Notes =
                    [ "This report reads the SQLite working index under graph/working/index/."
                      "Working batches remain derived and rebuildable from canonical history."
                      "Legacy alias: report-working-graph-slice"
                      "Detailed guide: docs/how-to/report-working-graph-batch.md" ] }
        | "report-working-graph-neighborhood" ->
            Some
                { Name = name
                  Summary = "Show the local neighborhood of one node inside an import-local graph working batch."
                  Usage =
                    [ sprintf "%s report-working-graph-neighborhood --import-id <uuid> --node-id <node-id>" cliInvocation
                      sprintf "%s report-working-graph-neighborhood --import-id <uuid> --node-id <node-id> --limit 10" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--import-id <uuid>", "Required. Select the graph working import batch to inspect."
                      "--node-id <node-id>", "Required. Select the node to inspect."
                      "--limit <n>", "Limit outgoing, incoming, and literal rows. Defaults to 20." ]
                  Examples =
                    [ sprintf "%s report-working-graph-neighborhood --import-id 019d174e-e953-7e8b-b506-5f1475399fc7 --node-id 019d174e-e960-7507-8aa6-06ee0064e499" cliInvocation
                      sprintf "%s report-working-graph-neighborhood --event-store-root /tmp/nexus-event-store --import-id <uuid> --node-id <node-id> --limit 10" cliInvocation ]
                  Notes =
                    [ "This report reads the SQLite working index and stays scoped to one import-local working batch."
                      "Use find-working-graph-nodes first if you need help locating node IDs."
                      "Detailed guide: docs/how-to/report-working-graph-neighborhood.md" ] }
        | "verify-working-graph-batch"
        | "verify-working-graph-slice" ->
            Some
                { Name = "verify-working-graph-batch"
                  Summary = "Verify one graph working batch back to canonical events and preserved raw objects."
                  Usage =
                    [ sprintf "%s verify-working-graph-batch --import-id <uuid>" cliInvocation
                      sprintf "%s verify-working-graph-batch --import-id <uuid> --objects-root /tmp/nexus-objects" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot
                      "--objects-root <path>", sprintf "Override the objects root. Defaults to %s." defaultObjectsRoot
                      "--import-id <uuid>", "Required. Select the graph working import batch to verify." ]
                  Examples =
                    [ sprintf "%s verify-working-graph-batch --import-id 019d174e-e953-7e8b-b506-5f1475399fc7" cliInvocation
                      sprintf "%s verify-working-graph-batch --event-store-root /tmp/nexus-event-store --objects-root /tmp/nexus-objects --import-id <uuid>" cliInvocation ]
                  Notes =
                    [ "This command verifies the traceability chain from the working batch back to canonical events and raw object refs."
                      "It exits non-zero when canonical event links or raw object refs are missing."
                      "Legacy alias: verify-working-graph-slice"
                      "Detailed guide: docs/how-to/verify-working-graph-batch.md" ] }
        | "rebuild-working-graph-index" ->
            Some
                { Name = name
                  Summary = "Rebuild the SQLite graph working index from the existing graph working import batches."
                  Usage =
                    [ sprintf "%s rebuild-working-graph-index" cliInvocation
                      sprintf "%s rebuild-working-graph-index --event-store-root /tmp/nexus-event-store" cliInvocation ]
                  Options =
                    [ "--event-store-root <path>", sprintf "Override the event-store root. Defaults to %s." defaultEventStoreRoot ]
                  Examples =
                    [ sprintf "%s rebuild-working-graph-index" cliInvocation
                      sprintf "%s rebuild-working-graph-index --event-store-root /tmp/nexus-event-store" cliInvocation ]
                  Notes =
                    [ "This command rescans graph/working/imports/... and repopulates graph/working/index/graph-working.sqlite."
                      "Use it when the SQLite working index is missing, stale, or intentionally reset."
                      "Detailed guide: docs/how-to/rebuild-working-graph-index.md" ] }
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
        | "create-logos-intake-note" ->
            Some
                { Name = name
                  Summary = "Create a durable LOGOS intake seed note from explicit source, channel, signal, locator, and handling-policy metadata."
                  Usage =
                    [ sprintf "%s create-logos-intake-note --slug <slug> --title <title> --source-system <slug> --intake-channel <slug> --signal-kind <slug> --source-uri <uri>" cliInvocation
                      sprintf "%s create-logos-intake-note --slug support-thread-123 --title \"Support Thread 123\" --source-system forum --intake-channel forum-thread --signal-kind support-question --source-uri https://community.example.com/t/123" cliInvocation ]
                  Options =
                    [ "--slug <slug>", "Required. Explicit file-safe slug using lowercase ascii letters, digits, and '-'."
                      "--title <title>", "Required. Human-readable title for the intake note."
                      "--source-system <slug>", "Required. Explicit allowlisted source system. Run report-logos-catalog to inspect values."
                      "--intake-channel <slug>", "Required. Explicit allowlisted intake channel."
                      "--signal-kind <slug>", "Required. Explicit allowlisted signal kind."
                      "--sensitivity <slug>", "Optional explicit allowlisted sensitivity. Defaults to internal-restricted."
                      "--sharing-scope <slug>", "Optional explicit allowlisted sharing scope. Defaults to owner-only."
                      "--sanitization-status <slug>", "Optional explicit allowlisted sanitization status. Defaults to raw."
                      "--retention-class <slug>", "Optional explicit allowlisted retention class. Defaults to durable."
                      "--native-item-id <id>", "Optional explicit source locator. Repeatable with other locator options."
                      "--native-thread-id <id>", "Optional explicit source locator. Repeatable with other locator options."
                      "--native-message-id <id>", "Optional explicit source locator. Repeatable with other locator options."
                      "--source-uri <uri>", "Optional explicit source locator. Repeatable with other locator options."
                      "--captured-at <iso-8601>", "Optional capture timestamp."
                      "--summary <text>", "Optional seed summary."
                      "--tag <slug>", "Optional explicit tag slug. Repeatable."
                      "--docs-root <path>", sprintf "Override the docs root. Defaults to %s." defaultDocsRoot ]
                  Examples =
                    [ sprintf "%s create-logos-intake-note --slug support-thread-123 --title \"Support Thread 123\" --source-system forum --intake-channel forum-thread --signal-kind support-question --source-uri https://community.example.com/t/123" cliInvocation
                      sprintf "%s create-logos-intake-note --slug startup-feedback-2026-03 --title \"Startup Feedback\" --source-system app-feedback-surface --intake-channel app-feedback --signal-kind feedback --native-item-id fb-2026-03-25-001 --tag deployed-app" cliInvocation ]
                  Notes =
                    [ "This writes a Markdown seed note under docs/logos-intake/."
                      "Use it to represent early LOGOS intake before a full ingestion pipeline exists for that source type."
                      "New notes default to a restricted handling policy unless you explicitly choose other allowlisted values."
                      "At least one explicit locator is required."
                      "Detailed guide: docs/how-to/create-logos-intake-note.md" ] }
        | _ -> None

    let private availableCommands () =
        [ "write-sample-event-store"
          "compare-provider-exports"
          "compare-import-snapshots"
          "report-provider-import-history"
          "report-current-ingestion"
          "report-logos-catalog"
          "report-conversation-overlap-candidates"
          "rebuild-import-snapshots"
          "import-provider-export"
          "import-codex-sessions"
          "capture-artifact-payload"
          "rebuild-graph-assertions"
          "export-graphviz-dot"
          "render-graphviz-dot"
          "rebuild-artifact-projections"
          "report-unresolved-artifacts"
          "report-working-graph-imports"
          "report-working-import-conversations"
          "compare-working-import-conversations"
          "find-working-graph-nodes"
          "report-working-graph-batch"
          "report-working-graph-neighborhood"
          "verify-working-graph-batch"
          "rebuild-working-graph-index"
          "rebuild-conversation-projections"
          "create-logos-intake-note"
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

    let private parseCompareProviderExports (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "compare-provider-exports"))
        else
            let rec loop provider baseZipPath currentZipPath limit remaining =
                match remaining with
                | [] ->
                    match provider, baseZipPath, currentZipPath with
                    | Some providerKind, Some baseZipPathValue, Some currentZipPathValue ->
                        Ok (CompareProviderExports(providerKind, baseZipPathValue, currentZipPathValue, limit))
                    | None, _, _ ->
                        eprintfn "Missing required option for compare-provider-exports: --provider"
                        printCommandHelp "compare-provider-exports"
                        Error 1
                    | _, None, _ ->
                        eprintfn "Missing required option for compare-provider-exports: --base-zip"
                        printCommandHelp "compare-provider-exports"
                        Error 1
                    | _, _, None ->
                        eprintfn "Missing required option for compare-provider-exports: --current-zip"
                        printCommandHelp "compare-provider-exports"
                        Error 1
                | "--provider" :: value :: rest ->
                    match ProviderNaming.tryParse value with
                    | Some ChatGpt ->
                        loop (Some ChatGpt) baseZipPath currentZipPath limit rest
                    | Some Claude ->
                        loop (Some Claude) baseZipPath currentZipPath limit rest
                    | Some Grok ->
                        loop (Some Grok) baseZipPath currentZipPath limit rest
                    | Some Codex ->
                        eprintfn "Codex sessions are not compared through raw export zips."
                        printCommandHelp "compare-provider-exports"
                        Error 1
                    | Some (OtherProvider _) ->
                        eprintfn "Unsupported provider for compare-provider-exports: %s" value
                        printCommandHelp "compare-provider-exports"
                        Error 1
                    | None ->
                        eprintfn "Unsupported provider: %s" value
                        printCommandHelp "compare-provider-exports"
                        Error 1
                | "--base-zip" :: value :: rest ->
                    loop provider (Some value) currentZipPath limit rest
                | "--current-zip" :: value :: rest ->
                    loop provider baseZipPath (Some value) limit rest
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop provider baseZipPath currentZipPath parsedValue rest
                    | _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "compare-provider-exports"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for compare-provider-exports: %s" option
                    printCommandHelp "compare-provider-exports"
                    Error 1

            loop None None None 20 args

    let private parseProviderSlugOption commandName optionName value =
        match ProviderNaming.tryParse value with
        | Some ChatGpt -> Ok "chatgpt"
        | Some Claude -> Ok "claude"
        | Some Grok -> Ok "grok"
        | Some Codex -> Ok "codex"
        | Some (OtherProvider _) ->
            eprintfn "Unsupported provider for %s: %s" commandName value
            printCommandHelp commandName
            Error 1
        | None ->
            eprintfn "Unsupported value for %s: %s" optionName value
            printCommandHelp commandName
            Error 1

    let private parseReportConversationOverlapCandidates (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "report-conversation-overlap-candidates"))
        else
            let rec loop eventStoreRoot leftProvider rightProvider limit remaining =
                match remaining with
                | [] ->
                    match leftProvider, rightProvider with
                    | Some leftProviderValue, Some rightProviderValue when leftProviderValue = rightProviderValue ->
                        eprintfn "Left and right providers must be different for report-conversation-overlap-candidates."
                        printCommandHelp "report-conversation-overlap-candidates"
                        Error 1
                    | Some leftProviderValue, Some rightProviderValue ->
                        Ok (ReportConversationOverlapCandidates(eventStoreRoot, leftProviderValue, rightProviderValue, limit))
                    | None, _ ->
                        eprintfn "Missing required option for report-conversation-overlap-candidates: --left-provider"
                        printCommandHelp "report-conversation-overlap-candidates"
                        Error 1
                    | _, None ->
                        eprintfn "Missing required option for report-conversation-overlap-candidates: --right-provider"
                        printCommandHelp "report-conversation-overlap-candidates"
                        Error 1
                | "--event-store-root" :: value :: rest ->
                    loop value leftProvider rightProvider limit rest
                | "--left-provider" :: value :: rest ->
                    match parseProviderSlugOption "report-conversation-overlap-candidates" "--left-provider" value with
                    | Ok parsedValue -> loop eventStoreRoot (Some parsedValue) rightProvider limit rest
                    | Error code -> Error code
                | "--right-provider" :: value :: rest ->
                    match parseProviderSlugOption "report-conversation-overlap-candidates" "--right-provider" value with
                    | Ok parsedValue -> loop eventStoreRoot leftProvider (Some parsedValue) limit rest
                    | Error code -> Error code
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop eventStoreRoot leftProvider rightProvider parsedValue rest
                    | _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "report-conversation-overlap-candidates"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for report-conversation-overlap-candidates: %s" option
                    printCommandHelp "report-conversation-overlap-candidates"
                    Error 1

            loop defaultEventStoreRoot None None 20 args

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
            let rec loop eventStoreRoot objectsRootOverride outputPath outputRoot verification provider providerConversationId conversationId importId workingImportId workingNodeId remaining =
                match remaining with
                | [] ->
                    match outputPath, outputRoot with
                    | Some _, Some _ ->
                        eprintfn "Use either --output or --output-root for export-graphviz-dot, not both."
                        printCommandHelp "export-graphviz-dot"
                        Error 1
                    | _ ->
                        match workingImportId, provider, providerConversationId, conversationId, importId with
                        | Some _, Some _, _, _, _
                        | Some _, _, Some _, _, _
                        | Some _, _, _, Some _, _
                        | Some _, _, _, _, Some _ ->
                            eprintfn "Use either --working-import-id or the durable graph filter options, not both."
                            printCommandHelp "export-graphviz-dot"
                            Error 1
                        | Some workingImportIdValue, None, None, None, None ->
                            match verification, objectsRootOverride with
                            | NoVerification, Some _ ->
                                eprintfn "Use --objects-root only together with --verification traceable and --working-import-id."
                                printCommandHelp "export-graphviz-dot"
                                Error 1
                            | _ ->
                                let objectsRoot = objectsRootOverride |> Option.defaultValue defaultObjectsRoot
                                Ok
                                    (ExportGraphvizDot(
                                        eventStoreRoot,
                                        objectsRoot,
                                        outputPath,
                                        outputRoot,
                                        verification,
                                        None,
                                        None,
                                        None,
                                        None,
                                        Some workingImportIdValue,
                                        workingNodeId))
                        | None, _, _, _, _ ->
                            match verification, objectsRootOverride, workingNodeId with
                            | TraceableWorkingSlice, _, _ ->
                                eprintfn "Traceable verification for export-graphviz-dot is currently supported only with --working-import-id."
                                printCommandHelp "export-graphviz-dot"
                                Error 1
                            | NoVerification, Some _, _ ->
                                eprintfn "Use --objects-root only together with --verification traceable and --working-import-id."
                                printCommandHelp "export-graphviz-dot"
                                Error 1
                            | NoVerification, None, Some _ ->
                                eprintfn "Use --working-node-id only together with --working-import-id."
                                printCommandHelp "export-graphviz-dot"
                                Error 1
                            | NoVerification, None, None ->
                                Ok
                                    (ExportGraphvizDot(
                                        eventStoreRoot,
                                        defaultObjectsRoot,
                                        outputPath,
                                        outputRoot,
                                        verification,
                                        provider,
                                        providerConversationId,
                                        conversationId,
                                        importId,
                                        None,
                                        None))
                | "--event-store-root" :: value :: rest ->
                    loop value objectsRootOverride outputPath outputRoot verification provider providerConversationId conversationId importId workingImportId workingNodeId rest
                | "--objects-root" :: value :: rest ->
                    loop eventStoreRoot (Some value) outputPath outputRoot verification provider providerConversationId conversationId importId workingImportId workingNodeId rest
                | "--output" :: value :: rest ->
                    loop eventStoreRoot objectsRootOverride (Some value) outputRoot verification provider providerConversationId conversationId importId workingImportId workingNodeId rest
                | "--output-root" :: value :: rest ->
                    loop eventStoreRoot objectsRootOverride outputPath (Some value) verification provider providerConversationId conversationId importId workingImportId workingNodeId rest
                | "--verification" :: value :: rest ->
                    match parseGraphvizExportVerification value with
                    | Some verificationValue ->
                        loop eventStoreRoot objectsRootOverride outputPath outputRoot verificationValue provider providerConversationId conversationId importId workingImportId workingNodeId rest
                    | None ->
                        eprintfn "Unsupported verification mode: %s" value
                        printCommandHelp "export-graphviz-dot"
                        Error 1
                | "--provider" :: value :: rest ->
                    match ProviderNaming.tryParse value with
                    | Some providerKind ->
                        loop eventStoreRoot objectsRootOverride outputPath outputRoot verification (Some (ProviderNaming.slug providerKind)) providerConversationId conversationId importId workingImportId workingNodeId rest
                    | None ->
                        eprintfn "Unsupported provider: %s" value
                        printCommandHelp "export-graphviz-dot"
                        Error 1
                | "--conversation-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot objectsRootOverride outputPath outputRoot verification provider providerConversationId (Some value) importId workingImportId workingNodeId rest
                    | false, _ ->
                        eprintfn "Invalid canonical conversation ID: %s" value
                        printCommandHelp "export-graphviz-dot"
                        Error 1
                | "--provider-conversation-id" :: value :: rest ->
                    loop eventStoreRoot objectsRootOverride outputPath outputRoot verification provider (Some value) conversationId importId workingImportId workingNodeId rest
                | "--import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot objectsRootOverride outputPath outputRoot verification provider providerConversationId conversationId (Some value) workingImportId workingNodeId rest
                    | false, _ ->
                        eprintfn "Invalid import ID: %s" value
                        printCommandHelp "export-graphviz-dot"
                        Error 1
                | "--working-import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot objectsRootOverride outputPath outputRoot verification provider providerConversationId conversationId importId (Some value) workingNodeId rest
                    | false, _ ->
                        eprintfn "Invalid working import ID: %s" value
                        printCommandHelp "export-graphviz-dot"
                        Error 1
                | "--working-node-id" :: value :: rest ->
                    let normalized = value.Trim()

                    if String.IsNullOrWhiteSpace(normalized) then
                        eprintfn "Invalid working node ID: %s" value
                        printCommandHelp "export-graphviz-dot"
                        Error 1
                    else
                        loop eventStoreRoot objectsRootOverride outputPath outputRoot verification provider providerConversationId conversationId importId workingImportId (Some normalized) rest
                | option :: _ ->
                    eprintfn "Unknown option for export-graphviz-dot: %s" option
                    printCommandHelp "export-graphviz-dot"
                    Error 1

            loop defaultEventStoreRoot None None None NoVerification None None None None None None args

    let private parseGraphvizEngine (value: string) =
        match value.Trim().ToLowerInvariant() with
        | "dot" -> Some Dot
        | "sfdp" -> Some Sfdp
        | _ -> None

    let private parseGraphvizFormat (value: string) =
        match value.Trim().ToLowerInvariant() with
        | "svg" -> Some Svg
        | "png" -> Some Png
        | _ -> None

    let private parseRenderGraphvizDot (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "render-graphviz-dot"))
        else
            let rec loop inputPath outputPath outputRoot engine format remaining =
                match remaining with
                | [] ->
                    match outputPath, outputRoot, inputPath with
                    | Some _, Some _, _ ->
                        eprintfn "Use either --output or --output-root for render-graphviz-dot, not both."
                        printCommandHelp "render-graphviz-dot"
                        Error 1
                    | _, _, Some inputPathValue ->
                        Ok (RenderGraphvizDot(inputPathValue, outputPath, outputRoot, engine, format))
                    | _, _, None ->
                        eprintfn "Missing required option for render-graphviz-dot: --input"
                        printCommandHelp "render-graphviz-dot"
                        Error 1
                | "--input" :: value :: rest ->
                    loop (Some value) outputPath outputRoot engine format rest
                | "--output" :: value :: rest ->
                    loop inputPath (Some value) outputRoot engine format rest
                | "--output-root" :: value :: rest ->
                    loop inputPath outputPath (Some value) engine format rest
                | "--engine" :: value :: rest ->
                    match parseGraphvizEngine value with
                    | Some engineValue ->
                        loop inputPath outputPath outputRoot engineValue format rest
                    | None ->
                        eprintfn "Unsupported Graphviz engine: %s" value
                        printCommandHelp "render-graphviz-dot"
                        Error 1
                | "--format" :: value :: rest ->
                    match parseGraphvizFormat value with
                    | Some formatValue ->
                        loop inputPath outputPath outputRoot engine formatValue rest
                    | None ->
                        eprintfn "Unsupported Graphviz format: %s" value
                        printCommandHelp "render-graphviz-dot"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for render-graphviz-dot: %s" option
                    printCommandHelp "render-graphviz-dot"
                    Error 1

            loop None None None Dot Svg args

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

    let private parseReportWorkingImportConversations (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "report-working-import-conversations"))
        else
            let rec loop eventStoreRoot importId limit remaining =
                match remaining with
                | [] ->
                    match importId with
                    | Some importIdValue ->
                        Ok (ReportWorkingImportConversations(eventStoreRoot, importIdValue, limit))
                    | None ->
                        eprintfn "Missing required option: --import-id"
                        printCommandHelp "report-working-import-conversations"
                        Error 1
                | "--event-store-root" :: value :: rest ->
                    loop value importId limit rest
                | "--import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot (Some (ImportId.parse value)) limit rest
                    | false, _ ->
                        eprintfn "Invalid import ID: %s" value
                        printCommandHelp "report-working-import-conversations"
                        Error 1
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop eventStoreRoot importId parsedValue rest
                    | _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "report-working-import-conversations"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for report-working-import-conversations: %s" option
                    printCommandHelp "report-working-import-conversations"
                    Error 1

            loop defaultEventStoreRoot None 20 args

    let private parseCompareWorkingImportConversations (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "compare-working-import-conversations"))
        else
            let rec loop eventStoreRoot baseImportId currentImportId limit remaining =
                match remaining with
                | [] ->
                    match baseImportId, currentImportId with
                    | Some baseImportIdValue, Some currentImportIdValue ->
                        Ok (CompareWorkingImportConversations(eventStoreRoot, baseImportIdValue, currentImportIdValue, limit))
                    | None, _ ->
                        eprintfn "Missing required option: --base-import-id"
                        printCommandHelp "compare-working-import-conversations"
                        Error 1
                    | _, None ->
                        eprintfn "Missing required option: --current-import-id"
                        printCommandHelp "compare-working-import-conversations"
                        Error 1
                | "--event-store-root" :: value :: rest ->
                    loop value baseImportId currentImportId limit rest
                | "--base-import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot (Some (ImportId.parse value)) currentImportId limit rest
                    | false, _ ->
                        eprintfn "Invalid base import ID: %s" value
                        printCommandHelp "compare-working-import-conversations"
                        Error 1
                | "--current-import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot baseImportId (Some (ImportId.parse value)) limit rest
                    | false, _ ->
                        eprintfn "Invalid current import ID: %s" value
                        printCommandHelp "compare-working-import-conversations"
                        Error 1
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop eventStoreRoot baseImportId currentImportId parsedValue rest
                    | _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "compare-working-import-conversations"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for compare-working-import-conversations: %s" option
                    printCommandHelp "compare-working-import-conversations"
                    Error 1

            loop defaultEventStoreRoot None None 20 args

    let private parseCompareImportSnapshots (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "compare-import-snapshots"))
        else
            let rec loop eventStoreRoot baseImportId currentImportId limit remaining =
                match remaining with
                | [] ->
                    match baseImportId, currentImportId with
                    | Some baseImportIdValue, Some currentImportIdValue ->
                        Ok (CompareImportSnapshots(eventStoreRoot, baseImportIdValue, currentImportIdValue, limit))
                    | None, _ ->
                        eprintfn "Missing required option: --base-import-id"
                        printCommandHelp "compare-import-snapshots"
                        Error 1
                    | _, None ->
                        eprintfn "Missing required option: --current-import-id"
                        printCommandHelp "compare-import-snapshots"
                        Error 1
                | "--event-store-root" :: value :: rest ->
                    loop value baseImportId currentImportId limit rest
                | "--base-import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot (Some (ImportId.parse value)) currentImportId limit rest
                    | false, _ ->
                        eprintfn "Invalid base import ID: %s" value
                        printCommandHelp "compare-import-snapshots"
                        Error 1
                | "--current-import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot baseImportId (Some (ImportId.parse value)) limit rest
                    | false, _ ->
                        eprintfn "Invalid current import ID: %s" value
                        printCommandHelp "compare-import-snapshots"
                        Error 1
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop eventStoreRoot baseImportId currentImportId parsedValue rest
                    | _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "compare-import-snapshots"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for compare-import-snapshots: %s" option
                    printCommandHelp "compare-import-snapshots"
                    Error 1

            loop defaultEventStoreRoot None None 20 args

    let private parseReportProviderImportHistory (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "report-provider-import-history"))
        else
            let rec loop eventStoreRoot objectsRoot provider limit remaining =
                match remaining with
                | [] ->
                    match provider with
                    | Some providerValue -> Ok (ReportProviderImportHistory(eventStoreRoot, objectsRoot, providerValue, limit))
                    | None ->
                        eprintfn "Missing required option: --provider"
                        printCommandHelp "report-provider-import-history"
                        Error 1
                | "--event-store-root" :: value :: rest ->
                    loop value objectsRoot provider limit rest
                | "--objects-root" :: value :: rest ->
                    loop eventStoreRoot value provider limit rest
                | "--provider" :: value :: rest ->
                    match ProviderNaming.tryParse value with
                    | Some providerKind ->
                        loop eventStoreRoot objectsRoot (Some (ProviderNaming.slug providerKind)) limit rest
                    | None ->
                        eprintfn "Unsupported provider: %s" value
                        printCommandHelp "report-provider-import-history"
                        Error 1
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop eventStoreRoot objectsRoot provider parsedValue rest
                    | _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "report-provider-import-history"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for report-provider-import-history: %s" option
                    printCommandHelp "report-provider-import-history"
                    Error 1

            loop defaultEventStoreRoot defaultObjectsRoot None 20 args

    let private parseReportCurrentIngestion (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "report-current-ingestion"))
        else
            let rec loop eventStoreRoot objectsRoot remaining =
                match remaining with
                | [] -> Ok (ReportCurrentIngestion(eventStoreRoot, objectsRoot))
                | "--event-store-root" :: value :: rest ->
                    loop value objectsRoot rest
                | "--objects-root" :: value :: rest ->
                    loop eventStoreRoot value rest
                | option :: _ ->
                    eprintfn "Unknown option for report-current-ingestion: %s" option
                    printCommandHelp "report-current-ingestion"
                    Error 1

            loop defaultEventStoreRoot defaultObjectsRoot args

    let private parseReportLogosCatalog (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "report-logos-catalog"))
        else
            match args with
            | [] -> Ok ReportLogosCatalog
            | option :: _ ->
                eprintfn "Unknown option for report-logos-catalog: %s" option
                printCommandHelp "report-logos-catalog"
                Error 1

    let private parseRebuildImportSnapshots (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "rebuild-import-snapshots"))
        else
            let rec loop eventStoreRoot objectsRoot scope force remaining =
                match remaining with
                | [] ->
                    match scope with
                    | Some scopeValue ->
                        Ok (RebuildImportSnapshots(eventStoreRoot, objectsRoot, scopeValue, force))
                    | None ->
                        eprintfn "Specify either --import-id <uuid> or --all."
                        printCommandHelp "rebuild-import-snapshots"
                        Error 1
                | "--event-store-root" :: value :: rest ->
                    loop value objectsRoot scope force rest
                | "--objects-root" :: value :: rest ->
                    loop eventStoreRoot value scope force rest
                | "--import-id" :: value :: rest ->
                    match Guid.TryParse(value), scope with
                    | (true, _), None ->
                        loop eventStoreRoot objectsRoot (Some (SpecificImport (ImportId.parse value))) force rest
                    | (true, _), Some AllImports ->
                        eprintfn "Do not combine --import-id with --all."
                        printCommandHelp "rebuild-import-snapshots"
                        Error 1
                    | (true, _), Some (SpecificImport _) ->
                        eprintfn "Only one --import-id value is allowed."
                        printCommandHelp "rebuild-import-snapshots"
                        Error 1
                    | (false, _), _ ->
                        eprintfn "Invalid import ID: %s" value
                        printCommandHelp "rebuild-import-snapshots"
                        Error 1
                | "--all" :: rest ->
                    match scope with
                    | None ->
                        loop eventStoreRoot objectsRoot (Some AllImports) force rest
                    | Some _ ->
                        eprintfn "Do not combine --all with --import-id."
                        printCommandHelp "rebuild-import-snapshots"
                        Error 1
                | "--force" :: rest ->
                    loop eventStoreRoot objectsRoot scope true rest
                | option :: _ ->
                    eprintfn "Unknown option for rebuild-import-snapshots: %s" option
                    printCommandHelp "rebuild-import-snapshots"
                    Error 1

            loop defaultEventStoreRoot defaultObjectsRoot None false args

    let private parseFindWorkingGraphNodes (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "find-working-graph-nodes"))
        else
            let rec loop eventStoreRoot importId provider matchText semanticRole messageRole limit remaining =
                match remaining with
                | [] ->
                    match matchText, semanticRole, messageRole with
                    | None, None, None ->
                        eprintfn "At least one of --match, --semantic-role, or --message-role is required."
                        printCommandHelp "find-working-graph-nodes"
                        Error 1
                    | _ ->
                        Ok (FindWorkingGraphNodes(eventStoreRoot, importId, provider, matchText, semanticRole, messageRole, limit))
                | "--event-store-root" :: value :: rest ->
                    loop value importId provider matchText semanticRole messageRole limit rest
                | "--import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot (Some (ImportId.parse value)) provider matchText semanticRole messageRole limit rest
                    | false, _ ->
                        eprintfn "Invalid import ID: %s" value
                        printCommandHelp "find-working-graph-nodes"
                        Error 1
                | "--provider" :: value :: rest ->
                    match ProviderNaming.tryParse value with
                    | Some providerKind ->
                        loop eventStoreRoot importId (Some (ProviderNaming.slug providerKind)) matchText semanticRole messageRole limit rest
                    | None ->
                        eprintfn "Unsupported provider: %s" value
                        printCommandHelp "find-working-graph-nodes"
                        Error 1
                | "--match" :: value :: rest ->
                    let normalized = value.Trim()

                    if String.IsNullOrWhiteSpace(normalized) then
                        eprintfn "Invalid match text: %s" value
                        printCommandHelp "find-working-graph-nodes"
                        Error 1
                    else
                        loop eventStoreRoot importId provider (Some normalized) semanticRole messageRole limit rest
                | "--semantic-role" :: value :: rest ->
                    match tryNormalizeGraphSlugFilter value with
                    | Some normalized ->
                        loop eventStoreRoot importId provider matchText (Some normalized) messageRole limit rest
                    | None ->
                        eprintfn "Invalid semantic-role filter: %s" value
                        printCommandHelp "find-working-graph-nodes"
                        Error 1
                | "--message-role" :: value :: rest ->
                    match tryNormalizeGraphSlugFilter value with
                    | Some normalized ->
                        loop eventStoreRoot importId provider matchText semanticRole (Some normalized) limit rest
                    | None ->
                        eprintfn "Invalid message-role filter: %s" value
                        printCommandHelp "find-working-graph-nodes"
                        Error 1
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop eventStoreRoot importId provider matchText semanticRole messageRole parsedValue rest
                    | _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "find-working-graph-nodes"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for find-working-graph-nodes: %s" option
                    printCommandHelp "find-working-graph-nodes"
                    Error 1

            loop defaultEventStoreRoot None None None None None 20 args

    let private parseReportWorkingGraphSlice (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "report-working-graph-batch"))
        else
            let rec loop eventStoreRoot importId limit remaining =
                match remaining with
                | [] ->
                    match importId with
                    | Some importIdValue -> Ok (ReportWorkingGraphSlice(eventStoreRoot, importIdValue, limit))
                    | None ->
                        eprintfn "Missing required option: --import-id"
                        printCommandHelp "report-working-graph-batch"
                        Error 1
                | "--event-store-root" :: value :: rest ->
                    loop value importId limit rest
                | "--import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot (Some (ImportId.parse value)) limit rest
                    | false, _ ->
                        eprintfn "Invalid import ID: %s" value
                        printCommandHelp "report-working-graph-batch"
                        Error 1
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop eventStoreRoot importId parsedValue rest
                    | true, _
                    | false, _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "report-working-graph-batch"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for report-working-graph-batch: %s" option
                    printCommandHelp "report-working-graph-batch"
                    Error 1

            loop defaultEventStoreRoot None 10 args

    let private parseReportWorkingGraphNeighborhood (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "report-working-graph-neighborhood"))
        else
            let rec loop eventStoreRoot importId nodeId limit remaining =
                match remaining with
                | [] ->
                    match importId, nodeId with
                    | Some importIdValue, Some nodeIdValue ->
                        Ok (ReportWorkingGraphNeighborhood(eventStoreRoot, importIdValue, nodeIdValue, limit))
                    | None, _ ->
                        eprintfn "Missing required option: --import-id"
                        printCommandHelp "report-working-graph-neighborhood"
                        Error 1
                    | _, None ->
                        eprintfn "Missing required option: --node-id"
                        printCommandHelp "report-working-graph-neighborhood"
                        Error 1
                | "--event-store-root" :: value :: rest ->
                    loop value importId nodeId limit rest
                | "--import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot (Some (ImportId.parse value)) nodeId limit rest
                    | false, _ ->
                        eprintfn "Invalid import ID: %s" value
                        printCommandHelp "report-working-graph-neighborhood"
                        Error 1
                | "--node-id" :: value :: rest ->
                    let normalized = value.Trim()

                    if String.IsNullOrWhiteSpace(normalized) then
                        eprintfn "Invalid node ID: %s" value
                        printCommandHelp "report-working-graph-neighborhood"
                        Error 1
                    else
                        loop eventStoreRoot importId (Some normalized) limit rest
                | "--limit" :: value :: rest ->
                    match Int32.TryParse(value) with
                    | true, parsedValue when parsedValue > 0 ->
                        loop eventStoreRoot importId nodeId parsedValue rest
                    | _ ->
                        eprintfn "Invalid limit: %s" value
                        printCommandHelp "report-working-graph-neighborhood"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for report-working-graph-neighborhood: %s" option
                    printCommandHelp "report-working-graph-neighborhood"
                    Error 1

            loop defaultEventStoreRoot None None 20 args

    let private parseVerifyWorkingGraphSlice (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "verify-working-graph-batch"))
        else
            let rec loop eventStoreRoot objectsRoot importId remaining =
                match remaining with
                | [] ->
                    match importId with
                    | Some importIdValue -> Ok (VerifyWorkingGraphSlice(eventStoreRoot, objectsRoot, importIdValue))
                    | None ->
                        eprintfn "Missing required option: --import-id"
                        printCommandHelp "verify-working-graph-batch"
                        Error 1
                | "--event-store-root" :: value :: rest ->
                    loop value objectsRoot importId rest
                | "--objects-root" :: value :: rest ->
                    loop eventStoreRoot value importId rest
                | "--import-id" :: value :: rest ->
                    match Guid.TryParse(value) with
                    | true, _ ->
                        loop eventStoreRoot objectsRoot (Some (ImportId.parse value)) rest
                    | false, _ ->
                        eprintfn "Invalid import ID: %s" value
                        printCommandHelp "verify-working-graph-batch"
                        Error 1
                | option :: _ ->
                    eprintfn "Unknown option for verify-working-graph-batch: %s" option
                    printCommandHelp "verify-working-graph-batch"
                    Error 1

            loop defaultEventStoreRoot defaultObjectsRoot None args

    let private parseRebuildWorkingGraphIndex (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "rebuild-working-graph-index"))
        else
            let rec loop eventStoreRoot remaining =
                match remaining with
                | [] -> Ok (RebuildWorkingGraphIndex eventStoreRoot)
                | "--event-store-root" :: value :: rest ->
                    loop value rest
                | option :: _ ->
                    eprintfn "Unknown option for rebuild-working-graph-index: %s" option
                    printCommandHelp "rebuild-working-graph-index"
                    Error 1

            loop defaultEventStoreRoot args

    let private parseCreateLogosIntakeNote (args: string list) =
        if containsHelpSwitch args then
            Ok (ShowHelp (Some "create-logos-intake-note"))
        else
            let defaultPolicy = LogosHandlingPolicy.restrictedDefault

            let rec loop
                docsRoot
                slug
                title
                sourceSystem
                intakeChannel
                signalKind
                sensitivity
                sharingScope
                sanitizationStatus
                retentionClass
                locators
                capturedAt
                summary
                tags
                remaining
                =
                match remaining with
                | [] ->
                    match slug, title, sourceSystem, intakeChannel, signalKind with
                    | None, _, _, _, _ ->
                        eprintfn "Missing required option for create-logos-intake-note: --slug"
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                    | _, None, _, _, _ ->
                        eprintfn "Missing required option for create-logos-intake-note: --title"
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                    | _, _, None, _, _ ->
                        eprintfn "Missing required option for create-logos-intake-note: --source-system"
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                    | _, _, _, None, _ ->
                        eprintfn "Missing required option for create-logos-intake-note: --intake-channel"
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                    | _, _, _, _, None ->
                        eprintfn "Missing required option for create-logos-intake-note: --signal-kind"
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                    | Some slugValue, Some titleValue, Some sourceSystemValue, Some intakeChannelValue, Some signalKindValue ->
                        match locators with
                        | [] ->
                            eprintfn "At least one explicit locator is required."
                            printCommandHelp "create-logos-intake-note"
                            Error 1
                        | _ ->
                            let policy =
                                LogosHandlingPolicy.create
                                    (defaultArg sensitivity defaultPolicy.SensitivityId)
                                    (defaultArg sharingScope defaultPolicy.SharingScopeId)
                                    (defaultArg sanitizationStatus defaultPolicy.SanitizationStatusId)
                                    (defaultArg retentionClass defaultPolicy.RetentionClassId)

                            Ok
                                (CreateLogosIntakeNote
                                    { DocsRoot = docsRoot
                                      Slug = slugValue
                                      Title = titleValue
                                      SourceSystemId = sourceSystemValue
                                      IntakeChannelId = intakeChannelValue
                                      SignalKindId = signalKindValue
                                      Policy = policy
                                      Locators = List.rev locators
                                      CapturedAt = capturedAt
                                      Summary = summary
                                      Tags = List.rev tags })
                | "--docs-root" :: value :: rest ->
                    loop
                        value
                        slug
                        title
                        sourceSystem
                        intakeChannel
                        signalKind
                        sensitivity
                        sharingScope
                        sanitizationStatus
                        retentionClass
                        locators
                        capturedAt
                        summary
                        tags
                        rest
                | "--slug" :: value :: rest ->
                    let normalized = value.Trim()

                    if String.IsNullOrWhiteSpace(normalized) then
                        eprintfn "Invalid slug: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                    else
                        loop
                            docsRoot
                            (Some normalized)
                            title
                            sourceSystem
                            intakeChannel
                            signalKind
                            sensitivity
                            sharingScope
                            sanitizationStatus
                            retentionClass
                            locators
                            capturedAt
                            summary
                            tags
                            rest
                | "--title" :: value :: rest ->
                    let normalized = value.Trim()

                    if String.IsNullOrWhiteSpace(normalized) then
                        eprintfn "Invalid title: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                    else
                        loop
                            docsRoot
                            slug
                            (Some normalized)
                            sourceSystem
                            intakeChannel
                            signalKind
                            sensitivity
                            sharingScope
                            sanitizationStatus
                            retentionClass
                            locators
                            capturedAt
                            summary
                            tags
                            rest
                | "--source-system" :: value :: rest ->
                    match KnownSourceSystems.tryFind value with
                    | Some identifier ->
                        loop
                            docsRoot
                            slug
                            title
                            (Some identifier)
                            intakeChannel
                            signalKind
                            sensitivity
                            sharingScope
                            sanitizationStatus
                            retentionClass
                            locators
                            capturedAt
                            summary
                            tags
                            rest
                    | None ->
                        eprintfn "Unsupported LOGOS source system: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                | "--intake-channel" :: value :: rest ->
                    match CoreIntakeChannels.tryFind value with
                    | Some identifier ->
                        loop
                            docsRoot
                            slug
                            title
                            sourceSystem
                            (Some identifier)
                            signalKind
                            sensitivity
                            sharingScope
                            sanitizationStatus
                            retentionClass
                            locators
                            capturedAt
                            summary
                            tags
                            rest
                    | None ->
                        eprintfn "Unsupported LOGOS intake channel: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                | "--signal-kind" :: value :: rest ->
                    match CoreSignalKinds.tryFind value with
                    | Some identifier ->
                        loop
                            docsRoot
                            slug
                            title
                            sourceSystem
                            intakeChannel
                            (Some identifier)
                            sensitivity
                            sharingScope
                            sanitizationStatus
                            retentionClass
                            locators
                            capturedAt
                            summary
                            tags
                            rest
                    | None ->
                        eprintfn "Unsupported LOGOS signal kind: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                | "--sensitivity" :: value :: rest ->
                    match KnownSensitivities.tryFind value with
                    | Some identifier ->
                        loop
                            docsRoot
                            slug
                            title
                            sourceSystem
                            intakeChannel
                            signalKind
                            (Some identifier)
                            sharingScope
                            sanitizationStatus
                            retentionClass
                            locators
                            capturedAt
                            summary
                            tags
                            rest
                    | None ->
                        eprintfn "Unsupported LOGOS sensitivity: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                | "--sharing-scope" :: value :: rest ->
                    match KnownSharingScopes.tryFind value with
                    | Some identifier ->
                        loop
                            docsRoot
                            slug
                            title
                            sourceSystem
                            intakeChannel
                            signalKind
                            sensitivity
                            (Some identifier)
                            sanitizationStatus
                            retentionClass
                            locators
                            capturedAt
                            summary
                            tags
                            rest
                    | None ->
                        eprintfn "Unsupported LOGOS sharing scope: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                | "--sanitization-status" :: value :: rest ->
                    match KnownSanitizationStatuses.tryFind value with
                    | Some identifier ->
                        loop
                            docsRoot
                            slug
                            title
                            sourceSystem
                            intakeChannel
                            signalKind
                            sensitivity
                            sharingScope
                            (Some identifier)
                            retentionClass
                            locators
                            capturedAt
                            summary
                            tags
                            rest
                    | None ->
                        eprintfn "Unsupported LOGOS sanitization status: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                | "--retention-class" :: value :: rest ->
                    match KnownRetentionClasses.tryFind value with
                    | Some identifier ->
                        loop
                            docsRoot
                            slug
                            title
                            sourceSystem
                            intakeChannel
                            signalKind
                            sensitivity
                            sharingScope
                            sanitizationStatus
                            (Some identifier)
                            locators
                            capturedAt
                            summary
                            tags
                            rest
                    | None ->
                        eprintfn "Unsupported LOGOS retention class: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                | "--native-item-id" :: value :: rest ->
                    loop
                        docsRoot
                        slug
                        title
                        sourceSystem
                        intakeChannel
                        signalKind
                        sensitivity
                        sharingScope
                        sanitizationStatus
                        retentionClass
                        (LogosLocator.nativeItemId value :: locators)
                        capturedAt
                        summary
                        tags
                        rest
                | "--native-thread-id" :: value :: rest ->
                    loop
                        docsRoot
                        slug
                        title
                        sourceSystem
                        intakeChannel
                        signalKind
                        sensitivity
                        sharingScope
                        sanitizationStatus
                        retentionClass
                        (LogosLocator.nativeThreadId value :: locators)
                        capturedAt
                        summary
                        tags
                        rest
                | "--native-message-id" :: value :: rest ->
                    loop
                        docsRoot
                        slug
                        title
                        sourceSystem
                        intakeChannel
                        signalKind
                        sensitivity
                        sharingScope
                        sanitizationStatus
                        retentionClass
                        (LogosLocator.nativeMessageId value :: locators)
                        capturedAt
                        summary
                        tags
                        rest
                | "--source-uri" :: value :: rest ->
                    loop
                        docsRoot
                        slug
                        title
                        sourceSystem
                        intakeChannel
                        signalKind
                        sensitivity
                        sharingScope
                        sanitizationStatus
                        retentionClass
                        (LogosLocator.sourceUri value :: locators)
                        capturedAt
                        summary
                        tags
                        rest
                | "--captured-at" :: value :: rest ->
                    match DateTimeOffset.TryParse(value) with
                    | true, parsedValue ->
                        loop
                            docsRoot
                            slug
                            title
                            sourceSystem
                            intakeChannel
                            signalKind
                            sensitivity
                            sharingScope
                            sanitizationStatus
                            retentionClass
                            locators
                            (Some parsedValue)
                            summary
                            tags
                            rest
                    | false, _ ->
                        eprintfn "Invalid captured-at timestamp: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                | "--summary" :: value :: rest ->
                    loop
                        docsRoot
                        slug
                        title
                        sourceSystem
                        intakeChannel
                        signalKind
                        sensitivity
                        sharingScope
                        sanitizationStatus
                        retentionClass
                        locators
                        capturedAt
                        (Some value)
                        tags
                        rest
                | "--tag" :: value :: rest ->
                    let normalized = value.Trim()

                    if String.IsNullOrWhiteSpace(normalized) then
                        eprintfn "Invalid tag: %s" value
                        printCommandHelp "create-logos-intake-note"
                        Error 1
                    else
                        loop
                            docsRoot
                            slug
                            title
                            sourceSystem
                            intakeChannel
                            signalKind
                            sensitivity
                            sharingScope
                            sanitizationStatus
                            retentionClass
                            locators
                            capturedAt
                            summary
                            (normalized :: tags)
                            rest
                | option :: _ ->
                    eprintfn "Unknown option for create-logos-intake-note: %s" option
                    printCommandHelp "create-logos-intake-note"
                    Error 1

            loop defaultDocsRoot None None None None None None None None None [] None None [] args

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
        | "compare-provider-exports" :: rest ->
            parseCompareProviderExports rest
        | "compare-import-snapshots" :: rest ->
            parseCompareImportSnapshots rest
        | "report-provider-import-history" :: rest ->
            parseReportProviderImportHistory rest
        | "report-current-ingestion" :: rest ->
            parseReportCurrentIngestion rest
        | "report-logos-catalog" :: rest ->
            parseReportLogosCatalog rest
        | "report-conversation-overlap-candidates" :: rest ->
            parseReportConversationOverlapCandidates rest
        | "rebuild-import-snapshots" :: rest ->
            parseRebuildImportSnapshots rest
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
        | "render-graphviz-dot" :: rest ->
            parseRenderGraphvizDot rest
        | "rebuild-artifact-projections" :: rest ->
            parseRebuildArtifactProjections rest
        | "report-unresolved-artifacts" :: rest ->
            parseReportUnresolvedArtifacts rest
        | "report-working-graph-imports" :: rest ->
            parseReportWorkingGraphImports rest
        | "report-working-import-conversations" :: rest ->
            parseReportWorkingImportConversations rest
        | "compare-working-import-conversations" :: rest ->
            parseCompareWorkingImportConversations rest
        | "find-working-graph-nodes" :: rest ->
            parseFindWorkingGraphNodes rest
        | "report-working-graph-batch" :: rest
        | "report-working-graph-slice" :: rest ->
            parseReportWorkingGraphSlice rest
        | "report-working-graph-neighborhood" :: rest ->
            parseReportWorkingGraphNeighborhood rest
        | "verify-working-graph-batch" :: rest
        | "verify-working-graph-slice" :: rest ->
            parseVerifyWorkingGraphSlice rest
        | "rebuild-working-graph-index" :: rest ->
            parseRebuildWorkingGraphIndex rest
        | "rebuild-conversation-projections" :: rest ->
            parseRebuildConversationProjections rest
        | "create-logos-intake-note" :: rest ->
            parseCreateLogosIntakeNote rest
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

    let private printWorkingGraphArtifacts manifestPath catalogPath indexPath assertionCount =
        match manifestPath with
        | Some value -> printfn "  Working graph manifest: %s" value
        | None -> ()

        match catalogPath with
        | Some value -> printfn "  Working graph catalog: %s" value
        | None -> ()

        match indexPath with
        | Some value -> printfn "  Working graph index: %s" value
        | None -> ()

        match assertionCount with
        | Some value -> printfn "  Working graph assertions written: %d" value
        | None -> ()

    let private printImportSnapshotArtifacts manifestPath conversationsPath =
        match manifestPath with
        | Some value -> printfn "  Import snapshot manifest: %s" value
        | None -> ()

        match conversationsPath with
        | Some value -> printfn "  Import snapshot conversations: %s" value
        | None -> ()

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

        let counts : ImportCounts =
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

    let private compareProviderExports provider baseZipPath currentZipPath limit =
        let report = ExportComparison.compare provider baseZipPath currentZipPath limit

        printfn "Provider export comparison."
        printfn "  Provider: %s" (ProviderNaming.slug report.Provider)
        printfn "  Base zip: %s" report.BaseZipPath
        printfn "  Current zip: %s" report.CurrentZipPath
        printfn "  Base SHA-256: %s" report.BaseZipSha256
        printfn "  Current SHA-256: %s" report.CurrentZipSha256
        printfn "  Zip artifacts identical: %b" report.ZipArtifactsIdentical
        printfn "  Base conversations: %d" report.BaseConversationCount
        printfn "  Current conversations: %d" report.CurrentConversationCount
        printfn "  Base messages: %d" report.BaseMessageCount
        printfn "  Current messages: %d" report.CurrentMessageCount
        printfn "  Base artifact refs: %d" report.BaseArtifactReferenceCount
        printfn "  Current artifact refs: %d" report.CurrentArtifactReferenceCount
        printfn "  Added conversations: %d" report.AddedConversationCount
        printfn "  Removed conversations: %d" report.RemovedConversationCount
        printfn "  Changed conversations: %d" report.ChangedConversationCount
        printfn "  Unchanged conversations: %d" report.UnchangedConversationCount

        if not report.AddedConversations.IsEmpty then
            printfn "  Added:"

            report.AddedConversations
            |> List.iter (fun item ->
                let label = item.Title |> Option.defaultValue item.ProviderConversationId
                printfn "    %s" item.ProviderConversationId
                printfn "      label=%s" label
                printfn "      messages=%d artifacts=%d" item.MessageCount item.ArtifactReferenceCount)

        if not report.RemovedConversations.IsEmpty then
            printfn "  Removed:"

            report.RemovedConversations
            |> List.iter (fun item ->
                let label = item.Title |> Option.defaultValue item.ProviderConversationId
                printfn "    %s" item.ProviderConversationId
                printfn "      label=%s" label
                printfn "      messages=%d artifacts=%d" item.MessageCount item.ArtifactReferenceCount)

        if not report.ChangedConversations.IsEmpty then
            printfn "  Changed:"

            report.ChangedConversations
            |> List.iter (fun item ->
                let label =
                    item.CurrentTitle
                    |> Option.orElse item.BaseTitle
                    |> Option.defaultValue item.ProviderConversationId

                printfn "    %s" item.ProviderConversationId
                printfn "      label=%s" label
                printfn "      messages=%d -> %d (added=%d removed=%d)" item.BaseMessageCount item.CurrentMessageCount item.AddedMessageCount item.RemovedMessageCount
                printfn
                    "      artifacts=%d -> %d (added=%d removed=%d)"
                    item.BaseArtifactReferenceCount
                    item.CurrentArtifactReferenceCount
                    item.AddedArtifactReferenceCount
                    item.RemovedArtifactReferenceCount)

        0

    let private compareImportSnapshots eventStoreRoot baseImportId currentImportId limit =
        match ImportSnapshots.tryBuildComparisonReport eventStoreRoot baseImportId currentImportId limit with
        | Some report ->
            printfn "Normalized import snapshot comparison."
            printfn "  Event store root: %s" eventStoreRoot
            printfn "  Base import ID: %s" (ImportId.format report.BaseImportId)
            printfn "  Current import ID: %s" (ImportId.format report.CurrentImportId)
            printfn "  Base provider: %s" report.BaseProvider
            printfn "  Current provider: %s" report.CurrentProvider

            match report.BaseWindow with
            | Some value -> printfn "  Base window: %s" value
            | None -> ()

            match report.CurrentWindow with
            | Some value -> printfn "  Current window: %s" value
            | None -> ()

            printfn "  Base imported at: %s" (report.BaseImportedAt.ToUniversalTime().ToString("O"))
            printfn "  Current imported at: %s" (report.CurrentImportedAt.ToUniversalTime().ToString("O"))
            printfn "  Added conversations: %d" report.AddedConversationCount
            printfn "  Removed conversations: %d" report.RemovedConversationCount
            printfn "  Changed conversations: %d" report.ChangedConversationCount
            printfn "  Unchanged conversations: %d" report.UnchangedConversationCount

            if not report.AddedConversations.IsEmpty then
                printfn "  Added:"

                report.AddedConversations
                |> List.iter (fun item ->
                    let label = item.Title |> Option.defaultValue item.ProviderConversationId
                    printfn "    %s" item.ProviderConversationId
                    printfn "      label=%s" label
                    printfn "      canonical_conversation_id=%s" item.CanonicalConversationId
                    printfn "      messages=%d artifacts=%d" item.MessageCount item.ArtifactReferenceCount)

            if not report.RemovedConversations.IsEmpty then
                printfn "  Removed:"

                report.RemovedConversations
                |> List.iter (fun item ->
                    let label = item.Title |> Option.defaultValue item.ProviderConversationId
                    printfn "    %s" item.ProviderConversationId
                    printfn "      label=%s" label
                    printfn "      canonical_conversation_id=%s" item.CanonicalConversationId
                    printfn "      messages=%d artifacts=%d" item.MessageCount item.ArtifactReferenceCount)

            if not report.ChangedConversations.IsEmpty then
                printfn "  Changed:"

                report.ChangedConversations
                |> List.iter (fun item ->
                    let label =
                        item.CurrentTitle
                        |> Option.orElse item.BaseTitle
                        |> Option.defaultValue item.ProviderConversationId

                    printfn "    %s" item.ProviderConversationId
                    printfn "      label=%s" label
                    printfn
                        "      canonical_conversation_id=%s -> %s"
                        item.BaseCanonicalConversationId
                        item.CurrentCanonicalConversationId
                    printfn "      messages=%d -> %d" item.BaseMessageCount item.CurrentMessageCount
                    printfn
                        "      artifacts=%d -> %d"
                        item.BaseArtifactReferenceCount
                        item.CurrentArtifactReferenceCount)

            0
        | None ->
            eprintfn "Missing normalized import snapshot files for one or both imports."
            eprintfn "These snapshots are written during provider import and may be absent for older imports."
            eprintfn "Run rebuild-import-snapshots to backfill older provider-export imports from preserved raw artifacts."
            1

    let private reportProviderImportHistory eventStoreRoot objectsRoot provider limit =
        let resolveRawArtifactInfo (relativePath: string) =
            let absolutePath =
                Path.Combine(Path.GetFullPath(objectsRoot), relativePath.Replace('/', Path.DirectorySeparatorChar))

            if File.Exists(absolutePath) then
                Some (sha256ForFile absolutePath, true)
            else
                Some ("missing", false)

        match ImportSnapshots.tryBuildHistoryReport eventStoreRoot provider limit with
        | Some report ->
            printfn "Normalized import snapshot history."
            printfn "  Event store root: %s" eventStoreRoot
            printfn "  Objects root: %s" objectsRoot
            printfn "  Provider: %s" report.Provider
            printfn "  Available snapshots: %d" report.AvailableSnapshotCount
            printfn "  Reported entries: %d" report.ReportedEntryCount

            if not report.Entries.IsEmpty then
                printfn "  History:"

                let mutable previousRawArtifactHash: string option = None

                report.Entries
                |> List.iteri (fun index entry ->
                    let windowLabel = entry.Window |> Option.defaultValue "unknown"
                    printfn
                        "    %d. %s | imported_at=%s | window=%s | conversations=%d messages=%d artifacts=%d"
                        (index + 1)
                        (ImportId.format entry.ImportId)
                        (entry.ImportedAt.ToUniversalTime().ToString("O"))
                        windowLabel
                        entry.ConversationCount
                        entry.MessageCount
                        entry.ArtifactReferenceCount

                    match entry.NormalizationVersion with
                    | Some value -> printfn "      normalization_version=%s" value
                    | None -> ()

                    match entry.SourceArtifactRelativePath with
                    | Some value ->
                        printfn "      source_artifact_relative_path=%s" value

                        match resolveRawArtifactInfo value with
                        | Some (rawArtifactHash, true) ->
                            printfn "      source_artifact_sha256=%s" rawArtifactHash

                            match previousRawArtifactHash with
                            | Some previousHash ->
                                printfn "      source_artifact_matches_previous=%b" (String.Equals(previousHash, rawArtifactHash, StringComparison.Ordinal))
                            | None ->
                                printfn "      source_artifact_matches_previous=none (first raw artifact for provider)"

                            previousRawArtifactHash <- Some rawArtifactHash
                        | Some (_, false) ->
                            printfn "      source_artifact_sha256=missing"
                            printfn "      source_artifact_matches_previous=unknown"
                            previousRawArtifactHash <- None
                        | None -> ()
                    | None -> ()

                    match entry.DeltaFromPrevious with
                    | Some delta ->
                        printfn
                            "      delta_from_previous=%s added=%d removed=%d changed=%d unchanged=%d"
                            (ImportId.format delta.PreviousImportId)
                            delta.AddedConversationCount
                            delta.RemovedConversationCount
                            delta.ChangedConversationCount
                            delta.UnchangedConversationCount
                    | None ->
                        printfn "      delta_from_previous=none (first snapshot for provider)")

            0
        | None ->
            eprintfn "No normalized import snapshots found for provider %s." provider
            eprintfn "Run provider imports first, or use rebuild-import-snapshots if older imports predate snapshot materialization."
            1

    let private reportCurrentIngestion eventStoreRoot objectsRoot =
        let report = CurrentIngestion.buildReport eventStoreRoot objectsRoot

        printfn "Current ingestion status."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Objects root: %s" objectsRoot
        printfn "  Import manifests available: %d" report.ImportManifestCount
        printfn "  Providers with imports: %d" report.ProviderCount

        match report.MissingKnownProviders with
        | [] ->
            printfn "  Missing known providers: none"
        | values ->
            printfn "  Missing known providers: %s" (String.concat ", " values)

        if report.Entries.IsEmpty then
            printfn "  Latest imports: none"
        else
            printfn "  Latest imports:"

            report.Entries
            |> List.iteri (fun index entry ->
                let acquisitionLabel = entry.SourceAcquisition |> Option.defaultValue "unknown"
                let windowLabel = entry.Window |> Option.defaultValue "unknown"

                printfn
                    "    %d. %s | import_id=%s | imported_at=%s | acquisition=%s | window=%s"
                    (index + 1)
                    entry.Provider
                    (ImportId.format entry.ImportId)
                    (entry.ImportedAt.ToUniversalTime().ToString("O"))
                    acquisitionLabel
                    windowLabel

                match entry.NormalizationVersion with
                | Some value -> printfn "      normalization_version=%s" value
                | None -> ()

                match entry.LogosSourceSystem, entry.LogosIntakeChannel, entry.LogosPrimarySignalKind with
                | Some sourceSystem, Some intakeChannel, Some primarySignal ->
                    printfn
                        "      logos source_system=%s intake_channel=%s primary_signal=%s"
                        sourceSystem
                        intakeChannel
                        primarySignal

                    match entry.LogosRelatedSignalKinds with
                    | [] -> ()
                    | values -> printfn "      logos related_signals=%s" (String.concat ", " values)
                | _ -> ()

                match entry.RootArtifactRelativePath with
                | Some value ->
                    printfn "      root_artifact_relative_path=%s" value

                    match entry.RootArtifactExists, entry.RootArtifactSha256 with
                    | Some true, Some sha256 -> printfn "      root_artifact_sha256=%s" sha256
                    | Some false, _ -> printfn "      root_artifact_sha256=missing"
                    | _ -> ()
                | None -> ()

                printfn
                    "      counts conversations=%d messages=%d artifacts=%d new_events=%d duplicates=%d revisions=%d reparses=%d"
                    entry.Counts.ConversationsSeen
                    entry.Counts.MessagesSeen
                    entry.Counts.ArtifactsReferenced
                    entry.Counts.NewEventsAppended
                    entry.Counts.DuplicatesSkipped
                    entry.Counts.RevisionsObserved
                    entry.Counts.ReparseObservationsAppended

                printfn "      normalized_snapshot_available=%b" entry.SnapshotAvailable

                match entry.SnapshotConversationCount, entry.SnapshotMessageCount, entry.SnapshotArtifactReferenceCount with
                | Some conversations, Some messages, Some artifacts ->
                    printfn
                        "      snapshot conversations=%d messages=%d artifacts=%d"
                        conversations
                        messages
                        artifacts
                | _ -> ())

        0

    let private reportLogosCatalog () =
        let report = LogosCatalog.build ()

        let printItems (heading: string) (items: LogosCatalogItem list) =
            printfn "  %s (%d):" heading items.Length

            items
            |> List.iter (fun item ->
                printfn "    %s" item.Slug
                printfn "      %s" item.Summary)

        printfn "LOGOS catalog."
        printfn "  This catalog is an explicit allowlist, not an open-ended taxonomy."
        printItems "Source systems" report.SourceSystems
        printItems "Intake channels" report.IntakeChannels
        printItems "Signal kinds" report.SignalKinds
        printItems "Sensitivities" report.Sensitivities
        printItems "Sharing scopes" report.SharingScopes
        printItems "Sanitization statuses" report.SanitizationStatuses
        printItems "Retention classes" report.RetentionClasses
        0

    let private timestampLabel (value: DateTimeOffset option) =
        value
        |> Option.map (fun timestamp -> timestamp.ToUniversalTime().ToString("O"))
        |> Option.defaultValue "unknown"

    let private reportConversationOverlapCandidates eventStoreRoot leftProvider rightProvider limit =
        let report = ConversationOverlap.buildReport eventStoreRoot leftProvider rightProvider limit

        printfn "Conversation overlap candidates."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Left provider: %s" report.LeftProvider
        printfn "  Right provider: %s" report.RightProvider
        printfn "  Left conversations inspected: %d" report.LeftConversationCount
        printfn "  Right conversations inspected: %d" report.RightConversationCount
        printfn "  Candidate count: %d" report.CandidateCount
        printfn "  Reported candidates: %d" report.ReportedCount
        printfn "  These are heuristic candidates only. They do not reconcile or merge history."

        match report.Candidates with
        | [] ->
            printfn "  Candidates: none"
        | values ->
            printfn "  Candidates:"

            values
            |> List.iteri (fun index value ->
                printfn
                    "    %d. score=%d | %s -> %s"
                    (index + 1)
                    value.Score
                    value.LeftConversationId
                    value.RightConversationId
                printfn
                    "      left provider=%s title=%s messages=%d first=%s last=%s"
                    value.LeftProvider
                    (value.LeftTitle |> Option.defaultValue "(untitled)")
                    value.LeftMessageCount
                    (timestampLabel value.LeftFirstOccurredAt)
                    (timestampLabel value.LeftLastOccurredAt)
                printfn
                    "      right provider=%s title=%s messages=%d first=%s last=%s"
                    value.RightProvider
                    (value.RightTitle |> Option.defaultValue "(untitled)")
                    value.RightMessageCount
                    (timestampLabel value.RightFirstOccurredAt)
                    (timestampLabel value.RightLastOccurredAt)
                printfn "      signals=%s" (String.concat ", " value.Signals))

        0

    let private backfillOutcomeLabel =
        function
        | Rebuilt -> "rebuilt"
        | SkippedExisting -> "skipped-existing"
        | SkippedUnsupported -> "skipped-unsupported"
        | Failed -> "failed"

    let private rebuildImportSnapshots eventStoreRoot objectsRoot scope force =
        let result =
            ImportSnapshotBackfill.runWithStatus
                (fun message -> printfn "  %s" message)
                { EventStoreRoot = eventStoreRoot
                  ObjectsRoot = objectsRoot
                  Scope = scope
                  Force = force }

        printfn "Normalized import snapshots rebuilt."
        printfn "  Event store root: %s" result.EventStoreRoot
        printfn "  Objects root: %s" result.ObjectsRoot
        printfn "  Scope: %s" result.ScopeDescription
        printfn "  Parser normalization version: %s" result.ParserNormalizationVersion
        printfn "  Imports processed: %d" result.ProcessedCount
        printfn "  Snapshots rebuilt: %d" result.RebuiltCount
        printfn "  Existing snapshots skipped: %d" result.SkippedExistingCount
        printfn "  Unsupported imports skipped: %d" result.SkippedUnsupportedCount
        printfn "  Failed rebuilds: %d" result.FailedCount

        if not result.Imports.IsEmpty then
            printfn "  Import results:"

            result.Imports
            |> List.iter (fun item ->
                let providerLabel = item.Provider |> Option.defaultValue "unknown"
                printfn
                    "    %s | provider=%s | outcome=%s"
                    (ImportId.format item.ImportId)
                    providerLabel
                    (backfillOutcomeLabel item.Outcome)

                match item.ManifestRelativePath, item.ConversationsRelativePath with
                | Some manifestRelativePath, Some conversationsRelativePath ->
                    printfn "      manifest=%s" manifestRelativePath
                    printfn "      conversations=%s" conversationsRelativePath
                | _ -> ()

                match item.ConversationCount, item.MessageCount, item.ArtifactReferenceCount with
                | Some conversations, Some messages, Some artifacts ->
                    printfn "      conversations=%d messages=%d artifacts=%d" conversations messages artifacts
                | _ -> ()

                match item.Reason with
                | Some reason -> printfn "      note=%s" reason
                | None -> ())

        if result.FailedCount = 0 then 0 else 2

    let private importProviderExport request =
        let result = ImportWorkflow.runWithStatus (fun message -> printfn "  %s" message) request

        printfn "Provider export imported."
        printfn "  Provider: %s" (ProviderNaming.slug result.Provider)
        printfn "  Import ID: %s" (ImportId.format result.ImportId)
        printfn "  Archived zip: %s" result.ArchivedZipRelativePath
        printfn "  Latest zip: %s" result.LatestZipRelativePath

        match result.ExtractedPayloadRelativePath with
        | Some path -> printfn "  Extracted provider payload: %s" path
        | None -> ()

        printfn "  Event manifest: %s" result.ManifestRelativePath
        printImportSnapshotArtifacts result.ImportSnapshotManifestRelativePath result.ImportSnapshotConversationsRelativePath
        printWorkingGraphArtifacts
            result.WorkingGraphManifestRelativePath
            result.WorkingGraphCatalogRelativePath
            result.WorkingGraphIndexRelativePath
            result.WorkingGraphAssertionCount
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
        printWorkingGraphArtifacts
            result.WorkingGraphManifestRelativePath
            result.WorkingGraphCatalogRelativePath
            result.WorkingGraphIndexRelativePath
            result.WorkingGraphAssertionCount
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

    let private exportGraphvizDot eventStoreRoot objectsRoot outputPath outputRoot verification provider providerConversationId conversationId importId workingImportId workingNodeId =
        match workingImportId with
        | Some workingImportIdValue ->
            let verificationResult =
                match verification with
                | NoVerification -> Ok None
                | TraceableWorkingSlice ->
                    let parsedImportId = ImportId.parse workingImportIdValue
                    printfn "  Verifying working-batch traceability before DOT export..."
                    let report = GraphWorkingVerification.verifyImportSlice eventStoreRoot objectsRoot parsedImportId

                    if GraphWorkingVerification.isClean report then
                        Ok (Some report)
                    else
                        Error report

            match verificationResult with
            | Error report ->
                eprintfn "Working-batch traceability verification failed."
                eprintfn "  Event store root: %s" eventStoreRoot
                eprintfn "  Objects root: %s" objectsRoot
                eprintfn "  Working import ID: %s" workingImportIdValue
                eprintfn "  Missing canonical event refs: %d" report.MissingCanonicalEventReferences.Length
                eprintfn "  Missing raw object refs: %d" report.MissingRawObjectReferences.Length
                eprintfn "  Re-run verify-working-graph-batch for the full verification report."
                2
            | Ok verificationReport ->
                let result =
                    match workingNodeId with
                    | Some workingNodeIdValue ->
                        GraphvizDot.exportWorkingNodeNeighborhoodWithRoot eventStoreRoot workingImportIdValue workingNodeIdValue outputPath outputRoot
                    | None ->
                        GraphvizDot.exportWorkingImportBatchWithRoot eventStoreRoot workingImportIdValue outputPath outputRoot

                printfn "Graphviz DOT exported."
                printfn "  Event store root: %s" eventStoreRoot
                match workingNodeId with
                | Some _ -> printfn "  Source: graph working batch neighborhood"
                | None -> printfn "  Source: graph working batch"
                printfn "  Working import ID: %s" workingImportIdValue

                match workingNodeId with
                | Some workingNodeIdValue -> printfn "  Working node ID: %s" workingNodeIdValue
                | None -> ()

                match verificationReport with
                | Some report ->
                    printfn "  Verification: traceable"
                    printfn "  Objects root: %s" objectsRoot
                    printfn "  Supporting events verified: %d" report.DistinctSupportingEventCount
                    printfn "  Raw objects verified: %d" report.DistinctRawObjectCount
                | None -> ()

                printfn "  Output path: %s" result.OutputPath
                printfn "  Assertions scanned: %d" result.ScannedAssertionCount
                printfn "  Assertions exported: %d" result.AssertionCount
                printfn "  Nodes written: %d" result.NodeCount
                printfn "  Edges written: %d" result.EdgeCount
                0
        | None ->
            let filter =
                { GraphvizDot.ExportFilter.empty with
                    Provider = provider
                    ProviderConversationId = providerConversationId
                    ConversationId = conversationId
                    ImportId = importId }

            let result = GraphvizDot.exportFilteredWithRoot eventStoreRoot outputPath outputRoot filter
            printfn "Graphviz DOT exported."
            printfn "  Event store root: %s" eventStoreRoot
            printfn "  Source: durable graph assertions"
            printfn "  Output path: %s" result.OutputPath
            printfn "  Assertions scanned: %d" result.ScannedAssertionCount
            printfn "  Assertions exported: %d" result.AssertionCount
            printfn "  Nodes written: %d" result.NodeCount
            printfn "  Edges written: %d" result.EdgeCount
            0

    let private renderGraphvizDot inputPath outputPath outputRoot engine format =
        let result = GraphvizRendering.renderWithRoot inputPath outputPath outputRoot engine format
        printfn "Graphviz DOT rendered."
        printfn "  Input path: %s" result.InputPath
        printfn "  Output path: %s" result.OutputPath
        printfn "  Engine: %s" (GraphvizRendering.engineValue result.Engine)
        printfn "  Format: %s" (GraphvizRendering.formatValue result.Format)
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
        printfn "  Working batches: %d" report.WorkingSliceCount
        printfn "  Total canonical events: %d" report.TotalCanonicalEvents
        printfn "  Total graph assertions: %d" report.TotalGraphAssertions

        if not report.ProviderCounts.IsEmpty then
            printfn "  Providers:"

            report.ProviderCounts
            |> List.iter (fun (providerValue, count) ->
                printfn "    %s: %d" providerValue count)

        if not report.Items.IsEmpty then
            printfn "  Recent batches:"

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

    let private reportWorkingImportConversations eventStoreRoot importId limit =
        match GraphWorkingIndex.tryBuildImportConversationReport eventStoreRoot importId limit with
        | Some report ->
            printfn "Working import conversations report."
            printfn "  Event store root: %s" eventStoreRoot
            printfn "  Index: %s" report.IndexRelativePath
            printfn "  Import ID: %s" (ImportId.format report.ImportId)

            match report.Provider with
            | Some value -> printfn "  Provider: %s" value
            | None -> ()

            match report.Window with
            | Some value -> printfn "  Window: %s" value
            | None -> ()

            match report.ImportedAt with
            | Some value -> printfn "  Imported at: %s" (value.ToUniversalTime().ToString("O"))
            | None -> ()

            printfn "  Materialized at: %s" (report.MaterializedAt.ToUniversalTime().ToString("O"))
            printfn "  Working root: %s" report.WorkingRootRelativePath
            printfn "  Manifest: %s" report.ManifestRelativePath
            printfn "  Conversations in batch: %d" report.ConversationCount

            if not report.Items.IsEmpty then
                printfn "  Conversations:"

                report.Items
                |> List.iter (fun item ->
                    let label =
                        item.Title
                        |> Option.orElse item.Slug
                        |> Option.defaultValue item.ConversationNodeId

                    printfn "    %s" item.ConversationNodeId
                    printfn "      label=%s" label
                    printfn "      messages=%d artifacts=%d" item.MessageCount item.ArtifactCount

                    if not item.SemanticRoles.IsEmpty then
                        printfn "      semantic_roles=%s" (String.concat ", " item.SemanticRoles))

            0
        | None ->
            eprintfn "No working-import conversation summary found for import %s." (ImportId.format importId)
            eprintfn "Import a batch first or refresh the working index from existing batches."
            1

    let private compareWorkingImportConversations eventStoreRoot baseImportId currentImportId limit =
        match GraphWorkingIndex.tryBuildImportConversationComparisonReport eventStoreRoot baseImportId currentImportId limit with
        | Some report ->
            printfn "Working import conversation comparison report."
            printfn "  Event store root: %s" eventStoreRoot
            printfn "  Index: %s" report.IndexRelativePath
            printfn "  Base import ID: %s" (ImportId.format report.BaseImportId)
            printfn "  Current import ID: %s" (ImportId.format report.CurrentImportId)

            match report.BaseProvider with
            | Some value -> printfn "  Base provider: %s" value
            | None -> ()

            match report.CurrentProvider with
            | Some value -> printfn "  Current provider: %s" value
            | None -> ()

            match report.BaseWindow with
            | Some value -> printfn "  Base window: %s" value
            | None -> ()

            match report.CurrentWindow with
            | Some value -> printfn "  Current window: %s" value
            | None -> ()

            match report.BaseImportedAt with
            | Some value -> printfn "  Base imported at: %s" (value.ToUniversalTime().ToString("O"))
            | None -> ()

            match report.CurrentImportedAt with
            | Some value -> printfn "  Current imported at: %s" (value.ToUniversalTime().ToString("O"))
            | None -> ()

            printfn "  Base materialized at: %s" (report.BaseMaterializedAt.ToUniversalTime().ToString("O"))
            printfn "  Current materialized at: %s" (report.CurrentMaterializedAt.ToUniversalTime().ToString("O"))
            printfn "  Added conversations: %d" report.AddedConversationCount
            printfn "  Removed conversations: %d" report.RemovedConversationCount
            printfn "  Changed conversations: %d" report.ChangedConversationCount
            printfn "  Unchanged conversations: %d" report.UnchangedConversationCount

            if not report.AddedConversations.IsEmpty then
                printfn "  Added:"

                report.AddedConversations
                |> List.iter (fun item ->
                    let label =
                        item.Title
                        |> Option.orElse item.Slug
                        |> Option.defaultValue item.ConversationNodeId

                    printfn "    %s" item.ConversationNodeId
                    printfn "      label=%s" label
                    printfn "      messages=%d artifacts=%d" item.MessageCount item.ArtifactCount)

            if not report.RemovedConversations.IsEmpty then
                printfn "  Removed:"

                report.RemovedConversations
                |> List.iter (fun item ->
                    let label =
                        item.Title
                        |> Option.orElse item.Slug
                        |> Option.defaultValue item.ConversationNodeId

                    printfn "    %s" item.ConversationNodeId
                    printfn "      label=%s" label
                    printfn "      messages=%d artifacts=%d" item.MessageCount item.ArtifactCount)

            if not report.ChangedConversations.IsEmpty then
                printfn "  Changed:"

                report.ChangedConversations
                |> List.iter (fun item ->
                    let label =
                        item.CurrentTitle
                        |> Option.orElse item.BaseTitle
                        |> Option.orElse item.CurrentSlug
                        |> Option.orElse item.BaseSlug
                        |> Option.defaultValue item.ConversationNodeId

                    printfn "    %s" item.ConversationNodeId
                    printfn "      label=%s" label
                    printfn "      messages=%d -> %d" item.BaseMessageCount item.CurrentMessageCount
                    printfn "      artifacts=%d -> %d" item.BaseArtifactCount item.CurrentArtifactCount)

            0
        | None ->
            eprintfn "No working-import conversation comparison could be built for the selected imports."
            eprintfn "Import the batches first or refresh the working index from existing slices."
            1

    let private findWorkingGraphNodes eventStoreRoot importId provider matchText semanticRole messageRole limit =
        let matches =
            GraphWorkingIndex.findNodes eventStoreRoot importId provider matchText semanticRole messageRole limit

        printfn "Graph working node search."
        printfn "  Event store root: %s" eventStoreRoot

        match importId with
        | Some value -> printfn "  Import filter: %s" (ImportId.format value)
        | None -> ()

        match provider with
        | Some value -> printfn "  Provider filter: %s" value
        | None -> ()

        match matchText with
        | Some value -> printfn "  Match text: %s" value
        | None -> ()

        match semanticRole with
        | Some value -> printfn "  Semantic role: %s" value
        | None -> ()

        match messageRole with
        | Some value -> printfn "  Message role: %s" value
        | None -> ()

        printfn "  Matches: %d" matches.Length

        if not matches.IsEmpty then
            printfn "  Nodes:"

            matches
            |> List.iter (fun item ->
                let label =
                    item.Title
                    |> Option.orElse item.Slug
                    |> Option.defaultValue item.NodeId

                printfn "    %s" item.NodeId
                printfn
                    "      label=%s import_id=%s provider=%s"
                    label
                    (ImportId.format item.ImportId)
                    (item.Provider |> Option.defaultValue "unknown")

                item.NodeKind
                |> Option.iter (printfn "      kind=%s")

                item.Slug
                |> Option.iter (printfn "      slug=%s")

                if not item.SemanticRoles.IsEmpty then
                    printfn "      semantic_roles=%s" (String.concat ", " item.SemanticRoles)

                if not item.MessageRoles.IsEmpty then
                    printfn "      message_roles=%s" (String.concat ", " item.MessageRoles)

                if not item.MatchReasons.IsEmpty then
                    printfn "      matched_on=%s" (String.concat ", " item.MatchReasons))

        0

    let private reportWorkingGraphSlice eventStoreRoot importId limit =
        match GraphWorkingIndex.tryBuildImportSliceReport eventStoreRoot importId limit with
        | Some report ->
            printfn "Graph working batch report."
            printfn "  Event store root: %s" eventStoreRoot
            printfn "  Index: %s" report.IndexRelativePath
            printfn "  Import ID: %s" (ImportId.format report.ImportId)

            match report.Provider with
            | Some value -> printfn "  Provider: %s" value
            | None -> ()

            match report.Window with
            | Some value -> printfn "  Window: %s" value
            | None -> ()

            match report.ImportedAt with
            | Some value -> printfn "  Imported at: %s" (value.ToUniversalTime().ToString("O"))
            | None -> ()

            printfn "  Materialized at: %s" (report.MaterializedAt.ToUniversalTime().ToString("O"))
            printfn "  Canonical events: %d" report.CanonicalEventCount
            printfn "  Graph assertions: %d" report.GraphAssertionCount
            printfn "  Distinct subjects: %d" report.DistinctSubjectCount
            printfn "  Node-ref assertions: %d" report.NodeRefAssertionCount
            printfn "  Literal assertions: %d" report.LiteralAssertionCount
            printfn "  Working root: %s" report.WorkingRootRelativePath
            printfn "  Manifest: %s" report.ManifestRelativePath

            if not report.PredicateCounts.IsEmpty then
                printfn "  Predicates:"

                report.PredicateCounts
                |> List.iter (fun item ->
                    printfn "    %s: %d" item.Predicate item.Count)

            0
        | None ->
            eprintfn "No graph working batch found in the SQLite index for import %s." (ImportId.format importId)
            eprintfn "Import a batch first or refresh the working index from a new import."
            1

    let private reportWorkingGraphNeighborhood eventStoreRoot importId nodeId limit =
        match GraphWorkingIndex.tryBuildNeighborhoodReport eventStoreRoot importId nodeId limit with
        | Some report ->
            printfn "Graph working neighborhood report."
            printfn "  Event store root: %s" eventStoreRoot
            printfn "  Index: %s" report.IndexRelativePath
            printfn "  Import ID: %s" (ImportId.format report.ImportId)
            printfn "  Node ID: %s" report.NodeId

            match report.Provider with
            | Some value -> printfn "  Provider: %s" value
            | None -> ()

            match report.Window with
            | Some value -> printfn "  Window: %s" value
            | None -> ()

            match report.ImportedAt with
            | Some value -> printfn "  Imported at: %s" (value.ToUniversalTime().ToString("O"))
            | None -> ()

            printfn "  Materialized at: %s" (report.MaterializedAt.ToUniversalTime().ToString("O"))

            report.Title
            |> Option.iter (printfn "  Title: %s")

            report.Slug
            |> Option.iter (printfn "  Slug: %s")

            report.NodeKind
            |> Option.iter (printfn "  Kind: %s")

            if not report.SemanticRoles.IsEmpty then
                printfn "  Semantic roles: %s" (String.concat ", " report.SemanticRoles)

            if not report.MessageRoles.IsEmpty then
                printfn "  Message roles: %s" (String.concat ", " report.MessageRoles)

            printfn "  Working root: %s" report.WorkingRootRelativePath
            printfn "  Manifest: %s" report.ManifestRelativePath
            printfn "  Total literal assertions: %d" report.TotalLiteralAssertionCount
            printfn "  Total outgoing connections: %d" report.TotalOutgoingConnectionCount
            printfn "  Total incoming connections: %d" report.TotalIncomingConnectionCount

            if not report.Literals.IsEmpty then
                printfn "  Literals:"

                report.Literals
                |> List.iter (fun item ->
                    match item.ValueType with
                    | Some valueType ->
                        printfn "    %s = %s (%s)" item.Predicate item.Value valueType
                    | None ->
                        printfn "    %s = %s" item.Predicate item.Value)

            if not report.OutgoingConnections.IsEmpty then
                printfn "  Outgoing:"

                report.OutgoingConnections
                |> List.iter (fun item ->
                    let label = item.RelatedTitle |> Option.orElse item.RelatedSlug |> Option.defaultValue item.RelatedNodeId
                    printfn "    %s -> %s | %s" item.Predicate item.RelatedNodeId label)

            if not report.IncomingConnections.IsEmpty then
                printfn "  Incoming:"

                report.IncomingConnections
                |> List.iter (fun item ->
                    let label = item.RelatedTitle |> Option.orElse item.RelatedSlug |> Option.defaultValue item.RelatedNodeId
                    printfn "    %s <- %s | %s" item.Predicate item.RelatedNodeId label)

            0
        | None ->
            eprintfn "No graph working neighborhood found for import %s and node %s." (ImportId.format importId) nodeId
            eprintfn "Use find-working-graph-nodes to discover node IDs in the selected working batch."
            1

    let private verifyWorkingGraphSlice eventStoreRoot objectsRoot importId =
        let report = GraphWorkingVerification.verifyImportSlice eventStoreRoot objectsRoot importId

        printfn "Graph working batch verification."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Objects root: %s" objectsRoot
        printfn "  Import ID: %s" (ImportId.format report.ImportId)
        printfn "  Working root: %s" report.WorkingRootRelativePath
        printfn "  Manifest: %s" report.ManifestRelativePath
        printfn "  Verified at: %s" (report.VerifiedAt.ToUniversalTime().ToString("O"))
        printfn "  SQLite index entry available: %b" report.IndexAvailable
        printfn "  Assertions scanned: %d" report.AssertionCount
        printfn "  Assertion import mismatches: %d" report.AssertionImportMismatches
        printfn "  Assertions without supporting events: %d" report.AssertionsWithoutSupportingEvents
        printfn "  Supporting event refs: %d" report.SupportingEventReferenceCount
        printfn "  Distinct supporting events: %d" report.DistinctSupportingEventCount
        printfn "  Raw object refs: %d" report.RawObjectReferenceCount
        printfn "  Distinct raw objects: %d" report.DistinctRawObjectCount
        printfn "  Missing canonical event refs: %d" report.MissingCanonicalEventReferences.Length
        printfn "  Missing raw object refs: %d" report.MissingRawObjectReferences.Length

        if not report.MissingCanonicalEventReferences.IsEmpty then
            printfn "  Missing canonical events:"

            report.MissingCanonicalEventReferences
            |> List.truncate 10
            |> List.iter (fun item ->
                printfn "    %s referenced by %s" (CanonicalEventId.format item.EventId) (FactId.format item.ReferencedByFactId))

        if not report.MissingRawObjectReferences.IsEmpty then
            printfn "  Missing raw objects:"

            report.MissingRawObjectReferences
            |> List.truncate 10
            |> List.iter (fun item ->
                printfn "    %s referenced by %s" item.RelativePath (FactId.format item.ReferencedByFactId))

        if GraphWorkingVerification.isClean report then
            0
        else
            2

    let private rebuildWorkingGraphIndex eventStoreRoot =
        let result =
            GraphWorkingIndex.rebuildFromCatalogWithStatus
                (fun message -> printfn "  %s" message)
                eventStoreRoot

        printfn "Graph working SQLite index rebuilt."
        printfn "  Event store root: %s" eventStoreRoot
        printfn "  Catalog: %s" result.CatalogRelativePath
        printfn "  Index: %s" result.IndexRelativePath
        printfn "  Working batches indexed: %d" result.WorkingSliceCount
        printfn "  Graph assertions indexed: %d" result.GraphAssertionCount
        0

    let private createLogosIntakeNote request =
        match LogosIntakeNotes.create request with
        | Ok result ->
            let source = LogosSignal.source result.Signal
            let policy = result.Policy

            printfn "LOGOS intake note created."
            printfn "  Output path: %s" result.OutputPath
            printfn "  Slug: %s" result.NormalizedSlug
            printfn "  Source system: %s" (LogosSourceRef.sourceSystemId source |> SourceSystemId.value)
            printfn "  Intake channel: %s" (LogosSourceRef.intakeChannelId source |> IntakeChannelId.value)
            printfn "  Signal kind: %s" (LogosSignal.signalKindId result.Signal |> SignalKindId.value)
            printfn "  Sensitivity: %s" (SensitivityId.value policy.SensitivityId)
            printfn "  Sharing scope: %s" (SharingScopeId.value policy.SharingScopeId)
            printfn "  Sanitization status: %s" (SanitizationStatusId.value policy.SanitizationStatusId)
            printfn "  Retention class: %s" (RetentionClassId.value policy.RetentionClassId)
            printfn "  Locators: %d" (LogosSourceRef.locators source |> List.length)
            0
        | Error error ->
            eprintfn "%s" error
            1

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
        | Ok (CompareProviderExports(provider, baseZipPath, currentZipPath, limit)) ->
            compareProviderExports provider baseZipPath currentZipPath limit
        | Ok (CompareImportSnapshots(eventStoreRoot, baseImportId, currentImportId, limit)) ->
            compareImportSnapshots eventStoreRoot baseImportId currentImportId limit
        | Ok (ReportProviderImportHistory(eventStoreRoot, objectsRoot, provider, limit)) ->
            reportProviderImportHistory eventStoreRoot objectsRoot provider limit
        | Ok (ReportCurrentIngestion(eventStoreRoot, objectsRoot)) ->
            reportCurrentIngestion eventStoreRoot objectsRoot
        | Ok ReportLogosCatalog ->
            reportLogosCatalog ()
        | Ok (ReportConversationOverlapCandidates(eventStoreRoot, leftProvider, rightProvider, limit)) ->
            reportConversationOverlapCandidates eventStoreRoot leftProvider rightProvider limit
        | Ok (RebuildImportSnapshots(eventStoreRoot, objectsRoot, scope, force)) ->
            rebuildImportSnapshots eventStoreRoot objectsRoot scope force
        | Ok (ImportProviderExport request) ->
            importProviderExport request
        | Ok (ImportCodexSessions request) ->
            importCodexSessions request
        | Ok (CaptureArtifactPayload request) ->
            captureArtifactPayload request
        | Ok (RebuildGraphAssertions(eventStoreRoot, approved)) ->
            rebuildGraphAssertions eventStoreRoot approved
        | Ok (ExportGraphvizDot(eventStoreRoot, objectsRoot, outputPath, outputRoot, verification, provider, providerConversationId, conversationId, importId, workingImportId, workingNodeId)) ->
            exportGraphvizDot eventStoreRoot objectsRoot outputPath outputRoot verification provider providerConversationId conversationId importId workingImportId workingNodeId
        | Ok (RenderGraphvizDot(inputPath, outputPath, outputRoot, engine, format)) ->
            renderGraphvizDot inputPath outputPath outputRoot engine format
        | Ok (RebuildArtifactProjections eventStoreRoot) ->
            rebuildArtifactProjections eventStoreRoot
        | Ok (ReportUnresolvedArtifacts(eventStoreRoot, provider, limit)) ->
            reportUnresolvedArtifacts eventStoreRoot provider limit
        | Ok (ReportWorkingGraphImports(eventStoreRoot, limit)) ->
            reportWorkingGraphImports eventStoreRoot limit
        | Ok (ReportWorkingImportConversations(eventStoreRoot, importId, limit)) ->
            reportWorkingImportConversations eventStoreRoot importId limit
        | Ok (CompareWorkingImportConversations(eventStoreRoot, baseImportId, currentImportId, limit)) ->
            compareWorkingImportConversations eventStoreRoot baseImportId currentImportId limit
        | Ok (FindWorkingGraphNodes(eventStoreRoot, importId, provider, matchText, semanticRole, messageRole, limit)) ->
            findWorkingGraphNodes eventStoreRoot importId provider matchText semanticRole messageRole limit
        | Ok (ReportWorkingGraphSlice(eventStoreRoot, importId, limit)) ->
            reportWorkingGraphSlice eventStoreRoot importId limit
        | Ok (ReportWorkingGraphNeighborhood(eventStoreRoot, importId, nodeId, limit)) ->
            reportWorkingGraphNeighborhood eventStoreRoot importId nodeId limit
        | Ok (VerifyWorkingGraphSlice(eventStoreRoot, objectsRoot, importId)) ->
            verifyWorkingGraphSlice eventStoreRoot objectsRoot importId
        | Ok (RebuildWorkingGraphIndex eventStoreRoot) ->
            rebuildWorkingGraphIndex eventStoreRoot
        | Ok (RebuildConversationProjections eventStoreRoot) ->
            rebuildConversationProjections eventStoreRoot
        | Ok (CreateLogosIntakeNote request) ->
            createLogosIntakeNote request
        | Ok (CreateConceptNote request) ->
            createConceptNote request
        | Error exitCode ->
            exitCode
