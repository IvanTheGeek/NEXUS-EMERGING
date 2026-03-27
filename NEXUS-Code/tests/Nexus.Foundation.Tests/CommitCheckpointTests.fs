namespace Nexus.Tests

open System
open System.IO
open Expecto
open Nexus.EventStore
open Nexus.Importers

[<RequireQualifiedAccess>]
module CommitCheckpointTests =
    let private runGit workingDirectory arguments =
        let result = TestHelpers.runProcess workingDirectory "git" arguments

        if result.ExitCode <> 0 then
            failwithf "git %s failed in %s\nstdout:\n%s\nstderr:\n%s" (String.concat " " arguments) workingDirectory result.StandardOutput result.StandardError

        result.StandardOutput.Trim()

    let private createCommittedRepo repoRoot =
        Directory.CreateDirectory(repoRoot) |> ignore
        runGit repoRoot [ "init"; "-b"; "main" ] |> ignore
        runGit repoRoot [ "config"; "user.name"; "Nexus Tests" ] |> ignore
        runGit repoRoot [ "config"; "user.email"; "nexus-tests@example.com" ] |> ignore
        runGit repoRoot [ "remote"; "add"; "origin"; "git@github.com:IvanTheGeek/fixture.git" ] |> ignore

        let readmePath = Path.Combine(repoRoot, "README.md")
        File.WriteAllText(readmePath, "# Fixture Repo\n")
        runGit repoRoot [ "add"; "README.md" ] |> ignore
        runGit repoRoot [ "commit"; "-m"; "Fixture checkpoint commit" ] |> ignore
        runGit repoRoot [ "rev-parse"; "--verify"; "HEAD" ]

    let tests =
        testList
            "commit checkpoints"
            [ testCase "CaptureCodexCommitCheckpoint writes durable commit-linked manifest" (fun () ->
                  TestHelpers.withTempDirectory "nexus-commit-checkpoint" (fun tempRoot ->
                      let repoRoot = Path.Combine(tempRoot, "repo")
                      let commitSha = createCommittedRepo repoRoot
                      let sourceRoot = Path.Combine(tempRoot, "codex-source")
                      let objectsRoot = Path.Combine(tempRoot, "objects")
                      let eventStoreRoot = Path.Combine(tempRoot, "event-store")
                      TestHelpers.copyFixtureDirectory "codex/latest" sourceRoot

                      let request =
                          { RepoRoot = repoRoot
                            CodexSourceRoot = sourceRoot
                            ObjectsRoot = objectsRoot
                            EventStoreRoot = eventStoreRoot
                            Force = false }

                      let result =
                          match CodexCommitCheckpointWorkflow.run request with
                          | Ok value -> value
                          | Error message -> failwith message

                      Expect.equal result.CommitSha commitSha "Expected the checkpoint to bind to the Git HEAD commit."
                      Expect.equal result.CommitSummary "Fixture checkpoint commit" "Expected the Git commit summary to be persisted."
                      Expect.equal result.ImportResult.Counts.ConversationsSeen 1 "Expected one Codex conversation from the fixture snapshot."
                      Expect.equal result.ImportResult.Counts.MessagesSeen 2 "Expected two Codex messages from the fixture snapshot."

                      let checkpointManifestPath = Path.Combine(eventStoreRoot, result.CheckpointManifestRelativePath)
                      Expect.isTrue (File.Exists(checkpointManifestPath)) "Expected the durable checkpoint manifest to be written."

                      let checkpoint =
                          CommitCheckpoints.tryLoad eventStoreRoot result.RepoSlug result.CommitSha
                          |> Option.defaultWith (fun () -> failwith "Expected the checkpoint manifest to load.")

                      Expect.equal checkpoint.CommitSha commitSha "Expected the stored checkpoint commit SHA."
                      Expect.equal checkpoint.CommitSummary "Fixture checkpoint commit" "Expected the stored checkpoint commit summary."
                      Expect.equal checkpoint.RemoteOrigin (Some "git@github.com:IvanTheGeek/fixture.git") "Expected the stored remote origin."
                      Expect.equal checkpoint.Conversations.Length 1 "Expected one stored conversation hint."
                      Expect.equal checkpoint.Conversations.Head.Title (Some "Codex Fixture Session") "Expected the stored conversation title."

                      let snapshotManifestPath = Path.Combine(objectsRoot, result.SnapshotManifestRelativePath)
                      Expect.isTrue (File.Exists(snapshotManifestPath)) "Expected the archived Codex snapshot manifest to be written."

                      let report =
                          match CodexCommitCheckpointWorkflow.report eventStoreRoot repoRoot None with
                          | Ok value -> value
                          | Error message -> failwith message

                      Expect.equal report.CommitSha commitSha "Expected the report flow to resolve the same commit."
                      Expect.equal report.ManifestRelativePath result.CheckpointManifestRelativePath "Expected the report manifest path to match the captured checkpoint." ))

              testCase "CaptureCodexCommitCheckpoint refuses duplicate commit capture without force" (fun () ->
                  TestHelpers.withTempDirectory "nexus-commit-checkpoint-duplicate" (fun tempRoot ->
                      let repoRoot = Path.Combine(tempRoot, "repo")
                      ignore (createCommittedRepo repoRoot)

                      let sourceRoot = Path.Combine(tempRoot, "codex-source")
                      let objectsRoot = Path.Combine(tempRoot, "objects")
                      let eventStoreRoot = Path.Combine(tempRoot, "event-store")
                      TestHelpers.copyFixtureDirectory "codex/latest" sourceRoot

                      let request =
                          { RepoRoot = repoRoot
                            CodexSourceRoot = sourceRoot
                            ObjectsRoot = objectsRoot
                            EventStoreRoot = eventStoreRoot
                            Force = false }

                      match CodexCommitCheckpointWorkflow.run request with
                      | Error message -> failwithf "Expected first checkpoint capture to succeed, but got: %s" message
                      | Ok _ -> ()

                      match CodexCommitCheckpointWorkflow.run request with
                      | Ok _ -> failwith "Expected duplicate checkpoint capture to be refused without --force."
                      | Error message ->
                          Expect.stringContains message "already exists" "Expected duplicate checkpoint refusal guidance." )) ]
