namespace Nexus.Logos

[<Struct>]
/// <summary>
/// Identifies the originating system for a LOGOS intake source.
/// </summary>
/// <remarks>
/// Uses a stable allowlisted slug because LOGOS source-system IDs should stay narrow and deterministic.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
type SourceSystemId = private SourceSystemId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.SourceSystemId" />.
/// </summary>
[<RequireQualifiedAccess>]
module SourceSystemId =
    /// <summary>
    /// Creates a source-system identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "SourceSystemId" value |> SourceSystemId

    /// <summary>
    /// Parses a persisted source-system identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying source-system slug.
    /// </summary>
    let value (SourceSystemId value) = value

[<Struct>]
/// <summary>
/// Identifies the intake channel through which a LOGOS signal was acquired.
/// </summary>
/// <remarks>
/// Examples include AI conversations, email threads, forum threads, or deployed-app feedback channels.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
type IntakeChannelId = private IntakeChannelId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.IntakeChannelId" />.
/// </summary>
[<RequireQualifiedAccess>]
module IntakeChannelId =
    /// <summary>
    /// Creates an intake-channel identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "IntakeChannelId" value |> IntakeChannelId

    /// <summary>
    /// Parses a persisted intake-channel identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying intake-channel slug.
    /// </summary>
    let value (IntakeChannelId value) = value

[<Struct>]
/// <summary>
/// Identifies the semantic kind of a knowledge-bearing LOGOS signal.
/// </summary>
/// <remarks>
/// Signal kinds classify intake meaning without replacing canonical observed history.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
type SignalKindId = private SignalKindId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.SignalKindId" />.
/// </summary>
[<RequireQualifiedAccess>]
module SignalKindId =
    /// <summary>
    /// Creates a signal-kind identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "SignalKindId" value |> SignalKindId

    /// <summary>
    /// Parses a persisted signal-kind identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying signal-kind slug.
    /// </summary>
    let value (SignalKindId value) = value
