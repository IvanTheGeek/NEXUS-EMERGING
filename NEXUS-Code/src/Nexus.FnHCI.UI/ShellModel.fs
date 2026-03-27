namespace FnTools.FnHCI.UI

open System
open FnTools.FnHCI

/// Identifies one navigable visual view within the FnUI application shell.
type ViewId = private ViewId of string

[<RequireQualifiedAccess>]
module ViewId =
    let private isAllowedCharacter character =
        Char.IsAsciiLetterOrDigit character || character = '-'

    /// Creates a validated view identifier using an explicit lowercase slug allowlist.
    let tryCreate (value: string) =
        if String.IsNullOrWhiteSpace value then
            Error "View identifiers must not be blank."
        elif value |> Seq.exists (isAllowedCharacter >> not) then
            Error "View identifiers may contain only ASCII letters, digits, and hyphen."
        elif value <> value.ToLowerInvariant() then
            Error "View identifiers must use lowercase values."
        else
            Ok(ViewId value)

    /// Extracts the raw slug value.
    let value (ViewId value) = value

/// Declares the major visual regions within the first FnUI application shell.
type ShellRegion =
    | Navigation
    | Workspace
    | Inspector
    | StatusBar
    | CommandPalette

/// Describes one navigable view exposed by the FnUI shell.
type ViewDefinition =
    { ViewId: ViewId
      Title: string
      Summary: string
      InteractionLine: InteractionLine }

/// Describes one navigation entry inside the FnUI shell.
type NavigationItem =
    { ViewId: ViewId
      Label: string }

/// The stable, renderer-neutral description of the first FnUI shell boundary.
type ApplicationShell =
    private
        { ProductName: string
          Regions: ShellRegion list
          Views: ViewDefinition list
          Navigation: NavigationItem list
          DefaultView: ViewId }

[<RequireQualifiedAccess>]
module ApplicationShell =
    let private containsViewId (targetViewId: ViewId) (views: ViewDefinition list) =
        views |> List.exists (fun view -> view.ViewId = targetViewId)

    let private hasDuplicateViewIds (views: ViewDefinition list) =
        let viewIds = views |> List.map (fun view -> ViewId.value view.ViewId)

        viewIds.Length <> (viewIds |> List.distinct).Length

    let private allNavigationTargetsExist (views: ViewDefinition list) (navigation: NavigationItem list) =
        navigation |> List.forall (fun item -> containsViewId item.ViewId views)

    /// Creates a validated application shell without committing to a specific renderer.
    let tryCreate
        (productName: string)
        (regions: ShellRegion list)
        (views: ViewDefinition list)
        (navigation: NavigationItem list)
        (defaultView: ViewId)
        =
        if String.IsNullOrWhiteSpace productName then
            Error "Application shells must have a product name."
        elif List.isEmpty regions then
            Error "Application shells must declare at least one region."
        elif List.isEmpty views then
            Error "Application shells must declare at least one view."
        elif hasDuplicateViewIds views then
            Error "Application shells must not declare duplicate view identifiers."
        elif containsViewId defaultView views |> not then
            Error "The default view must exist in the declared views."
        elif allNavigationTargetsExist views navigation |> not then
            Error "Every navigation item must target a declared view."
        else
            Ok
                { ProductName = productName
                  Regions = regions
                  Views = views
                  Navigation = navigation
                  DefaultView = defaultView }

    /// Returns the validated default view.
    let defaultView shell = shell.DefaultView

    /// Returns the declared view set.
    let views shell = shell.Views
