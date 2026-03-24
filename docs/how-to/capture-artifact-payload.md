# Capture Artifact Payload

Use this command when an imported conversation already references an artifact, but the provider export did not include the payload and you want to add that file manually.

This workflow:

1. finds the existing canonical `artifact_id`
2. copies your file into `NEXUS-Objects/`
3. appends an `ArtifactPayloadCaptured` event to the artifact stream
4. skips the append if the same artifact already has the same payload content hash

## Command

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  capture-artifact-payload \
  --artifact-id <artifact-id> \
  --file <path-to-local-file>
```

## Targeting Options

Use one of these targeting styles.

By internal artifact ID:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  capture-artifact-payload \
  --artifact-id 019d174f-2c81-71e3-9cec-527f951cd6cf \
  --file /path/to/recovered-file.mhtml
```

By provider conversation/message plus provider artifact ID:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  capture-artifact-payload \
  --provider chatgpt \
  --provider-conversation-id 69a7cf11-666c-8327-a2b3-f22a72bb6965 \
  --provider-message-id 73161fc5-00a9-4085-bfc6-d1e226b6b455 \
  --provider-artifact-id file-Q4pskxcmbz29buyiw7bC5U \
  --file /path/to/recovered-file.mhtml
```

By provider conversation/message plus file name fallback:

```bash
dotnet run --project NEXUS-Code/src/Nexus.Cli/Nexus.Cli.fsproj -- \
  capture-artifact-payload \
  --provider claude \
  --provider-conversation-id <provider-conversation-id> \
  --provider-message-id <provider-message-id> \
  --file-name recovered-notes.txt \
  --file /path/to/recovered-notes.txt
```

If `--file-name` is omitted in provider lookup mode, the command falls back to the basename of `--file`.

## Optional Flags

```bash
--media-type text/plain
--notes "Recovered manually from local archive"
--objects-root /some/other/objects-root
--event-store-root /some/other/event-store-root
```

## How To Find an Artifact ID

Search the artifact event streams:

```bash
rg -n "artifact_id|provider_artifact_id|file_name" NEXUS-EventStore/events/artifacts
```

Or search by provider conversation/message IDs:

```bash
rg -n "69a7cf11-666c-8327-a2b3-f22a72bb6965|73161fc5-00a9-4085-bfc6-d1e226b6b455" NEXUS-EventStore/events/artifacts
```

## Notes

- This command appends canonical history. It does not rewrite earlier `ArtifactReferenced` events.
- The captured file is archived under `NEXUS-Objects/providers/<provider>/manual-artifacts/archive/...`
- The canonical event uses `source_acquisition = "manual_artifact_add"`
- Duplicate detection is content-hash based per artifact stream
