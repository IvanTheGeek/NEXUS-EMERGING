namespace Nexus.Logos

open System
open System.IO
open System.Text

/// <summary>
/// One sanitized LOGOS note exported into a public-safe output set after handling and rights checks.
/// </summary>
type LogosPublicExportedNote =
    { Slug: string
      Title: string
      SourceRelativePath: string
      OutputFileName: string
      RightsContext: LogosRightsContext
      Policy: LogosHandlingPolicy }

/// <summary>
/// One attribution obligation carried by a public-safe export.
/// </summary>
type LogosPublicAttributionRequirement =
    { Slug: string
      Title: string
      OutputFileName: string
      RightsPolicyId: RightsPolicyId
      AttributionReference: string }

/// <summary>
/// One sanitized LOGOS note skipped during public-safe export.
/// </summary>
type LogosPublicSkippedNote =
    { RelativePath: string
      Slug: string
      Title: string
      Reason: string }

/// <summary>
/// The result of exporting public-safe LOGOS notes, including carried attribution obligations.
/// </summary>
type LogosPublicExportResult =
    { DocsRoot: string
      OutputRoot: string
      ManifestPath: string
      EligibleNotesScanned: int
      ExportedNotes: LogosPublicExportedNote list
      AttributionRequirements: LogosPublicAttributionRequirement list
      SkippedNotes: LogosPublicSkippedNote list
      ExportedAt: DateTimeOffset }

/// <summary>
/// Exports explicitly public-safe LOGOS notes into a dedicated output folder.
/// </summary>
/// <remarks>
/// This is a real boundary-crossing workflow: only notes that promote into
/// <see cref="T:Nexus.Logos.PublicSafePoolItem`1" /> with rights that also allow public distribution are exported.
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
        builder.AppendLine(sprintf "eligible_notes_scanned = %d" result.EligibleNotesScanned) |> ignore
        builder.AppendLine(sprintf "exported_notes = %d" result.ExportedNotes.Length) |> ignore
        builder.AppendLine(sprintf "attribution_requirements = %d" result.AttributionRequirements.Length) |> ignore
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
            builder.AppendLine(sprintf "rights_policy = \"%s\"" (RightsPolicyId.value note.RightsContext.RightsPolicyId)) |> ignore
            note.RightsContext.AttributionReference
            |> Option.iter (fun attributionReference ->
                builder.AppendLine(sprintf "attribution_reference = \"%s\"" (tomlEscape attributionReference)) |> ignore)
            builder.AppendLine() |> ignore

        for requirement in result.AttributionRequirements do
            builder.AppendLine("[[attribution_requirement]]") |> ignore
            builder.AppendLine(sprintf "slug = \"%s\"" requirement.Slug) |> ignore
            builder.AppendLine(sprintf "title = \"%s\"" (tomlEscape requirement.Title)) |> ignore
            builder.AppendLine(sprintf "output_file = \"%s\"" (tomlEscape requirement.OutputFileName)) |> ignore
            builder.AppendLine(sprintf "rights_policy = \"%s\"" (RightsPolicyId.value requirement.RightsPolicyId)) |> ignore
            builder.AppendLine(sprintf "attribution_reference = \"%s\"" (tomlEscape requirement.AttributionReference)) |> ignore
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
    /// Exports public-safe LOGOS notes into a dedicated output root and records attribution obligations in the manifest.
    /// </summary>
    let export docsRoot outputRoot =
        try
            let normalizedDocsRoot = normalizeRequiredPath "docsRoot" docsRoot
            let normalizedOutputRoot = normalizeRequiredPath "outputRoot" outputRoot

            match LogosHandlingReports.build normalizedDocsRoot with
            | Error error -> Error error
            | Ok report ->
                let eligibleNotes =
                    report.Notes
                    |> List.filter (fun note ->
                        note.RelativePath.StartsWith("logos-intake/", StringComparison.Ordinal)
                        || note.RelativePath.StartsWith("logos-intake-derived/", StringComparison.Ordinal))

                Directory.CreateDirectory(normalizedOutputRoot) |> ignore

                let exportedNotes, skippedNotes =
                    eligibleNotes
                    |> List.fold
                        (fun (exported, skipped) note ->
                            match note.RightsPolicyId with
                            | None ->
                                let skippedNote =
                                    { RelativePath = note.RelativePath
                                      Slug = note.Slug
                                      Title = note.Title
                                      Reason = "Public export requires an explicit rights_policy." }

                                (exported, skippedNote :: skipped)
                            | Some rightsPolicyId ->
                                let rightsContext = LogosRightsContext.create rightsPolicyId note.AttributionReference

                                match PublicSafePoolItem.tryCreate note note.Policy rightsContext with
                                | Ok publicSafeNote ->
                                    let exportNote = PublicSafePoolItem.value publicSafeNote
                                    let exportRights = PublicSafePoolItem.rights publicSafeNote
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
                                              RightsContext = exportRights
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

                let attributionRequirements =
                    exportedNotes
                    |> List.choose (fun note ->
                        if KnownRightsPolicies.requiresAttribution note.RightsContext.RightsPolicyId then
                            note.RightsContext.AttributionReference
                            |> Option.map (fun attributionReference ->
                                { Slug = note.Slug
                                  Title = note.Title
                                  OutputFileName = note.OutputFileName
                                  RightsPolicyId = note.RightsContext.RightsPolicyId
                                  AttributionReference = attributionReference })
                        else
                            None)

                let now = DateTimeOffset.UtcNow

                let result =
                    { DocsRoot = normalizedDocsRoot
                      OutputRoot = normalizedOutputRoot
                      ManifestPath = Path.Combine(normalizedOutputRoot, "manifest.toml")
                      EligibleNotesScanned = eligibleNotes.Length
                      ExportedNotes = exportedNotes |> List.rev
                      AttributionRequirements = attributionRequirements |> List.rev
                      SkippedNotes = skippedNotes |> List.rev
                      ExportedAt = now }

                File.WriteAllText(result.ManifestPath, renderManifest result, utf8WithoutBom)
                Ok result
        with :? ArgumentException as ex ->
            Error ex.Message
