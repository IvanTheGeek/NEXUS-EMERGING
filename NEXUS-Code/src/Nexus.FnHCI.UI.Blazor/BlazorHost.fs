namespace FnTools.FnHCI.UI.Blazor

open System
open FnTools.FnHCI.UI

/// Enumerates the Blazor render modes relevant to the first FnUI host seam.
type BlazorRenderMode =
    | Static
    | InteractiveServer
    | InteractiveWebAssembly
    | InteractiveAuto

/// Identifies a Blazor component type without binding FnUI to one concrete implementation yet.
type ComponentTypeName = private ComponentTypeName of string

[<RequireQualifiedAccess>]
module ComponentTypeName =
    let private isAllowedCharacter character =
        Char.IsAsciiLetterOrDigit character || character = '.' || character = '_'

    /// Creates a validated component type name using explicit ASCII allowlists.
    let tryCreate (value: string) =
        if String.IsNullOrWhiteSpace value then
            Error "Component type names must not be blank."
        elif value |> Seq.exists (isAllowedCharacter >> not) then
            Error "Component type names may contain only ASCII letters, digits, dot, and underscore."
        else
            Ok(ComponentTypeName value)

    /// Extracts the underlying component type name.
    let value (ComponentTypeName value) = value

/// Represents one Blazor host binding for the renderer-neutral FnUI shell.
type BlazorHost =
    private
        { Shell: ApplicationShell
          RootComponent: ComponentTypeName
          RenderMode: BlazorRenderMode
          HostPagePath: string }

[<RequireQualifiedAccess>]
module BlazorHost =
    let private isAllowedPathCharacter character =
        Char.IsAsciiLetterOrDigit character || character = '/' || character = '-' || character = '.'

    /// Creates a validated Blazor host binding for the current FnUI shell.
    let tryCreate shell rootComponent renderMode hostPagePath =
        if String.IsNullOrWhiteSpace hostPagePath then
            Error "Host page paths must not be blank."
        elif hostPagePath.StartsWith "/" |> not then
            Error "Host page paths must start with '/'."
        elif hostPagePath |> Seq.exists (isAllowedPathCharacter >> not) then
            Error "Host page paths may contain only ASCII letters, digits, slash, hyphen, and dot."
        else
            Ok
                { Shell = shell
                  RootComponent = rootComponent
                  RenderMode = renderMode
                  HostPagePath = hostPagePath }

    /// Returns the bound render mode.
    let renderMode host = host.RenderMode
