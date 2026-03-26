namespace Nexus.Logos

/// <summary>
/// A raw-pool item that may retain the fullest preserved intake detail.
/// </summary>
[<Struct>]
type RawPoolItem<'payload> = private RawPoolItem of 'payload * LogosHandlingPolicy

/// <summary>
/// Constructors and accessors for <see cref="T:Nexus.Logos.RawPoolItem`1" />.
/// </summary>
[<RequireQualifiedAccess>]
module RawPoolItem =
    /// <summary>
    /// Wraps a payload and handling policy as a raw-pool item.
    /// </summary>
    let create payload (policy: LogosHandlingPolicy) : RawPoolItem<'payload> = RawPoolItem(payload, policy)

    /// <summary>
    /// Extracts the wrapped payload.
    /// </summary>
    let value (RawPoolItem(payload, _): RawPoolItem<'payload>) = payload

    /// <summary>
    /// Extracts the handling policy attached to the wrapped payload.
    /// </summary>
    let policy (RawPoolItem(_, policy): RawPoolItem<'payload>) = policy

/// <summary>
/// A private-pool item intended for owner or restricted internal use.
/// </summary>
[<Struct>]
type PrivatePoolItem<'payload> = private PrivatePoolItem of 'payload * LogosHandlingPolicy

/// <summary>
/// Constructors and accessors for <see cref="T:Nexus.Logos.PrivatePoolItem`1" />.
/// </summary>
[<RequireQualifiedAccess>]
module PrivatePoolItem =
    /// <summary>
    /// Wraps a payload and handling policy as a private-pool item.
    /// </summary>
    let create payload (policy: LogosHandlingPolicy) : PrivatePoolItem<'payload> = PrivatePoolItem(payload, policy)

    /// <summary>
    /// Extracts the wrapped payload.
    /// </summary>
    let value (PrivatePoolItem(payload, _): PrivatePoolItem<'payload>) = payload

    /// <summary>
    /// Extracts the handling policy attached to the wrapped payload.
    /// </summary>
    let policy (PrivatePoolItem(_, policy): PrivatePoolItem<'payload>) = policy

/// <summary>
/// A public-safe pool item that has passed the explicit policy boundary for public-facing use.
/// </summary>
[<Struct>]
type PublicSafePoolItem<'payload> = private PublicSafePoolItem of 'payload * LogosHandlingPolicy

/// <summary>
/// Constructors and accessors for <see cref="T:Nexus.Logos.PublicSafePoolItem`1" />.
/// </summary>
/// <remarks>
/// Public-safe promotion is intentionally stricter than generic classification.
/// Future public/export flows should depend on this type instead of rechecking loose properties ad hoc.
/// Full notes: docs/decisions/0012-pool-based-handling-boundaries.md
/// </remarks>
[<RequireQualifiedAccess>]
module PublicSafePoolItem =
    let private validate (policy: LogosHandlingPolicy) =
        if policy.SanitizationStatusId <> KnownSanitizationStatuses.approvedForSharing then
            Error "Public-safe promotion requires sanitization_status = approved-for-sharing."
        elif policy.SensitivityId <> KnownSensitivities.publicData then
            Error "Public-safe promotion requires sensitivity = public."
        elif policy.SharingScopeId <> KnownSharingScopes.publicAudience then
            Error "Public-safe promotion requires sharing_scope = public."
        else
            Ok ()

    /// <summary>
    /// Attempts to wrap a payload as a public-safe pool item using its explicit handling policy.
    /// </summary>
    let tryCreate payload (policy: LogosHandlingPolicy) : Result<PublicSafePoolItem<'payload>, string> =
        validate policy
        |> Result.map (fun () -> PublicSafePoolItem(payload, policy))

    /// <summary>
    /// Extracts the wrapped payload.
    /// </summary>
    let value (PublicSafePoolItem(payload, _): PublicSafePoolItem<'payload>) = payload

    /// <summary>
    /// Extracts the handling policy attached to the wrapped payload.
    /// </summary>
    let policy (PublicSafePoolItem(_, policy): PublicSafePoolItem<'payload>) = policy

/// <summary>
/// Explicit transitions between handling pools.
/// </summary>
[<RequireQualifiedAccess>]
module LogosPoolTransitions =
    /// <summary>
    /// Promotes a raw-pool item into the private pool.
    /// </summary>
    let rawToPrivate (rawItem: RawPoolItem<'payload>) : PrivatePoolItem<'payload> =
        PrivatePoolItem.create (RawPoolItem.value rawItem) (RawPoolItem.policy rawItem)

    /// <summary>
    /// Attempts to promote a private-pool item into the public-safe pool.
    /// </summary>
    let tryPrivateToPublicSafe (privateItem: PrivatePoolItem<'payload>) : Result<PublicSafePoolItem<'payload>, string> =
        PublicSafePoolItem.tryCreate (PrivatePoolItem.value privateItem) (PrivatePoolItem.policy privateItem)

/// <summary>
/// Bridges current LOGOS workflow results into explicit pool boundary types.
/// </summary>
[<RequireQualifiedAccess>]
module LogosPoolResults =
    /// <summary>
    /// Wraps a LOGOS intake-note result as a raw-pool item.
    /// </summary>
    let intakeAsRaw (result: CreateLogosIntakeNoteResult) : RawPoolItem<CreateLogosIntakeNoteResult> =
        RawPoolItem.create result result.Policy

    /// <summary>
    /// Wraps a LOGOS intake-note result as a private-pool item.
    /// </summary>
    let intakeAsPrivate (result: CreateLogosIntakeNoteResult) : PrivatePoolItem<CreateLogosIntakeNoteResult> =
        intakeAsRaw result |> LogosPoolTransitions.rawToPrivate

    /// <summary>
    /// Wraps a derived sanitized-note result as a private-pool item.
    /// </summary>
    let sanitizedAsPrivate (result: CreateLogosSanitizedNoteResult) : PrivatePoolItem<CreateLogosSanitizedNoteResult> =
        PrivatePoolItem.create result result.Policy

    /// <summary>
    /// Attempts to promote a derived sanitized-note result into the public-safe pool.
    /// </summary>
    let trySanitizedAsPublicSafe (result: CreateLogosSanitizedNoteResult) : Result<PublicSafePoolItem<CreateLogosSanitizedNoteResult>, string> =
        sanitizedAsPrivate result |> LogosPoolTransitions.tryPrivateToPublicSafe
