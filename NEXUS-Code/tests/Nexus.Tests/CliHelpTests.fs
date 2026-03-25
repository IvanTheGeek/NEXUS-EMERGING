namespace Nexus.Tests

open Expecto

[<RequireQualifiedAccess>]
module CliHelpTests =
    let tests =
        testList
            "cli help"
            [ testCase "Global help lists commands and guidance" (fun () ->
                  let result = TestHelpers.runCli [ "--help" ]

                  Expect.equal result.ExitCode 0 "Expected global help to exit successfully."
                  Expect.stringContains result.StandardOutput "NEXUS CLI" "Expected the CLI banner."
                  Expect.stringContains result.StandardOutput "help <command>" "Expected command-specific help guidance."
                  Expect.stringContains result.StandardOutput "import-provider-export" "Expected the provider import command in global help."
                  Expect.stringContains result.StandardOutput "docs/how-to/cli-commands.md" "Expected the CLI guide reference in global help."
                  Expect.equal result.StandardError "" "Did not expect stderr output for global help.")

              testCase "Raw export comparison help exposes the source-layer workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "compare-provider-exports" ]

                  Expect.equal result.ExitCode 0 "Expected help compare-provider-exports to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: compare-provider-exports" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--provider <chatgpt|claude|grok>" "Expected provider allowlist guidance."
                  Expect.stringContains result.StandardOutput "--base-zip <path>" "Expected base-zip guidance."
                  Expect.stringContains result.StandardOutput "--current-zip <path>" "Expected current-zip guidance."
                  Expect.stringContains result.StandardOutput "source-layer" "Expected source-layer comparison note."
                  Expect.stringContains result.StandardOutput "docs/how-to/compare-provider-exports.md" "Expected the raw comparison guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help compare-provider-exports.")

              testCase "Normalized snapshot comparison help exposes the snapshot workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "compare-import-snapshots" ]

                  Expect.equal result.ExitCode 0 "Expected help compare-import-snapshots to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: compare-import-snapshots" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--base-import-id <uuid>" "Expected base-import guidance."
                  Expect.stringContains result.StandardOutput "--current-import-id <uuid>" "Expected current-import guidance."
                  Expect.stringContains result.StandardOutput "snapshot semantics" "Expected the normalized snapshot note."
                  Expect.stringContains result.StandardOutput "docs/how-to/compare-import-snapshots.md" "Expected the normalized snapshot guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help compare-import-snapshots.")

              testCase "Provider import history help exposes the chronological snapshot workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "report-provider-import-history" ]

                  Expect.equal result.ExitCode 0 "Expected help report-provider-import-history to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: report-provider-import-history" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--provider <chatgpt|claude|grok|codex>" "Expected provider allowlist guidance."
                  Expect.stringContains result.StandardOutput "--objects-root <path>" "Expected objects-root guidance."
                  Expect.stringContains result.StandardOutput "--limit <n>" "Expected limit guidance."
                  Expect.stringContains result.StandardOutput "adjacent deltas" "Expected the adjacent-delta note."
                  Expect.stringContains result.StandardOutput "docs/how-to/report-provider-import-history.md" "Expected the history guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help report-provider-import-history.")

              testCase "Current ingestion help exposes the cross-provider status workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "report-current-ingestion" ]

                  Expect.equal result.ExitCode 0 "Expected help report-current-ingestion to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: report-current-ingestion" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--event-store-root <path>" "Expected event-store-root guidance."
                  Expect.stringContains result.StandardOutput "--objects-root <path>" "Expected objects-root guidance."
                  Expect.stringContains result.StandardOutput "latest known import state across providers" "Expected the cross-provider summary note."
                  Expect.stringContains result.StandardOutput "docs/how-to/report-current-ingestion.md" "Expected the current-ingestion guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help report-current-ingestion.")

              testCase "Conversation overlap help exposes the explicit candidate workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "report-conversation-overlap-candidates" ]

                  Expect.equal result.ExitCode 0 "Expected help report-conversation-overlap-candidates to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: report-conversation-overlap-candidates" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--left-provider <chatgpt|claude|grok|codex>" "Expected left-provider allowlist guidance."
                  Expect.stringContains result.StandardOutput "--right-provider <chatgpt|claude|grok|codex>" "Expected right-provider allowlist guidance."
                  Expect.stringContains result.StandardOutput "heuristic candidate report only" "Expected the explicit candidate warning."
                  Expect.stringContains result.StandardOutput "docs/how-to/report-conversation-overlap-candidates.md" "Expected the overlap guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help report-conversation-overlap-candidates.")

              testCase "Snapshot rebuild help exposes the backfill workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "rebuild-import-snapshots" ]

                  Expect.equal result.ExitCode 0 "Expected help rebuild-import-snapshots to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: rebuild-import-snapshots" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--import-id <uuid>" "Expected import-id guidance."
                  Expect.stringContains result.StandardOutput "--all" "Expected the explicit all-imports guidance."
                  Expect.stringContains result.StandardOutput "--force" "Expected overwrite guidance."
                  Expect.stringContains result.StandardOutput "current provider-export parser rules" "Expected the parser-version note."
                  Expect.stringContains result.StandardOutput "docs/how-to/rebuild-import-snapshots.md" "Expected the backfill guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help rebuild-import-snapshots.")

              testCase "Command help works through both help forms" (fun () ->
                  let helpCommandResult = TestHelpers.runCli [ "help"; "import-provider-export" ]
                  let switchResult = TestHelpers.runCli [ "import-provider-export"; "--help" ]

                  Expect.equal helpCommandResult.ExitCode 0 "Expected help import-provider-export to exit successfully."
                  Expect.equal switchResult.ExitCode 0 "Expected import-provider-export --help to exit successfully."

                  for output in [ helpCommandResult.StandardOutput; switchResult.StandardOutput ] do
                      Expect.stringContains output "Command: import-provider-export" "Expected the command header."
                      Expect.stringContains output "--provider <chatgpt|claude|grok>" "Expected required provider option guidance."
                      Expect.stringContains output "docs/how-to/import-provider-export.md" "Expected the detailed guide link."

                  Expect.equal helpCommandResult.StandardError "" "Did not expect stderr from help import-provider-export."
                  Expect.equal switchResult.StandardError "" "Did not expect stderr from import-provider-export --help.")

              testCase "Graphviz help exposes the external graph export workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "export-graphviz-dot" ]

                  Expect.equal result.ExitCode 0 "Expected help export-graphviz-dot to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: export-graphviz-dot" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--objects-root <path>" "Expected traceable objects-root guidance."
                  Expect.stringContains result.StandardOutput "--output <path>" "Expected custom output guidance."
                  Expect.stringContains result.StandardOutput "--provider <chatgpt|claude|grok|codex>" "Expected provider slice guidance."
                  Expect.stringContains result.StandardOutput "--conversation-id <uuid>" "Expected canonical conversation slice guidance."
                  Expect.stringContains result.StandardOutput "--provider-conversation-id <id>" "Expected provider conversation slice guidance."
                  Expect.stringContains result.StandardOutput "--import-id <uuid>" "Expected import slice guidance."
                  Expect.stringContains result.StandardOutput "--working-import-id <uuid>" "Expected graph working slice guidance."
                  Expect.stringContains result.StandardOutput "--working-node-id <node-id>" "Expected graph working neighborhood guidance."
                  Expect.stringContains result.StandardOutput "--verification <none|traceable>" "Expected verification allowlist guidance."
                  Expect.stringContains result.StandardOutput "--output-root <path>" "Expected output-root guidance."
                  Expect.stringContains result.StandardOutput "docs/how-to/export-graphviz-dot.md" "Expected the Graphviz guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help export-graphviz-dot.")

              testCase "Graphviz render help exposes allowlisted engines and formats" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "render-graphviz-dot" ]

                  Expect.equal result.ExitCode 0 "Expected help render-graphviz-dot to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: render-graphviz-dot" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--input <path>" "Expected input path guidance."
                  Expect.stringContains result.StandardOutput "--output-root <path>" "Expected output-root guidance."
                  Expect.stringContains result.StandardOutput "--engine <dot|sfdp>" "Expected engine allowlist guidance."
                  Expect.stringContains result.StandardOutput "--format <svg|png>" "Expected format allowlist guidance."
                  Expect.stringContains result.StandardOutput "docs/how-to/render-graphviz-dot.md" "Expected the render guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help render-graphviz-dot.")

              testCase "Graph rebuild help exposes the heavyweight approval flag" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "rebuild-graph-assertions" ]

                  Expect.equal result.ExitCode 0 "Expected help rebuild-graph-assertions to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: rebuild-graph-assertions" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--yes" "Expected explicit approval guidance."
                  Expect.stringContains result.StandardOutput "heavyweight" "Expected the heavyweight-operation warning."
                  Expect.stringContains result.StandardOutput "docs/how-to/rebuild-graph-assertions.md" "Expected the graph rebuild guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help rebuild-graph-assertions.")

              testCase "Working graph report help exposes the catalog workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "report-working-graph-imports" ]

                  Expect.equal result.ExitCode 0 "Expected help report-working-graph-imports to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: report-working-graph-imports" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--limit <n>" "Expected limit guidance."
                  Expect.stringContains result.StandardOutput "graph/working/catalog" "Expected the graph working catalog note."
                  Expect.stringContains result.StandardOutput "docs/how-to/report-working-graph-imports.md" "Expected the working-graph report guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help report-working-graph-imports.")

              testCase "Working import conversation help exposes the conversation-centric slice workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "report-working-import-conversations" ]

                  Expect.equal result.ExitCode 0 "Expected help report-working-import-conversations to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: report-working-import-conversations" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--import-id <uuid>" "Expected import-id guidance."
                  Expect.stringContains result.StandardOutput "--limit <n>" "Expected limit guidance."
                  Expect.stringContains result.StandardOutput "docs/how-to/report-working-import-conversations.md" "Expected the conversation report guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help report-working-import-conversations.")

              testCase "Working import comparison help exposes the batch-delta workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "compare-working-import-conversations" ]

                  Expect.equal result.ExitCode 0 "Expected help compare-working-import-conversations to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: compare-working-import-conversations" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--base-import-id <uuid>" "Expected base-import guidance."
                  Expect.stringContains result.StandardOutput "--current-import-id <uuid>" "Expected current-import guidance."
                  Expect.stringContains result.StandardOutput "batch-local" "Expected the batch-local contribution note."
                  Expect.stringContains result.StandardOutput "docs/how-to/compare-working-import-conversations.md" "Expected the comparison guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help compare-working-import-conversations.")

              testCase "Working graph node search help exposes the indexed discovery workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "find-working-graph-nodes" ]

                  Expect.equal result.ExitCode 0 "Expected help find-working-graph-nodes to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: find-working-graph-nodes" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--match <text>" "Expected title/slug match guidance."
                  Expect.stringContains result.StandardOutput "--semantic-role <slug>" "Expected semantic-role guidance."
                  Expect.stringContains result.StandardOutput "--message-role <slug>" "Expected message-role guidance."
                  Expect.stringContains result.StandardOutput "docs/how-to/find-working-graph-nodes.md" "Expected the node-search guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help find-working-graph-nodes.")

              testCase "Working graph slice help exposes the SQLite index workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "report-working-graph-slice" ]

                  Expect.equal result.ExitCode 0 "Expected help report-working-graph-slice to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: report-working-graph-slice" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--import-id <uuid>" "Expected import-id guidance."
                  Expect.stringContains result.StandardOutput "SQLite" "Expected the SQLite working-index note."
                  Expect.stringContains result.StandardOutput "docs/how-to/report-working-graph-slice.md" "Expected the working-slice guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help report-working-graph-slice.")

              testCase "Working graph neighborhood help exposes the local neighborhood workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "report-working-graph-neighborhood" ]

                  Expect.equal result.ExitCode 0 "Expected help report-working-graph-neighborhood to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: report-working-graph-neighborhood" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--import-id <uuid>" "Expected import-id guidance."
                  Expect.stringContains result.StandardOutput "--node-id <node-id>" "Expected node-id guidance."
                  Expect.stringContains result.StandardOutput "docs/how-to/report-working-graph-neighborhood.md" "Expected the neighborhood guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help report-working-graph-neighborhood.")

              testCase "Working graph index rebuild help exposes the rebuild workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "rebuild-working-graph-index" ]

                  Expect.equal result.ExitCode 0 "Expected help rebuild-working-graph-index to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: rebuild-working-graph-index" "Expected the command header."
                  Expect.stringContains result.StandardOutput "graph/working/imports" "Expected the working-slice source note."
                  Expect.stringContains result.StandardOutput "graph-working.sqlite" "Expected the SQLite index target note."
                  Expect.stringContains result.StandardOutput "docs/how-to/rebuild-working-graph-index.md" "Expected the working-index rebuild guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help rebuild-working-graph-index.")

              testCase "Working graph verification help exposes the traceability workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "verify-working-graph-slice" ]

                  Expect.equal result.ExitCode 0 "Expected help verify-working-graph-slice to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: verify-working-graph-slice" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--objects-root <path>" "Expected objects-root guidance."
                  Expect.stringContains result.StandardOutput "canonical events" "Expected canonical verification guidance."
                  Expect.stringContains result.StandardOutput "docs/how-to/verify-working-graph-slice.md" "Expected the verification guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help verify-working-graph-slice.")

              testCase "Concept note help exposes the curation workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "create-concept-note" ]

                  Expect.equal result.ExitCode 0 "Expected help create-concept-note to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: create-concept-note" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--slug <slug>" "Expected slug guidance."
                  Expect.stringContains result.StandardOutput "--title <title>" "Expected title guidance."
                  Expect.stringContains result.StandardOutput "--conversation-id <uuid>" "Expected provenance conversation guidance."
                  Expect.stringContains result.StandardOutput "docs/how-to/create-concept-note.md" "Expected the detailed guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help create-concept-note.")

              testCase "Unknown help target fails clearly" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "not-a-real-command" ]

                  Expect.equal result.ExitCode 1 "Expected unknown command help to fail."
                  Expect.stringContains result.StandardError "Unknown command: not-a-real-command" "Expected a clear unknown-command error."
                  Expect.stringContains result.StandardOutput "NEXUS CLI" "Expected the CLI usage block after the error."
                  Expect.stringContains result.StandardOutput "write-sample-event-store" "Expected the usage block to list available commands.") ]
