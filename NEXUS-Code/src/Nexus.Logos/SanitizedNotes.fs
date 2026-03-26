namespace Nexus.Logos

open System
open System.IO
open System.Text

/// <summary>
/// The input used to create a derived sanitized LOGOS note from an existing intake note.
/// </summary>
/// <remarks>
/// This is an explicit derived sanitization step. It does not copy raw locators or raw source text forward.
/// Full workflow notes: docs/how-to/create-logos-sanitized-note.md
/// </remarks>
type CreateLogosSanitizedNoteRequest =
    { DocsRoot: string
      SourceSlug: string
      Slug: string
      Title: string
      SanitizationStatusId: SanitizationStatusId
      SensitivityId: SensitivityId option
      SharingScopeId: SharingScopeId option
      RetentionClassId: RetentionClassId option
      Summary: string option
      Tags: string list }

/// <summary>
/// The result of creating a derived sanitized LOGOS note.
/// </summary>
type CreateLogosSanitizedNoteResult =
    { OutputPath: string
      NormalizedSlug: string
      SourceNotePath: string
      EntryPool: LogosPool
      RightsContext: LogosRightsContext
      Policy: LogosHandlingPolicy }

[<RequireQualifiedAccess>]
module LogosSanitizedNotes =
    type private ParsedSeedNote =
        { NoteKind: string
          SourceInstanceId: SourceInstanceId option
          AccessContext: LogosAccessContext
          SourceSystemId: SourceSystemId
          IntakeChannelId: IntakeChannelId
          SignalKindId: SignalKindId
          Policy: LogosHandlingPolicy
          RightsContext: LogosRightsContext }

    let private utf8WithoutBom = UTF8Encoding(false)

    let private normalizeRequiredText (name: string) (value: string) =
        let normalized = value.Trim()

        if String.IsNullOrWhiteSpace(normalized) then
            invalidArg name $"{name} cannot be blank."

        normalized

    let private normalizeStableSlug (name: string) (value: string) =
        normalizeRequiredText name value |> StableSlugs.validate name

    let private normalizeOptionalText (value: string option) =
        match value with
        | Some text ->
            let normalized = text.Trim()

            if String.IsNullOrWhiteSpace(normalized) then
                None
            else
                Some normalized
        | None -> None

    let private tomlEscape (value: string) =
        value.Replace("\\", "\\\\").Replace("\"", "\\\"")

    let private markdownEscapeInline (value: string) =
        value.Replace("`", "\\`")

    let private appendTomlStringArray (builder: StringBuilder) key values =
        let rendered =
            values
            |> List.map (fun value -> sprintf "\"%s\"" (tomlEscape value))
            |> String.concat ", "

        builder.AppendLine(sprintf "%s = [%s]" key rendered) |> ignore

    let private tomlUnescape (value: string) =
        value.Replace("\\\"", "\"").Replace("\\\\", "\\")

    let private tryGetFrontMatterBlock (text: string) =
        let normalized = text.Replace("\r\n", "\n")
        let delimiter = "+++"

        if not (normalized.StartsWith(delimiter + "\n", StringComparison.Ordinal)) then
            None
        else
            let secondDelimiterIndex = normalized.IndexOf("\n+++\n", delimiter.Length + 1, StringComparison.Ordinal)

            if secondDelimiterIndex < 0 then
                None
            else
                let startIndex = delimiter.Length + 1
                let length = secondDelimiterIndex - startIndex
                Some(normalized.Substring(startIndex, length))

    let private tryReadFrontMatterString key (frontMatter: string) =
        let prefix = key + " = \""

        frontMatter.Split('\n', StringSplitOptions.None)
        |> Array.tryPick (fun rawLine ->
            let line = rawLine.Trim()

            if line.StartsWith(prefix, StringComparison.Ordinal) && line.EndsWith("\"", StringComparison.Ordinal) then
                let value = line.Substring(prefix.Length, line.Length - prefix.Length - 1)
                Some(tomlUnescape value)
            else
                None)

    let private requireFrontMatterString name frontMatter =
        match tryReadFrontMatterString name frontMatter with
        | Some value -> value
        | None -> invalidArg name $"Missing required front-matter key '{name}'."

    let private parseSeedNote sourceNotePath =
        let text = File.ReadAllText(sourceNotePath)

        let frontMatter =
            match tryGetFrontMatterBlock text with
            | Some value -> value
            | None -> invalidArg "sourceSlug" $"Missing valid TOML front matter in %s{sourceNotePath}."

        let noteKind = requireFrontMatterString "note_kind" frontMatter

        if noteKind <> "logos_intake_seed" then
            invalidArg "sourceSlug" $"Expected a logos_intake_seed source note, but found %s{noteKind}."

        let sourceInstanceId =
            tryReadFrontMatterString "source_instance" frontMatter
            |> Option.map SourceInstanceId.parse

        let accessContextId =
            tryReadFrontMatterString "access_context" frontMatter
            |> Option.map AccessContextId.parse
            |> Option.defaultValue LogosAccessContext.restrictedDefault.AccessContextId

        let acquisitionKindId =
            tryReadFrontMatterString "acquisition_kind" frontMatter
            |> Option.map AcquisitionKindId.parse
            |> Option.defaultValue LogosAccessContext.restrictedDefault.AcquisitionKindId

        let sourceSystemId = requireFrontMatterString "source_system" frontMatter |> SourceSystemId.parse
        let intakeChannelId = requireFrontMatterString "intake_channel" frontMatter |> IntakeChannelId.parse
        let signalKindId = requireFrontMatterString "signal_kind" frontMatter |> SignalKindId.parse
        let sensitivityId = requireFrontMatterString "sensitivity" frontMatter |> SensitivityId.parse
        let sharingScopeId = requireFrontMatterString "sharing_scope" frontMatter |> SharingScopeId.parse
        let sanitizationStatusId = requireFrontMatterString "sanitization_status" frontMatter |> SanitizationStatusId.parse
        let retentionClassId = requireFrontMatterString "retention_class" frontMatter |> RetentionClassId.parse
        let rightsPolicyId =
            tryReadFrontMatterString "rights_policy" frontMatter
            |> Option.map RightsPolicyId.parse
            |> Option.defaultValue LogosRightsContext.restrictedDefault.RightsPolicyId

        let attributionReference = tryReadFrontMatterString "attribution_reference" frontMatter

        { NoteKind = noteKind
          SourceInstanceId = sourceInstanceId
          AccessContext = LogosAccessContext.create sourceInstanceId accessContextId acquisitionKindId
          SourceSystemId = sourceSystemId
          IntakeChannelId = intakeChannelId
          SignalKindId = signalKindId
          Policy =
            LogosHandlingPolicy.create
                sensitivityId
                sharingScopeId
                sanitizationStatusId
                retentionClassId
          RightsContext = LogosRightsContext.create rightsPolicyId attributionReference }

    let private ensureDerivedSanitizationStatus (identifier: SanitizationStatusId) =
        match SanitizationStatusId.value identifier with
        | "redacted"
        | "anonymized"
        | "approved-for-sharing" -> ()
        | "raw" ->
            invalidArg "sanitizationStatus" "Derived sanitized notes must not use the raw sanitization status."
        | value ->
            invalidArg "sanitizationStatus" $"Unsupported derived sanitization status: %s{value}."

    let private determineDerivedPool policy rightsContext =
        match PublicSafePoolItem.tryCreate () policy rightsContext with
        | Ok _ -> LogosPool.PublicSafe
        | Error _ -> LogosPool.Private

    let private tryFindSourceNotePath docsRoot normalizedSourceSlug =
        let intakeRoot = Path.Combine(docsRoot, "logos-intake")

        if not (Directory.Exists(intakeRoot)) then
            None
        else
            match
                Directory.EnumerateFiles(intakeRoot, $"{normalizedSourceSlug}.md", SearchOption.AllDirectories)
                |> Seq.sort
                |> Seq.toList
            with
            | [ path ] -> Some path
            | _ -> None

    let private renderNote
        (normalizedSlug: string)
        (title: string)
        (summary: string option)
        (tags: string list)
        (sourceRelativePath: string)
        (sourceSlug: string)
        (sourceNote: ParsedSeedNote)
        (entryPool: LogosPool)
        (policy: LogosHandlingPolicy)
        (rightsContext: LogosRightsContext)
        (now: DateTimeOffset)
        =
        let builder = StringBuilder()

        builder.AppendLine("+++") |> ignore
        builder.AppendLine("note_kind = \"logos_intake_sanitized\"") |> ignore
        builder.AppendLine(sprintf "title = \"%s\"" (tomlEscape title)) |> ignore
        builder.AppendLine(sprintf "slug = \"%s\"" normalizedSlug) |> ignore
        builder.AppendLine("status = \"derived\"") |> ignore
        builder.AppendLine(sprintf "created_at = \"%s\"" (now.ToString("O"))) |> ignore
        builder.AppendLine(sprintf "updated_at = \"%s\"" (now.ToString("O"))) |> ignore
        builder.AppendLine(sprintf "derived_from = \"%s\"" (tomlEscape sourceRelativePath)) |> ignore
        builder.AppendLine(sprintf "source_slug = \"%s\"" sourceSlug) |> ignore
        builder.AppendLine(sprintf "source_note_kind = \"%s\"" sourceNote.NoteKind) |> ignore
        builder.AppendLine(sprintf "source_system = \"%s\"" (SourceSystemId.value sourceNote.SourceSystemId)) |> ignore
        sourceNote.SourceInstanceId
        |> Option.iter (fun sourceInstanceId ->
            builder.AppendLine(sprintf "source_instance = \"%s\"" (SourceInstanceId.value sourceInstanceId)) |> ignore)
        builder.AppendLine(sprintf "access_context = \"%s\"" (AccessContextId.value sourceNote.AccessContext.AccessContextId)) |> ignore
        builder.AppendLine(sprintf "acquisition_kind = \"%s\"" (AcquisitionKindId.value sourceNote.AccessContext.AcquisitionKindId)) |> ignore
        builder.AppendLine(sprintf "intake_channel = \"%s\"" (IntakeChannelId.value sourceNote.IntakeChannelId)) |> ignore
        builder.AppendLine(sprintf "signal_kind = \"%s\"" (SignalKindId.value sourceNote.SignalKindId)) |> ignore
        builder.AppendLine(sprintf "entry_pool = \"%s\"" (LogosPool.value entryPool)) |> ignore
        builder.AppendLine(sprintf "source_sensitivity = \"%s\"" (SensitivityId.value sourceNote.Policy.SensitivityId)) |> ignore
        builder.AppendLine(sprintf "source_sharing_scope = \"%s\"" (SharingScopeId.value sourceNote.Policy.SharingScopeId)) |> ignore
        builder.AppendLine(sprintf "source_sanitization_status = \"%s\"" (SanitizationStatusId.value sourceNote.Policy.SanitizationStatusId)) |> ignore
        builder.AppendLine(sprintf "source_retention_class = \"%s\"" (RetentionClassId.value sourceNote.Policy.RetentionClassId)) |> ignore
        builder.AppendLine(sprintf "sensitivity = \"%s\"" (SensitivityId.value policy.SensitivityId)) |> ignore
        builder.AppendLine(sprintf "sharing_scope = \"%s\"" (SharingScopeId.value policy.SharingScopeId)) |> ignore
        builder.AppendLine(sprintf "sanitization_status = \"%s\"" (SanitizationStatusId.value policy.SanitizationStatusId)) |> ignore
        builder.AppendLine(sprintf "retention_class = \"%s\"" (RetentionClassId.value policy.RetentionClassId)) |> ignore
        builder.AppendLine(sprintf "rights_policy = \"%s\"" (RightsPolicyId.value rightsContext.RightsPolicyId)) |> ignore
        rightsContext.AttributionReference
        |> Option.iter (fun attributionReference ->
            builder.AppendLine(sprintf "attribution_reference = \"%s\"" (tomlEscape attributionReference)) |> ignore)
        appendTomlStringArray builder "tags" tags
        builder.AppendLine("+++") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine(sprintf "# %s" title) |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("## Summary") |> ignore
        builder.AppendLine() |> ignore

        match summary with
        | Some value -> builder.AppendLine(value) |> ignore
        | None ->
            builder.AppendLine("Derived sanitized LOGOS note created. Replace this summary with the redacted or anonymized version intended for the new sharing scope.") |> ignore

        builder.AppendLine() |> ignore
        builder.AppendLine("## Derivation") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine(sprintf "- derived from: `%s`" (markdownEscapeInline sourceRelativePath)) |> ignore
        builder.AppendLine(sprintf "- source note kind: `%s`" (markdownEscapeInline sourceNote.NoteKind)) |> ignore
        builder.AppendLine("- raw locators and raw text stay in the restricted source intake note") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("## Source Classification") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine(sprintf "- source system: `%s`" (SourceSystemId.value sourceNote.SourceSystemId |> markdownEscapeInline)) |> ignore
        sourceNote.SourceInstanceId
        |> Option.iter (fun sourceInstanceId ->
            builder.AppendLine(sprintf "- source instance: `%s`" (SourceInstanceId.value sourceInstanceId |> markdownEscapeInline)) |> ignore)
        builder.AppendLine(sprintf "- access context: `%s`" (AccessContextId.value sourceNote.AccessContext.AccessContextId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- acquisition kind: `%s`" (AcquisitionKindId.value sourceNote.AccessContext.AcquisitionKindId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- intake channel: `%s`" (IntakeChannelId.value sourceNote.IntakeChannelId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- signal kind: `%s`" (SignalKindId.value sourceNote.SignalKindId |> markdownEscapeInline)) |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("## Source Handling Policy") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine(sprintf "- sensitivity: `%s`" (SensitivityId.value sourceNote.Policy.SensitivityId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- sharing scope: `%s`" (SharingScopeId.value sourceNote.Policy.SharingScopeId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- sanitization status: `%s`" (SanitizationStatusId.value sourceNote.Policy.SanitizationStatusId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- retention class: `%s`" (RetentionClassId.value sourceNote.Policy.RetentionClassId |> markdownEscapeInline)) |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("## Derived Handling Policy") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine(sprintf "- entry pool: `%s`" (LogosPool.value entryPool |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- sensitivity: `%s`" (SensitivityId.value policy.SensitivityId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- sharing scope: `%s`" (SharingScopeId.value policy.SharingScopeId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- sanitization status: `%s`" (SanitizationStatusId.value policy.SanitizationStatusId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- retention class: `%s`" (RetentionClassId.value policy.RetentionClassId |> markdownEscapeInline)) |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("## Rights") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine(sprintf "- rights policy: `%s`" (RightsPolicyId.value rightsContext.RightsPolicyId |> markdownEscapeInline)) |> ignore
        rightsContext.AttributionReference
        |> Option.iter (fun attributionReference ->
            builder.AppendLine(sprintf "- attribution reference: `%s`" (markdownEscapeInline attributionReference)) |> ignore)
        builder.AppendLine() |> ignore
        builder.AppendLine("## Sanitization Notes") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("- What was removed, masked, or generalized?") |> ignore
        builder.AppendLine("- What residual risk remains?") |> ignore
        builder.AppendLine("- Is this ready for the intended sharing scope?") |> ignore
        builder.ToString()

    /// <summary>
    /// Creates a derived sanitized LOGOS note under docs/logos-intake-derived/.
    /// </summary>
    let create request =
        try
            let normalizedSourceSlug = normalizeStableSlug "sourceSlug" request.SourceSlug
            let normalizedSlug = normalizeStableSlug "slug" request.Slug
            let title = normalizeRequiredText "title" request.Title
            let summary = normalizeOptionalText request.Summary
            let normalizedTags = request.Tags |> List.map (normalizeStableSlug "tag") |> List.distinct
            ensureDerivedSanitizationStatus request.SanitizationStatusId

            if request.SanitizationStatusId = KnownSanitizationStatuses.approvedForSharing
               && request.SharingScopeId.IsNone then
                Error "approved-for-sharing requires an explicit --sharing-scope."
            else
                match tryFindSourceNotePath request.DocsRoot normalizedSourceSlug with
                | None ->
                    Error(sprintf "Source LOGOS intake note '%s' was not found under docs/logos-intake/." normalizedSourceSlug)
                | Some sourceNotePath ->
                    let sourceNote = parseSeedNote sourceNotePath
                    let rightsContext = sourceNote.RightsContext
                    let policy =
                        LogosHandlingPolicy.create
                            (defaultArg request.SensitivityId sourceNote.Policy.SensitivityId)
                            (defaultArg request.SharingScopeId sourceNote.Policy.SharingScopeId)
                            request.SanitizationStatusId
                            (defaultArg request.RetentionClassId sourceNote.Policy.RetentionClassId)

                    let entryPool = determineDerivedPool policy rightsContext
                    let outputDirectory =
                        Path.Combine(request.DocsRoot, "logos-intake-derived", LogosPool.value entryPool)

                    let outputPath = Path.Combine(outputDirectory, $"{normalizedSlug}.md")

                    if File.Exists(outputPath) then
                        Error(sprintf "LOGOS sanitized note already exists at %s." outputPath)
                    else
                        Directory.CreateDirectory(outputDirectory) |> ignore
                        let now = DateTimeOffset.UtcNow
                        let sourceRelativePath = Path.GetRelativePath(request.DocsRoot, sourceNotePath).Replace('\\', '/')
                        let content =
                            renderNote
                                normalizedSlug
                                title
                                summary
                                normalizedTags
                                sourceRelativePath
                                normalizedSourceSlug
                                sourceNote
                                entryPool
                                policy
                                rightsContext
                                now
                        File.WriteAllText(outputPath, content, utf8WithoutBom)

                        Ok
                            { OutputPath = outputPath
                              NormalizedSlug = normalizedSlug
                              SourceNotePath = sourceNotePath
                              EntryPool = entryPool
                              RightsContext = rightsContext
                              Policy = policy }
        with :? ArgumentException as ex ->
            Error ex.Message
