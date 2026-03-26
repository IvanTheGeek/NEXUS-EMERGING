namespace Nexus.Logos

open System
open System.IO
open System.Text

/// <summary>
/// One sanitized LOGOS note exported into a public-safe output set.
/// </summary>
type LogosPublicExportedNote =
    { Slug: string
      Title: string
      SourceRelativePath: string
      OutputFileName: string
      Policy: LogosHandlingPolicy }

/// <summary>
/// One sanitized LOGOS note skipped during public-safe export.
/// </summary>
type LogosPublicSkippedNote =
    { RelativePath: string
      Slug: string
      Title: string
      Reason: string }

/// <summary>
/// The result of exporting public-safe LOGOS notes.
/// </summary>
type LogosPublicExportResult =
    { DocsRoot: string
      OutputRoot: string
      ManifestPath: string
      SanitizedNotesScanned: int
      ExportedNotes: LogosPublicExportedNote list
      SkippedNotes: LogosPublicSkippedNote list
      ExportedAt: DateTimeOffset }

/// <summary>
/// Exports explicitly public-safe LOGOS notes into a dedicated output folder.
/// </summary>
/// <remarks>
/// This is a real boundary-crossing workflow: only notes that promote into
/// <see cref="T:Nexus.Logos.PublicSafePoolItem`1" /> are exported.
/// Full notes: docs/decisions/0012-pool-based-handling-boundaries.md
/// </remarks>
[<RequireQualifiedAccess>]
module LogosPublicExports =
    let private utf8WithoutBom = UTF8Encoding(false)

    let private normalizeRequiredPath (name: string) (value: string) =
        let normalized = value.Trim()

        if String.IsNullOrWhiteSpace(normalized) then
            invalidArg name $"{name} cannot be blank."

        Path.GetFullPath(normalized)

    let private tomlEscape (value: string) =
        value.Replace("\\", "\\\\").Replace("\"", "\\\"")

    let private appendTomlStringArray (builder: StringBuilder) key values =
        let rendered =
            values
            |> List.map (fun value -> sprintf "\"%s\"" (tomlEscape value))
            |> String.concat ", "

        builder.AppendLine(sprintf "%s = [%s]" key rendered) |> ignore

    let private renderManifest (result: LogosPublicExportResult) =
        let builder = StringBuilder()

        builder.AppendLine("schema_version = 1") |> ignore
        builder.AppendLine("manifest_kind = \"logos_public_export\"") |> ignore
        builder.AppendLine(sprintf "exported_at = \"%s\"" (result.ExportedAt.ToString("O"))) |> ignore
        builder.AppendLine(sprintf "docs_root = \"%s\"" (tomlEscape result.DocsRoot)) |> ignore
        builder.AppendLine(sprintf "output_root = \"%s\"" (tomlEscape result.OutputRoot)) |> ignore
        builder.AppendLine(sprintf "sanitized_notes_scanned = %d" result.SanitizedNotesScanned) |> ignore
        builder.AppendLine(sprintf "exported_notes = %d" result.ExportedNotes.Length) |> ignore
        builder.AppendLine(sprintf "skipped_notes = %d" result.SkippedNotes.Length) |> ignore
        builder.AppendLine() |> ignore

        for note in result.ExportedNotes do
            builder.AppendLine("[[exported_note]]") |> ignore
            builder.AppendLine(sprintf "slug = \"%s\"" note.Slug) |> ignore
            builder.AppendLine(sprintf "title = \"%s\"" (tomlEscape note.Title)) |> ignore
            builder.AppendLine(sprintf "source_relative_path = \"%s\"" (tomlEscape note.SourceRelativePath)) |> ignore
            builder.AppendLine(sprintf "output_file = \"%s\"" (tomlEscape note.OutputFileName)) |> ignore
            builder.AppendLine(sprintf "sensitivity = \"%s\"" (SensitivityId.value note.Policy.SensitivityId)) |> ignore
            builder.AppendLine(sprintf "sharing_scope = \"%s\"" (SharingScopeId.value note.Policy.SharingScopeId)) |> ignore
            builder.AppendLine(sprintf "sanitization_status = \"%s\"" (SanitizationStatusId.value note.Policy.SanitizationStatusId)) |> ignore
            builder.AppendLine(sprintf "retention_class = \"%s\"" (RetentionClassId.value note.Policy.RetentionClassId)) |> ignore
            builder.AppendLine() |> ignore

        for note in result.SkippedNotes do
            builder.AppendLine("[[skipped_note]]") |> ignore
            builder.AppendLine(sprintf "relative_path = \"%s\"" (tomlEscape note.RelativePath)) |> ignore
            builder.AppendLine(sprintf "slug = \"%s\"" note.Slug) |> ignore
            builder.AppendLine(sprintf "title = \"%s\"" (tomlEscape note.Title)) |> ignore
            builder.AppendLine(sprintf "reason = \"%s\"" (tomlEscape note.Reason)) |> ignore
            builder.AppendLine() |> ignore

        builder.ToString()

    /// <summary>
    /// Exports public-safe LOGOS notes from docs/logos-intake-derived/ into a dedicated output root.
    /// </summary>
    let export docsRoot outputRoot =
        try
            let normalizedDocsRoot = normalizeRequiredPath "docsRoot" docsRoot
            let normalizedOutputRoot = normalizeRequiredPath "outputRoot" outputRoot

            match LogosHandlingReports.build normalizedDocsRoot with
            | Error error -> Error error
            | Ok report ->
                let sanitizedNotes =
                    report.Notes
                    |> List.filter (fun note ->
                        note.NoteKind = "logos_intake_sanitized"
                        && note.RelativePath.StartsWith("logos-intake-derived/", StringComparison.Ordinal))

                Directory.CreateDirectory(normalizedOutputRoot) |> ignore

                let exportedNotes, skippedNotes =
                    sanitizedNotes
                    |> List.fold
                        (fun (exported, skipped) note ->
                            match PublicSafePoolItem.tryCreate note note.Policy with
                            | Ok publicSafeNote ->
                                let exportNote = PublicSafePoolItem.value publicSafeNote
                                let sourcePath = Path.Combine(normalizedDocsRoot, exportNote.RelativePath.Replace('/', Path.DirectorySeparatorChar))
                                let outputFileName = $"{exportNote.Slug}.md"
                                let outputPath = Path.Combine(normalizedOutputRoot, outputFileName)

                                if File.Exists(sourcePath) then
                                    File.Copy(sourcePath, outputPath, true)

                                    let exportedNote =
                                        { Slug = exportNote.Slug
                                          Title = exportNote.Title
                                          SourceRelativePath = exportNote.RelativePath
                                          OutputFileName = outputFileName
                                          Policy = exportNote.Policy }

                                    (exportedNote :: exported, skipped)
                                else
                                    let skippedNote =
                                        { RelativePath = exportNote.RelativePath
                                          Slug = exportNote.Slug
                                          Title = exportNote.Title
                                          Reason = "Source note file is missing." }

                                    (exported, skippedNote :: skipped)
                            | Error reason ->
                                let skippedNote =
                                    { RelativePath = note.RelativePath
                                      Slug = note.Slug
                                      Title = note.Title
                                      Reason = reason }

                                (exported, skippedNote :: skipped))
                        ([], [])

                let now = DateTimeOffset.UtcNow

                let result =
                    { DocsRoot = normalizedDocsRoot
                      OutputRoot = normalizedOutputRoot
                      ManifestPath = Path.Combine(normalizedOutputRoot, "manifest.toml")
                      SanitizedNotesScanned = sanitizedNotes.Length
                      ExportedNotes = exportedNotes |> List.rev
                      SkippedNotes = skippedNotes |> List.rev
                      ExportedAt = now }

                File.WriteAllText(result.ManifestPath, renderManifest result, utf8WithoutBom)
                Ok result
        with :? ArgumentException as ex ->
            Error ex.Message
