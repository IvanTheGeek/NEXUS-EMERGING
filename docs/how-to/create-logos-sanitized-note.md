# Create LOGOS Sanitized Note

Use this when a restricted LOGOS intake note should produce a safer derived note for a broader sharing scope.

This is not the same as widening access to the raw intake note.

The intended pattern is:

- keep the raw or restricted intake note under `docs/logos-intake/`
- create an explicit derived note under `docs/logos-intake-derived/`
- carry forward source classification and handling-policy provenance
- do not copy raw locators or raw source text into the derived note

## What It Does

`create-logos-sanitized-note`:

- reads one existing `logos_intake_seed` note from `docs/logos-intake/`
- preserves the source classification and source handling policy
- writes a new derived note under `docs/logos-intake-derived/`
- requires an explicit derived sanitization status
- lets you narrow or widen the derived handling policy explicitly, within the allowlisted model

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  create-logos-sanitized-note \
  --source-slug support-thread-123 \
  --slug support-thread-123-redacted \
  --title "Support Thread 123 (Redacted)" \
  --sanitization-status redacted
```

## Useful Options

- `--source-slug <slug>`
  Required. Picks the source note under `docs/logos-intake/`.
- `--slug <slug>`
  Required. The new derived note slug.
- `--title <title>`
  Required. The new derived note title.
- `--sanitization-status <redacted|anonymized|approved-for-sharing>`
  Required. The derived sanitization state.
- `--sensitivity <slug>`
  Optional. Defaults to the source note value.
- `--sharing-scope <slug>`
  Optional. Defaults to the source note value.
- `--retention-class <slug>`
  Optional. Defaults to the source note value.
- `--summary <text>`
  Optional. Sanitized summary text for the derived note.
- `--tag <slug>`
  Optional. Repeatable.
- `--docs-root <path>`
  Optional. Override the docs root.

## Important Rules

- import permission is not publication permission
- the source note remains the restricted note of record
- the derived note is an explicit transform, not a silent replacement
- raw locators and raw source text stay in the source note
- `approved-for-sharing` requires an explicit `--sharing-scope`

## Example

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  create-logos-sanitized-note \
  --source-slug cheddarbooks-debug-case-42 \
  --slug cheddarbooks-case-42-anonymized \
  --title "CheddarBooks Case 42 (Anonymized)" \
  --sanitization-status anonymized \
  --sharing-scope project-team \
  --summary "Customer-identifying details removed while preserving the debugging pattern."
```

## Result

The command writes:

- source note remains in `docs/logos-intake/`
- derived note appears in `docs/logos-intake-derived/`

The derived note includes:

- source classification
- source handling policy
- derived handling policy
- derivation pointer back to the source note

The derived note excludes:

- raw locator lists
- raw copied source text
