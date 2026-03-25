namespace Nexus.Logos

/// <summary>
/// A human-facing catalog row for an allowlisted LOGOS identifier.
/// </summary>
type LogosCatalogItem =
    { Slug: string
      Summary: string }

/// <summary>
/// A compact report of the currently allowlisted LOGOS source vocabulary.
/// </summary>
type LogosCatalogReport =
    { SourceSystems: LogosCatalogItem list
      IntakeChannels: LogosCatalogItem list
      SignalKinds: LogosCatalogItem list
      Sensitivities: LogosCatalogItem list
      SharingScopes: LogosCatalogItem list
      SanitizationStatuses: LogosCatalogItem list
      RetentionClasses: LogosCatalogItem list }

/// <summary>
/// Builds human-facing reports over the allowlisted LOGOS source vocabulary.
/// </summary>
/// <remarks>
/// This keeps the early LOGOS source model explicit and inspectable without allowing ad hoc categories to drift into the codebase.
/// Full notes: docs/logos-source-model-v0.md
/// </remarks>
[<RequireQualifiedAccess>]
module LogosCatalog =
    let private renderItems items =
        items
        |> List.map (fun (slug, summary) ->
            { Slug = slug
              Summary = summary })

    /// <summary>
    /// Builds the current allowlisted LOGOS source-system, intake-channel, signal-kind, and handling-policy catalog.
    /// </summary>
    let build () =
        { SourceSystems = KnownSourceSystems.described |> renderItems
          IntakeChannels = CoreIntakeChannels.described |> renderItems
          SignalKinds = CoreSignalKinds.described |> renderItems
          Sensitivities = KnownSensitivities.described |> renderItems
          SharingScopes = KnownSharingScopes.described |> renderItems
          SanitizationStatuses = KnownSanitizationStatuses.described |> renderItems
          RetentionClasses = KnownRetentionClasses.described |> renderItems }
