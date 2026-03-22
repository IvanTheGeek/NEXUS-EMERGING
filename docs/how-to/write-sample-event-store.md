# Write Sample Event Store Data

This guide explains how to write a small sample canonical history bundle into an event-store root.

Use this when you want to:

- smoke-test the TOML serializers
- verify the append-only file layout
- inspect the canonical event files without running a real provider import yet

## Command

Run from the repository root:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- write-sample-event-store
```

By default this writes into:

- `NEXUS-EventStore/`

## Write Somewhere Else

To avoid touching the repo event-store, point it at another directory:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  write-sample-event-store \
  --event-store-root /tmp/nexus-event-store-smoke
```

## What It Writes

The command creates a small bundle including:

- import stream events
- one conversation observed event
- two message observed events
- one artifact reference event
- an import manifest

## Why This Exists

This is not a real importer.

It is a validation path for the current bounded context so we can confirm:

- the code-level types serialize as expected
- filenames and stream paths match the chosen layout
- the event-store root can be populated in a predictable way

It is the bridge between:

- the domain model
- the TOML/file writer layer
- the later importer workflow
