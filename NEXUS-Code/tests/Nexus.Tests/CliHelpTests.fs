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

              testCase "Unknown help target fails clearly" (fun () ->
                  let result = TestHelpers.runCli [ "help"; "not-a-real-command" ]

                  Expect.equal result.ExitCode 1 "Expected unknown command help to fail."
                  Expect.stringContains result.StandardError "Unknown command: not-a-real-command" "Expected a clear unknown-command error."
                  Expect.stringContains result.StandardOutput "NEXUS CLI" "Expected the CLI usage block after the error."
                  Expect.stringContains result.StandardOutput "write-sample-event-store" "Expected the usage block to list available commands.") ]
