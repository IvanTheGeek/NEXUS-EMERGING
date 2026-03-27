# FnHCI.UI.Blazor Native Host Requirements

This note captures the native-host requirements that sit under the broader [`FnHCI.UI.Blazor` requirements](fnhci-ui-blazor-requirements.md).

## Purpose

The visual system should remain usable in native shells without requiring a second completely different UI stack for every host.

## Required Native Host Direction

The first explicitly recognized native host family is:

- .NET MAUI Blazor Hybrid
- WPF Blazor Hybrid
- WinForms Blazor Hybrid

These are the current official native-host paths that matter most for the first pass.

## Architecture Requirement

The native-host line must depend on the same core visual shell and host abstractions as the browser-facing line.

That means:

- the app shell belongs in `FnHCI.UI`
- the Blazor-backed renderer/host seam belongs in `FnHCI.UI.Blazor`
- native shells host that line rather than replacing it with an unrelated visual model

## Desktop And Mobile Direction

Native hosting is important because it can provide:

- desktop app options
- later mobile options
- installable experiences without requiring full native app development from the first pass

This should remain compatible with later evaluation of which host gives the best real user experience per app.

## Future Evaluation Targets

The architecture should leave room for later evaluation of:

- Photino
- Electron
- other webview-based or hybrid shells

These are future host candidates, not current guaranteed equivalents to the official Blazor Hybrid paths.

## Non-Goals

This note does not yet decide:

- which native host should become the primary NEXUS desktop shell
- whether CheddarBooks LaundryLog should prefer native host over PWA
- exact mobile packaging choices
- exact host-specific capability boundaries
