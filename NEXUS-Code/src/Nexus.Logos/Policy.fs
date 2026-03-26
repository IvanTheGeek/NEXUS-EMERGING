namespace Nexus.Logos

/// <summary>
/// Identifies the sensitivity classification for a LOGOS intake signal or derivative.
/// </summary>
/// <remarks>
/// Sensitivity is explicit because import permission does not imply publication permission.
/// Full notes: docs/decisions/0011-restricted-by-default-intake-and-explicit-publication.md
/// </remarks>
[<Struct>]
type SensitivityId = private SensitivityId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.SensitivityId" />.
/// </summary>
[<RequireQualifiedAccess>]
module SensitivityId =
    /// <summary>
    /// Creates a sensitivity identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "SensitivityId" value |> SensitivityId

    /// <summary>
    /// Parses a persisted sensitivity identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying sensitivity slug.
    /// </summary>
    let value (SensitivityId value) = value

/// <summary>
/// Identifies who may access or use a LOGOS intake signal or derivative.
/// </summary>
[<Struct>]
type SharingScopeId = private SharingScopeId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.SharingScopeId" />.
/// </summary>
[<RequireQualifiedAccess>]
module SharingScopeId =
    /// <summary>
    /// Creates a sharing-scope identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "SharingScopeId" value |> SharingScopeId

    /// <summary>
    /// Parses a persisted sharing-scope identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying sharing-scope slug.
    /// </summary>
    let value (SharingScopeId value) = value

/// <summary>
/// Identifies the sanitization state of a LOGOS intake signal or derivative.
/// </summary>
[<Struct>]
type SanitizationStatusId = private SanitizationStatusId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.SanitizationStatusId" />.
/// </summary>
[<RequireQualifiedAccess>]
module SanitizationStatusId =
    /// <summary>
    /// Creates a sanitization-status identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "SanitizationStatusId" value |> SanitizationStatusId

    /// <summary>
    /// Parses a persisted sanitization-status identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying sanitization-status slug.
    /// </summary>
    let value (SanitizationStatusId value) = value

/// <summary>
/// Identifies the retention class for a LOGOS intake signal or derivative.
/// </summary>
[<Struct>]
type RetentionClassId = private RetentionClassId of string

/// <summary>
/// Constructors and converters for <see cref="T:Nexus.Logos.RetentionClassId" />.
/// </summary>
[<RequireQualifiedAccess>]
module RetentionClassId =
    /// <summary>
    /// Creates a retention-class identifier from a stable slug.
    /// </summary>
    let create (value: string) =
        StableSlugs.validate "RetentionClassId" value |> RetentionClassId

    /// <summary>
    /// Parses a persisted retention-class identifier.
    /// </summary>
    let parse (value: string) = create value

    /// <summary>
    /// Extracts the underlying retention-class slug.
    /// </summary>
    let value (RetentionClassId value) = value

/// <summary>
/// The explicit handling policy carried with a LOGOS intake signal or derivative.
/// </summary>
type LogosHandlingPolicy =
    { SensitivityId: SensitivityId
      SharingScopeId: SharingScopeId
      SanitizationStatusId: SanitizationStatusId
      RetentionClassId: RetentionClassId }

/// <summary>
/// The explicit access metadata carried with a LOGOS intake signal or derivative.
/// </summary>
type LogosAccessContext =
    { SourceInstanceId: SourceInstanceId option
      AccessContextId: AccessContextId
      AcquisitionKindId: AcquisitionKindId }

/// <summary>
/// The explicit rights metadata carried with a LOGOS intake signal or derivative.
/// </summary>
type LogosRightsContext =
    { RightsPolicyId: RightsPolicyId
      AttributionReference: string option }

/// <summary>
/// Stable sensitivity identifiers for early LOGOS handling policy.
/// </summary>
[<RequireQualifiedAccess>]
module KnownSensitivities =
    let personalPrivate = SensitivityId.create "personal-private"
    let customerConfidential = SensitivityId.create "customer-confidential"
    let internalRestricted = SensitivityId.create "internal-restricted"
    let publicData = SensitivityId.create "public"

    let private catalog =
        [ personalPrivate, "Private personal material that should remain tightly restricted."
          customerConfidential, "Customer-confidential material that may require sanitization before broader sharing."
          internalRestricted, "Restricted internal material that is not approved for public sharing."
          publicData, "Material already approved or inherently safe for public sharing." ]

    let private bySlug =
        catalog
        |> List.map (fun (identifier, summary) -> SensitivityId.value identifier, identifier, summary)

    let described =
        bySlug |> List.map (fun (slug, _, summary) -> slug, summary)

    let tryFind (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if System.String.IsNullOrWhiteSpace(normalized) then
            None
        else
            bySlug
            |> List.tryPick (fun (slug, identifier, _) ->
                if slug = normalized then Some identifier else None)

/// <summary>
/// Stable sharing-scope identifiers for early LOGOS handling policy.
/// </summary>
[<RequireQualifiedAccess>]
module KnownSharingScopes =
    let ownerOnly = SharingScopeId.create "owner-only"
    let caseTeam = SharingScopeId.create "case-team"
    let projectTeam = SharingScopeId.create "project-team"
    let publicAudience = SharingScopeId.create "public"

    let private catalog =
        [ ownerOnly, "Accessible only to the owner or primary steward."
          caseTeam, "Accessible to the case-specific support or debugging team."
          projectTeam, "Accessible to the project team."
          publicAudience, "Approved for public sharing." ]

    let private bySlug =
        catalog
        |> List.map (fun (identifier, summary) -> SharingScopeId.value identifier, identifier, summary)

    let described =
        bySlug |> List.map (fun (slug, _, summary) -> slug, summary)

    let tryFind (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if System.String.IsNullOrWhiteSpace(normalized) then
            None
        else
            bySlug
            |> List.tryPick (fun (slug, identifier, _) ->
                if slug = normalized then Some identifier else None)

/// <summary>
/// Stable sanitization-status identifiers for early LOGOS handling policy.
/// </summary>
[<RequireQualifiedAccess>]
module KnownSanitizationStatuses =
    let raw = SanitizationStatusId.create "raw"
    let redacted = SanitizationStatusId.create "redacted"
    let anonymized = SanitizationStatusId.create "anonymized"
    let approvedForSharing = SanitizationStatusId.create "approved-for-sharing"

    let private catalog =
        [ raw, "No sanitization has been applied yet."
          redacted, "Sensitive portions have been explicitly removed or masked."
          anonymized, "Identifying details have been removed or generalized."
          approvedForSharing, "Explicitly approved for the intended wider sharing scope." ]

    let private bySlug =
        catalog
        |> List.map (fun (identifier, summary) -> SanitizationStatusId.value identifier, identifier, summary)

    let described =
        bySlug |> List.map (fun (slug, _, summary) -> slug, summary)

    let tryFind (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if System.String.IsNullOrWhiteSpace(normalized) then
            None
        else
            bySlug
            |> List.tryPick (fun (slug, identifier, _) ->
                if slug = normalized then Some identifier else None)

/// <summary>
/// Stable retention-class identifiers for early LOGOS handling policy.
/// </summary>
[<RequireQualifiedAccess>]
module KnownRetentionClasses =
    let ephemeral = RetentionClassId.create "ephemeral"
    let caseBound = RetentionClassId.create "case-bound"
    let durable = RetentionClassId.create "durable"

    let private catalog =
        [ ephemeral, "Short-lived material that should not become durable project memory."
          caseBound, "Retained for the life of a case, issue, or support investigation."
          durable, "Retained as durable project memory or long-lived intake metadata." ]

    let private bySlug =
        catalog
        |> List.map (fun (identifier, summary) -> RetentionClassId.value identifier, identifier, summary)

    let described =
        bySlug |> List.map (fun (slug, _, summary) -> slug, summary)

    let tryFind (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if System.String.IsNullOrWhiteSpace(normalized) then
            None
        else
            bySlug
            |> List.tryPick (fun (slug, identifier, _) ->
                if slug = normalized then Some identifier else None)

/// <summary>
/// Stable access-context identifiers for early LOGOS intake governance.
/// </summary>
[<RequireQualifiedAccess>]
module KnownAccessContexts =
    let publicAnonymous = AccessContextId.create "public-anonymous"
    let registeredUser = AccessContextId.create "registered-user"
    let owner = AccessContextId.create "owner"
    let admin = AccessContextId.create "admin"
    let bot = AccessContextId.create "bot"
    let apiClient = AccessContextId.create "api-client"

    let private catalog =
        [ publicAnonymous, "Observed through a public non-authenticated view."
          registeredUser, "Observed through a logged-in but non-owner account."
          owner, "Observed through the system owner's or steward's account."
          admin, "Observed through an elevated administrative view."
          bot, "Observed or captured through an approved bot identity."
          apiClient, "Observed through an API client context." ]

    let private bySlug =
        catalog
        |> List.map (fun (identifier, summary) -> AccessContextId.value identifier, identifier, summary)

    let described =
        bySlug |> List.map (fun (slug, _, summary) -> slug, summary)

    let tryFind (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if System.String.IsNullOrWhiteSpace(normalized) then
            None
        else
            bySlug
            |> List.tryPick (fun (slug, identifier, _) ->
                if slug = normalized then Some identifier else None)

/// <summary>
/// Stable acquisition-kind identifiers for early LOGOS intake governance.
/// </summary>
[<RequireQualifiedAccess>]
module KnownAcquisitionKinds =
    let manualNote = AcquisitionKindId.create "manual-note"
    let webScrape = AcquisitionKindId.create "web-scrape"
    let apiPull = AcquisitionKindId.create "api-pull"
    let manualExport = AcquisitionKindId.create "manual-export"
    let liveCapture = AcquisitionKindId.create "live-capture"

    let private catalog =
        [ manualNote, "Manually entered note-based intake."
          webScrape, "Captured through a web-scrape flow."
          apiPull, "Captured through an API pull."
          manualExport, "Captured from a manually produced export artifact."
          liveCapture, "Captured live as the interaction happened." ]

    let private bySlug =
        catalog
        |> List.map (fun (identifier, summary) -> AcquisitionKindId.value identifier, identifier, summary)

    let described =
        bySlug |> List.map (fun (slug, _, summary) -> slug, summary)

    let tryFind (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if System.String.IsNullOrWhiteSpace(normalized) then
            None
        else
            bySlug
            |> List.tryPick (fun (slug, identifier, _) ->
                if slug = normalized then Some identifier else None)

/// <summary>
/// Stable rights-policy identifiers for early LOGOS intake governance.
/// </summary>
[<RequireQualifiedAccess>]
module KnownRightsPolicies =
    let ownerControlled = RightsPolicyId.create "owner-controlled"
    let personalTrainingOnly = RightsPolicyId.create "personal-training-only"
    let siteTermsRestricted = RightsPolicyId.create "site-terms-restricted"
    let ccBy = RightsPolicyId.create "cc-by"
    let ccBySa = RightsPolicyId.create "cc-by-sa"
    let apiContractRestricted = RightsPolicyId.create "api-contract-restricted"
    let customerConfidential = RightsPolicyId.create "customer-confidential"
    let reviewRequired = RightsPolicyId.create "review-required"

    let private catalog =
        [ ownerControlled, "Owned or explicitly stewarded material controlled by the operator."
          personalTrainingOnly, "May be used for personal/private model training but not assumed safe for public redistribution."
          siteTermsRestricted, "Visible under site terms or usage policies that require careful review before broader reuse."
          ccBy, "Material governed by CC-BY style attribution requirements."
          ccBySa, "Material governed by CC-BY-SA style attribution and share-alike requirements."
          apiContractRestricted, "Material governed by a source API contract or bot/developer agreement."
          customerConfidential, "Material governed by explicit confidentiality boundaries."
          reviewRequired, "Rights or reuse boundaries are not yet clear enough to automate." ]

    let private bySlug =
        catalog
        |> List.map (fun (identifier, summary) -> RightsPolicyId.value identifier, identifier, summary)

    let described =
        bySlug |> List.map (fun (slug, _, summary) -> slug, summary)

    let tryFind (value: string) =
        let normalized = value.Trim().ToLowerInvariant()

        if System.String.IsNullOrWhiteSpace(normalized) then
            None
        else
            bySlug
            |> List.tryPick (fun (slug, identifier, _) ->
                if slug = normalized then Some identifier else None)

    let requiresAttribution (identifier: RightsPolicyId) =
        identifier = ccBy || identifier = ccBySa

    let allowsPublicDistribution (identifier: RightsPolicyId) =
        identifier = ownerControlled || identifier = ccBy || identifier = ccBySa

/// <summary>
/// Constructors and defaults for <see cref="T:Nexus.Logos.LogosHandlingPolicy" />.
/// </summary>
[<RequireQualifiedAccess>]
module LogosHandlingPolicy =
    /// <summary>
    /// Creates an explicit LOGOS handling policy from allowlisted dimensions.
    /// </summary>
    let create sensitivity sharingScope sanitizationStatus retentionClass =
        { SensitivityId = sensitivity
          SharingScopeId = sharingScope
          SanitizationStatusId = sanitizationStatus
          RetentionClassId = retentionClass }

    /// <summary>
    /// The default restricted handling policy for new intake.
    /// </summary>
    let restrictedDefault =
        create
            KnownSensitivities.internalRestricted
            KnownSharingScopes.ownerOnly
            KnownSanitizationStatuses.raw
            KnownRetentionClasses.durable

/// <summary>
/// Constructors and defaults for <see cref="T:Nexus.Logos.LogosAccessContext" />.
/// </summary>
[<RequireQualifiedAccess>]
module LogosAccessContext =
    /// <summary>
    /// Creates explicit access metadata for LOGOS intake.
    /// </summary>
    let create sourceInstance accessContext acquisitionKind =
        { SourceInstanceId = sourceInstance
          AccessContextId = accessContext
          AcquisitionKindId = acquisitionKind }

    /// <summary>
    /// Conservative default access metadata for manually seeded notes.
    /// </summary>
    let restrictedDefault =
        create None KnownAccessContexts.owner KnownAcquisitionKinds.manualNote

/// <summary>
/// Constructors and defaults for <see cref="T:Nexus.Logos.LogosRightsContext" />.
/// </summary>
[<RequireQualifiedAccess>]
module LogosRightsContext =
    let private normalizeOptionalText (value: string option) =
        match value with
        | Some text ->
            let normalized = text.Trim()

            if System.String.IsNullOrWhiteSpace(normalized) then
                None
            else
                Some normalized
        | None -> None

    /// <summary>
    /// Creates explicit rights metadata for LOGOS intake.
    /// </summary>
    let create rightsPolicy attributionReference =
        { RightsPolicyId = rightsPolicy
          AttributionReference = normalizeOptionalText attributionReference }

    /// <summary>
    /// Conservative default rights metadata for newly seeded notes.
    /// </summary>
    let restrictedDefault =
        create KnownRightsPolicies.reviewRequired None
