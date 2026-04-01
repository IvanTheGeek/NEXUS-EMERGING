namespace Nexus.Tests

open System.IO
open Expecto
open Nexus.Cli

[<RequireQualifiedAccess>]
module EventStoreRootResolutionTests =
    let tests =
        testList
            "event store root resolution"
            [ testCase "Resolver prefers environment variable over repo-local candidates" (fun () ->
                  let repoRoot = "/tmp/nexus-emerging"

                  let resolution =
                      Program.resolveDefaultEventStoreRootFor repoRoot (Some "/tmp/custom-event-store") true true

                  Expect.equal resolution.Source Program.EventStoreRootDefaultSource.EnvironmentVariable "Expected the environment variable to win."
                  Expect.equal resolution.SelectedPath (Path.GetFullPath("/tmp/custom-event-store")) "Expected the configured environment path."
                  Expect.equal resolution.InRepoPath (Path.Combine(repoRoot, "NEXUS-EventStore") |> Path.GetFullPath) "Expected the in-repo path to stay visible in the resolution."
                  Expect.equal resolution.SiblingPath (Path.Combine(repoRoot, "..", "NEXUS-EventStore") |> Path.GetFullPath) "Expected the sibling path to stay visible in the resolution." )

              testCase "Resolver prefers sibling repo when in-repo event store is absent" (fun () ->
                  let repoRoot = "/tmp/nexus-emerging"

                  let resolution =
                      Program.resolveDefaultEventStoreRootFor repoRoot None false true

                  Expect.equal resolution.Source Program.EventStoreRootDefaultSource.Sibling "Expected the sibling repo to be the default after extraction."
                  Expect.equal resolution.SelectedPath (Path.Combine(repoRoot, "..", "NEXUS-EventStore") |> Path.GetFullPath) "Expected the sibling event-store path." )

              testCase "Validation explains how to recover when no default event store exists" (fun () ->
                  let repoRoot = "/tmp/nexus-emerging"

                  let resolution =
                      Program.resolveDefaultEventStoreRootFor repoRoot None false false

                  match Program.validateDefaultEventStoreRootFor resolution resolution.SelectedPath with
                  | Ok _ -> failwith "Expected missing default event-store resolution to be rejected."
                  | Error message ->
                      Expect.stringContains message "IvanTheGeek/NEXUS-EventStore" "Expected the recovery guidance to mention the extracted repo."
                      Expect.stringContains message "--event-store-root <path>" "Expected the override guidance."
                      Expect.stringContains message resolution.SiblingPath "Expected the sibling target path in the recovery guidance." ) ]
