namespace Nexus.Tests

open System
open System.IO
open System.IO.Compression
open Nexus.EventStore

[<RequireQualifiedAccess>]
module TestHelpers =
    let fixturesRoot =
        Path.Combine(__SOURCE_DIRECTORY__, "fixtures")

    let fixturePath relativePath =
        Path.Combine(fixturesRoot, relativePath)

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
