namespace Nexus.Tests

open Expecto
open Nexus.FnHCI
open Nexus.FnHCI.UI
open Nexus.FnHCI.UI.Blazor

[<RequireQualifiedAccess>]
module FnHCITests =
    let private currentIngestionViewId =
        match ViewId.tryCreate "current-ingestion" with
        | Ok value -> value
        | Error message -> failtest $"Expected a valid view identifier. {message}"

    let private ingestionView =
        { ViewId = currentIngestionViewId
          Title = "Current Ingestion"
          Summary = "Cross-provider ingestion status."
          InteractionLine = InteractionLine.UI }

    let private shell =
        match
            ApplicationShell.tryCreate
                "NEXUS"
                [ ShellRegion.Navigation; ShellRegion.Workspace; ShellRegion.StatusBar ]
                [ ingestionView ]
                [ { ViewId = currentIngestionViewId
                    Label = "Current Ingestion" } ]
                currentIngestionViewId
        with
        | Ok shell -> shell
        | Error message -> failtest $"Expected a valid application shell. {message}"

    let tests =
        testList
            "fnhci"
            [ testCase "Interaction lines map to stable namespace segments" (fun () ->
                  Expect.equal (InteractionLine.namespaceSegment InteractionLine.UI) "UI" "Expected the UI namespace segment."
                  Expect.equal (InteractionLine.displayName InteractionLine.A11y) "FnA11y" "Expected the accessibility display name.")

              testCase "View identifiers reject characters outside the explicit allowlist" (fun () ->
                  match ViewId.tryCreate "CurrentIngestion" with
                  | Ok _ -> failtest "Expected uppercase view identifiers to be rejected."
                  | Error message -> Expect.stringContains message "lowercase" "Expected the lowercase validation message.")

              testCase "Application shell requires the default view to exist" (fun () ->
                  let missingView =
                      match ViewId.tryCreate "missing-view" with
                      | Ok value -> value
                      | Error message -> failtest $"Expected a valid view identifier. {message}"

                  match
                      ApplicationShell.tryCreate
                          "NEXUS"
                          [ ShellRegion.Navigation ]
                          [ ingestionView ]
                          []
                          missingView
                  with
                  | Ok _ -> failtest "Expected shells with missing default views to be rejected."
                  | Error message -> Expect.stringContains message "default view" "Expected the default-view validation message.")

              testCase "Blazor host keeps the renderer-neutral shell boundary" (fun () ->
                  let rootComponent =
                      match ComponentTypeName.tryCreate "Nexus.FnHCI.UI.Blazor.App" with
                      | Ok value -> value
                      | Error message -> failtest $"Expected a valid component type name. {message}"

                  match BlazorHost.tryCreate shell rootComponent BlazorRenderMode.InteractiveAuto "/app" with
                  | Ok host ->
                      Expect.equal
                          (BlazorHost.renderMode host)
                          BlazorRenderMode.InteractiveAuto
                          "Expected the configured Blazor render mode."
                  | Error message -> failtest $"Expected a valid Blazor host. {message}") ]
