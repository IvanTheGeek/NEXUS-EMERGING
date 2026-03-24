namespace Nexus.EventStore

open System
open System.Diagnostics
open System.IO

/// <summary>
/// The explicitly supported Graphviz layout engines for NEXUS rendering.
/// </summary>
type GraphvizEngine =
    | Dot
    | Sfdp

/// <summary>
/// The explicitly supported Graphviz output formats for NEXUS rendering.
/// </summary>
type GraphvizFormat =
    | Svg
    | Png

/// <summary>
/// Describes the result of rendering a DOT file through Graphviz.
/// </summary>
type GraphvizRenderResult =
    { InputPath: string
      OutputPath: string
      Engine: GraphvizEngine
      Format: GraphvizFormat }

/// <summary>
/// Renders existing DOT files through an allowlisted Graphviz engine and format.
/// </summary>
/// <remarks>
/// Full workflow notes: docs/how-to/render-graphviz-dot.md
/// </remarks>
[<RequireQualifiedAccess>]
module GraphvizRendering =
    let private engineExecutable =
        function
        | Dot -> "dot"
        | Sfdp -> "sfdp"

    let private engineName =
        function
        | Dot -> "dot"
        | Sfdp -> "sfdp"

    let private formatName =
        function
        | Svg -> "svg"
        | Png -> "png"

    let private defaultOutputPath inputPath format =
        let absoluteInputPath = Path.GetFullPath(inputPath)
        let directory = Path.GetDirectoryName(absoluteInputPath)
        let fileName = Path.GetFileNameWithoutExtension(absoluteInputPath)
        Path.Combine(directory, $"{fileName}.{formatName format}")

    let private defaultOutputFileName inputPath format =
        let absoluteInputPath = Path.GetFullPath(inputPath)
        let fileName = Path.GetFileNameWithoutExtension(absoluteInputPath)
        $"{fileName}.{formatName format}"

    /// <summary>
    /// Renders a DOT file into a visual format using an explicitly supported Graphviz engine.
    /// </summary>
    /// <param name="inputPath">Path to the source DOT file.</param>
    /// <param name="outputPath">Optional output path. Defaults to the DOT file path with the selected format extension.</param>
    /// <param name="engine">The allowlisted Graphviz engine to use.</param>
    /// <param name="format">The allowlisted output format to emit.</param>
    /// <returns>The rendered output details.</returns>
    let renderWithRoot inputPath outputPath outputRoot engine format =
        let absoluteInputPath = Path.GetFullPath(inputPath)

        if not (File.Exists(absoluteInputPath)) then
            raise (FileNotFoundException($"DOT file not found: {absoluteInputPath}", absoluteInputPath))

        let absoluteOutputPath =
            match outputPath, outputRoot with
            | Some explicitPath, None
            | Some explicitPath, Some _ -> Path.GetFullPath(explicitPath)
            | None, Some outputRootPath ->
                Path.Combine(Path.GetFullPath(outputRootPath), defaultOutputFileName absoluteInputPath format)
                |> Path.GetFullPath
            | None, None ->
                defaultOutputPath absoluteInputPath format
                |> Path.GetFullPath

        let outputDirectory = Path.GetDirectoryName(absoluteOutputPath)

        if not (String.IsNullOrWhiteSpace(outputDirectory)) then
            Directory.CreateDirectory(outputDirectory) |> ignore

        let startInfo = ProcessStartInfo()
        startInfo.FileName <- engineExecutable engine
        startInfo.UseShellExecute <- false
        startInfo.RedirectStandardOutput <- true
        startInfo.RedirectStandardError <- true
        startInfo.ArgumentList.Add($"-T{formatName format}")
        startInfo.ArgumentList.Add(absoluteInputPath)
        startInfo.ArgumentList.Add("-o")
        startInfo.ArgumentList.Add(absoluteOutputPath)

        use graphvizProcess = new Process()
        graphvizProcess.StartInfo <- startInfo
        graphvizProcess.Start() |> ignore
        let standardOutput = graphvizProcess.StandardOutput.ReadToEnd()
        let standardError = graphvizProcess.StandardError.ReadToEnd()
        graphvizProcess.WaitForExit()

        if graphvizProcess.ExitCode <> 0 then
            let message =
                [ if not (String.IsNullOrWhiteSpace(standardOutput)) then
                      $"stdout: {standardOutput.Trim()}"
                  if not (String.IsNullOrWhiteSpace(standardError)) then
                      $"stderr: {standardError.Trim()}" ]
                |> String.concat " | "

            raise (InvalidOperationException($"Graphviz render failed with exit code {graphvizProcess.ExitCode}. {message}".Trim()))

        if not (File.Exists(absoluteOutputPath)) then
            raise (InvalidOperationException($"Graphviz did not produce the expected output file: {absoluteOutputPath}"))

        { InputPath = absoluteInputPath
          OutputPath = absoluteOutputPath
          Engine = engine
          Format = format }

    let render inputPath outputPath engine format =
        renderWithRoot inputPath outputPath None engine format

    /// <summary>
    /// Returns the persisted string form of a supported Graphviz engine.
    /// </summary>
    let engineValue engine =
        engineName engine

    /// <summary>
    /// Returns the persisted string form of a supported Graphviz output format.
    /// </summary>
    let formatValue format =
        formatName format
