namespace Nexus.FnHCI

/// Distinguishes the major interaction lines that sit under the broader FnHCI concern.
type InteractionLine =
    | UI
    | CLI
    | API
    | A11y

[<RequireQualifiedAccess>]
module InteractionLine =
    /// Returns the stable slug used for docs, configuration, and derived metadata.
    let slug =
        function
        | InteractionLine.UI -> "ui"
        | InteractionLine.CLI -> "cli"
        | InteractionLine.API -> "api"
        | InteractionLine.A11y -> "a11y"

    /// Returns the namespace segment that should sit under `Nexus.FnHCI`.
    let namespaceSegment =
        function
        | InteractionLine.UI -> "UI"
        | InteractionLine.CLI -> "Cli"
        | InteractionLine.API -> "Api"
        | InteractionLine.A11y -> "A11y"

    /// Returns the user-facing label for the interaction line.
    let displayName =
        function
        | InteractionLine.UI -> "FnUI"
        | InteractionLine.CLI -> "FnCLI"
        | InteractionLine.API -> "FnAPI"
        | InteractionLine.A11y -> "FnA11y"
