namespace Nexus.LaundryLog.UI

open Nexus.FnHCI
open Nexus.FnHCI.UI
open Nexus.LaundryLog

/// Enumerates the first concrete LaundryLog views hosted inside the FnUI shell.
type LaundryLogView =
    | NewSession
    | EntryForm

[<RequireQualifiedAccess>]
module LaundryLogView =
    /// Returns the stable view slug used for navigation and renderer-neutral binding.
    let slug =
        function
        | LaundryLogView.NewSession -> "new-session"
        | LaundryLogView.EntryForm -> "entry-form"

    /// Returns the user-facing title for the view.
    let title =
        function
        | LaundryLogView.NewSession -> "New Session"
        | LaundryLogView.EntryForm -> "Entry Form"

    /// Returns the concise view summary used in shell metadata.
    let summary =
        function
        | LaundryLogView.NewSession -> "Establish laundry location context before logging expenses."
        | LaundryLogView.EntryForm -> "Log repeatable laundry expenses within the current outing."

    /// Returns the current LaundryLog workflow stages represented by the view.
    let flowStages =
        function
        | LaundryLogView.NewSession -> [ SessionFlowStage.NewSession; SessionFlowStage.LocationEntered ]
        | LaundryLogView.EntryForm -> [ SessionFlowStage.EntryForm ]

    let private toViewId view =
        match ViewId.tryCreate (slug view) with
        | Ok viewId -> viewId
        | Error message -> failwith $"Expected a stable LaundryLog view slug. {message}"

    /// Builds the renderer-neutral FnUI view definition for the LaundryLog view.
    let definition view =
        { ViewId = toViewId view
          Title = title view
          Summary = summary view
          InteractionLine = InteractionLine.UI }

/// Represents the first validated LaundryLog application shell.
type LaundryLogShell = private LaundryLogShell of ApplicationShell

[<RequireQualifiedAccess>]
module LaundryLogShell =
    let private viewDefinitions =
        [ LaundryLogView.NewSession; LaundryLogView.EntryForm ]
        |> List.map LaundryLogView.definition

    let private navigationItems =
        viewDefinitions
        |> List.map (fun view ->
            { ViewId = view.ViewId
              Label = view.Title })

    /// Creates the first LaundryLog shell on top of the renderer-neutral FnUI shell model.
    let tryCreate () =
        let defaultView = viewDefinitions.Head.ViewId

        ApplicationShell.tryCreate
            "LaundryLog"
            [ ShellRegion.Navigation; ShellRegion.Workspace; ShellRegion.StatusBar ]
            viewDefinitions
            navigationItems
            defaultView
        |> Result.map LaundryLogShell

    /// Extracts the wrapped renderer-neutral application shell.
    let applicationShell (LaundryLogShell shell) = shell
