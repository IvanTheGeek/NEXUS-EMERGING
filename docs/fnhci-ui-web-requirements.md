# FnHCI.UI.Blazor Web Requirements

This note captures the browser-facing and web-delivery requirements that sit under the broader [`FnHCI.UI.Blazor` requirements](fnhci-ui-blazor-requirements.md).

## Purpose

These requirements exist so the visual system can support the major web-facing deployment shapes without forcing the whole UI line into one execution model.

## Required Web Shapes

The web line must support:

1. server-only web app
2. wasm-first web app
3. connected wasm app
4. mixed web app using Blazor's evolving render-mode composition
5. Progressive Web App packaging where it fits the product

## PWA Requirement

Progressive Web App support is now an explicit requirement for the web-facing FnUI line.

This matters because:

- CheddarBooks LaundryLog is a strong candidate for phone-first use
- a PWA shape can reduce friction for individual and household adoption
- a PWA shape keeps the same core visual line usable across browser and installable experiences

PWA support should be treated as a deployment and packaging concern over the web line, not as a separate UI model.

## Render-Mode Requirements

The web line must support the current major Blazor browser-facing execution patterns:

- static or non-interactive rendering where appropriate
- interactive server rendering
- interactive WebAssembly rendering
- mixed or component-level render-mode composition where Blazor supports it
- automatic or intermediate routing choices that ride Blazor improvements over time

## Connected WASM Requirement

`connected wasm app` remains a named NEXUS deployment shape:

- browser-hosted UI
- remote backend services over the network
- optional cloud-backed capabilities where they add value

This is intentionally not the same thing as official `Blazor Hybrid`.

## Offline Requirement

The web line should not assume constant connectivity.

This matters especially for CheddarBooks LaundryLog-style usage:

- phone use
- travel use
- intermittent network conditions
- later convergence with desktop or other nodes

The exact offline strategy is still open, but the web requirements must leave room for:

- offline entry
- delayed sync
- later convergence of changes

## Non-Goals

This note does not yet define:

- exact service-worker behavior
- exact caching policy
- exact sync protocol
- exact authentication model

Those should follow from the domain and host seams rather than being guessed here.
