namespace Nexus.Tests

open Expecto

module Program =
    [<EntryPoint>]
    let main argv =
        testList
            "nexus"
            [ KernelTests.tests
              ProviderAdapterTests.tests
              WorkflowTests.tests
              GraphAssertionTests.tests
              GraphvizDotTests.tests
              PropertyTests.tests
              SnapshotTests.tests
              CliHelpTests.tests ]
        |> runTestsWithCLIArgs [] argv
