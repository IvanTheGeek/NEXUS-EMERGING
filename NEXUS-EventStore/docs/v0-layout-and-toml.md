# NEXUS-EventStore v0 Layout and TOML Schema

This document defines the first durable file layout and TOML wire shape for the canonical history layer.

The intent is:

- append-only canonical events
- explicit stream boundaries
- strong provenance back to raw objects
- reparsable storage
- filenames that do not require mutable per-stream counters

## ID Strategy

Default internal IDs:

- `import_id`
- `event_id`
- `conversation_id`
- `message_id`
- `artifact_id`
- `turn_id`
- `node_id`
- `edge_id`
- `fact_id`

These should default to UUIDv7 values.

Reason:

- sortable by creation time in practice
- globally unique without coordination
- good fit for append-only event files

Semantic taxonomy identifiers are different:

- `domain_id`
- `bounded_context_id`
- `lens_id`

These should stay human-readable slugs like `ingestion`, `canonical-history`, and `developer-lens`.

## Root Layout

```text
NEXUS-EventStore/
  events/
    conversations/
      <conversation-id>/
        <event-id>__provider-conversation-observed.toml
        <event-id>__provider-message-observed.toml
    artifacts/
      <artifact-id>/
        <event-id>__artifact-referenced.toml
        <event-id>__artifact-payload-captured.toml
    imports/
      <import-id>/
        <event-id>__provider-artifact-received.toml
        <event-id>__raw-snapshot-extracted.toml
        <event-id>__import-completed.toml
  imports/
    <import-id>.toml
  projections/
    conversations/
      <conversation-id>.toml
    artifacts/
      <artifact-id>.toml
  graph/
    assertions/
      <fact-id>.toml
```

## File Naming

Event filenames should be:

```text
<event-id>__<event-kind>.toml
```

Examples:

- `0195d6f4-4f31-7b0a-b1f4-0de9a0df4452__provider-message-observed.toml`
- `0195d6f4-4f80-7d23-9ef8-cce2d9158bc3__artifact-payload-captured.toml`

Why this shape:

- stable and append-only
- no mutable sequence bookkeeping
- event kind remains visible in Git and in file listings

Ordering is determined by:

- UUIDv7 event ID
- `observed_at`
- `occurred_at` where relevant

## Event File Shape

Every canonical event file has:

- top-level envelope metadata
- zero or more provider refs
- zero or more raw object refs
- one `[body]` table shaped by `event_kind`

### Common Envelope Fields

```toml
schema_version = 1
event_id = "0195d6f4-4f31-7b0a-b1f4-0de9a0df4452"
event_kind = "provider_message_observed"
stream_kind = "conversation"
stream_id = "0195d6f4-4eed-74ce-9392-fb393d5cfb18"
conversation_id = "0195d6f4-4eed-74ce-9392-fb393d5cfb18"
message_id = "0195d6f4-4f08-7d34-b870-6e4a0e1e5cf0"
domain_id = "ingestion"
bounded_context_id = "canonical-history"
occurred_at = "2026-03-22T12:13:53Z"
observed_at = "2026-03-22T12:15:10Z"
imported_at = "2026-03-22T12:15:10Z"
source_acquisition = "export_zip"
normalization_version = "provider-export-v0"
import_id = "0195d6f4-4ec4-7380-9c5c-7e0eb6d9580d"
```

Optional values that are unknown should be omitted rather than filled with placeholder strings.

`normalization_version` identifies the canonicalizer/parser shape that produced the event.

- same provider object + same `normalization_version` + different content hash => provider-side revision observation
- same provider object + different `normalization_version` => new normalization observation, not a provider revision

### Hash Section

```toml
[content_hash]
algorithm = "sha256"
value = "4caa0f4a7f7f0dbcd8f0b39d0d9d80af9fba5226e4f7f3fd0c2cb829634f8c89"
```

### Provider Refs

```toml
[[provider_refs]]
provider = "claude"
object_kind = "message_object"
native_id = "019cce65-86c1-7007-bfa7-2847c82547d1"
conversation_native_id = "bb96fb46-e42e-4bde-a885-4b59d5290fc0"
message_native_id = "019cce65-86c1-7007-bfa7-2847c82547d1"
```

### Raw Object Refs

`relative_path` is relative to the root of `NEXUS-Objects`.

```toml
[[raw_objects]]
kind = "provider_export_zip"
relative_path = "providers/claude/archive/2026-03-22T12-13-53Z/data-2026-03-22-12-13-53-batch-0000.zip"
source_description = "Original Claude export zip"
```

## Event Body Shapes

### `provider_artifact_received`

```toml
[body]
provider = "claude"
file_name = "data-2026-03-22-12-13-53-batch-0000.zip"
window_kind = "full"
byte_count = 36619935
```

### `raw_snapshot_extracted`

```toml
[body]
artifact_id = "0195d6f4-4ef0-7f7c-86a1-75957c2f3d15"
extracted_entries = 4
notes = "Top-level JSON payloads extracted for parsing"
```

### `provider_conversation_observed`

```toml
[body]
provider_conversation_id = "bb96fb46-e42e-4bde-a885-4b59d5290fc0"
title = "Mermaid sequence diagram for chat"
is_archived = false
message_count_hint = 4
```

### `provider_message_observed`

```toml
[body]
provider_message_id = "019cce65-86c1-7007-bfa7-2847c82547d1"
role = "human"
sequence_hint = 1

[[body.segments]]
kind = "plain_text"
text = "can you make a mermaid sequence diagram that renders in this chat?"
```

### `provider_message_revision_observed`

```toml
[body]
message_id = "0195d6f4-4f08-7d34-b870-6e4a0e1e5cf0"
revision_reason = "Same provider message ID observed with different content hash"

[body.prior_content_hash]
algorithm = "sha256"
value = "1111111111111111111111111111111111111111111111111111111111111111"

[body.revised_content_hash]
algorithm = "sha256"
value = "2222222222222222222222222222222222222222222222222222222222222222"
```

### `artifact_referenced`

```toml
[body]
file_name = "6-slices-adam.txt"
media_type = "text/plain"
disposition = "payload_included"
```

### `artifact_payload_captured`

```toml
[body]
media_type = "application/pdf"
byte_count = 245167
capture_notes = "Manually added recovered artifact payload"

[body.captured_object]
kind = "manual_artifact"
relative_path = "manual-intake/2026-03-22/domain-modeling-made-functional.pdf"
source_description = "Manually added by Ivan for reconciliation"
```

### `import_completed`

```toml
[body]
window_kind = "full"
notes = "Initial Claude import"

[body.counts]
conversations_seen = 119
messages_seen = 476
artifacts_referenced = 12
new_events_appended = 608
duplicates_skipped = 0
revisions_observed = 0
reparse_observations_appended = 0
```

## Import Manifest Shape

Each import run also writes a single manifest file at:

```text
imports/<import-id>.toml
```

Example:

```toml
schema_version = 1
manifest_kind = "import_manifest"
import_id = "0195d6f4-4ec4-7380-9c5c-7e0eb6d9580d"
provider = "claude"
source_acquisition = "export_zip"
window_kind = "full"
imported_at = "2026-03-22T12:15:10Z"
notes = ["Initial Claude export import"]
new_canonical_event_ids = [
  "0195d6f4-4f10-7fef-9224-f71e892b969c",
  "0195d6f4-4f31-7b0a-b1f4-0de9a0df4452"
]

[root_artifact]
kind = "provider_export_zip"
relative_path = "providers/claude/archive/2026-03-22T12-13-53Z/data-2026-03-22-12-13-53-batch-0000.zip"
source_description = "Original Claude export zip"

[counts]
conversations_seen = 119
messages_seen = 476
artifacts_referenced = 12
new_events_appended = 608
duplicates_skipped = 0
revisions_observed = 0
```

## Stream Assignment Rules

Use these stream rules in v0:

- conversation-related events go to `events/conversations/<conversation-id>/`
- artifact reference and capture events go to `events/artifacts/<artifact-id>/`
- import process events go to `events/imports/<import-id>/`

This keeps conversation history, artifact history, and import history independently append-only.

## Deliberate Non-Goals for v0

- no mutable sequence counters in filenames
- no rewrite-in-place event updates
- no requirement that graph assertions exist before imports work
- no requirement that every referenced artifact payload be present at import time
