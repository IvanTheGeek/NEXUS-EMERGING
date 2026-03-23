namespace Nexus.EventStore

open System
open System.Collections.Generic
open System.Globalization
open System.IO

type ArtifactProjectionRecord =
    { ArtifactId: string
      ConversationId: string option
      MessageId: string option
      FileName: string option
      MediaType: string option
      ReferenceDisposition: string option
      ReferenceCount: int
      CaptureCount: int
      PayloadCaptured: bool
      LastObservedAt: DateTimeOffset option
      LatestCapturedPath: string option
      Providers: string list
      ProviderConversationIds: string list
      ProviderMessageIds: string list
      ProviderArtifactIds: string list }

type UnresolvedArtifactReport =
    { TotalArtifacts: int
      CapturedArtifacts: int
      UnresolvedArtifacts: int
      ProviderCounts: (string * int) list
      Items: ArtifactProjectionRecord list }

[<RequireQualifiedAccess>]
module ArtifactProjectionReports =
    let private tryParseInt (value: string option) =
        value
        |> Option.bind (fun raw ->
            match Int32.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture) with
            | true, parsedValue -> Some parsedValue
            | _ -> None)

    let private tryParseBool (value: string option) =
        value
        |> Option.bind (fun raw ->
            match Boolean.TryParse(raw) with
            | true, parsedValue -> Some parsedValue
            | _ -> None)

    let private tryParseTimestamp (value: string option) =
        value
        |> Option.bind (fun raw ->
            match DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal) with
            | true, parsedValue -> Some parsedValue
            | _ -> None)

    let private parseStringList (value: string option) =
        let unquote (raw: string) =
            let trimmed = raw.Trim()

            if trimmed.StartsWith("\"", StringComparison.Ordinal)
               && trimmed.EndsWith("\"", StringComparison.Ordinal)
               && trimmed.Length >= 2 then
                trimmed.Substring(1, trimmed.Length - 2)
                    .Replace("\\\"", "\"")
                    .Replace("\\\\", "\\")
                    .Replace("\\n", "\n")
                    .Replace("\\r", "\r")
                    .Replace("\\t", "\t")
            else
                trimmed

        value
        |> Option.map (fun raw ->
            let trimmed = raw.Trim()

            if trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal) then
                let inner = trimmed.Substring(1, trimmed.Length - 2).Trim()

                if String.IsNullOrWhiteSpace(inner) then
                    []
                else
                    inner.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    |> Array.map unquote
                    |> Array.toList
            else
                [])
        |> Option.defaultValue []

    let private loadProjection path =
        let document = File.ReadAllText(path) |> TomlDocument.parse

        { ArtifactId = TomlDocument.tryScalar "artifact_id" document |> Option.defaultWith (fun () -> Path.GetFileNameWithoutExtension(path))
          ConversationId = TomlDocument.tryScalar "conversation_id" document
          MessageId = TomlDocument.tryScalar "message_id" document
          FileName = TomlDocument.tryScalar "file_name" document
          MediaType = TomlDocument.tryScalar "media_type" document
          ReferenceDisposition = TomlDocument.tryScalar "reference_disposition" document
          ReferenceCount = TomlDocument.tryScalar "reference_count" document |> tryParseInt |> Option.defaultValue 0
          CaptureCount = TomlDocument.tryScalar "capture_count" document |> tryParseInt |> Option.defaultValue 0
          PayloadCaptured = TomlDocument.tryScalar "payload_captured" document |> tryParseBool |> Option.defaultValue false
          LastObservedAt = TomlDocument.tryScalar "last_observed_at" document |> tryParseTimestamp
          LatestCapturedPath = TomlDocument.tryScalar "latest_captured_path" document
          Providers = TomlDocument.tryScalar "providers" document |> parseStringList
          ProviderConversationIds = TomlDocument.tryScalar "provider_conversation_ids" document |> parseStringList
          ProviderMessageIds = TomlDocument.tryScalar "provider_message_ids" document |> parseStringList
          ProviderArtifactIds = TomlDocument.tryScalar "provider_artifact_ids" document |> parseStringList }

    let load (eventStoreRoot: string) =
        let projectionsRoot = Path.Combine(Path.GetFullPath(eventStoreRoot), "projections", "artifacts")

        if Directory.Exists(projectionsRoot) then
            Directory.EnumerateFiles(projectionsRoot, "*.toml", SearchOption.TopDirectoryOnly)
            |> Seq.map loadProjection
            |> Seq.toList
        else
            []

    let buildUnresolvedReport eventStoreRoot providerFilter limit =
        let allArtifacts = load eventStoreRoot

        let unresolvedArtifacts =
            allArtifacts
            |> List.filter (fun artifact -> not artifact.PayloadCaptured)
            |> List.filter (fun artifact ->
                match providerFilter with
                | None -> true
                | Some provider ->
                    artifact.Providers
                    |> List.exists (fun candidate -> String.Equals(candidate, provider, StringComparison.OrdinalIgnoreCase)))
            |> List.sortByDescending (fun artifact -> artifact.LastObservedAt |> Option.defaultValue DateTimeOffset.MinValue)

        let providerCounts =
            unresolvedArtifacts
            |> Seq.collect (fun artifact -> artifact.Providers |> List.distinct)
            |> Seq.countBy id
            |> Seq.sortBy fst
            |> Seq.toList

        { TotalArtifacts = allArtifacts.Length
          CapturedArtifacts = allArtifacts |> List.filter (fun artifact -> artifact.PayloadCaptured) |> List.length
          UnresolvedArtifacts = unresolvedArtifacts.Length
          ProviderCounts = providerCounts
          Items = unresolvedArtifacts |> List.truncate limit }
