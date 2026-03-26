namespace Nexus.Logos

/// <summary>
/// The explicit LOGOS handling pools used to separate raw, private, and public-safe material.
/// </summary>
type LogosPool =
    | Raw
    | Private
    | PublicSafe

/// <summary>
/// Stable conversions and helpers for <see cref="T:Nexus.Logos.LogosPool" />.
/// </summary>
[<RequireQualifiedAccess>]
module LogosPool =
    /// <summary>
    /// Returns the persisted stable slug for a LOGOS pool.
    /// </summary>
    let value =
        function
        | Raw -> "raw"
        | Private -> "private"
        | PublicSafe -> "public-safe"

    /// <summary>
    /// Parses a stable LOGOS pool slug.
    /// </summary>
    let tryParse (value: string) =
        match value.Trim().ToLowerInvariant() with
        | "raw" -> Some Raw
        | "private" -> Some Private
        | "public-safe" -> Some PublicSafe
        | _ -> None

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
type PublicSafePoolItem<'payload> = private PublicSafePoolItem of 'payload * LogosHandlingPolicy * LogosRightsContext

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
    let private validateHandling (policy: LogosHandlingPolicy) =
        if policy.SanitizationStatusId <> KnownSanitizationStatuses.approvedForSharing then
            Error "Public-safe promotion requires sanitization_status = approved-for-sharing."
        elif policy.SensitivityId <> KnownSensitivities.publicData then
            Error "Public-safe promotion requires sensitivity = public."
        elif policy.SharingScopeId <> KnownSharingScopes.publicAudience then
            Error "Public-safe promotion requires sharing_scope = public."
        else
            Ok ()

    let private validateRights (rightsContext: LogosRightsContext) =
        if not (KnownRightsPolicies.allowsPublicDistribution rightsContext.RightsPolicyId) then
            Error "Public-safe promotion requires a rights policy that explicitly allows public distribution."
        elif
            KnownRightsPolicies.requiresAttribution rightsContext.RightsPolicyId
            && rightsContext.AttributionReference.IsNone
        then
            Error "Public-safe promotion requires an attribution reference when the rights policy requires attribution."
        else
            Ok ()

    /// <summary>
    /// Attempts to wrap a payload as a public-safe pool item using its explicit handling policy and rights metadata.
    /// </summary>
    let tryCreate payload (policy: LogosHandlingPolicy) (rightsContext: LogosRightsContext) : Result<PublicSafePoolItem<'payload>, string> =
        match validateHandling policy with
        | Error error -> Error error
        | Ok () ->
            validateRights rightsContext
            |> Result.map (fun () -> PublicSafePoolItem(payload, policy, rightsContext))

    /// <summary>
    /// Extracts the wrapped payload.
    /// </summary>
    let value (PublicSafePoolItem(payload, _, _): PublicSafePoolItem<'payload>) = payload

    /// <summary>
    /// Extracts the handling policy attached to the wrapped payload.
    /// </summary>
    let policy (PublicSafePoolItem(_, policy, _): PublicSafePoolItem<'payload>) = policy

    /// <summary>
    /// Extracts the rights metadata attached to the wrapped payload.
    /// </summary>
    let rights (PublicSafePoolItem(_, _, rightsContext): PublicSafePoolItem<'payload>) = rightsContext

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
    let tryPrivateToPublicSafe (privateItem: PrivatePoolItem<'payload>) (rightsContext: LogosRightsContext) : Result<PublicSafePoolItem<'payload>, string> =
        PublicSafePoolItem.tryCreate (PrivatePoolItem.value privateItem) (PrivatePoolItem.policy privateItem) rightsContext
