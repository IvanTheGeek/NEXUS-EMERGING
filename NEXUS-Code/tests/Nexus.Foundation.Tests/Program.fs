namespace Nexus.Tests

open Expecto

module Program =
    [<EntryPoint>]
    let main argv =
        testList
            "nexus-foundation"
            [ KernelTests.tests
              LogosTests.tests
              BlogImportTests.tests
              ProviderAdapterTests.tests
              WorkflowTests.tests
              CommitCheckpointTests.tests
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
              EventStoreRootResolutionTests.tests
              ConceptNoteTests.tests ]
        |> runTestsWithCLIArgs [] argv
