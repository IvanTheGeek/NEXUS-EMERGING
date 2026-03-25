namespace Nexus.Logos

open System
open System.IO
open System.Text

/// <summary>
/// The input used to create a durable LOGOS intake seed note.
/// </summary>
/// <remarks>
/// Full workflow notes: docs/how-to/create-logos-intake-note.md
/// </remarks>
type CreateLogosIntakeNoteRequest =
    { DocsRoot: string
      Slug: string
      Title: string
      SourceSystemId: SourceSystemId
      IntakeChannelId: IntakeChannelId
      SignalKindId: SignalKindId
      Policy: LogosHandlingPolicy
      Locators: LogosLocator list
      CapturedAt: DateTimeOffset option
      Summary: string option
      Tags: string list }

/// <summary>
/// The result of creating a durable LOGOS intake seed note.
/// </summary>
type CreateLogosIntakeNoteResult =
    { OutputPath: string
      NormalizedSlug: string
      Signal: LogosSignal
      Policy: LogosHandlingPolicy }

/// <summary>
/// Creates durable LOGOS intake seed notes from explicit source classifications and locators.
/// </summary>
[<RequireQualifiedAccess>]
module LogosIntakeNotes =
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

    let private locatorLabel locator =
        match locator with
        | NativeItemId value -> "native-item-id", value
        | NativeThreadId value -> "native-thread-id", value
        | NativeMessageId value -> "native-message-id", value
        | SourceUri value -> "source-uri", value

    let private renderNote
        (normalizedSlug: string)
        (title: string)
        (tags: string list)
        (signal: LogosSignal)
        (policy: LogosHandlingPolicy)
        (now: DateTimeOffset)
        =
        let builder = StringBuilder()
        let source = LogosSignal.source signal
        let locators = LogosSourceRef.locators source

        builder.AppendLine("+++") |> ignore
        builder.AppendLine("note_kind = \"logos_intake_seed\"") |> ignore
        builder.AppendLine(sprintf "title = \"%s\"" (tomlEscape title)) |> ignore
        builder.AppendLine(sprintf "slug = \"%s\"" normalizedSlug) |> ignore
        builder.AppendLine("status = \"seed\"") |> ignore
        builder.AppendLine(sprintf "created_at = \"%s\"" (now.ToString("O"))) |> ignore
        builder.AppendLine(sprintf "updated_at = \"%s\"" (now.ToString("O"))) |> ignore
        builder.AppendLine(sprintf "source_system = \"%s\"" (LogosSourceRef.sourceSystemId source |> SourceSystemId.value)) |> ignore
        builder.AppendLine(sprintf "intake_channel = \"%s\"" (LogosSourceRef.intakeChannelId source |> IntakeChannelId.value)) |> ignore
        builder.AppendLine(sprintf "signal_kind = \"%s\"" (LogosSignal.signalKindId signal |> SignalKindId.value)) |> ignore
        builder.AppendLine(sprintf "sensitivity = \"%s\"" (SensitivityId.value policy.SensitivityId)) |> ignore
        builder.AppendLine(sprintf "sharing_scope = \"%s\"" (SharingScopeId.value policy.SharingScopeId)) |> ignore
        builder.AppendLine(sprintf "sanitization_status = \"%s\"" (SanitizationStatusId.value policy.SanitizationStatusId)) |> ignore
        builder.AppendLine(sprintf "retention_class = \"%s\"" (RetentionClassId.value policy.RetentionClassId)) |> ignore

        LogosSignal.capturedAt signal
        |> Option.iter (fun capturedAt ->
            builder.AppendLine(sprintf "captured_at = \"%s\"" (capturedAt.ToString("O"))) |> ignore)

        appendTomlStringArray
            builder
            "locators"
            (locators
             |> List.map (fun locator ->
                 let kind, value = locatorLabel locator
                 $"{kind}:{value}"))

        appendTomlStringArray builder "tags" tags
        builder.AppendLine("+++") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine(sprintf "# %s" title) |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("## Summary") |> ignore
        builder.AppendLine() |> ignore

        match LogosSignal.summary signal with
        | Some summary ->
            builder.AppendLine(summary) |> ignore
        | None ->
            builder.AppendLine("Seed LOGOS intake note created from explicit source metadata. Refine this note as the intake becomes better understood.") |> ignore

        builder.AppendLine() |> ignore
        builder.AppendLine("## Source") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine(sprintf "- source system: `%s`" (LogosSourceRef.sourceSystemId source |> SourceSystemId.value |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- intake channel: `%s`" (LogosSourceRef.intakeChannelId source |> IntakeChannelId.value |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- signal kind: `%s`" (LogosSignal.signalKindId signal |> SignalKindId.value |> markdownEscapeInline)) |> ignore

        LogosSignal.capturedAt signal
        |> Option.iter (fun capturedAt ->
            builder.AppendLine(sprintf "- captured at: `%s`" (capturedAt.ToString("O"))) |> ignore)

        if not tags.IsEmpty then
            builder.AppendLine(sprintf "- tags: `%s`" (String.concat "`, `" (tags |> List.map markdownEscapeInline))) |> ignore

        builder.AppendLine() |> ignore
        builder.AppendLine("## Handling Policy") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine(sprintf "- sensitivity: `%s`" (SensitivityId.value policy.SensitivityId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- sharing scope: `%s`" (SharingScopeId.value policy.SharingScopeId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- sanitization status: `%s`" (SanitizationStatusId.value policy.SanitizationStatusId |> markdownEscapeInline)) |> ignore
        builder.AppendLine(sprintf "- retention class: `%s`" (RetentionClassId.value policy.RetentionClassId |> markdownEscapeInline)) |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("## Locators") |> ignore
        builder.AppendLine() |> ignore

        locators
        |> List.iter (fun locator ->
            let kind, value = locatorLabel locator
            builder.AppendLine(sprintf "- `%s`: `%s`" kind (markdownEscapeInline value)) |> ignore)

        builder.AppendLine() |> ignore
        builder.AppendLine("## Working Notes") |> ignore
        builder.AppendLine() |> ignore
        builder.AppendLine("- What does this signal mean inside LOGOS?") |> ignore
        builder.AppendLine("- Is this best understood as raw intake, refined knowledge, or doctrine?") |> ignore
        builder.AppendLine("- Which later canonical or derived flows should this connect to?") |> ignore
        builder.ToString()

    /// <summary>
    /// Creates a durable LOGOS intake seed note under docs/logos-intake/.
    /// </summary>
    let create request =
        try
            let normalizedSlug = normalizeStableSlug "slug" request.Slug
            let title = normalizeRequiredText "title" request.Title
            let summary = normalizeOptionalText request.Summary
            let normalizedTags = request.Tags |> List.map (normalizeStableSlug "tag") |> List.distinct
            let policy = request.Policy
            let source = LogosSourceRef.create request.SourceSystemId request.IntakeChannelId request.Locators
            let signal = LogosSignal.create request.SignalKindId source request.CapturedAt (Some title) summary
            let outputDirectory = Path.Combine(request.DocsRoot, "logos-intake")
            let outputPath = Path.Combine(outputDirectory, $"{normalizedSlug}.md")

            if File.Exists(outputPath) then
                Error(sprintf "LOGOS intake note already exists at %s." outputPath)
            else
                Directory.CreateDirectory(outputDirectory) |> ignore
                let now = DateTimeOffset.UtcNow
                let content = renderNote normalizedSlug title normalizedTags signal policy now
                File.WriteAllText(outputPath, content, utf8WithoutBom)

                Ok
                    { OutputPath = outputPath
                      NormalizedSlug = normalizedSlug
                      Signal = signal
                      Policy = policy }
        with :? ArgumentException as ex ->
            Error ex.Message
