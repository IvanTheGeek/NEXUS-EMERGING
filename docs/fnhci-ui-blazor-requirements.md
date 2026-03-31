# FnHCI.UI.Blazor Requirements

This document defines the first concrete requirements for `FnHCI.UI.Blazor`.

For shorthand, this line may be referred to as `FnUI` in conversation, package naming, or internal notes.

Architecture truth remains:

- top concern and namespace: `FnHCI`
- visual subsystem line: `FnHCI.UI`
- Blazor-specific host/runtime seam: `FnHCI.UI.Blazor`

See also:

- [`docs/fnhci-namespace-map.md`](fnhci-namespace-map.md)
- [`docs/fnui-foundation.md`](fnui-foundation.md)
- [`docs/fnhci-penpot-abstraction.md`](fnhci-penpot-abstraction.md)
- [`docs/fnhci-ui-token-model.md`](fnhci-ui-token-model.md)
- [`docs/concepts/fnhci.md`](concepts/fnhci.md)
- [`docs/fnhci-ui-web-requirements.md`](fnhci-ui-web-requirements.md)
- [`docs/fnhci-ui-native-host-requirements.md`](fnhci-ui-native-host-requirements.md)
- [`docs/laundrylog-fnui-proving-ground.md`](laundrylog-fnui-proving-ground.md)

## Purpose

`FnHCI.UI.Blazor` is the first concrete runtime line for the reusable visual system that should replace Bolero for NEXUS needs while still riding on Blazor as the substrate.

The intent is:

- keep Blazor as the underlying runtime/rendering platform
- avoid depending on Bolero as the long-term abstraction owner
- preserve ownership of the authoring model, view model, and projection seams
- allow the visual system to grow beyond one hosting style or deployment model

This document is now the convergence layer over smaller, more specific requirement notes rather than the only requirements document in this area.

## Requirement Layers

The current split is:

1. [`docs/fnhci-ui-blazor-requirements.md`](fnhci-ui-blazor-requirements.md)
   the convergence document for the Blazor-backed line
2. [`docs/fnhci-ui-web-requirements.md`](fnhci-ui-web-requirements.md)
   browser-facing, wasm, connected, mixed, and PWA requirements
3. [`docs/fnhci-ui-native-host-requirements.md`](fnhci-ui-native-host-requirements.md)
   native-shell requirements and host-candidate boundaries
4. [`docs/laundrylog-fnui-proving-ground.md`](laundrylog-fnui-proving-ground.md)
   concrete pressure from the first real proving-ground application

## Recorded Direction

The current requirements are grounded in the recorded discussions, especially:

- [`019d174f-2cd6-772c-97db-8fdcb16a0050.toml`](https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/d0da2b9d1130c85ede291ede2cf0a7863653fd25/NEXUS-EventStore/projections/conversations/019d174f-2cd6-772c-97db-8fdcb16a0050.toml)
- [`019d174f-2ce1-7496-a7f3-2e5cae80727e.toml`](https://github.com/IvanTheGeek/NEXUS-EMERGING/blob/d0da2b9d1130c85ede291ede2cf0a7863653fd25/NEXUS-EventStore/projections/conversations/019d174f-2ce1-7496-a7f3-2e5cae80727e.toml)

The most important current signals from those discussions are:

- interface with Blazor at essentially the same seam Bolero uses
- do not make raw `BuildRenderTree` the main authoring surface
- keep a model that can feed more than one target over time
- keep `FnHCI` as the broader interaction namespace
- allow `FnUI` to remain the narrower visual/package-facing term

## Core Requirement

`FnHCI.UI.Blazor` must be a Blazor-backed implementation of the `FnHCI.UI` visual system, not a thin renaming of Bolero and not a GUI model tied directly to one deployment mode.

That means it must:

- sit above Blazor, not beside it
- own the abstraction that authors views and state transitions
- target Blazor's render/runtime seam rather than exposing that seam directly as the primary authoring API
- allow later sibling runtime lines without forcing the whole `FnHCI` model to be Blazor-specific

## Hosting And Runtime Modes

One important distinction must stay explicit:

- `render modes`
  how Blazor components execute
- `host modes`
  where the overall app runs
- `deployment patterns`
  how the app is packaged and connected to backend services

These are related, but they are not the same thing.

### Required Browser-Facing Modes

`FnHCI.UI.Blazor` must support the current major Blazor browser-facing execution patterns:

1. static or non-interactive rendering where appropriate
2. interactive server rendering
3. interactive WebAssembly rendering
4. mixed or component-level render-mode composition where Blazor supports it
5. automatic or intermediate routing choices that ride Blazor improvements over time

This requirement exists so the visual system does not hard-code itself to one execution model too early.

### Required NEXUS Deployment Shapes

For NEXUS purposes, the following deployment shapes must be supportable over time:

1. server-only web app
   a server-hosted Blazor application where interaction stays server-side
2. wasm-first web app
   a browser-hosted interactive WebAssembly application
3. connected wasm app
   a WebAssembly client that talks to remote backend services over the network
4. mixed web app
   a web application that uses Blazor's evolving mixed or component-level execution options
5. native-hosted app
   a desktop or later mobile shell that hosts the Blazor-backed UI inside a native application container
6. PWA-capable web app
   a browser-based app that can also package as an installable Progressive Web App when the product benefits from it

`connected wasm app` is intentionally named here as a NEXUS deployment shape, not as the same thing as official `Blazor Hybrid`.

## Native Host Requirement

`FnHCI.UI.Blazor` must keep a host seam that allows native-hosted applications.

The first explicitly recognized native host family is:

- .NET MAUI Blazor Hybrid
- WPF Blazor Hybrid
- WinForms Blazor Hybrid

Those are the current official Microsoft-supported native host lines that matter here.

The architecture should also leave room for later exploration of:

- Photino
- Electron
- other webview-based or hybrid shells

but those should remain future evaluation targets rather than being treated as already equivalent to the official Blazor Hybrid hosts.

More detailed host-specific notes now live in [`docs/fnhci-ui-native-host-requirements.md`](fnhci-ui-native-host-requirements.md).

## Shell And View Requirements

`FnHCI.UI.Blazor` must support the first stable NEXUS GUI surfaces identified in [`docs/fnui-foundation.md`](fnui-foundation.md):

- current-ingestion dashboard
- provider/import history
- concept and LOGOS memory surfaces
- graph scope and batch exploration
- help/about and attribution visibility

CheddarBooks LaundryLog is the first named proving ground for this line:

- [`docs/laundrylog-fnui-proving-ground.md`](laundrylog-fnui-proving-ground.md)

The Blazor line must not bypass the renderer-neutral shell boundary already forming in the reusable `FnHCI.UI` layer.

That means:

- app shell shape belongs in `FnHCI.UI`
- Blazor host details belong in `FnHCI.UI.Blazor`
- runtime-specific rendering should adapt the shell, not replace it

## Abstraction Requirements

The system must provide an authoring model that is:

- F#-friendly
- explicit and deterministic
- not raw `BuildRenderTree` as the primary authoring experience
- capable of adapting to Blazor render output
- capable of later supporting other runtime lines where the higher-level visual model still makes sense

This also means `FnHCI.UI.Blazor` should consume and render a higher-level primitive model rather than owning that primitive model itself.

Penpot should fit beside this as a design and authoring projection surface:

- `FnHCI` owns the reusable interaction primitive
- Penpot maps design components and variants to that primitive
- Blazor maps the same primitive to runtime output

The same general rule should apply to tokens:

- Penpot can author and validate tokens
- `FnHCI.UI` should keep a stable token-model direction
- `FnHCI.UI.Blazor` should consume and project those tokens into the browser/runtime layer

This does not mean `FnHCI.UI.Blazor` must immediately support every other target.

It does mean the abstraction should avoid premature coupling to:

- one hosting model
- one deployment model
- one packaging story
- one visual authoring style

## Package And Naming Requirements

The target reusable-library namespace direction should continue to prefer the broader namespace truth:

- `FnTools.FnHCI`
- `FnTools.FnHCI.UI`
- `FnTools.FnHCI.UI.Blazor`

The current code namespace now uses `FnTools.FnHCI.*`.

Project and filesystem paths may still temporarily use the older `Nexus.FnHCI` project-path names until that extraction work happens.

Public package names may still use the narrower `FnUI` line when that is the clearest outward-facing name.

Likely candidates include:

- `FnUI`
- `FnUI.Blazor`

This is allowed because internal namespace truth and outward package naming do not have to be identical.

## Non-Goals For The First Pass

The first `FnHCI.UI.Blazor` foundation is not yet trying to:

- implement a full component library
- choose a final styling/theming system
- solve native mobile UX in detail
- replace all of Blazor
- build a full desktop shell
- define the whole of `FnHCI`

## First Implementation Targets

The next practical code steps should likely be:

1. define the renderer-neutral shell/view model more fully
2. define the first primitive catalog for cross-platform controls such as `Button` and `TextInput`
3. define the first token-model catalog for foundations, semantic tokens, and theme axes
4. define a Blazor host adapter over that shell, primitive set, and token model
5. define the first view contracts for ingestion, concepts, LOGOS, graph, and help/about
6. define how interaction commands and view state cross the host seam
7. evaluate which public package names should exist from the start versus later

## External References

These current requirements also intentionally track the current official Blazor shape:

- [ASP.NET Core Blazor render modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes)
- [Windows developer FAQ: hybrid and web development](https://learn.microsoft.com/windows/apps/get-started/windows-developer-faq)

Those official docs matter here because:

- Blazor render modes continue to evolve
- component-level mode composition is a real part of the platform
- .NET MAUI, WPF, and WinForms are current official native-host paths for Blazor-based UI
