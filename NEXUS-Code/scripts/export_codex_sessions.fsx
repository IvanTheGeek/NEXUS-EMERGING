#!/usr/bin/env -S dotnet fsi

open System
open System.IO
open System.Security.Cryptography

type Config =
    { SourceRoot: string
      DestinationRoot: string
      SnapshotName: string
      DryRun: bool }

type ExportFile =
    { RelativePath: string
      SourcePath: string
      Kind: string
      SizeBytes: int64
      Sha256: string }

let repoRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", ".."))

let defaultSourceRoot =
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".codex")

let defaultDestinationRoot =
    Path.Combine(repoRoot, "NEXUS-Objects", "providers", "codex")

let timestampUtc () =
    DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH-mm-ssZ")

let usage () =
    printfn "Usage:"
    printfn "  dotnet fsi NEXUS-Code/scripts/export_codex_sessions.fsx -- [options]"
    printfn ""
    printfn "Options:"
    printfn "  --source-root <path>       Override Codex source root (default: ~/.codex)"
    printfn "  --destination-root <path>  Override destination root"
    printfn "  --snapshot-name <value>    Override archive snapshot name"
    printfn "  --dry-run                  Show what would be copied without writing files"
    printfn "  --help                     Show this help"

let rec parseArgs (config: Config) (args: string list) =
    match args with
    | [] -> Ok config
    | "--source-root" :: value :: rest ->
        parseArgs { config with SourceRoot = value } rest
    | "--destination-root" :: value :: rest ->
        parseArgs { config with DestinationRoot = value } rest
    | "--snapshot-name" :: value :: rest ->
        parseArgs { config with SnapshotName = value } rest
    | "--dry-run" :: rest ->
        parseArgs { config with DryRun = true } rest
    | "--help" :: _ ->
        usage ()
        Error 0
    | option :: _ ->
        eprintfn "Unknown argument: %s" option
        usage ()
        Error 1

let normalizeRelativePath (value: string) =
    value.Replace('\\', '/')

let sha256ForFile (path: string) =
    use stream = File.OpenRead(path)
    use hash = SHA256.Create()
    let bytes = hash.ComputeHash(stream)
    Convert.ToHexString(bytes).ToLowerInvariant()

let collectExportFiles (sourceRoot: string) =
    let sessionIndexPath = Path.Combine(sourceRoot, "session_index.jsonl")
    let sessionsRoot = Path.Combine(sourceRoot, "sessions")

    let indexFiles =
        if File.Exists(sessionIndexPath) then
            let info = FileInfo(sessionIndexPath)

            [ { RelativePath = "session_index.jsonl"
                SourcePath = sessionIndexPath
                Kind = "session_index"
                SizeBytes = info.Length
                Sha256 = sha256ForFile sessionIndexPath } ]
        else
            []

    let sessionFiles =
        if Directory.Exists(sessionsRoot) then
            Directory.EnumerateFiles(sessionsRoot, "*", SearchOption.AllDirectories)
            |> Seq.map (fun sourcePath ->
                let relativePath = Path.GetRelativePath(sourceRoot, sourcePath) |> normalizeRelativePath
                let info = FileInfo(sourcePath)

                { RelativePath = relativePath
                  SourcePath = sourcePath
                  Kind = "session_transcript"
                  SizeBytes = info.Length
                  Sha256 = sha256ForFile sourcePath })
            |> Seq.sortBy (fun file -> file.RelativePath)
            |> Seq.toList
        else
            []

    indexFiles @ sessionFiles

let ensureParentDirectory (path: string) =
    let parent = Path.GetDirectoryName(path)

    if not (String.IsNullOrWhiteSpace(parent)) then
        Directory.CreateDirectory(parent) |> ignore

let copyFile (dryRun: bool) (sourcePath: string) (destinationPath: string) =
    if dryRun then
        ()
    else
        ensureParentDirectory destinationPath
        File.Copy(sourcePath, destinationPath, true)

let tomlEscape (value: string) =
    value.Replace("\\", "\\\\").Replace("\"", "\\\"")

let writeManifest (dryRun: bool) (destinationPath: string) (config: Config) (files: ExportFile list) =
    let exportedAt = DateTimeOffset.UtcNow.ToString("O")
    let totalBytes = files |> List.sumBy (fun file -> file.SizeBytes)
    let sessionCount = files |> List.filter (fun file -> file.Kind = "session_transcript") |> List.length

    let lines =
        [ yield "schema_version = 1"
          yield "manifest_kind = \"codex_session_export\""
          yield $"snapshot_name = \"{tomlEscape config.SnapshotName}\""
          yield $"exported_at = \"{exportedAt}\""
          yield $"source_root = \"{tomlEscape config.SourceRoot}\""
          yield $"destination_root = \"{tomlEscape config.DestinationRoot}\""
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

    if dryRun then
        ()
    else
        ensureParentDirectory destinationPath
        File.WriteAllLines(destinationPath, lines)

let runExport (config: Config) =
    if not (Directory.Exists(config.SourceRoot)) then
        eprintfn "Source root does not exist: %s" config.SourceRoot
        1
    else
        let files = collectExportFiles config.SourceRoot

        if List.isEmpty files then
            eprintfn "No Codex session files were found under: %s" config.SourceRoot
            1
        else
            let latestRoot = Path.Combine(config.DestinationRoot, "latest")
            let archiveRoot = Path.Combine(config.DestinationRoot, "archive", config.SnapshotName)
            let sessionCount = files |> List.filter (fun file -> file.Kind = "session_transcript") |> List.length

            for file in files do
                let latestPath = Path.Combine(latestRoot, file.RelativePath)
                let archivePath = Path.Combine(archiveRoot, file.RelativePath)
                copyFile config.DryRun file.SourcePath latestPath
                copyFile config.DryRun file.SourcePath archivePath

            writeManifest
                config.DryRun
                (Path.Combine(latestRoot, "export-manifest.toml"))
                config
                files

            writeManifest
                config.DryRun
                (Path.Combine(archiveRoot, "export-manifest.toml"))
                config
                files

            printfn "Codex session export complete."
            printfn "  Source root: %s" config.SourceRoot
            printfn "  Destination root: %s" config.DestinationRoot
            printfn "  Snapshot: %s" config.SnapshotName
            printfn "  Files exported: %d" files.Length
            printfn "  Session transcripts: %d" sessionCount
            printfn "  Dry run: %b" config.DryRun
            0

let initialConfig =
    { SourceRoot = defaultSourceRoot
      DestinationRoot = defaultDestinationRoot
      SnapshotName = timestampUtc ()
      DryRun = false }

match parseArgs initialConfig (fsi.CommandLineArgs |> Array.skip 1 |> Array.toList) with
| Ok config ->
    runExport config |> exit
| Error code ->
    exit code
