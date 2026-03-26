namespace Nexus.Logos

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
