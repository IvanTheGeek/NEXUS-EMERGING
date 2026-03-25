# Create LOGOS Intake Note

Use this when you want to seed a non-chat intake item into durable repo memory before a full ingestion path exists for that source type.

This writes a Markdown note under `docs/logos-intake/`.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  create-logos-intake-note \
  --slug support-thread-123 \
  --title "Support Thread 123" \
  --source-system forum \
  --intake-channel forum-thread \
  --signal-kind support-question \
  --source-uri https://community.example.com/t/123
```

## Required Inputs

- `--slug`
  explicit file-safe slug using lowercase ascii letters, digits, and `-`
- `--title`
  human-readable note title
- `--source-system`
  explicit allowlisted source system
- `--intake-channel`
  explicit allowlisted intake channel
- `--signal-kind`
  explicit allowlisted signal kind
- at least one locator:
  - `--native-item-id`
  - `--native-thread-id`
  - `--native-message-id`
  - `--source-uri`

## Optional Inputs

- `--captured-at <iso-8601>`
- `--summary <text>`
- `--tag <slug>` repeatable
- `--docs-root <path>`

## Example Flows

Forum support thread:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  create-logos-intake-note \
  --slug support-thread-123 \
  --title "Support Thread 123" \
  --source-system forum \
  --intake-channel forum-thread \
  --signal-kind support-question \
  --source-uri https://community.example.com/t/123 \
  --tag support
```

Deployed app feedback item:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  create-logos-intake-note \
  --slug startup-feedback-2026-03 \
  --title "Startup Feedback" \
  --source-system app-feedback-surface \
  --intake-channel app-feedback \
  --signal-kind feedback \
  --native-item-id fb-2026-03-25-001 \
  --summary "Users are confused about the onboarding transition."
```

## Why This Exists

This is a bridge workflow.

It lets LOGOS intake become durable and inspectable now, while keeping the future option open for richer ingestion, indexing, and refinement later.

## Related

- `docs/how-to/report-logos-catalog.md`
- `docs/logos-source-model-v0.md`
