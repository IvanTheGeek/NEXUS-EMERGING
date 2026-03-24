namespace Nexus.Tests

open Expecto
open Nexus.Kernel

[<RequireQualifiedAccess>]
module KernelTests =
    let tests =
        testList
            "kernel"
            [ testCase "Imprint role stays stable" (fun () ->
                  Expect.equal (RoleId.value CoreRoles.imprint) "imprint" "Expected the core imprint role slug.")

              testCase "Relation kind identifiers reject blank values" (fun () ->
                  Expect.throws
                      (fun () -> RelationKindId.create "   " |> ignore)
                      "Expected blank relation kind identifiers to be rejected.") ]
