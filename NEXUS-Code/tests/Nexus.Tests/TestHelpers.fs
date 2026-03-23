namespace Nexus.Tests

open System
open System.IO
open System.IO.Compression
open System.Diagnostics
open System.Text
open Nexus.EventStore

[<RequireQualifiedAccess>]
module TestHelpers =
    type ProcessResult =
        { ExitCode: int
          StandardOutput: string
          StandardError: string }

    let fixturesRoot =
        Path.Combine(__SOURCE_DIRECTORY__, "fixtures")

    let fixturePath relativePath =
        Path.Combine(fixturesRoot, relativePath)

    let repoRoot =
        Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

    let cliAssemblyPath =
        Path.Combine(repoRoot, "NEXUS-Code", "src", "Nexus.Cli", "bin", "Debug", "net10.0", "Nexus.Cli.dll")

    let fileLength path =
        FileInfo(path).Length

    let withTempDirectory prefix action =
        let path =
            Path.Combine(Path.GetTempPath(), $"{prefix}-{Guid.NewGuid():N}")

        Directory.CreateDirectory(path) |> ignore

        try
            action path
        finally
            if Directory.Exists(path) then
                Directory.Delete(path, true)

    let rec copyDirectory sourceDirectory destinationDirectory =
        Directory.CreateDirectory(destinationDirectory) |> ignore

        for filePath in Directory.EnumerateFiles(sourceDirectory) do
            let destinationPath = Path.Combine(destinationDirectory, Path.GetFileName(filePath))
            File.Copy(filePath, destinationPath, true)

        for childDirectory in Directory.EnumerateDirectories(sourceDirectory) do
            let childName = Path.GetFileName(childDirectory)
            copyDirectory childDirectory (Path.Combine(destinationDirectory, childName))

    let copyFixtureDirectory relativePath destinationDirectory =
        copyDirectory (fixturePath relativePath) destinationDirectory

    let createZipFromFixture relativePath zipPath =
        let sourceDirectory = fixturePath relativePath

        if File.Exists(zipPath) then
            File.Delete(zipPath)

        ZipFile.CreateFromDirectory(sourceDirectory, zipPath)

    let readToml path =
        File.ReadAllText(path) |> TomlDocument.parse

    let runProcess workingDirectory executable arguments =
        let startInfo = ProcessStartInfo()
        startInfo.FileName <- executable
        startInfo.WorkingDirectory <- workingDirectory
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

        { ExitCode = proc.ExitCode
          StandardOutput = standardOutput.ToString()
          StandardError = standardError.ToString() }

    let runCli arguments =
        if not (File.Exists(cliAssemblyPath)) then
            failwithf "CLI assembly not found at %s" cliAssemblyPath

        runProcess repoRoot "dotnet" (cliAssemblyPath :: arguments)
