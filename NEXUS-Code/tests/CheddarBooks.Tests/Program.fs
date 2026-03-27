namespace CheddarBooks.Tests

open Expecto

module Program =
    [<EntryPoint>]
    let main argv =
        testList "cheddarbooks" [ LaundryLogTests.tests ]
        |> runTestsWithCLIArgs [] argv
