# FnHCI.UI Token Model

This note captures the first durable token-model direction for `FnHCI.UI` and the narrower `FnUI` visual system line.

The goal is to keep the token model:

- compatible with what Penpot is already doing well
- useful across multiple runtime targets
- structured enough to support later deterministic tooling
- separate from business meaning while still close enough to UI semantics to matter

## Why This Note Exists

The current Penpot evidence is now strong enough to justify a first explicit token-model note.

Real `.penpot` exports now show:

- first-class `tokens.json` in some files
- `design-tokens/v1` feature support even in files that have not yet emitted a populated `tokens.json`
- token sets
- theme groups
- active theme selection metadata
- responsive breakpoint themes
- semantic token names such as `buttonPrimary.background.default`

That means we should stop treating tokens as just "some colors" and instead start treating them as a structured part of the `FnHCI.UI` and `FnUI` architecture.

## Boundary

Tokens are not:

- business events
- domain commands
- the owner of path meaning
- the owner of cross-platform interaction primitives

Tokens are:

- named reusable visual values
- named reusable visual meanings
- themeable inputs to runtime projection
- a bridge between Penpot and `FnUI`

So the general layering remains:

1. business and path semantics
2. `FnHCI` interaction primitives and view composition
3. token model
4. design and runtime projections

## Token Layers

The current best split is:

1. foundation tokens
2. semantic UI tokens
3. theme axes
4. active theme composition

### Foundation Tokens

Foundation tokens are the raw reusable building blocks.

Examples:

- base colors and palettes
- spacing values
- size scales
- typography families and weights
- radius values
- opacity values
- shadow values
- line widths
- breakpoint sizes

These should be reusable without already implying one specific component role.

In Penpot evidence, these show up as sets like:

- `Foundations`
- `Scale/Linear`
- `Scale/Modular`
- `Typography`
- `Shadows`

### Semantic UI Tokens

Semantic UI tokens sit above foundations and express visual meaning closer to components and layout roles.

Examples already visible in Penpot:

- `buttonPrimary.background.default`
- `buttonSecondary.border.focused`
- `layerBase.text`
- `input.border`
- `heading.text`

This is the most important bridge layer for `FnUI`, because these tokens are closer to interaction and layout meaning without being tied to one renderer.

### Theme Axes

Theme axes are orthogonal dimensions that activate different token sets.

The current evidence strongly suggests we should keep these axes separate instead of flattening them into huge combined names.

Current likely axes include:

- breakpoint
- color mode
- color theme
- density
- later brand
- later contrast or accessibility modes

This means we should prefer:

- breakpoint: `mobile`, `tablet`, `desktop`
- color mode: `light`, `dark`
- density: `compact`, `comfortable`, `spacious`

instead of combined preset names like:

- `light-mobile`
- `dark-desktop`

### Active Theme Composition

The active UI style should come from composing multiple theme axes at once.

That is already how Penpot models themes in the current examples.

So the working rule is:

- one active configuration may involve several enabled theme groups simultaneously
- the final visual values come from combining shared defaults plus axis-specific overrides

This is much closer to CSS-like composition than to one giant preset switch.

## Responsive Design Direction

Laura Kalbag's breakpoint explanation and the `Tokens and breakpoints.penpot` file both point toward the same responsive design pattern:

- keep shared defaults in a global set
- apply one breakpoint set as an override
- let that breakpoint theme combine with other themes such as light/dark

That means responsive design in our future model should likely treat breakpoint as:

- a theme axis
- not a separate parallel UI system

Examples of what a breakpoint axis might influence:

- viewport board width
- max-width values
- typography scale multipliers
- spacing scale choices
- component layout thresholds

## What Belongs In `FnHCI` Versus Tokens

The rough boundary should be:

- `FnHCI`
  owns the interaction primitive and the semantic role of the control
- tokens
  own the named visual-value and visual-meaning layer used to render that primitive

So:

- `Button` as an interaction primitive belongs in `FnHCI`
- `buttonPrimary.background.default` belongs in the token model

This keeps visual styling flexible without losing semantic UI structure.

## Penpot Mapping Direction

Penpot should be treated as:

- one strong design-token authoring and inspection surface
- one source of real token examples
- one place where theme composition can be validated visually

But Penpot should not be the only owner of the token model.

The future NEXUS and FnTools direction should likely be:

- define a stable internal token model
- import and export Penpot token structures
- preserve Penpot-compatible naming where it helps
- keep enough structure to support other runtimes too

The first concrete proving-ground note for this direction now lives in [LaundryLog FnHCI.UI Token Vocabulary](fnhci-ui-laundrylog-token-vocabulary.md).

## Runtime Mapping Direction

The token model should be renderer-neutral enough to project into multiple runtime targets.

Examples:

- Blazor or HTML:
  CSS custom properties, classes, inline styles, generated stylesheets, or mixed approaches
- Android:
  theme resources, Compose tokens, or generated constants
- iOS:
  SwiftUI/environment tokens or generated constants
- Web Components:
  custom properties and component styling contracts for a browser-specific runtime target

Web Components are relevant here as a possible browser runtime packaging target, not as the owner of the token model.

## Suggested First Internal Shapes

The first internal model likely wants to distinguish:

- token id
- token layer
  `foundation` or `semantic`
- token type
  `color`, `dimension`, `font-size`, `spacing`, `shadow`, and similar
- token value
- references to other tokens
- theme axis membership
- theme group membership
- runtime projection hints where necessary

That model should be explicit enough for later F# code generation, validation, and testing.

## What We Should Not Do

We should avoid:

- collapsing every token into one flat string bag
- treating all tokens as just colors
- hard-wiring token names to one renderer
- forcing responsive behavior into duplicated combined theme names
- relying on Penpot GUI state without preserving the structured token data

## Near-Term Next Steps

The most useful next moves are probably:

1. define a first explicit token vocabulary for LaundryLog
2. separate LaundryLog foundation tokens from semantic tokens
3. pick the first orthogonal theme axes we actually need
4. sketch the first F# token-model types in `FnTools`
5. later test projection of the same token set into Penpot and `FnUI`

## Evidence

Key sources for this direction include:

- [Penpot Access And Structure](penpot-access-and-structure.md)
- [FnHCI And Penpot Abstraction Boundary](fnhci-penpot-abstraction.md)
- [Penpot Working Loop For Event Modeling And FnHCI](penpot-working-loop.md)
- [LaundryLog FnHCI.UI Token Vocabulary](fnhci-ui-laundrylog-token-vocabulary.md)
- [Penpot Design Tokens Starter Pack](https://community.penpot.app/t/penpot-design-tokens-starter-pack/8982)
- [Breakpoint themes discussion](https://community.penpot.app/t/is-it-possible-to-create-breakpoints-modes-like-in-figma/9731/4)
