namespace Nexus.Importers

open System
open System.Diagnostics
open System.IO
open System.Text

[<RequireQualifiedAccess>]
module CodexCommitCheckpointHooks =
    let private markerStart = "# >>> NEXUS CODEX COMMIT CHECKPOINT >>>"
    let private markerEnd = "# <<< NEXUS CODEX COMMIT CHECKPOINT <<<"
    let private managedLogRelativePath = "nexus-hooks/codex-commit-checkpoint.log"

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

    let private resolveRepoPaths repoRoot =
        match runGit repoRoot [ "rev-parse"; "--show-toplevel" ] with
        | Error message -> Error message
        | Ok topLevel ->
            let normalizedRepoRoot = Path.GetFullPath(topLevel)

            match runGit normalizedRepoRoot [ "rev-parse"; "--git-dir" ] with
            | Error message -> Error message
            | Ok gitDirRaw ->
                let gitDirectory =
                    if Path.IsPathRooted(gitDirRaw) then
                        Path.GetFullPath(gitDirRaw)
                    else
                        Path.GetFullPath(Path.Combine(normalizedRepoRoot, gitDirRaw))

                Ok(normalizedRepoRoot, gitDirectory)

    let private shellEscape (value: string) =
        "'" + value.Replace("'", "'\"'\"'") + "'"

    let private renderManagedBlock request =
        let cliProjectPath =
            Path.Combine(
                Path.GetFullPath(request.NexusRepoRoot),
                "NEXUS-Code",
                "src",
                "Nexus.Cli",
                "Nexus.Cli.fsproj")

        let lines =
            [ markerStart
              "if command -v dotnet >/dev/null 2>&1; then"
              "  NEXUS_REPO_ROOT=$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
              "  NEXUS_GIT_DIR=$(git rev-parse --git-dir 2>/dev/null || printf '.git')"
              "  NEXUS_HOOK_LOG_DIR=\"$NEXUS_GIT_DIR/nexus-hooks\""
              "  mkdir -p \"$NEXUS_HOOK_LOG_DIR\""
              "  NEXUS_HOOK_LOG_PATH=\"$NEXUS_HOOK_LOG_DIR/codex-commit-checkpoint.log\""
              $"  if ! dotnet run --project {shellEscape cliProjectPath} -- capture-codex-commit-checkpoint --repo-root \"$NEXUS_REPO_ROOT\" --source-root {shellEscape (Path.GetFullPath(request.CodexSourceRoot))} --objects-root {shellEscape (Path.GetFullPath(request.ObjectsRoot))} --event-store-root {shellEscape (Path.GetFullPath(request.EventStoreRoot))} >>\"$NEXUS_HOOK_LOG_PATH\" 2>&1; then"
              "    printf '%s\\n' \"Codex commit checkpoint capture failed. See $NEXUS_HOOK_LOG_PATH\" >&2"
              "  fi"
              "else"
              "  printf '%s\\n' \"dotnet is not installed; Codex commit checkpoint hook skipped.\" >&2"
              "fi"
              markerEnd ]

        String.concat Environment.NewLine lines

    let private mergeManagedBlock (existingText: string) (managedBlock: string) =
        let startIndex = existingText.IndexOf(markerStart, StringComparison.Ordinal)
        let endIndex = existingText.IndexOf(markerEnd, StringComparison.Ordinal)

        if startIndex >= 0 && endIndex >= startIndex then
            let endMarkerInclusive = endIndex + markerEnd.Length
            let before = existingText.Substring(0, startIndex).TrimEnd()
            let after = existingText.Substring(endMarkerInclusive).TrimStart()

            let merged =
                [ if not (String.IsNullOrWhiteSpace(before)) then
                      yield before
                  yield managedBlock
                  if not (String.IsNullOrWhiteSpace(after)) then
                      yield after ]
                |> String.concat (Environment.NewLine + Environment.NewLine)

            merged, false, true
        else
            let trimmed = existingText.TrimEnd()

            if String.IsNullOrWhiteSpace(trimmed) then
                "#!/usr/bin/env bash"
                + Environment.NewLine
                + Environment.NewLine
                + managedBlock
                + Environment.NewLine,
                true,
                false
            else
                trimmed + Environment.NewLine + Environment.NewLine + managedBlock + Environment.NewLine,
                true,
                false

    let private ensureExecutable (path: string) =
        if not (OperatingSystem.IsWindows()) then
            let startInfo = ProcessStartInfo()
            startInfo.FileName <- "chmod"
            startInfo.UseShellExecute <- false
            startInfo.RedirectStandardError <- true
            startInfo.ArgumentList.Add("+x")
            startInfo.ArgumentList.Add(path)

            use proc = new Process()
            proc.StartInfo <- startInfo
            proc.Start() |> ignore
            proc.WaitForExit()

            if proc.ExitCode <> 0 then
                let stderr = proc.StandardError.ReadToEnd()
                invalidOp $"chmod +x failed for {path}: {stderr}"

    /// <summary>
    /// Installs or refreshes the managed post-commit hook block that captures Codex commit checkpoints.
    /// </summary>
    /// <remarks>
    /// Full workflow notes: docs/how-to/install-codex-commit-checkpoint-hook.md
    /// </remarks>
    let install (request: CodexCommitCheckpointHookInstallRequest) =
        match resolveRepoPaths request.RepoRoot with
        | Error message -> Error message
        | Ok (repoRoot, gitDirectory) ->
            let hookPath = Path.Combine(gitDirectory, "hooks", "post-commit")
            let existingText =
                if File.Exists(hookPath) then
                    File.ReadAllText(hookPath)
                else
                    String.Empty

            let managedBlock = renderManagedBlock request
            let mergedText, insertedManagedBlock, updatedManagedBlock = mergeManagedBlock existingText managedBlock
            let createdHookFile = not (File.Exists(hookPath))
            let hookDirectory = Path.GetDirectoryName(hookPath)

            if not (String.IsNullOrWhiteSpace(hookDirectory)) then
                Directory.CreateDirectory(hookDirectory) |> ignore

            File.WriteAllText(hookPath, mergedText)
            ensureExecutable hookPath

            let cliProjectPath =
                Path.Combine(
                    Path.GetFullPath(request.NexusRepoRoot),
                    "NEXUS-Code",
                    "src",
                    "Nexus.Cli",
                    "Nexus.Cli.fsproj")

            let commandPreview =
                $"dotnet run --project {cliProjectPath} -- capture-codex-commit-checkpoint --repo-root <git-head-repo> --source-root {Path.GetFullPath(request.CodexSourceRoot)} --objects-root {Path.GetFullPath(request.ObjectsRoot)} --event-store-root {Path.GetFullPath(request.EventStoreRoot)}"

            Ok
                { RepoRoot = repoRoot
                  GitDirectory = gitDirectory
                  HookPath = hookPath
                  CreatedHookFile = createdHookFile
                  UpdatedManagedBlock = updatedManagedBlock
                  InsertedManagedBlock = insertedManagedBlock
                  HookLogRelativePath = managedLogRelativePath
                  CommandPreview = commandPreview }
