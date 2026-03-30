# F# Usage, Learning, And Guidance

This note establishes a NEXUS-wide concern line for how F# usage guidance, learned seams, and AI-facing working rules should be captured once they become known.

It is meant to stay practical.

## Why This Note Exists

As NEXUS and downstream repos like CheddarBooks evolve, the same kinds of F# questions keep appearing:

- how this repo usually wants something done
- which of several valid implementation shapes is preferred
- when a local renderer or generated artifact must be inspected directly
- when tests should be added or changed
- when a human preference is strong enough to stop guessing and ask

If those learnings stay only in chat, every later AI has to rediscover them.

This note makes that learning line durable and reusable.

It also creates the beginnings of an F# knowledgebase that can later be reworked into:

- human onboarding material
- internal tutorials
- training modules
- reusable examples and playbooks

## Concern Line Status

This is currently best treated as a `concern line`, not a frozen bounded context.

Why:

- the need is clearly real
- the stable sub-areas are still emerging
- some guidance is NEXUS-wide while some should stay local to a repo, tool, or workstream

Short version:

- foundation guidance lives in NEXUS
- local specializations live in the repo where the work is happening

## What This Concern Line Is For

Use this concern line to capture:

- the usual way NEXUS wants F# work approached
- stable lessons learned after hitting an issue once
- AI-facing guidance about how to inspect, verify, and change F# code safely
- when to prefer one implementation shape over another
- when to ask the human for clarification instead of guessing

Done well, this line should eventually support both:

- AI agents that need operational guidance now
- humans who need clearer training and examples later

This is not mainly for:

- basic F# language tutorials
- exhaustive style-law bureaucracy
- replacing code, tests, or API docs

## Tool Choice And Language Choice

NEXUS defaults toward F# and its general paradigm because of the benefits it often provides for:

- correctness
- determinism
- explicit modeling
- reusable composition
- durable reviewable transforms

But this is not a rule that everything must be rebuilt in F# regardless of fit.

NEXUS is explicitly allowed to use other tools and languages when they fit the concern better or unblock progress more effectively.

Short rule:

- default toward F#
- do not force F# where another tool is the better fit for the current concern
- make the reason visible and durable when the choice matters later
- AI agents are welcome to suggest those alternatives proactively rather than waiting to be asked, as long as the suggestion is explicit and the tradeoff is explained
- the human has the final decision; once a choice is made, follow it, but later agents may still respectfully suggest a stronger option if the concern materially changes or a clearly better fit becomes apparent

Examples:

- Playwright for browser interaction verification
- Expecto for .NET and F# code testing
- Python libraries when the needed capability is already mature there
- existing systems such as Talkyard when contributing, extending, or integrating is more valuable than rebuilding

## The Three Main Moves

When F# is the preferred center but another tool, system, or library is involved, NEXUS will usually do one of three things:

### 1. Build On Top

Use the existing thing as the substrate and add value above it.

Examples:

- use Blazor as the underlying web/UI substrate and build a stronger seam above it
- use Talkyard and extend or contribute back rather than replacing it immediately
- use an existing browser automation tool while keeping NEXUS-owned modeling and artifact generation above it

### 2. Wrap

Keep the existing thing, but create an F#-friendly or NEXUS-friendly seam over it.

Examples:

- create an F# wrapper over `Verify` rather than rebuilding snapshot verification from scratch
- create a local renderer or orchestration seam over an existing library instead of scattering direct low-level calls everywhere

### 3. Replace

Replace the existing thing only when the fit is no longer acceptable and progress is being blocked enough to justify the cost.

Examples:

- `FnHCI.Blazor` as a replacement direction after investigating Bolero and finding the seam too problematic
- swapping out an existing dependency when a better alternative is clearly stronger for the concern

## When To Shift Away From The Default

A good trigger for using something other than the default F#-centered path is when:

- the current path is blocking progress
- the required behavior is already mature elsewhere
- the concern is better owned by a specialized tool
- wrapping or building on top is clearly cheaper and safer than rebuilding
- replacement is justified by repeated friction or architectural mismatch

This is not treated as betrayal of the F# direction.

It is treated as choosing the right tool at the right layer while keeping the broader NEXUS direction coherent.

## The Main Layers

### NEXUS Foundation Layer

NEXUS should hold the durable rules that are broad enough to matter across multiple repos or workstreams.

Examples:

- docs/tests/source layering
- durable learning protocol
- when to inspect actual rendered artifacts
- when tests are expected by default
- how to think about “usual way” versus “this time”

### Local Repo Layer

When a repo has stronger local rules, record them there.

Examples:

- a specific verify sequence
- renderer-specific inspection rules
- a local UI or HTML generation contract
- a preferred library seam for that repo

Rule:

- local repo guidance can focus or override the broader NEXUS guidance when the local repo has already made a clearer decision

### Code, Docs, And Tests Layer

The practical working pattern remains:

- Markdown docs explain concepts, rules, and guidance
- source explains the actual implementation and public API intent
- tests prove behavior and act as executable examples

## Core Working Rules

### 1. Record Learned Seams Once They Are Understood

When an issue is encountered, understood, and resolved well enough to matter later:

- add or update a durable note
- do not leave the learning only in chat

Examples:

- a renderer needs direct artifact inspection
- a generated output should be verified through snapshot tests
- a local build/test sequence must be run serially
- a repo-provided verify script should be preferred over ad hoc command reconstruction

### 2. Prefer The Usual Way When It Exists

When there are multiple technically valid ways to do something:

- prefer the repo’s usual way if it is already known
- do not invent a new local pattern casually

Examples:

- use the repo’s established test stack rather than a new ad hoc harness
- use the repo’s established renderer verification pattern rather than guessing
- follow the repo’s established doc/source/test layering rather than moving everything into one layer
- when a repo already has a checked-in helper script for build/test/refresh/verify, use that script as the normal entry point unless there is a task-specific reason not to

### 3. Ask When “This Time” Could Reasonably Mean Something Different

If multiple valid approaches exist and the choice has non-obvious consequences, ask before committing to one.

Good triggers for clarification:

- public API shape may change
- naming has lasting semantic consequences
- visible UX or layout can reasonably go more than one direction
- multiple implementation seams are valid and the usual way is not yet settled
- the human may want an exception to the usual way for this task

Short rule:

- prefer the usual way by default
- ask when “this time” might intentionally be different

### 4. Let Human Feedback Harden The Rule

When the human corrects a recurring choice:

- treat that as a candidate durable rule
- add it to the right repo surface if it should guide future work

Examples:

- “inspect the real renderer/artifact first instead of guessing”
- “add or update tests when behavior changes”
- “use the current repo startup docs before substantial work”

### 5. Prefer Direct Inspection Over Inference For Behavior Work

For renderer, HTML, CSS, UI, generated-output, or visible-behavior work:

- inspect the actual current source
- inspect the actual generated artifact
- then change behavior

Do not rely on memory or assumption when the real artifact can be inspected locally.

When rendering a live DOM preview of an interactive surface:

- do not wrap the whole preview in a literal `<button>` if the preview already contains buttons, inputs, links, or other interactive controls
- that creates invalid nested interactive HTML and browsers may silently restructure the DOM in ways that break scaling, clipping, and layout assumptions
- prefer either:
  - a non-button activator wrapper with `role="button"` and keyboard handling
  - or a separate explicit expand button beside the preview
- if the preview must be visually scaled, use a clipped preview frame with explicit sizing instead of hoping a bare `transform: scale(...)` contract will preserve layout on its own

### 6. Distinguish Inspection Surfaces From Automation Harnesses

A browser that an AI can inspect or control directly is not the same thing as a durable automation harness.

Direct browser inspection is useful for:

- visual debugging
- layout checks
- confirming what rendered right now

Automation harnesses such as Playwright are useful for:

- repeatable browser assertions
- stable path checks
- rerunnable bug reproduction
- later CI-friendly verification

### 7. Distinguish `file://` From Local HTTP In Browser Automation

For local generated HTML artifacts, manual browser review and browser automation do not always share the same access rules.

Current durable rule:

- `file://` may still be acceptable for manual browser opening or a secondary smoke check
- Playwright MCP should not default to `file://`
- the Playwright MCP browser sandbox blocks `file:` URLs
- when using Playwright MCP against a local artifact, serve the tracked artifact tree over local HTTP first, then target `http://127.0.0.1/...`

Practical consequence:

- use `file://` only when the task specifically calls for manual or secondary compatibility verification
- for interactive MCP browser debugging and for the formal Playwright browser suite, use local HTTP as the normal path

Example:

- tracked LaundryLog workspace HTML is reviewed from `workspace/`
- Playwright Test serves that workspace tree over local HTTP
- Playwright MCP should do the same instead of trying to open the tracked artifact directly with `file://`

### 7. Prefer `dotnet fsi --exec` Over Raw REPL Heredocs

When F# Interactive is used from shell automation, artifact generation, or AI tool execution:

- do not treat raw `dotnet fsi <<'EOF' ... EOF` REPL invocation as the preferred normal path
- prefer a checked-in `.fsx` script or a temporary `.fsx` file
- run it with `dotnet fsi --exec path/to/script.fsx`

Why:

- raw `dotnet fsi` behaves like the REPL and may exit with code `1` after evaluating the script body even when the F# code itself succeeded
- the command display from wrapped shell tools can make `$\"...\"` F# interpolation look shell-mangled even when the actual script content is still correct
- `--exec` gives a cleaner non-interactive execution shape and a more trustworthy success/failure signal

Working rule:

- if the task is one-off, write a temporary `.fsx` file and run it with `--exec`
- if the task is recurring or important, prefer a checked-in `.fsx` helper script
- use a quoted heredoc such as `<<'EOF'` only for writing the script file content, not for driving the `dotnet fsi` REPL directly

Short version:

- the real issue is usually REPL-mode invocation, not F# string interpolation
- the preferred solution is `.fsx` plus `dotnet fsi --exec`

Short rule:

- inspect directly to understand the current behavior
- use automation harnesses when the behavior needs to be proven repeatably

## The Current Usual Way

The current NEXUS-wide usual way is:

1. docs spine first
2. examples and tests next
3. source after that
4. XML docs and low-level API inspection as supporting detail

And for code changes:

- update tests when behavior changes
- if a relevant test is not added or updated, say why explicitly
- when a learning should matter later, record it durably

This is meant to reduce repeated rediscovery, not to force ceremony.

## Examples

### Example 1: Renderer Work

If an AI is changing an HTML renderer:

- inspect the current renderer source
- inspect the current generated HTML artifact
- change the code
- update tests if visible behavior changes
- if a recurring seam is discovered, record it in docs

### Example 2: Multiple Valid Layout Tools

If both CSS flexbox and CSS grid could solve the problem:

- prefer the repo’s usual way if one already exists for that surface
- if both are still valid and the choice affects future extensibility, ask
- if the human clarifies the intended direction, record that as guidance

### Example 3: Local Verify Sequence

If a repo learns that `dotnet build` and tests should be run serially against one output tree:

- keep the local command rule in that repo
- do not assume the same command pattern applies everywhere
- keep the broader NEXUS rule at the level of “verify behavior and record local procedure where it matters”

### Example 4: Browser Control Versus Playwright

If an AI can open a generated HTML file and inspect it in a browser:

- that is a useful inspection surface
- it is not automatically the same thing as a reusable browser test

If the team later needs to prove that a path button advances exactly one screen column at a time:

- use the direct browser surface to understand the current bug
- then prefer a Playwright-style harness for the repeatable assertion

For horizontally scrolling path surfaces, a useful learned rule is:

- do not guess whole-column targets from container widths alone
- measure the real rendered column starts in the browser
- derive the logical left-edge targets from those measured positions
- and allow explicit trailing space when a readable whole-column target needs a small blank tail at the far right
- if a separate scroll rail is representing that logical range, size the rail from the logical range too, not only from the native content width
- and if stable scrollbar gutters are enabled, account for the rail's full box width rather than only its client width

And if button-driven smooth scrolling is still drifting or stopping early:

- do not keep guessing at CSS widths
- verify whether the browser is applying native smooth scrolling the way the interaction needs
- if not, prefer an explicit programmatic animation that keeps the related scroll surfaces in sync from one shared target value

For local `file://` HTML artifacts that need update awareness:

- do not assume the page can reliably `fetch()` itself like a served web app
- verify what the actual browser/file-origin behavior allows
- if direct self-fetch is blocked, prefer a small regenerated sidecar manifest script that the page can poll
- keep the current artifact version in that companion file
- and keep sticky viewer/update preferences in browser `localStorage`, not in the artifact itself

## Relationship To Existing Docs

This note complements, not replaces:

- [`fsharp-documentation-convention.md`](fsharp-documentation-convention.md)
- [`how-to/run-tests.md`](how-to/run-tests.md)
- [`agent-readme.md`](agent-readme.md)
- [`cortex-repo-memory-protocol.md`](cortex-repo-memory-protocol.md)

Short mapping:

- `fsharp-documentation-convention.md`
  where docs/source/tests each belong
- `how-to/run-tests.md`
  how to run tests in the foundation repo
- this note
  how learned F# guidance becomes durable and how AI should behave when implementation choices are not trivial

## Open Questions

Still open:

- whether this concern line later deserves one or more bounded contexts beneath it
- which parts should eventually move into reusable FnTools/FnHCI guidance
- how much should stay foundation-wide versus local to app-line repos

## Working Rule

When an F# issue or implementation choice is encountered:

1. inspect the stronger local sources first
2. use the usual way if it already exists
3. ask when the choice has non-obvious consequences and “this time” may legitimately differ
4. once the seam is understood, record it durably in the right repo surface

That is how NEXUS should get progressively easier for both humans and AI to work in.
