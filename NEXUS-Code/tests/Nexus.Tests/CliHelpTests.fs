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

              testCase "Command help works through both help forms" (fun () ->
                  let helpCommandResult = TestHelpers.runCli [ "help"; "import-provider-export" ]
                  let switchResult = TestHelpers.runCli [ "import-provider-export"; "--help" ]

                  Expect.equal helpCommandResult.ExitCode 0 "Expected help import-provider-export to exit successfully."
                  Expect.equal switchResult.ExitCode 0 "Expected import-provider-export --help to exit successfully."

                  for output in [ helpCommandResult.StandardOutput; switchResult.StandardOutput ] do
                      Expect.stringContains output "Command: import-provider-export" "Expected the command header."
                      Expect.stringContains output "--provider <chatgpt|claude>" "Expected required provider option guidance."
                      Expect.stringContains output "docs/how-to/import-provider-export.md" "Expected the detailed guide link."

                  Expect.equal helpCommandResult.StandardError "" "Did not expect stderr from help import-provider-export."
                  Expect.equal switchResult.StandardError "" "Did not expect stderr from import-provider-export --help.")

              testCase "Graphviz help exposes the external graph export workflow" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "export-graphviz-dot" ]

                  Expect.equal result.ExitCode 0 "Expected help export-graphviz-dot to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: export-graphviz-dot" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--output <path>" "Expected custom output guidance."
                  Expect.stringContains result.StandardOutput "--provider <chatgpt|claude|codex>" "Expected provider slice guidance."
                  Expect.stringContains result.StandardOutput "--conversation-id <uuid>" "Expected canonical conversation slice guidance."
                  Expect.stringContains result.StandardOutput "--provider-conversation-id <id>" "Expected provider conversation slice guidance."
                  Expect.stringContains result.StandardOutput "--import-id <uuid>" "Expected import slice guidance."
                  Expect.stringContains result.StandardOutput "--working-import-id <uuid>" "Expected graph working slice guidance."
                  Expect.stringContains result.StandardOutput "docs/how-to/export-graphviz-dot.md" "Expected the Graphviz guide link."
                  Expect.equal result.StandardError "" "Did not expect stderr from help export-graphviz-dot.")

              testCase "Graphviz render help exposes allowlisted engines and formats" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "render-graphviz-dot" ]

                  Expect.equal result.ExitCode 0 "Expected help render-graphviz-dot to exit successfully."
                  Expect.stringContains result.StandardOutput "Command: render-graphviz-dot" "Expected the command header."
                  Expect.stringContains result.StandardOutput "--input <path>" "Expected input path guidance."
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
