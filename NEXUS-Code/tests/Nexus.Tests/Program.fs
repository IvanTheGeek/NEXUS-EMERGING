namespace Nexus.Tests

open Expecto

module Program =
    [<EntryPoint>]
    let main argv =
        testList
            "nexus"
            [ ProviderAdapterTests.tests
              WorkflowTests.tests
              GraphAssertionTests.tests
              PropertyTests.tests
              SnapshotTests.tests
              CliHelpTests.tests ]
        |> runTestsWithCLIArgs [] argv
