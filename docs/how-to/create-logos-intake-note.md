# Create LOGOS Intake Note

Use this when you want to seed a non-chat intake item into durable repo memory before a full ingestion path exists for that source type.

This writes a Markdown note under `docs/logos-intake/<pool>/`.
New notes default to a restricted handling policy unless you explicitly choose other allowlisted values.
Access and rights metadata are also explicit from entry time because technical visibility does not imply publication permission.

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  create-logos-intake-note \
  --slug support-thread-123 \
  --title "Support Thread 123" \
  --source-system talkyard \
  --source-instance forum-ivanthegeek \
  --access-context registered-user \
  --acquisition-kind web-scrape \
  --rights-policy review-required \
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
- `--source-instance <slug>`
  optional explicit source instance such as one concrete forum host or repo surface
- `--access-context <slug>`
  optional explicit access context. Defaults to `owner`.
- `--acquisition-kind <slug>`
  optional explicit acquisition kind. Defaults to `manual-note`.
- `--rights-policy <slug>`
  optional explicit rights policy. Defaults to `review-required`.
- `--attribution-reference <text>`
  optional attribution or license reference that should be preserved for later public-safe use
- `--intake-channel`
  explicit allowlisted intake channel
- `--signal-kind`
  explicit allowlisted signal kind
- `--entry-pool <raw|private|public-safe>`
  optional explicit pool boundary for where the new note enters. Defaults to `raw`.
- at least one locator:
  - `--native-item-id`
  - `--native-thread-id`
  - `--native-message-id`
  - `--source-uri`

## Optional Inputs

- `--sensitivity <slug>`
- `--sharing-scope <slug>`
- `--sanitization-status <slug>`
- `--retention-class <slug>`
- `--captured-at <iso-8601>`
- `--summary <text>`
- `--tag <slug>` repeatable
- `--docs-root <path>`

Default handling policy:

- sensitivity: `internal-restricted`
- sharing scope: `owner-only`
- sanitization status: `raw`
- retention class: `durable`

Default access and rights policy:

- access context: `owner`
- acquisition kind: `manual-note`
- rights policy: `review-required`

Important entry-pool rule:

- `public-safe` at creation time is allowed only when the explicit handling and rights policy cross the `PublicSafe` boundary
- in practice that means:
  - `sanitization_status = approved-for-sharing`
  - `sensitivity = public`
  - `sharing_scope = public`
  - `rights_policy` must explicitly allow public distribution
  - `attribution_reference` is required when the rights policy requires attribution

## Example Flows

Forum support thread:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  create-logos-intake-note \
  --slug support-thread-123 \
  --title "Support Thread 123" \
  --source-system talkyard \
  --source-instance forum-ivanthegeek \
  --access-context registered-user \
  --acquisition-kind web-scrape \
  --rights-policy cc-by \
  --attribution-reference https://creativecommons.org/licenses/by/4.0/ \
  --intake-channel forum-thread \
  --signal-kind support-question \
  --source-uri https://forum.nexus.example/t/support-thread-123 \
  --tag support
```

Discord thread idea capture:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  create-logos-intake-note \
  --slug fnhci-discord-thread-2026-03-26 \
  --title "FnHCI Discord Thread" \
  --source-system discord \
  --intake-channel discord-thread \
  --signal-kind conversation \
  --entry-pool private \
  --rights-policy api-contract-restricted \
  --native-thread-id 1354987654321098765 \
  --source-uri https://discord.com/channels/123/456/789 \
  --summary "Discord discussion about FnHCI direction and UI substrate ideas."
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
  --entry-pool private \
  --native-item-id fb-2026-03-25-001 \
  --summary "Users are confused about the onboarding transition."
```

Customer-confidential support case:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  create-logos-intake-note \
  --slug cheddarbooks-debug-case-42 \
  --title "CheddarBooks Debug Case 42" \
  --source-system issue-tracker \
  --intake-channel bug-report \
  --signal-kind bug-report \
  --sensitivity customer-confidential \
  --sharing-scope case-team \
  --sanitization-status raw \
  --retention-class case-bound \
  --rights-policy customer-confidential \
  --native-item-id case-42
```

Explicit public-safe note:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  create-logos-intake-note \
  --slug public-release-note-2026-03 \
  --title "Public Release Note 2026-03" \
  --source-system talkyard \
  --intake-channel forum-thread \
  --signal-kind feedback \
  --entry-pool public-safe \
  --sensitivity public \
  --sharing-scope public \
  --sanitization-status approved-for-sharing \
  --rights-policy owner-controlled \
  --source-uri https://forum.nexus.example/t/releases-2026-03
```

## Why This Exists

This is a bridge workflow.

It lets LOGOS intake become durable and inspectable now, while keeping the future option open for richer ingestion, indexing, and refinement later.

It also lets intake enter NEXUS through an explicit pool boundary from the start instead of leaving safe-vs-restricted intent implicit.

## Related

- `docs/how-to/report-logos-catalog.md`
- `docs/logos-source-model-v0.md`
