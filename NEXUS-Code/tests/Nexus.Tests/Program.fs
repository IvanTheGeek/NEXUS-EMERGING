namespace Nexus.Tests

open Expecto

module Program =
    [<EntryPoint>]
    let main argv =
        testList
            "nexus"
            [ KernelTests.tests
              LogosTests.tests
              ProviderAdapterTests.tests
              WorkflowTests.tests
              ImportSnapshotTests.tests
              ImportSnapshotHistoryTests.tests
              CurrentIngestionTests.tests
              GraphAssertionTests.tests
              GraphWorkingCatalogTests.tests
              GraphWorkingIndexTests.tests
              GraphvizDotTests.tests
              PropertyTests.tests
              SnapshotTests.tests
              CliHelpTests.tests
              ConceptNoteTests.tests ]
        |> runTestsWithCLIArgs [] argv
