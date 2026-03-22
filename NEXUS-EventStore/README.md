# NEXUS-EventStore

This folder represents the canonical append-only history layer for NEXUS.

It is intended to hold:

- canonical event files
- import manifests
- projections
- later graph assertions or derived views

Git is intended to serve as the durable history mechanism for this layer.

## v0 Direction

The first storage shape is stream-oriented:

- `events/conversations/<conversation-id>/`
- `events/artifacts/<artifact-id>/`
- `events/imports/<import-id>/`
- `imports/`
- `projections/`
- later `graph/`

See `docs/v0-layout-and-toml.md` for the current layout and TOML schema draft.
