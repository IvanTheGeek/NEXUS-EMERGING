namespace Nexus.Tests

open Expecto

module Program =
    [<EntryPoint>]
    let main argv =
        testList
            "nexus"
            [ FnHCITests.tests
              LaundryLogTests.tests
              KernelTests.tests
              LogosTests.tests
              ProviderAdapterTests.tests
              WorkflowTests.tests
              ImportSnapshotTests.tests
              ImportSnapshotHistoryTests.tests
              CurrentIngestionTests.tests
              ConversationOverlapTests.tests
              GraphAssertionTests.tests
              GraphWorkingCatalogTests.tests
              GraphWorkingIndexTests.tests
              GraphvizDotTests.tests
              PropertyTests.tests
              SnapshotTests.tests
              CliHelpTests.tests
              ConceptNoteTests.tests ]
        |> runTestsWithCLIArgs [] argv
