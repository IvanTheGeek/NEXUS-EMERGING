# Interaction Concern Lines, Contexts, And Lenses

This note hardens the current interaction-model vocabulary emerging around `FnHCI`, LaundryLog, screen paths, and the distinction between business Event Modeling and app/runtime behavior.

It is meant to stay practical and example-driven.

## Why This Note Exists

The recent LaundryLog work made it clear that several useful interaction concerns were being discussed together:

- business Event Modeling
- app startup and route behavior
- screen-by-screen user-visible state changes
- how screens and other interaction surfaces are composed
- how UI-side behavior coordinates with business-side behavior

Those are related, but they are not the same thing.

This note separates:

- `concern line`
- `domain`
- `bounded context`
- `lens`

so the later NEXUS, FnHCI, and FORGE work can evolve without flattening everything into one board or one vocabulary.

## Working Vocabulary

### Concern Line

A `concern line` is a practical thread of work, meaning, or pressure that we know matters, even if its final formal boundary is not settled yet.

Use it when:

- the area is clearly real
- the terms are still being worked out
- we do not want to pretend the final bounded-context shape is already stable

Examples:

- app startup behavior
- screen-path flow
- interaction composition
- business Event Modeling

Working rule:

- use `concern line` first when the semantics are still moving
- promote it to a `bounded context` when the vocabulary, rules, and responsibility become stable enough

### Domain

A `domain` is a broad area of reality or work.

Examples:

- `SoftwareDevelopment`
- `InteractionDesign`
- `BusinessOperations`

For the current discussion, the broader domain pressure is roughly:

- interaction / software-development work around app behavior and projections

### Bounded Context

A `bounded context` is a specific semantic boundary with its own vocabulary, rules, and responsibilities.

Use it when we can say:

- what the terms mean here
- what belongs here
- what does not belong here

Examples for the current interaction direction:

- `EventModeling`
- `ApplicationLifecycle`
- `RuntimeOrchestration`
- `ScreenPath`
- `InteractionComposition`

### Lens

A `lens` is a way of viewing or projecting the underlying reality for a purpose.

A lens does not change the deeper truth. It changes:

- emphasis
- grouping
- naming
- visibility
- audience fit

Examples:

- `AEM Lens`
- `Screen Path Lens`
- `App Runtime Lens`
- `Interaction Composition Lens`

## The Current Structural Direction

### FnHCI

`FnHCI` should be read as the broader interaction-system area, not just visual UI and not just one renderer.

It is currently the best umbrella for:

- visual UI
- terminal and text interaction
- CLI-guided workflows
- API and contract-facing interaction surfaces
- device interaction
- cross-surface human workflow
- coordination between interaction surfaces and business behavior

That means `FnHCI` is broader than:

- one screen renderer
- one app
- one UI runtime
- one Event Modeling board

### FnUI

`FnUI` should be treated as one sub-area or lens inside the wider `FnHCI` direction, not the whole thing.

In practical terms:

- HTML/CSS rendering can be one `FnUI` projection
- Android native rendering can be another
- iOS native rendering can be another

### Current Candidate Bounded Contexts Under The Interaction Area

These are the current most useful working areas.

They are not all frozen forever, but they are stable enough to guide the next phase.

#### EventModeling

This is the business Event Modeling context.

It is where:

- commands
- events
- business views

are modeled as business meaning.

Example:

- `CaptureLaundryLocation`
- `LaundryLocationCaptured`
- `CurrentLaundrySession`

Useful lens:

- `AEM Lens`

#### ApplicationLifecycle

This is the bounded context for app lifecycle state and transitions.

It is where events like these belong:

- `AppStarted`
- `AppResumed`
- `AppSuspended`
- `StartupChecksCompleted`
- `LocalStateHydrated`

Short rule:

- if it is about the app entering, leaving, or advancing lifecycle phase, it likely belongs here

Useful lens:

- `App Runtime Lens`

#### RuntimeOrchestration

This is the bounded context for coordinating behavior between the UI-side system and the business-side system.

This is very close to the seam `FnHCI` should help provide.

It includes things like:

- route resolution
- startup sequencing
- local-state hydration before the first screen is shown
- deciding which screen/state to show next
- sync/retry behavior that changes what the user sees
- UI/business coordination that is real but not itself the business fact

Example:

- app starts
- PWA/runtime checks run
- local session state is checked
- app routes to `Need Location`

Short rule:

- if it is about coordinating what the interaction system does next rather than about the business event itself, it likely belongs here

Useful lens:

- `App Runtime Lens`

#### ScreenPath

This is the bounded context for ordered user-visible screen states.

It is not mainly about business facts.

It is about:

- what screen the user sees
- what the next screen state is
- how those screen states flow in order

Example LaundryLog path:

1. `Need Location`
2. `Ready To Set Location`
3. `Entry Form Ready`
4. `Washer Draft`
5. `Logged Success`

Useful lens:

- `Screen Path Lens`

### Path Page Labeling Rule

For the current LaundryLog-style path pages, use this practical hierarchy:

- `domain`
  broader and usually page-level, not repeated on every step
- `bounded context`
  shown per step when it helps disambiguate meaning
- `lens`
  shown per step and controllable at the page level when the viewer wants to enable or disable families of meaning
- `surface`
  the concrete rendered thing being shown for that step

Example:

- `Context · RuntimeOrchestration`
- `app runtime lens`
- `Screen.AppStart - Runtime Checks`

Short rule:

- do not use `lens` as a substitute for `context`
- on a path page, `context` answers “what semantic area is this step in?”
- `lens` answers “from what viewing/projection perspective are we currently looking at it?”
- the broader `domain` can usually stay implicit unless the page specifically needs to teach it

#### InteractionComposition

This is the bounded context for how an interaction surface is composed from reusable primitives, layout rules, states, and tokens.

This is deliberately broader than only `ScreenComposition`.

Why broader:

- some interaction surfaces are graphical screens
- some are terminal/text apps
- some are CLI-guided workflows that still feel app-like
- some may be contract or API-facing surfaces

Examples:

- compose a LaundryLog mobile screen in HTML/CSS
- compose an Android-native version of the same interaction surface
- compose a terminal/TUI version of the same flow

Short rule:

- if the question is “how is this interaction surface built?” rather than “what business fact happened?” or “which screen comes next?”, it likely belongs here

Useful lens:

- `Interaction Composition Lens`

## Worked Example

LaundryLog startup and first entry can now be described more clearly:

### ApplicationLifecycle

Examples:

- `AppStarted`
- `StartupChecksCompleted`

### RuntimeOrchestration

Examples:

- determine whether this is a first launch
- check local/PWA/runtime readiness
- decide whether to route to `Need Location`

### ScreenPath

Examples:

1. `Need Location`
2. `Ready To Set Location`
3. `Entry Form Ready`
4. `Washer Draft`
5. `Logged Success`

### EventModeling

Examples:

- `CaptureLaundryLocation`
- `LaundryLocationCaptured`
- `LogLaundryExpense`
- `LaundryExpenseLogged`
- `CurrentLaundrySession`

### InteractionComposition

Examples:

- HTML/CSS mobile screen for LaundryLog
- Android-native screen for the same flow
- later terminal/TUI version for the same conceptual interaction

## Chain From The Current Screen Work To The Full Term Set

The current LaundryLog screen work is easier to reason about if we say exactly where each part belongs.

### 1. Broader Interaction Direction

- `FnHCI`
  the broader interaction-system area
- `FnUI`
  one projection family inside that broader direction

The current HTML/CSS screen work is best read as a `FnUI` projection inside the wider `FnHCI` direction.

### 2. Bounded Contexts In Play

- `ApplicationLifecycle`
  for things like `AppStarted`
- `RuntimeOrchestration`
  for startup checks, hydration, and deciding what to show next
- `ScreenPath`
  for ordered screen states such as `Need Location` -> `Entry Form Ready` -> `Logged Success`
- `InteractionComposition`
  for how the current LaundryLog surface is built from reusable interaction parts
- `EventModeling`
  for business commands, events, and business views

### 3. Lenses In Play

- `App Runtime Lens`
  emphasizes lifecycle and orchestration behavior
- `Screen Path Lens`
  emphasizes ordered user-visible screen states
- `Interaction Composition Lens`
  emphasizes how the surface is built
- `AEM Lens`
  emphasizes Adam/Event Modeling business flow

### 4. LaundryLog Example Weave

One practical weave for the current LaundryLog direction is:

1. the user taps the app icon
2. `AppStarted` belongs to `ApplicationLifecycle`
3. startup checks, local-state hydration, and initial route resolution belong to `RuntimeOrchestration`
4. `Need Location`, `Ready To Set Location`, `Entry Form Ready`, `Washer Draft`, and `Logged Success` belong to `ScreenPath`
5. `CaptureLaundryLocation`, `LaundryLocationCaptured`, `LogLaundryExpense`, `LaundryExpenseLogged`, and `CurrentLaundrySession` belong to `EventModeling`
6. the HTML/CSS surface that shows those states belongs to `InteractionComposition`

That weave is useful because it stops one screen artifact from pretending to be the whole truth. The screen work is real, but it is only one woven part of the fuller model.

Important presentation rule:

- do not collapse this into a fixed linear `COMMAND -> EVENT -> VIEW` story
- the better reading is:
  - a command slice produces event fact(s)
  - later view slices consume prior event fact(s)
  - the consumed event does not need to come from the immediately previous slice
  - multiple views may consume the same prior event

So a phrase like:

- `LaunchApp -> AppStarted -> SplashVisible`

is too linear for the current NEXUS/LaundryLog rule.

Better wording is:

- `LaunchApp` is the command slice title
- `AppStarted` is the event produced by that command slice
- later view slices such as `SplashVisible` consume prior event fact(s) through their own view explanation rather than as a forced third link

## Browser Control, Playwright, And The Term Set

The recent LaundryLog HTML/CSS work also exposed a useful distinction between:

- direct browser inspection/control
- explicit browser automation frameworks such as Playwright

They are related, but they are not the same thing.

### Direct Browser Inspection Or Control

This is best understood as an inspection and debugging surface.

It is useful for:

- looking at the current rendered artifact
- checking layout or visible state quickly
- confirming what the browser actually showed
- manually exploring a path while the shape is still in flux

In the current term set, this mainly supports:

- `InteractionComposition`
- `ScreenPath`

It is especially useful through:

- `Interaction Composition Lens`
- `Screen Path Lens`

Short rule:

- direct browser control is good for inspection, exploration, and visual debugging
- it is not automatically a durable automation contract

### Playwright Or Similar Browser Automation

Playwright, Selenium, and similar tools are not mainly “another way to look at the browser.”

They are repeatable automation harnesses.

They are useful for:

- scripted navigation
- deterministic clicking and typing
- waiting for stable UI states
- asserting path behavior
- capturing reproducible failures
- running the same browser checks later in CI or by other humans

In the current term set, this most naturally supports:

- `InteractionComposition`
  when the question is whether the rendered interaction surface behaves correctly
- `ScreenPath`
  when the question is whether ordered visible states transition correctly
- `RuntimeOrchestration`
  when startup, routing, hydration, or app/runtime coordination changes what is shown

Short rule:

- Playwright is best understood as a deterministic testing and verification surface over the same interaction/runtime concerns

### Practical Distinction

Use direct browser inspection when:

- the goal is to understand what the current artifact does
- the work is still being shaped visually
- a human or AI needs quick feedback about what rendered

Use Playwright-style automation when:

- the behavior should be repeatable
- the path should be asserted rather than just observed
- the result should be rerunnable by other humans, AI, or CI
- a bug needs a stable reproduction harness

### LaundryLog Example

For the current LaundryLog HTML path and screen work:

- opening the generated HTML file and inspecting it visually is direct browser inspection
- clicking through it manually to see what feels wrong is still direct browser inspection
- a future scripted check that asserts the path nav buttons move exactly one column at a time would be Playwright-style automation

So:

- browser inspection helps us understand the current interaction surface
- Playwright would help us prove the surface behaves correctly in a repeatable way

## What This Means For FnHCI

`FnHCI` should not be reduced to only:

- widgets
- screen layout
- one HTML renderer

It is better understood as the broader interaction-system area spanning or helping weave:

- `ApplicationLifecycle`
- `RuntimeOrchestration`
- `ScreenPath`
- `InteractionComposition`

while still needing to relate correctly to:

- `EventModeling`

That gives `FnHCI` a healthier role:

- not just “UI”
- not just “runtime”
- not just “screens”
- but a broader interaction model that can support multiple projections and multiple human-facing surfaces

## Naming Guidance For Now

Use these names for the current work unless we later replace them deliberately:

- `AEM Lens`
  the Adam/Event Modeling business-flow lens
- `ApplicationLifecycle`
  bounded context for lifecycle events like `AppStarted`
- `RuntimeOrchestration`
  bounded context for UI/business/app coordination
- `ScreenPath`
  bounded context for ordered user-visible screen states
- `InteractionComposition`
  bounded context for building interaction surfaces
- `App Runtime Lens`
  lens over lifecycle and orchestration concerns
- `Screen Path Lens`
  lens over ordered screen-state flow
- `Interaction Composition Lens`
  lens over how a surface is built

## What Is Still Open

These are not fully settled yet:

- whether `RuntimeOrchestration` should stay the long-term name
- whether `ApplicationLifecycle` and `RuntimeOrchestration` later need one higher interaction-runtime umbrella context
- how `FnUI`, `FnCLI`, `FnAPI`, and other projection families should relate beneath `FnHCI`
- how the different path lenses should later weave together into one fuller model without losing clarity

## Working Rule

When a new interaction concern appears:

1. name it as a `concern line` first if its semantics are still moving
2. promote it to a `bounded context` when vocabulary and rules are stable enough
3. define one or more `lenses` over it only after the context is clear enough to support useful projections

That should keep the model flexible without staying vague forever.
