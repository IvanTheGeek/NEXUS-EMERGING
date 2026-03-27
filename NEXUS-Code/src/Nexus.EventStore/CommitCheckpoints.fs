namespace Nexus.EventStore

open System
open System.Globalization
open System.IO
open Nexus.Domain

/// <summary>
/// Persists and queries durable commit-linked Codex checkpoint manifests.
/// </summary>
[<RequireQualifiedAccess>]
module CommitCheckpoints =
    type CodexCommitConversation =
        { ProviderConversationId: string
          Title: string option
          MessageCountHint: int option }

    type CodexCommitCheckpoint =
        { RepoSlug: string
          RepoRoot: string
          BranchName: string option
          RemoteOrigin: string option
          CommitSha: string
          CommitShortSha: string
          CommitSummary: string
          CommitMessage: string
          CapturedAt: DateTimeOffset
          EventStoreRoot: string
          SnapshotName: string
          SnapshotRoot: string
          SnapshotManifestRelativePath: string
          RootArtifactRelativePath: string
          ImportId: ImportId
          ImportManifestRelativePath: string
          WorkingGraphManifestRelativePath: string option
          WorkingGraphCatalogRelativePath: string option
          WorkingGraphIndexRelativePath: string option
          Counts: ImportCounts
          Conversations: CodexCommitConversation list
          ManifestRelativePath: string }

    let private schemaVersion = 1
    let private checkpointKind = "codex_commit_checkpoint"

    let private normalizePath (path: string) =
        path.Replace('\\', '/')

    let private manifestDirectoryRelativePath repoSlug =
        normalizePath (Path.Combine("work-batches", "commit-checkpoints", repoSlug))

    /// <summary>
    /// Returns the canonical manifest path for a repo/commit checkpoint.
    /// </summary>
    let manifestRelativePath repoSlug commitSha =
        normalizePath (Path.Combine(manifestDirectoryRelativePath repoSlug, $"{commitSha}.toml"))

    let private tryParseInt (value: string) =
        match Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture) with
        | true, parsed -> Some parsed
        | _ -> None

    /// <summary>
    /// Returns true when a commit-linked checkpoint already exists for the repo/commit pair.
    /// </summary>
    let exists eventStoreRoot repoSlug commitSha =
        let absolutePath = Path.Combine(Path.GetFullPath(eventStoreRoot), manifestRelativePath repoSlug commitSha)
        File.Exists(absolutePath)

    /// <summary>
    /// Resolves an exact or unique prefixed commit SHA to the stored full manifest commit SHA.
    /// </summary>
    let tryResolveCommitSha eventStoreRoot repoSlug (commitReference: string) =
        let normalizedReference = commitReference.Trim()

        if String.IsNullOrWhiteSpace(normalizedReference) then
            None
        elif exists eventStoreRoot repoSlug normalizedReference then
            Some normalizedReference
        else
            let manifestDirectory = Path.Combine(Path.GetFullPath(eventStoreRoot), manifestDirectoryRelativePath repoSlug)

            if not (Directory.Exists(manifestDirectory)) then
                None
            else
                Directory.GetFiles(manifestDirectory, "*.toml")
                |> Array.map Path.GetFileNameWithoutExtension
                |> Array.filter (fun candidate -> candidate.StartsWith(normalizedReference, StringComparison.OrdinalIgnoreCase))
                |> Array.distinct
                |> function
                    | [| matchedCommitSha |] -> Some matchedCommitSha
                    | _ -> None

    /// <summary>
    /// Writes a durable checkpoint manifest under work-batches/commit-checkpoints/.
    /// </summary>
    let write eventStoreRoot (checkpoint: CodexCommitCheckpoint) =
        let builder = create ()
        appendInt builder "schema_version" schemaVersion
        appendString builder "checkpoint_kind" checkpointKind
        appendString builder "repo_slug" checkpoint.RepoSlug
        appendString builder "repo_root" (Path.GetFullPath(checkpoint.RepoRoot))
        appendStringOption builder "branch_name" checkpoint.BranchName
        appendStringOption builder "remote_origin" checkpoint.RemoteOrigin
        appendString builder "commit_sha" checkpoint.CommitSha
        appendString builder "commit_short_sha" checkpoint.CommitShortSha
        appendString builder "commit_summary" checkpoint.CommitSummary
        appendString builder "commit_message" checkpoint.CommitMessage
        appendTimestamp builder "captured_at" checkpoint.CapturedAt
        appendString builder "event_store_root" (Path.GetFullPath(eventStoreRoot))
        appendString builder "snapshot_name" checkpoint.SnapshotName
        appendString builder "snapshot_root" checkpoint.SnapshotRoot
        appendString builder "snapshot_manifest_relative_path" checkpoint.SnapshotManifestRelativePath
        appendString builder "root_artifact_relative_path" checkpoint.RootArtifactRelativePath
        appendString builder "import_id" (ImportId.format checkpoint.ImportId)
        appendString builder "import_manifest_relative_path" checkpoint.ImportManifestRelativePath
        appendStringOption builder "working_graph_manifest_relative_path" checkpoint.WorkingGraphManifestRelativePath
        appendStringOption builder "working_graph_catalog_relative_path" checkpoint.WorkingGraphCatalogRelativePath
        appendStringOption builder "working_graph_index_relative_path" checkpoint.WorkingGraphIndexRelativePath
        appendString builder "manifest_relative_path" checkpoint.ManifestRelativePath
        appendBlank builder

        appendTableHeader builder "import_counts"
        appendInt builder "conversations_seen" checkpoint.Counts.ConversationsSeen
        appendInt builder "messages_seen" checkpoint.Counts.MessagesSeen
        appendInt builder "artifacts_referenced" checkpoint.Counts.ArtifactsReferenced
        appendInt builder "new_events_appended" checkpoint.Counts.NewEventsAppended
        appendInt builder "duplicates_skipped" checkpoint.Counts.DuplicatesSkipped
        appendInt builder "revisions_observed" checkpoint.Counts.RevisionsObserved
        appendInt builder "reparse_observations_appended" checkpoint.Counts.ReparseObservationsAppended
        appendBlank builder

        for conversation in checkpoint.Conversations do
            appendArrayTableHeader builder "conversations"
            appendString builder "provider_conversation_id" conversation.ProviderConversationId
            appendStringOption builder "title" conversation.Title
            appendIntOption builder "message_count_hint" conversation.MessageCountHint
            appendBlank builder

        let absolutePath = Path.Combine(Path.GetFullPath(eventStoreRoot), checkpoint.ManifestRelativePath)
        let directory = Path.GetDirectoryName(absolutePath)

        if not (String.IsNullOrWhiteSpace(directory)) then
            Directory.CreateDirectory(directory) |> ignore

        File.WriteAllText(absolutePath, render builder)
        checkpoint.ManifestRelativePath

    /// <summary>
    /// Loads a previously written checkpoint manifest when it exists and parses cleanly.
    /// </summary>
    let tryLoad eventStoreRoot repoSlug commitSha =
        let absolutePath = Path.Combine(Path.GetFullPath(eventStoreRoot), manifestRelativePath repoSlug commitSha)

        if not (File.Exists(absolutePath)) then
            None
        else
            let document = File.ReadAllText(absolutePath) |> TomlDocument.parse

            let requiredScalar key =
                TomlDocument.tryScalar key document
                |> Option.defaultWith (fun () -> invalidOp $"Checkpoint manifest is missing required '{key}' at {absolutePath}.")

            let conversations =
                TomlDocument.tableArray "conversations" document
                |> List.map (fun table ->
                    let providerConversationId =
                        match table.TryGetValue("provider_conversation_id") with
                        | true, value -> value
                        | false, _ -> invalidOp $"Checkpoint manifest is missing conversations.provider_conversation_id at {absolutePath}."

                    let title =
                        match table.TryGetValue("title") with
                        | true, value when not (String.IsNullOrWhiteSpace(value)) -> Some value
                        | _ -> None

                    let messageCountHint =
                        match table.TryGetValue("message_count_hint") with
                        | true, value -> tryParseInt value
                        | false, _ -> None

                    { ProviderConversationId = providerConversationId
                      Title = title
                      MessageCountHint = messageCountHint })

            let counts =
                { ConversationsSeen =
                    TomlDocument.tryTableValue "import_counts" "conversations_seen" document
                    |> Option.bind tryParseInt
                    |> Option.defaultValue 0
                  MessagesSeen =
                    TomlDocument.tryTableValue "import_counts" "messages_seen" document
                    |> Option.bind tryParseInt
                    |> Option.defaultValue 0
                  ArtifactsReferenced =
                    TomlDocument.tryTableValue "import_counts" "artifacts_referenced" document
                    |> Option.bind tryParseInt
                    |> Option.defaultValue 0
                  NewEventsAppended =
                    TomlDocument.tryTableValue "import_counts" "new_events_appended" document
                    |> Option.bind tryParseInt
                    |> Option.defaultValue 0
                  DuplicatesSkipped =
                    TomlDocument.tryTableValue "import_counts" "duplicates_skipped" document
                    |> Option.bind tryParseInt
                    |> Option.defaultValue 0
                  RevisionsObserved =
                    TomlDocument.tryTableValue "import_counts" "revisions_observed" document
                    |> Option.bind tryParseInt
                    |> Option.defaultValue 0
                  ReparseObservationsAppended =
                    TomlDocument.tryTableValue "import_counts" "reparse_observations_appended" document
                    |> Option.bind tryParseInt
                    |> Option.defaultValue 0 }

            Some
                { RepoSlug = requiredScalar "repo_slug"
                  RepoRoot = requiredScalar "repo_root"
                  BranchName = TomlDocument.tryScalar "branch_name" document
                  RemoteOrigin = TomlDocument.tryScalar "remote_origin" document
                  CommitSha = requiredScalar "commit_sha"
                  CommitShortSha = requiredScalar "commit_short_sha"
                  CommitSummary = requiredScalar "commit_summary"
                  CommitMessage = requiredScalar "commit_message"
                  CapturedAt = requiredScalar "captured_at" |> DateTimeOffset.Parse
                  EventStoreRoot = requiredScalar "event_store_root"
                  SnapshotName = requiredScalar "snapshot_name"
                  SnapshotRoot = requiredScalar "snapshot_root"
                  SnapshotManifestRelativePath = requiredScalar "snapshot_manifest_relative_path"
                  RootArtifactRelativePath = requiredScalar "root_artifact_relative_path"
                  ImportId = requiredScalar "import_id" |> ImportId.parse
                  ImportManifestRelativePath = requiredScalar "import_manifest_relative_path"
                  WorkingGraphManifestRelativePath = TomlDocument.tryScalar "working_graph_manifest_relative_path" document
                  WorkingGraphCatalogRelativePath = TomlDocument.tryScalar "working_graph_catalog_relative_path" document
                  WorkingGraphIndexRelativePath = TomlDocument.tryScalar "working_graph_index_relative_path" document
                  Counts = counts
                  Conversations = conversations
                  ManifestRelativePath = requiredScalar "manifest_relative_path" }
