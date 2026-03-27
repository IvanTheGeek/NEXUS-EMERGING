namespace CheddarBooks.Tests

open Expecto
open FnTools.FnHCI.UI
open CheddarBooks.LaundryLog
open CheddarBooks.LaundryLog.UI

[<RequireQualifiedAccess>]
module LaundryLogTests =
    let tests =
        testList
            "laundrylog"
            [ testCase "Expense kinds map to stable slugs" (fun () ->
                  Expect.equal (ExpenseKind.slug ExpenseKind.Washer) "washer" "Expected the washer expense slug."
                  Expect.equal (ExpenseKind.displayName ExpenseKind.Supplies) "Supplies" "Expected the supplies display name.")

              testCase "Location names reject characters outside the explicit allowlist" (fun () ->
                  match LocationName.tryCreate "Main Street Laundry!" with
                  | Ok _ -> failtest "Expected disallowed punctuation to be rejected."
                  | Error message -> Expect.stringContains message "ASCII letters, digits" "Expected the allowlist validation message.")

              testCase "Session flow stages map to stable path slugs" (fun () ->
                  Expect.equal
                      (SessionFlowStage.pathSlug SessionFlowStage.NewSession)
                      "path1-1-new-session"
                      "Expected the new-session path slug."

                  Expect.equal
                      (SessionFlowStage.pathSlug SessionFlowStage.EntryForm)
                      "path1-3-entry-form"
                      "Expected the entry-form path slug.")

              testCase "LaundryLog shell creates the first stable app shell" (fun () ->
                  match LaundryLogShell.tryCreate () with
                  | Error message -> failtest $"Expected a valid LaundryLog shell. {message}"
                  | Ok shell ->
                      let applicationShell = LaundryLogShell.applicationShell shell
                      let defaultView = ApplicationShell.defaultView applicationShell |> ViewId.value
                      let declaredViews = ApplicationShell.views applicationShell |> List.map (fun view -> ViewId.value view.ViewId)

                      Expect.equal defaultView "new-session" "Expected the new-session view to be the default entry point."
                      Expect.sequenceEqual declaredViews [ "new-session"; "entry-form" ] "Expected the first two concrete LaundryLog views.") ]
