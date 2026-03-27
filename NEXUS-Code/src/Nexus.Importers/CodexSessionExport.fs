namespace Nexus.Importers

open System
open System.Globalization
open System.IO
open System.Security.Cryptography
open Nexus.EventStore

[<RequireQualifiedAccess>]
module CodexSessionExport =
    type ExportRequest =
        { SourceRoot: string
          ObjectsRoot: string
          SnapshotName: string }

    type ExportedFile =
        { RelativePath: string
          Kind: string
          SizeBytes: int64
          Sha256: string }

    type ExportResult =
        { SourceRoot: string
          DestinationRoot: string
          SnapshotName: string
          LatestRoot: string
          ArchiveRoot: string
          LatestManifestRelativePath: string
          ArchiveManifestRelativePath: string
          Files: ExportedFile list }

    let private normalizePath (path: string) =
        path.Replace('\\', '/')

    let private tomlEscape (value: string) =
        value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t")

    let private timestampLiteral (value: DateTimeOffset) =
        value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")

    let private tryParseInt64 (value: string) =
        match Int64.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture) with
        | true, parsedValue -> Some parsedValue
        | _ -> None

    let private sha256ForFile path =
        use stream = File.OpenRead(path)
        SHA256.HashData(stream) |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

    let private tryLoadManifestFiles manifestPath =
        if not (File.Exists(manifestPath)) then
            []
        else
            let document = File.ReadAllText(manifestPath) |> TomlDocument.parse

            TomlDocument.tableArray "files" document
            |> List.choose (fun table ->
                match table.TryGetValue("relative_path"), table.TryGetValue("kind"), table.TryGetValue("size_bytes"), table.TryGetValue("sha256") with
                | (true, relativePath), (true, kind), (true, sizeBytesText), (true, sha256) ->
                    tryParseInt64 sizeBytesText
                    |> Option.map (fun sizeBytes ->
                        { RelativePath = relativePath
                          Kind = kind
                          SizeBytes = sizeBytes
                          Sha256 = sha256 })
                | _ -> None)

    let private collectExportFiles sourceRoot =
        let sessionIndexPath = Path.Combine(sourceRoot, "session_index.jsonl")
        let sessionsRoot = Path.Combine(sourceRoot, "sessions")

        let indexFiles =
            if File.Exists(sessionIndexPath) then
                let info = FileInfo(sessionIndexPath)

                [ { RelativePath = "session_index.jsonl"
                    Kind = "session_index"
                    SizeBytes = info.Length
                    Sha256 = sha256ForFile sessionIndexPath } ]
            else
                []

        let sessionFiles =
            if Directory.Exists(sessionsRoot) then
                Directory.EnumerateFiles(sessionsRoot, "*", SearchOption.AllDirectories)
                |> Seq.sort
                |> Seq.map (fun sourcePath ->
                    let relativePath = Path.GetRelativePath(sourceRoot, sourcePath) |> normalizePath
                    let info = FileInfo(sourcePath)

                    { RelativePath = relativePath
                      Kind = "session_transcript"
                      SizeBytes = info.Length
                      Sha256 = sha256ForFile sourcePath })
                |> Seq.toList
            else
                []

        indexFiles @ sessionFiles

    let private copyFileToRoot sourceRoot destinationRoot (file: ExportedFile) =
        let sourcePath = Path.Combine(sourceRoot, file.RelativePath)
        let destinationPath = Path.Combine(destinationRoot, file.RelativePath)
        let directory = Path.GetDirectoryName(destinationPath)

        if not (String.IsNullOrWhiteSpace(directory)) then
            Directory.CreateDirectory(directory) |> ignore

        File.Copy(sourcePath, destinationPath, true)

    let private deleteLatestFile latestRoot relativePath =
        let absolutePath = Path.Combine(latestRoot, relativePath)

        if File.Exists(absolutePath) then
            File.Delete(absolutePath)

    let private writeManifest destinationPath (request: ExportRequest) (files: ExportedFile list) =
        let exportedAt = DateTimeOffset.UtcNow
        let totalBytes = files |> List.sumBy (fun file -> file.SizeBytes)
        let sessionCount = files |> List.filter (fun file -> file.Kind = "session_transcript") |> List.length
        let lines =
            [ yield "schema_version = 1"
              yield "manifest_kind = \"codex_session_export\""
              yield $"snapshot_name = \"{tomlEscape request.SnapshotName}\""
              yield $"exported_at = \"{timestampLiteral exportedAt}\""
              yield $"source_root = \"{tomlEscape (Path.GetFullPath(request.SourceRoot))}\""
              yield $"destination_root = \"{tomlEscape (Path.GetFullPath(request.ObjectsRoot))}\""
              yield $"session_count = {sessionCount}"
              yield $"file_count = {files.Length}"
              yield $"total_bytes = {totalBytes}"
              yield ""
              for file in files do
                  yield "[[files]]"
                  yield $"relative_path = \"{tomlEscape file.RelativePath}\""
                  yield $"kind = \"{tomlEscape file.Kind}\""
                  yield $"size_bytes = {file.SizeBytes}"
                  yield $"sha256 = \"{file.Sha256}\""
                  yield "" ]

        let directory = Path.GetDirectoryName(destinationPath : string)

        if not (String.IsNullOrWhiteSpace(directory)) then
            Directory.CreateDirectory(directory) |> ignore

        File.WriteAllLines(destinationPath, lines)

    /// <summary>
    /// Copies the current Codex local-session export into latest and archive object-layer roots.
    /// </summary>
    /// <remarks>
    /// Full workflow notes: docs/how-to/export-codex-sessions.md
    /// </remarks>
    let run (request: ExportRequest) =
        let sourceRoot = Path.GetFullPath(request.SourceRoot)
        let objectsRoot = Path.GetFullPath(request.ObjectsRoot)

        if not (Directory.Exists(sourceRoot)) then
            Error $"Codex source root does not exist: {sourceRoot}"
        else
            let files = collectExportFiles sourceRoot

            if List.isEmpty files then
                Error $"No Codex session files were found under: {sourceRoot}"
            else
                let destinationRoot = Path.Combine(objectsRoot, "providers", "codex")
                let latestRoot = Path.Combine(destinationRoot, "latest")
                let archiveRoot = Path.Combine(destinationRoot, "archive", request.SnapshotName)
                let latestManifestPath = Path.Combine(latestRoot, "export-manifest.toml")
                let previousLatestFiles =
                    tryLoadManifestFiles latestManifestPath
                    |> List.map (fun file -> file.RelativePath, file)
                    |> Map.ofList

                let currentFilesByRelativePath =
                    files
                    |> List.map (fun file -> file.RelativePath, file)
                    |> Map.ofList

                let changedOrNewFiles =
                    files
                    |> List.filter (fun file ->
                        match Map.tryFind file.RelativePath previousLatestFiles with
                        | Some previousFile
                            when previousFile.Kind = file.Kind
                                 && previousFile.SizeBytes = file.SizeBytes
                                 && String.Equals(previousFile.Sha256, file.Sha256, StringComparison.OrdinalIgnoreCase) ->
                            false
                        | _ -> true)

                let changedOrNewRelativePaths =
                    changedOrNewFiles
                    |> List.map (fun file -> file.RelativePath)
                    |> Set.ofList

                let latestFilesToRefresh =
                    files
                    |> List.filter (fun file ->
                        changedOrNewRelativePaths.Contains(file.RelativePath)
                        || not (File.Exists(Path.Combine(latestRoot, file.RelativePath))))

                let staleLatestRelativePaths =
                    previousLatestFiles
                    |> Map.toSeq
                    |> Seq.map fst
                    |> Seq.filter (fun relativePath -> not (Map.containsKey relativePath currentFilesByRelativePath))
                    |> Seq.toList

                let archiveFiles =
                    files
                    |> List.filter (fun file ->
                        file.Kind = "session_index"
                        || changedOrNewRelativePaths.Contains(file.RelativePath))

                if previousLatestFiles.IsEmpty && Directory.Exists(latestRoot) then
                    Directory.Delete(latestRoot, true)

                Directory.CreateDirectory(latestRoot) |> ignore

                for file in latestFilesToRefresh do
                    copyFileToRoot sourceRoot latestRoot file

                for relativePath in staleLatestRelativePaths do
                    deleteLatestFile latestRoot relativePath

                for file in archiveFiles do
                    copyFileToRoot sourceRoot archiveRoot file

                let latestManifestRelativePath =
                    normalizePath (Path.Combine("providers", "codex", "latest", "export-manifest.toml"))

                let archiveManifestRelativePath =
                    normalizePath (Path.Combine("providers", "codex", "archive", request.SnapshotName, "export-manifest.toml"))

                writeManifest (Path.Combine(objectsRoot, latestManifestRelativePath)) request files
                writeManifest (Path.Combine(objectsRoot, archiveManifestRelativePath)) request archiveFiles

                Ok
                    { SourceRoot = sourceRoot
                      DestinationRoot = destinationRoot
                      SnapshotName = request.SnapshotName
                      LatestRoot = latestRoot
                      ArchiveRoot = archiveRoot
                      LatestManifestRelativePath = latestManifestRelativePath
                      ArchiveManifestRelativePath = archiveManifestRelativePath
                      Files = archiveFiles }
