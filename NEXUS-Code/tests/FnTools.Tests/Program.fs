namespace FnTools.Tests

open Expecto

module Program =
    [<EntryPoint>]
    let main argv =
        testList "fntools" [ FnHCITests.tests ]
        |> runTestsWithCLIArgs [] argv
