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
/// Identifies one concrete source instance or authority within a broader source system.
/// </summary>
/// <remarks>
/// Examples include one hosted forum instance, one GitHub org/repo, or one Discord server.
/// This stays a stable slug rather than a raw URL so it can be used consistently across imports and notes.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
type SourceInstanceId = private SourceInstanceId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.SourceInstanceId" />.
/// </summary>
[<RequireQualifiedAccess>]
module SourceInstanceId =
    /// <summary>
    /// Creates a source-instance identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "SourceInstanceId" value |> SourceInstanceId

    /// <summary>
    /// Parses a persisted source-instance identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying source-instance slug.
    /// </summary>
    let value (SourceInstanceId value) = value

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

[<Struct>]
/// <summary>
/// Identifies the access context through which NEXUS observed a source.
/// </summary>
type AccessContextId = private AccessContextId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.AccessContextId" />.
/// </summary>
[<RequireQualifiedAccess>]
module AccessContextId =
    /// <summary>
    /// Creates an access-context identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "AccessContextId" value |> AccessContextId

    /// <summary>
    /// Parses a persisted access-context identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying access-context slug.
    /// </summary>
    let value (AccessContextId value) = value

[<Struct>]
/// <summary>
/// Identifies the technical acquisition kind through which NEXUS captured source material.
/// </summary>
type AcquisitionKindId = private AcquisitionKindId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.AcquisitionKindId" />.
/// </summary>
[<RequireQualifiedAccess>]
module AcquisitionKindId =
    /// <summary>
    /// Creates an acquisition-kind identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "AcquisitionKindId" value |> AcquisitionKindId

    /// <summary>
    /// Parses a persisted acquisition-kind identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying acquisition-kind slug.
    /// </summary>
    let value (AcquisitionKindId value) = value

[<Struct>]
/// <summary>
/// Identifies the governing rights or license policy attached to observed material.
/// </summary>
type RightsPolicyId = private RightsPolicyId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.RightsPolicyId" />.
/// </summary>
[<RequireQualifiedAccess>]
module RightsPolicyId =
    /// <summary>
    /// Creates a rights-policy identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "RightsPolicyId" value |> RightsPolicyId

    /// <summary>
    /// Parses a persisted rights-policy identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying rights-policy slug.
    /// </summary>
    let value (RightsPolicyId value) = value
