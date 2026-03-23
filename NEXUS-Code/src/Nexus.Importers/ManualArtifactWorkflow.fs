namespace Nexus.Importers

open System
open System.Globalization
open System.IO
open System.Security.Cryptography
open Nexus.Domain
open Nexus.EventStore

[<RequireQualifiedAccess>]
module ManualArtifactWorkflow =
    let private importedAtValue (ImportedAt value) = value
    let private observedAtFromImported (ImportedAt value) = ObservedAt value

    let private timestampFolderName (timestamp: DateTimeOffset) =
        timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH-mm-ssZ", CultureInfo.InvariantCulture)

    let private sha256ForFile (path: string) =
        use stream = File.OpenRead(path)
        SHA256.HashData(stream) |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

    let private contentHashForFile (path: string) =
        { Algorithm = "sha256"
          Value = sha256ForFile path }

    let private rawObjectKind (mediaType: string option) =
        match mediaType with
        | Some value when value.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) -> AudioPayload
        | Some _ -> AttachmentPayload
        | None -> ManualArtifact

    let private providerSlug (provider: ProviderKind option) =
        provider |> Option.map ProviderNaming.slug |> Option.defaultValue "unknown"

    let private conversationProviderRef provider conversationNativeId =
        { Provider = provider
          ObjectKind = ConversationObject
          NativeId = Some conversationNativeId
          ConversationNativeId = Some conversationNativeId
          MessageNativeId = None
          ArtifactNativeId = None }

    let private messageProviderRef provider conversationNativeId messageNativeId =
        { Provider = provider
          ObjectKind = MessageObject
          NativeId = Some messageNativeId
          ConversationNativeId = Some conversationNativeId
          MessageNativeId = Some messageNativeId
          ArtifactNativeId = None }

    let private artifactProviderRef provider conversationNativeId messageNativeId artifactNativeId =
        { Provider = provider
          ObjectKind = ArtifactObject
          NativeId = Some artifactNativeId
          ConversationNativeId = Some conversationNativeId
          MessageNativeId = Some messageNativeId
          ArtifactNativeId = Some artifactNativeId }

    let private providerRefsForState (state: ExistingArtifactState) =
        match state.Provider, state.ConversationNativeId with
        | Some provider, Some conversationNativeId ->
            [ yield conversationProviderRef provider conversationNativeId

              match state.MessageNativeId with
              | Some messageNativeId ->
                  yield messageProviderRef provider conversationNativeId messageNativeId

                  match state.ProviderArtifactId with
                  | Some artifactNativeId ->
                      yield artifactProviderRef provider conversationNativeId messageNativeId artifactNativeId
                  | None -> ()
              | None -> () ]
        | _ -> []

    let private tryResolveArtifactState (index: ExistingEventStoreIndex) (sourceFileName: string) (target: ManualArtifactCaptureTarget) =
        match target with
        | ExistingArtifactId artifactId ->
            match EventStoreIndex.tryFindArtifactState artifactId index with
            | Some state -> state
            | None -> invalidArg "request.Target" $"Artifact ID not found in the event store: {ArtifactId.format artifactId}"
        | ProviderArtifactReference(provider, conversationNativeId, messageNativeId, providerArtifactId, fileName) ->
            let effectiveFileName =
                fileName |> Option.orElseWith (fun () -> Path.GetFileName(sourceFileName) |> Some)

            match
                EventStoreIndex.tryResolveArtifactId
                    provider
                    conversationNativeId
                    messageNativeId
                    providerArtifactId
                    effectiveFileName
                    index
                |> Option.bind (fun artifactId -> EventStoreIndex.tryFindArtifactState artifactId index)
            with
            | Some state -> state
            | None ->
                invalidArg
                    "request.Target"
                    $"No existing artifact reference matched provider '{ProviderNaming.slug provider}', conversation '{conversationNativeId}', message '{messageNativeId}', and the supplied artifact/file identifiers."

    let private archiveCapturedArtifact (objectsRoot: string) (sourceFilePath: string) (artifactState: ExistingArtifactState) importedAt =
        let sourceFileName = Path.GetFileName(sourceFilePath)
        let archiveDirectoryRelativePath =
            Path.Combine(
                "providers",
                providerSlug artifactState.Provider,
                "manual-artifacts",
                "archive",
                $"{timestampFolderName (importedAtValue importedAt)}-{ArtifactId.format artifactState.ArtifactId}")
                .Replace('\\', '/')

        let archiveDirectoryAbsolutePath = Path.Combine(objectsRoot, archiveDirectoryRelativePath)
        Directory.CreateDirectory(archiveDirectoryAbsolutePath) |> ignore

        let archivedAbsolutePath = Path.Combine(archiveDirectoryAbsolutePath, sourceFileName)
        File.Copy(sourceFilePath, archivedAbsolutePath, true)

        Path.Combine(archiveDirectoryRelativePath, sourceFileName).Replace('\\', '/')

    let private buildCapturedObject archivedRelativePath importedAt mediaType =
        { RawObjectId = None
          Kind = rawObjectKind mediaType
          RelativePath = archivedRelativePath
          ArchivedAt = Some importedAt
          SourceDescription = Some "Manually added artifact payload" }

    let private buildCaptureEvent importedAt (artifactState: ExistingArtifactState) capturedObject contentHash byteCount mediaType notes =
        { Envelope =
            { EventId = CanonicalEventId.create ()
              ConversationId = artifactState.ConversationId
              MessageId = artifactState.MessageId
              ArtifactId = Some artifactState.ArtifactId
              TurnId = None
              DomainId = Some (DomainId.create "ingestion")
              BoundedContextId = Some (BoundedContextId.create "canonical-history")
              OccurredAt = None
              ObservedAt = observedAtFromImported importedAt
              ImportedAt = Some importedAt
              SourceAcquisition = ManualArtifactAdd
              NormalizationVersion = None
              ContentHash = Some contentHash
              ImportId = None
              ProviderRefs = providerRefsForState artifactState
              RawObjects = [ capturedObject ] }
          Body =
            ArtifactPayloadCaptured
                { ArtifactId = artifactState.ArtifactId
                  CapturedObject = capturedObject
                  MediaType = mediaType
                  ByteCount = Some byteCount
                  CaptureNotes = notes } }

    /// <summary>
    /// Hydrates an already-known artifact reference with a manually supplied payload file.
    /// </summary>
    /// <param name="request">The target artifact reference, source file, and output roots for the capture.</param>
    /// <returns>A summary describing whether a new payload event was appended or skipped as a duplicate.</returns>
    /// <remarks>
    /// This workflow is append-only and idempotent by content hash.
    /// Full workflow notes: docs/how-to/capture-artifact-payload.md
    /// </remarks>
    let run (request: ManualArtifactCaptureRequest) =
        let sourceFilePath = Path.GetFullPath(request.SourceFilePath)

        if not (File.Exists(sourceFilePath)) then
            invalidArg "request.SourceFilePath" $"Source file not found: {sourceFilePath}"

        let objectsRoot = Path.GetFullPath(request.ObjectsRoot)
        let eventStoreRoot = Path.GetFullPath(request.EventStoreRoot)
        Directory.CreateDirectory(objectsRoot) |> ignore
        Directory.CreateDirectory(eventStoreRoot) |> ignore

        let sourceFileName = Path.GetFileName(sourceFilePath)
        let index = EventStoreIndex.load eventStoreRoot
        let artifactState = tryResolveArtifactState index sourceFileName request.Target
        let byteCount = FileInfo(sourceFilePath).Length
        let contentHash = contentHashForFile sourceFilePath

        if EventStoreIndex.hasCapturedContentHash contentHash artifactState then
            { ArtifactId = artifactState.ArtifactId
              Provider = artifactState.Provider
              ArchivedRelativePath = None
              EventPath = None
              DuplicateSkipped = true
              ByteCount = byteCount
              ContentHash = contentHash }
        else
            let importedAt = ImportedAt DateTimeOffset.UtcNow
            let mediaType = request.MediaType |> Option.orElse artifactState.MediaType
            let archivedRelativePath = archiveCapturedArtifact objectsRoot sourceFilePath artifactState importedAt
            let capturedObject = buildCapturedObject archivedRelativePath importedAt mediaType
            let event = buildCaptureEvent importedAt artifactState capturedObject contentHash byteCount mediaType request.Notes
            let eventPath = CanonicalStore.writeCanonicalEvent eventStoreRoot event

            { ArtifactId = artifactState.ArtifactId
              Provider = artifactState.Provider
              ArchivedRelativePath = Some archivedRelativePath
              EventPath = Some eventPath
              DuplicateSkipped = false
              ByteCount = byteCount
              ContentHash = contentHash }
