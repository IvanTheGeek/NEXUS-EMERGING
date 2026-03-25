namespace Nexus.Logos

open System

/// <summary>
/// A concrete locator back to the originating source item inside a LOGOS source system.
/// </summary>
type LogosLocator =
    | NativeItemId of string
    | NativeThreadId of string
    | NativeMessageId of string
    | SourceUri of string

/// <summary>
/// Helpers for <see cref="T:Nexus.Logos.LogosLocator" />.
/// </summary>
[<RequireQualifiedAccess>]
module LogosLocator =
    let private normalizeRequiredValue (name: string) (value: string) =
        let normalized = value.Trim()

        if String.IsNullOrWhiteSpace(normalized) then
            invalidArg name $"{name} cannot be blank."

        normalized

    /// <summary>
    /// Creates a locator for a source-system native item identifier.
    /// </summary>
    let nativeItemId (value: string) =
        normalizeRequiredValue "value" value |> NativeItemId

    /// <summary>
    /// Creates a locator for a source-system native thread identifier.
    /// </summary>
    let nativeThreadId (value: string) =
        normalizeRequiredValue "value" value |> NativeThreadId

    /// <summary>
    /// Creates a locator for a source-system native message identifier.
    /// </summary>
    let nativeMessageId (value: string) =
        normalizeRequiredValue "value" value |> NativeMessageId

    /// <summary>
    /// Creates a locator for a source URI.
    /// </summary>
    let sourceUri (value: string) =
        normalizeRequiredValue "value" value |> SourceUri

    /// <summary>
    /// Extracts the underlying locator text.
    /// </summary>
    let value locator =
        match locator with
        | NativeItemId value -> value
        | NativeThreadId value -> value
        | NativeMessageId value -> value
        | SourceUri value -> value

type private LogosSourceRefRecord =
    { SourceSystemId: SourceSystemId
      IntakeChannelId: IntakeChannelId
      Locators: LogosLocator list }

[<Struct>]
/// <summary>
/// Identifies a LOGOS source item through source system, intake channel, and explicit locators.
/// </summary>
/// <remarks>
/// Source references are kept separate from canonical history so multiple acquisition paths can later be reconciled explicitly rather than collapsed implicitly.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
type LogosSourceRef = private LogosSourceRef of LogosSourceRefRecord

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.LogosSourceRef" />.
/// </summary>
[<RequireQualifiedAccess>]
module LogosSourceRef =
    /// <summary>
    /// Creates a LOGOS source reference with at least one explicit locator.
    /// </summary>
    let create (sourceSystemId: SourceSystemId) (intakeChannelId: IntakeChannelId) (locators: LogosLocator list) =
        match locators with
        | [] -> invalidArg "locators" "LogosSourceRef requires at least one locator."
        | _ ->
            { SourceSystemId = sourceSystemId
              IntakeChannelId = intakeChannelId
              Locators = locators |> List.distinct }
            |> LogosSourceRef

    /// <summary>
    /// Extracts the source-system identifier.
    /// </summary>
    let sourceSystemId (LogosSourceRef value) = value.SourceSystemId

    /// <summary>
    /// Extracts the intake-channel identifier.
    /// </summary>
    let intakeChannelId (LogosSourceRef value) = value.IntakeChannelId

    /// <summary>
    /// Extracts the explicit source locators.
    /// </summary>
    let locators (LogosSourceRef value) = value.Locators

type private LogosSignalRecord =
    { SignalKindId: SignalKindId
      Source: LogosSourceRef
      CapturedAt: DateTimeOffset option
      Title: string option
      Summary: string option }

[<Struct>]
/// <summary>
/// Represents a small LOGOS intake signal classified by source and signal kind.
/// </summary>
/// <remarks>
/// This is intentionally a narrow source-model envelope, not the full LOGOS knowledge model.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
type LogosSignal = private LogosSignal of LogosSignalRecord

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.LogosSignal" />.
/// </summary>
[<RequireQualifiedAccess>]
module LogosSignal =
    let private normalizeOptionalText (value: string option) =
        match value with
        | Some text ->
            let normalized = text.Trim()

            match String.IsNullOrWhiteSpace(normalized) with
            | true -> None
            | false -> Some normalized
        | None -> None

    /// <summary>
    /// Creates a LOGOS signal envelope from explicit source and signal-kind information.
    /// </summary>
    let create
        (signalKindId: SignalKindId)
        (source: LogosSourceRef)
        (capturedAt: DateTimeOffset option)
        (title: string option)
        (summary: string option)
        =
        { SignalKindId = signalKindId
          Source = source
          CapturedAt = capturedAt
          Title = normalizeOptionalText title
          Summary = normalizeOptionalText summary }
        |> LogosSignal

    /// <summary>
    /// Extracts the signal-kind identifier.
    /// </summary>
    let signalKindId (LogosSignal value) = value.SignalKindId

    /// <summary>
    /// Extracts the source reference.
    /// </summary>
    let source (LogosSignal value) = value.Source

    /// <summary>
    /// Extracts the signal capture time when known.
    /// </summary>
    let capturedAt (LogosSignal value) = value.CapturedAt

    /// <summary>
    /// Extracts the optional title.
    /// </summary>
    let title (LogosSignal value) = value.Title

    /// <summary>
    /// Extracts the optional summary.
    /// </summary>
    let summary (LogosSignal value) = value.Summary
