# LaundryLog FnHCI.UI Token Vocabulary

This note captures the first tiny `FnHCI.UI` and `FnUI` token vocabulary inferred from the current `LaundryLog.penpot` file.

The goal is not to freeze the design too early. The goal is to take the current Penpot work and express it in a form that can later become:

- explicit Penpot token sets
- explicit `FnTools` token-model types
- explicit runtime projection rules for `FnUI`

## Current Evidence

The current LaundryLog Penpot export shows:

- Penpot `2.14.1-RC1`
- file features including `design-tokens/v1`, `variants/v1`, `layout/grid`, and `components/v2`
- no populated `tokens.json` in the export archive yet
- a real shared color vocabulary under `colors/`
- component naming that already implies semantic UI roles
- a mobile-first board width of `375`

So the current file is token-capable, but it is still expressing most of the vocabulary through:

- shared colors
- shared components
- repeated frame sizes
- repeated text styles

That is a good enough base to define the first token vocabulary.

## Current Mobile Baseline

The current LaundryLog boards are clearly mobile-first.

Observed values include:

- `Screen.EntryForm` width: `375`
- `Path1.3-EntryForm` width: `375`
- `Screen.NewSession` width: `375`
- `Header` height: `72`
- `Input.Location` height: `52`
- secondary action button height such as `Button.GPS`: `56`
- primary submit button height such as `Button.Submit`: `64`

So the current first breakpoint assumption should be:

- `breakpoint = mobile`

Tablet and desktop should remain future theme-axis values, not invented visual rules yet.

## First Foundation Tokens

These are the first foundation tokens that appear to exist already in practice.

### Foundation Colors

The current Penpot file already exposes these shared colors:

- `color.neutral.0 = #ffffff`
- `color.neutral.50 = #f8f9fa`
- `color.neutral.100 = #e2e8f0`
- `color.neutral.300 = #cbd5e1`
- `color.neutral.400 = #94a3b8`
- `color.neutral.500 = #64748b`
- `color.neutral.600 = #475569`
- `color.neutral.800 = #2d3748`
- `color.primary.400 = #ffcc80`
- `color.primary.500 = #ffb74d`
- `color.primary.600 = #f57c00`
- `color.selected.background = #fff8e1`

These should become the base palette that semantic tokens point to, instead of every component carrying raw hex values separately.

### Foundation Typography

The current file strongly suggests this first typography foundation:

- `font.family.base = "Source Sans Pro"`
- `font.weight.regular = 400`
- `font.weight.bold = 700`
- `font.size.label = 14`
- `font.size.body = 16`
- `font.size.title = 24`
- `font.size.display = 48`
- `line-height.compact = 1.2`
- `letter-spacing.label = 0.7`

Observed examples:

- section labels such as `QUANTITY` use `14`, regular weight, and `0.7` letter spacing
- button and input text commonly use `16`, regular weight
- header title `LaundryLog` uses `24`, bold
- quantity display uses `48`, bold

### Foundation Dimensions

The current file suggests a small but useful first dimension layer:

- `size.viewport.mobile = 375`
- `size.header.height = 72`
- `size.field.height = 52`
- `size.action.height = 56`
- `size.submit.height = 64`

These are not yet a complete scale. They are simply the repeated dimensions that already appear stable enough to name.

### Foundation Spacing

The current file suggests these first spacing values:

- `space.12`
  observed between the location input and GPS button on the entry form
- `space.16`
  observed as the main entry-form horizontal gutter for the full-width submit area
- `space.24`
  observed as the stacked gap in the location prompt flow
- `space.32`
  observed as the horizontal gutter inside the centered location prompt

This does not mean the whole spacing scale is known yet. It means these values are already visible enough to become named candidates.

## First Semantic Tokens

The current LaundryLog file already carries a useful semantic layer through shared color names.

### Surface Tokens

- `surface.background.default -> color.neutral.50`
- `surface.card.default -> color.neutral.0`
- `surface.selected.soft -> color.selected.background`

These appear from:

- `Surface.Background`
- `Surface.Card`
- selected button backgrounds and current UI emphasis

### Text Tokens

- `text.primary -> color.neutral.800`
- `text.secondary -> color.neutral.600`
- `text.muted -> color.neutral.500`
- `text.disabled -> color.neutral.400`

These appear directly in the shared color vocabulary as:

- `Text.Primary`
- `Text.Secondary`
- `Text.Muted`
- `Text.Disabled`

### Header Tokens

- `header.background -> color.primary.500`
- `header.text -> color.neutral.0`
- `header.subtitle -> color.neutral.0`

These already exist as:

- `Header.Background`
- `Header.Text`
- `Header.Subtitle`

### Input Tokens

- `input.background -> color.neutral.0`
- `input.border.default -> color.neutral.100`
- `input.border.focus -> color.primary.500`
- `input.text -> color.neutral.800`
- `input.placeholder -> color.neutral.400`

These already exist as:

- `Input.Background`
- `Input.Border.Default`
- `Input.Border.Focus`
- `Input.Text`
- `Input.Placeholder`

### Button Tokens

The current Penpot file already names the first strong semantic button tokens:

- `button.background.default -> color.neutral.0`
- `button.background.selected -> color.selected.background`
- `button.background.disabled -> color.neutral.100`
- `button.border.default -> color.neutral.100`
- `button.border.selected -> color.primary.500`
- `button.border.disabled -> color.neutral.300`
- `button.text.default -> color.neutral.600`
- `button.text.selected -> color.primary.600`
- `button.text.disabled -> color.neutral.400`

This is especially important because the same token family should be usable across:

- `Button.Option`
- `Button.Counter`
- `Button.GPS`
- `Button.Machine.*`
- `Button.Payment.*`
- `Button.SetLocation`
- `Button.Submit`

The component variants should not each own separate color logic if they are really expressing the same semantic states.

### Status And Accent Tokens

One additional semantic accent is already visible in the quick-fill and price values:

- `price.text.accent -> color.primary.600`

This is not yet explicitly named as a shared color token in the same semantic way as header or input, but it is already visible enough to become one.

## First Theme-Axis Direction

The current LaundryLog token direction should stay minimal:

- `breakpoint = mobile`
- `color-mode = light`
- `density = default`

That means:

- only `mobile` is currently evidenced in the file
- dark mode should remain future work, not a guessed branch
- density should remain future work until we need compact or spacious variants

This matches the broader `FnHCI.UI` rule that theme axes should stay orthogonal instead of becoming flattened preset names.

## What This Means For FnHCI

The current LaundryLog file suggests the following split:

- `FnHCI`
  owns the primitives like `Button`, `TextInput`, `Header`, and `Card`
- tokens
  own the visual-value and semantic-style layer used by those primitives

So the current Penpot components are not the final abstraction by themselves.

Instead, they are evidence that we likely need:

- `Button` primitive roles
- `TextInput` primitive roles
- `Header` primitive roles
- `EntryCard` or `ListCard` roles

with tokens projected onto them.

## What Should Happen Next In Penpot

The next cleanup step in the actual LaundryLog Penpot file should probably be:

1. preserve the current shared color vocabulary
2. group it intentionally into foundation and semantic token sets
3. keep the existing names where they are already good
4. normalize inconsistent names where needed
5. make the file export a real `tokens.json`

That would give us a cleaner interchange surface for later automation.

## What Should Happen Next In FnTools

The next code-facing step should probably be:

1. create first explicit F# token ids for:
   - foundation colors
   - semantic text tokens
   - semantic input tokens
   - semantic button tokens
2. model `mobile` as the first concrete breakpoint token/theme value
3. keep theme axes explicit even when only one value exists today

## Relationship To The Broader Token Note

This LaundryLog note is the first concrete proving-ground example of the broader rules in:

- [FnHCI.UI Token Model](fnhci-ui-token-model.md)
- [CheddarBooks LaundryLog FnUI Proving Ground](laundrylog-fnui-proving-ground.md)
- [FnHCI And Penpot Abstraction Boundary](fnhci-penpot-abstraction.md)

It should remain easier to change than the broader token-model note.
