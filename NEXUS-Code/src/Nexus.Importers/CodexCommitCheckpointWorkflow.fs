namespace Nexus.Importers

open System
open System.Diagnostics
open System.Globalization
open System.IO
open System.Text
open Nexus.EventStore

[<RequireQualifiedAccess>]
module CodexCommitCheckpointWorkflow =
    type ReportResult =
        { RepoRoot: string
          RepoSlug: string
          CommitSha: string
          ManifestRelativePath: string
          Checkpoint: CommitCheckpoints.CodexCommitCheckpoint }

    type private GitHead =
        { RepoRoot: string
          RepoSlug: string
          BranchName: string option
          RemoteOrigin: string option
          CommitSha: string
          CommitShortSha: string
          CommitSummary: string
          CommitMessage: string }

    let private normalizePath (path: string) =
        path.Replace('\\', '/')

    let private runGit repoRoot arguments =
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- "git"
        startInfo.WorkingDirectory <- repoRoot
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.UseShellExecute <- false

        for argument in arguments do
            startInfo.ArgumentList.Add(argument)

        use proc = new Process()
        proc.StartInfo <- startInfo

        let standardOutput = StringBuilder()
        let standardError = StringBuilder()

        proc.OutputDataReceived.Add(fun args ->
            if not (isNull args.Data) then
                standardOutput.AppendLine(args.Data) |> ignore)

        proc.ErrorDataReceived.Add(fun args ->
            if not (isNull args.Data) then
                standardError.AppendLine(args.Data) |> ignore)

        proc.Start() |> ignore
        proc.BeginOutputReadLine()
        proc.BeginErrorReadLine()
        proc.WaitForExit()

        if proc.ExitCode = 0 then
            Ok(standardOutput.ToString().Trim())
        else
            let stderr = standardError.ToString().Trim()

            if String.IsNullOrWhiteSpace(stderr) then
                let renderedArguments = String.concat " " arguments
                Error $"git command failed in {repoRoot}: {renderedArguments}"
            else
                Error $"git command failed in {repoRoot}: {stderr}"

    let private normalizeRepoSlug (value: string) =
        let builder = StringBuilder()
        let mutable previousWasSeparator = false

        for character in value.Trim().ToLowerInvariant() do
            if Char.IsLetterOrDigit(character) then
                builder.Append(character) |> ignore
                previousWasSeparator <- false
            elif character = '-'
                 || character = '_'
                 || character = '.'
                 || character = ' '
                 || character = '/'
                 || character = '\\'
                 || character = ':' then
                if builder.Length > 0 && not previousWasSeparator then
                    builder.Append('-') |> ignore
                    previousWasSeparator <- true
            else
                ()

        let normalized = builder.ToString().Trim('-')

        if String.IsNullOrWhiteSpace(normalized) then
            Error $"Could not derive a valid repo slug from '{value}'."
        else
            Ok normalized

    let private tryNormalizeOptional value =
        if String.IsNullOrWhiteSpace(value) then
            None
        else
            Some value

    let private resolveGitHead repoRoot =
        match runGit repoRoot [ "rev-parse"; "--show-toplevel" ] with
        | Error message -> Error message
        | Ok topLevel ->
            let normalizedRepoRoot = Path.GetFullPath(topLevel)

            match runGit normalizedRepoRoot [ "rev-parse"; "--verify"; "HEAD" ] with
            | Error message -> Error message
            | Ok headCommit ->
                match runGit normalizedRepoRoot [ "show"; "-s"; "--format=%s"; "HEAD" ] with
                | Error message -> Error message
                | Ok commitSummary ->
                    match runGit normalizedRepoRoot [ "show"; "-s"; "--format=%B"; "HEAD" ] with
                    | Error message -> Error message
                    | Ok commitMessage ->
                        match runGit normalizedRepoRoot [ "branch"; "--show-current" ] with
                        | Error message -> Error message
                        | Ok branchRaw ->
                            let remoteOrigin =
                                match runGit normalizedRepoRoot [ "config"; "--get"; "remote.origin.url" ] with
                                | Ok value when not (String.IsNullOrWhiteSpace(value)) -> Some value
                                | _ -> None

                            let repoName = Path.GetFileName(normalizedRepoRoot)

                            match normalizeRepoSlug repoName with
                            | Error message -> Error message
                            | Ok repoSlug ->
                                Ok
                                    { RepoRoot = normalizedRepoRoot
                                      RepoSlug = repoSlug
                                      BranchName = tryNormalizeOptional branchRaw
                                      RemoteOrigin = remoteOrigin
                                      CommitSha = headCommit
                                      CommitShortSha = if headCommit.Length > 8 then headCommit.Substring(0, 8) else headCommit
                                      CommitSummary = commitSummary
                                      CommitMessage = commitMessage }

    let private buildSnapshotName (capturedAt: DateTimeOffset) repoSlug commitShortSha =
        let timestamp =
            capturedAt.ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ssZ", CultureInfo.InvariantCulture)

        $"{timestamp}__{repoSlug}__{commitShortSha}"

    /// <summary>
    /// Captures the current Codex local-session snapshot and links it to the current Git HEAD commit.
    /// </summary>
    /// <remarks>
    /// Full workflow notes: docs/how-to/capture-codex-commit-checkpoint.md
    /// </remarks>
    let run (request: CodexCommitCheckpointRequest) =
        match resolveGitHead request.RepoRoot with
        | Error message -> Error message
        | Ok gitHead ->
            let eventStoreRoot = Path.GetFullPath(request.EventStoreRoot)
            let objectsRoot = Path.GetFullPath(request.ObjectsRoot)
            let manifestRelativePath = CommitCheckpoints.manifestRelativePath gitHead.RepoSlug gitHead.CommitSha

            if CommitCheckpoints.exists eventStoreRoot gitHead.RepoSlug gitHead.CommitSha && not request.Force then
                Error
                    $"A Codex commit checkpoint already exists for {gitHead.RepoSlug}@{gitHead.CommitSha}. Rerun with --force to overwrite {manifestRelativePath}."
            else
                let capturedAt = DateTimeOffset.UtcNow
                let snapshotName = buildSnapshotName capturedAt gitHead.RepoSlug gitHead.CommitShortSha

                match
                    CodexSessionExport.run
                        { SourceRoot = request.CodexSourceRoot
                          ObjectsRoot = objectsRoot
                          SnapshotName = snapshotName }
                with
                | Error message -> Error message
                | Ok exportResult ->
                    let importRequest =
                        { SnapshotRoot = exportResult.ArchiveRoot
                          ObjectsRoot = objectsRoot
                          EventStoreRoot = eventStoreRoot }

                    let importResult = CodexImportWorkflow.run importRequest

                    let checkpoint : CommitCheckpoints.CodexCommitCheckpoint =
                        { RepoSlug = gitHead.RepoSlug
                          RepoRoot = gitHead.RepoRoot
                          BranchName = gitHead.BranchName
                          RemoteOrigin = gitHead.RemoteOrigin
                          CommitSha = gitHead.CommitSha
                          CommitShortSha = gitHead.CommitShortSha
                          CommitSummary = gitHead.CommitSummary
                          CommitMessage = gitHead.CommitMessage
                          CapturedAt = capturedAt
                          EventStoreRoot = eventStoreRoot
                          SnapshotName = snapshotName
                          SnapshotRoot = exportResult.ArchiveRoot
                          SnapshotManifestRelativePath = exportResult.ArchiveManifestRelativePath
                          RootArtifactRelativePath = importResult.RootArtifactRelativePath
                          ImportId = importResult.ImportId
                          ImportManifestRelativePath = importResult.ManifestRelativePath
                          WorkingGraphManifestRelativePath = importResult.WorkingGraphManifestRelativePath
                          WorkingGraphCatalogRelativePath = importResult.WorkingGraphCatalogRelativePath
                          WorkingGraphIndexRelativePath = importResult.WorkingGraphIndexRelativePath
                          Counts = importResult.Counts
                          Conversations =
                            importResult.ConversationSummaries
                            |> List.map (fun conversation ->
                                { ProviderConversationId = conversation.ProviderConversationId
                                  Title = conversation.Title
                                  MessageCountHint = conversation.MessageCountHint })
                          ManifestRelativePath = manifestRelativePath }

                    let checkpointManifestRelativePath = CommitCheckpoints.write eventStoreRoot checkpoint

                    Ok
                        { RepoRoot = gitHead.RepoRoot
                          RepoSlug = gitHead.RepoSlug
                          BranchName = gitHead.BranchName
                          RemoteOrigin = gitHead.RemoteOrigin
                          CommitSha = gitHead.CommitSha
                          CommitShortSha = gitHead.CommitShortSha
                          CommitSummary = gitHead.CommitSummary
                          CommitMessage = gitHead.CommitMessage
                          CapturedAt = capturedAt
                          SnapshotName = snapshotName
                          SnapshotRoot = exportResult.ArchiveRoot
                          SnapshotManifestRelativePath = exportResult.ArchiveManifestRelativePath
                          CheckpointManifestRelativePath = checkpointManifestRelativePath
                          ImportResult = importResult }

    /// <summary>
    /// Reports the durable checkpoint linked to a repo commit, defaulting to the current Git HEAD commit.
    /// </summary>
    let report eventStoreRoot repoRoot (commitSha: string option) =
        match resolveGitHead repoRoot with
        | Error message -> Error message
        | Ok gitHead ->
            let effectiveCommitSha =
                match commitSha |> Option.map (fun value -> value.Trim()) with
                | Some value when not (String.IsNullOrWhiteSpace(value)) -> value
                | _ -> gitHead.CommitSha

            let resolvedCommitSha =
                CommitCheckpoints.tryResolveCommitSha eventStoreRoot gitHead.RepoSlug effectiveCommitSha
                |> Option.defaultValue effectiveCommitSha

            let manifestRelativePath = CommitCheckpoints.manifestRelativePath gitHead.RepoSlug resolvedCommitSha

            match CommitCheckpoints.tryLoad eventStoreRoot gitHead.RepoSlug resolvedCommitSha with
            | Some checkpoint ->
                Ok
                    { RepoRoot = gitHead.RepoRoot
                      RepoSlug = gitHead.RepoSlug
                      CommitSha = resolvedCommitSha
                      ManifestRelativePath = manifestRelativePath
                      Checkpoint = checkpoint }
            | None ->
                Error
                    $"No Codex commit checkpoint was found for {gitHead.RepoSlug}@{effectiveCommitSha} under {normalizePath eventStoreRoot}."
